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
        public abstract class Node { }
        public class NodeState : Node
        {
            public Func<D, State<D>> state;
            public Func<D, string?> next;

            public NodeState(Func<D, State<D>> state, Func<D, string?> next)
            {
                this.state = state;
                this.next = next;
            }
        }
        public class NodeFlow : Node
        {
            public Func<D, Flow<D>> next;

            public NodeFlow(Func<D, Flow<D>> next)
            {
                this.next = next;
            }
        }

        private readonly List<(Func<D, bool> condition, Node node)> forceNodes;
        private readonly List<Node> nodes;

        // NOTE: I can avoid using dictionary if I create the INode variable
        // but... I am too lazy to do that every time
        private readonly Dictionary<string, int> indices;

        private Node? currentNode;
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
            forceNodes.Add((condition, new NodeState(state, next)));
            return this;
        }

        public Flow<D> ForceTo(
            Func<D, bool> condition,
            Func<D, Flow<D>> next)
        {
            forceNodes.Add((condition, new NodeFlow(next)));
            return this;
        }

        public Flow<D> Do(
            string name,
            Func<D, State<D>> state,
            Func<D, string?> next)
        {
            indices[name] = nodes.Count;
            nodes.Add(new NodeState(state, next));
            return this;
        }

        public Flow<D> To(
            string name,
            Func<D, Flow<D>> next)
        {
            indices[name] = nodes.Count;
            nodes.Add(new NodeFlow(next));
            return this;
        }

        public Node GetNodeByName(string name)
        {
            return nodes[indices[name]];
        }

        public Node? GetCurrentNode()
        {
            return currentNode;
        }

        public void SetCurrentNode(Node node)
        {
            currentNode = node;
        }

        public void SetInitialNode(D data)
        {
            currentNode = null;

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
            currentNode = nodes[0];
        }

        public Node? GetNextNode(D data)
        {
            // check force nodes
            for (int i = 0; i < forceNodes.Count; ++i)
            {
                var (condition, node) = forceNodes[i];
                if (condition(data))
                    return node;
            }

            if (currentNode is null)
                return nodes[0];

            if (currentNode is NodeState)
            {
                var nextNodeName = ((NodeState)currentNode).next(data);
                if (nextNodeName is not null)
                    return this.GetNodeByName(nextNodeName);

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

        private (Flow<D> nextFlow, Flow<D>.NodeState nextNodeState) RecFindNext(Flow<D> currentFlow, Flow<D>.Node currentNode)
        {
            // NOTE: this function can loop infinitely

            if (currentNode is Flow<D>.NodeState)
            {
                var currentNodeState = (Flow<D>.NodeState)currentNode;
                var nextNodeName = currentNodeState.next(data);
                if (nextNodeName is not null)
                {
                    // transition is found
                    var node = currentFlow.GetNodeByName(nextNodeName);
                    return this.RecFindNext(currentFlow, node);
                }
                else
                {
                    // no more transition is found
                    // stop recursing
                    return (currentFlow, currentNodeState);
                }
            }
            else
            {
                // get next flow
                var nextFlow = ((Flow<D>.NodeFlow)currentNode).next(data);
                nextFlow.SetInitialNode(data);

                // current node of `nextFlow` shouldn't be null after `SetInitialNode()` is called
                var nextNode = nextFlow.GetCurrentNode()!;
                return this.RecFindNext(nextFlow, nextNode);
            }
        }

        public void Update()
        {
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
                    var (nextFlow, nextNodeState) = this.RecFindNext(currentFlow, nextNode);

                    // change current flow
                    if (nextFlow != currentFlow)
                    {
                        currentFlow.OnExit(data);
                        currentFlow = nextFlow;
                        currentFlow.OnEnter(data);
                    }

                    currentFlow.SetCurrentNode(nextNodeState);

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
