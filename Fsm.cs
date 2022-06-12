using System;
using System.Collections.Generic;

namespace Fsm
{
    abstract class State<D>
    where D : class
    {
        public virtual void OnEnter(D data)
        {
#if FSM_DEBUG
            Console.WriteLine($"[FSM] state enter: {this.GetType()}");
#endif
        }

        public virtual void OnExit(D data)
        {
#if FSM_DEBUG
            Console.WriteLine($"[FSM] state exit: {this.GetType()}");
#endif
        }

        public virtual void OnUpdate(D data)
        {
#if FSM_DEBUG
            Console.WriteLine($"[FSM] state update: {this.GetType()}");
#endif
        }
    }

    class Flow<D>
    where D : class
    {
        public interface INode { }
        public struct NodeState : INode
        {
            public Func<D, State<D>> state;
            public Func<D, string?> next;
        }
        public struct NodeFlow : INode
        {
            public Func<D, Flow<D>> next;
        }

        private readonly List<(Func<D, bool> condition, INode node)> forceNodes;
        private readonly List<INode> nodes;

        // NOTE: I can avoid using dictionary if I create the INode variable
        // but... I am too lazy to do that every time
        private readonly Dictionary<string, int> indices;

        private INode? currentNode;
        private State<D>? currentState;

        public Flow()
        {
            forceNodes = new();
            nodes = new();
            indices = new();
        }

        public Flow<D> ForceDo(
            Func<D, bool> condition,
            Func<D, State<D>> state,
            Func<D, string?> next)
        {
            forceNodes.Add((condition, new NodeState() { state = state, next = next }));
            return this;
        }

        public Flow<D> ForceTo(
            Func<D, bool> condition,
            Func<D, Flow<D>> next)
        {
            forceNodes.Add((condition, new NodeFlow() { next = next }));
            return this;
        }

        public Flow<D> Do(
            string name,
            Func<D, State<D>> state,
            Func<D, string?> next)
        {
            indices[name] = nodes.Count;
            nodes.Add(new NodeState() { state = state, next = next });
            return this;
        }

        public Flow<D> To(
            string name,
            Func<D, Flow<D>> next)
        {
            indices[name] = nodes.Count;
            nodes.Add(new NodeFlow() { next = next });
            return this;
        }

        public INode GetNode(string name)
        {
            return nodes[indices[name]];
        }

        public INode? GetCurrentNode()
        {
            return currentNode;
        }

        public void SetCurrentNode(INode node)
        {
            currentNode = node;
        }

        public void SetInitialNode(D data)
        {
            currentNode = null;
            currentState = null;

            // check force nodes
            for (int i = 0; i < forceNodes.Count; ++i)
            {
                var (condition, node) = forceNodes[i];
                if (condition(data))
                {
                    currentNode = node;
                    return;
                }
            }

            // check normal nodes
            var firstNode = nodes[0];
            if (firstNode is NodeState)
            {
                var state = ((NodeState)firstNode).state(data);
                currentNode = firstNode;
                currentState = state;
            }
            else
            {
                currentNode = firstNode;
            }
        }

        public INode? GetNextNode(D data)
        {
            // check force nodes
            for (int i = 0; i < forceNodes.Count; ++i)
            {
                var (condition, node) = forceNodes[i];
                if (condition(data))
                    return node;
            }

            // unwrap current node
            var currentNode = this.currentNode!;

            if (currentNode is NodeState)
            {
                var nextNodeName = ((NodeState)currentNode).next(data);
                if (nextNodeName is not null)
                    return this.GetNode(nextNodeName);

                return null;
            }

            return currentNode;
        }

        public void SetState(D data, State<D> newState)
        {
            // self transition is not allowed
            // TODO: maybe somehow allow self transition?
            if (currentState != newState)
            {
                currentState?.OnExit(data);
                currentState = newState;
                currentState?.OnEnter(data);
            }
        }

        public void OnEnter(D data)
        {
            currentState?.OnEnter(data);
        }

        public void OnExit(D data)
        {
            currentState?.OnExit(data);
        }

        public void OnUpdate(D data)
        {
            currentState?.OnUpdate(data);
        }
    }

    class Fsm<D>
    where D : class
    {
        // TODO: expose current state
        public readonly D data;
        private Flow<D> currentFlow;

        public Fsm(D data, Flow<D> startingFlow)
        {
            this.data = data;
            this.currentFlow = startingFlow;
        }

        private (Flow<D> nextFlow, Flow<D>.NodeState nextNodeState) RecFindNext(Flow<D>.NodeFlow nodeFlow)
        {
            // get next flow
            var nextFlow = nodeFlow.next(data);

            // initialize next flow
            nextFlow.SetInitialNode(data);

            // unwrap current node
            var node = nextFlow.GetCurrentNode()!;

            if (node is Flow<D>.NodeState)
            {
                // next state is found
                return (nextFlow, (Flow<D>.NodeState)node);
            }
            else
            {
                // NOTE: this can cause infinite loop
                // recursively find next state
                return this.RecFindNext((Flow<D>.NodeFlow)node);
            }
        }

        public void Update()
        {
            // initialize current flow
            if (currentFlow.GetCurrentNode() is null)
            {
                currentFlow.SetInitialNode(data);
                currentFlow.OnEnter(data);
            }

            // try get next node
            var nextNode = currentFlow.GetNextNode(data);
            if (nextNode is not null)
            {
                currentFlow.SetCurrentNode(nextNode);

                if (nextNode is Flow<D>.NodeState)
                {
                    // change current state
                    var nodeState = (Flow<D>.NodeState)nextNode;
                    var state = nodeState.state(data);
                    currentFlow.SetState(data, state);
                }
                else
                {
                    var (nextFlow, nextNodeState) = this.RecFindNext((Flow<D>.NodeFlow)nextNode);
                    currentFlow.SetCurrentNode(nextNodeState);

                    // change current flow
                    if (nextFlow != currentFlow)
                    {
                        currentFlow.OnExit(data);
                        currentFlow = nextFlow;
                        currentFlow.OnEnter(data);
                    }

                    // change current state
                    var state = nextNodeState.state(data);
                    currentFlow.SetState(data, state);
                }
            }

            // update current flow
            currentFlow.OnUpdate(data);
        }
    }
}
