#nullable enable

#if FSM_DEBUG_CONSOLE || FSM_DEBUG_UNITY
#define FSM_DEBUG
#endif

using System;
using System.Collections.Generic;

#if FSM_DEBUG
namespace FsmDebug
{
    public static class Logger
    {
        public static void Log(string msg)
        {
#if FSM_DEBUG_CONSOLE
            Console.WriteLine(msg);
#endif
#if FSM_DEBUG_UNITY
            UnityEngine.Debug.Log(msg);
#endif
        }
        public static void Error(string msg)
        {
#if FSM_DEBUG_CONSOLE
            Console.WriteLine(msg);
            Environment.Exit(1);
#endif
#if FSM_DEBUG_UNITY
            UnityEngine.Debug.LogError(msg);
            UnityEngine.Debug.Break();
#endif
        }
    }
}
#endif

namespace Fsm
{
    public abstract class State<D>
    where D : class
    {
        public virtual void OnEnter(D data) { }
        public virtual void OnExit(D data) { }
        public virtual void OnUpdate(D data) { }
        public virtual void OnLateUpdate(D data) { }
        public virtual void OnFixedUpdate(D data) { }
    }

    public class Flow<D>
    where D : class
    {
        public abstract class Node { }
        public class NodeState : Node
        {
            public Func<D, State<D>> state;
            public Func<D, string?> next;
            public bool restart;

            public NodeState(Func<D, State<D>> state, Func<D, string?> next, bool restart)
            {
                this.state = state;
                this.next = next;
                this.restart = restart;
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
        private Action<D>? onEnter;
        private Action<D>? onExit;

        // map node name to index
        private readonly Dictionary<string, int> indices;

#if FSM_DEBUG
        private readonly Dictionary<int, string> debugFlowNames = new();
#endif

        private Node? currentNode;
        private State<D>? currentState;

        public Flow()
        {
            this.forceNodes = new();
            this.nodes = new();
            this.indices = new();
        }

        public Flow<D> OnEnter(Action<D> onEnter)
        {
            this.onEnter = onEnter;
            return this;
        }
        public Flow<D> OnExit(Action<D> onExit)
        {
            this.onExit = onExit;
            return this;
        }

        public Flow<D> ForceDo(
            Func<D, bool> condition,
            Func<D, State<D>> state,
            Func<D, string?> next,
            bool restart = false)
        {
            this.forceNodes.Add((condition, new NodeState(state, next, restart)));
            return this;
        }

        public Flow<D> ForceTo(
            Func<D, bool> condition,
            Func<D, Flow<D>> next)
        {
            this.forceNodes.Add((condition, new NodeFlow(next)));
            return this;
        }

        public Flow<D> Do(
            string name,
            Func<D, State<D>> state,
            Func<D, string?> next,
            bool restart = false)
        {
#if FSM_DEBUG
            this.debugFlowNames[this.nodes.Count] = name;
#endif
            this.indices[name] = this.nodes.Count;
            this.nodes.Add(new NodeState(state, next, restart));
            return this;
        }

        public Flow<D> To(
            string name,
            Func<D, Flow<D>> next)
        {
            this.indices[name] = this.nodes.Count;
            this.nodes.Add(new NodeFlow(next));
            return this;
        }

#if FSM_DEBUG
        public string GetName(Node node)
        {
            var i = this.nodes.IndexOf(node) - 1;
            if (i > 0)
                return this.debugFlowNames[i];
            return "??";
        }
#endif

        public Node GetNodeByName(string name)
        {
            return this.nodes[this.indices[name]];
        }

        public Node? GetCurrentNode()
        {
            return this.currentNode;
        }

        public void SetCurrentNode(Node node)
        {
            this.currentNode = node;
        }

        public void SetInitialNode(D data)
        {
            // check force nodes
            for (int i = 0; i < this.forceNodes.Count; ++i)
            {
                var (condition, node) = this.forceNodes[i];
                if (condition(data) == true)
                {
                    this.currentNode = node;
                    return;
                }
            }

            // set first node
            this.currentNode = this.nodes[0];
        }

        public Node? GetNextNode(D data)
        {
            // check force nodes
            for (int i = 0; i < this.forceNodes.Count; ++i)
            {
                var (condition, node) = this.forceNodes[i];
                if (condition(data) == true)
                    return node;
            }

            if (this.currentNode is null)
                return this.nodes[0];

            if (this.currentNode is NodeState currentNodeState)
            {
                var nextNodeName = currentNodeState.next(data);
                if (nextNodeName is not null)
                    return this.GetNodeByName(nextNodeName);

                return null;
            }

            return this.currentNode;
        }

        public State<D>? GetState() => this.currentState;

        public void SetState(State<D> newState)
        {
            this.currentState = newState;
        }

        public void OnEnter(D data)
        {
            this.onEnter?.Invoke(data);
            this.currentState = null;
        }
        public void OnExit(D data)
        {
            this.onExit?.Invoke(data);
            this.currentState = null;
        }

        public void OnUpdate(D data)
        {
            this.currentState?.OnUpdate(data);
        }

        public void OnLateUpdate(D data)
        {
            this.currentState?.OnLateUpdate(data);
        }

        public void OnFixedUpdate(D data)
        {
            this.currentState?.OnFixedUpdate(data);
        }
    }

    public class Fsm<D>
    where D : class
    {
#if FSM_DEBUG
        public bool printDebugMsg = false;
        private int currentRecurseCount = 0;
        private static int maxRecurseCount = 80;
        private static int maxTraceCount = 20;
        private Queue<string> nodeTraceQueue = new();
#endif

        // TODO: expose current state
        // TODO: expose previous state
        public readonly D data;
        private Flow<D> currentFlow;
        private State<D>? currentState;

        public Fsm(D data, Flow<D> startingFlow)
        {
            this.data = data;
            this.currentFlow = startingFlow;
        }

        // NOTE: this function can loop infinitely
        private (Flow<D> nextFlow, Flow<D>.NodeState nextNodeState) GetNextRecursive(Flow<D> currentFlow, Flow<D>.Node currentNode)
        {
            // https://www.danielcrabtree.com/blog/152/c-sharp-7-is-operator-patterns-you-wont-need-as-as-often

            // if current node is state node
            if (currentNode is Flow<D>.NodeState currentNodeState)
            {
                var nextNodeName = currentNodeState.next(data);
                if (nextNodeName is not null)
                {
#if FSM_DEBUG
                    if (this.printDebugMsg)
                    {
                        this.nodeTraceQueue.Enqueue(nextNodeName);
                        if (this.nodeTraceQueue.Count > Fsm<D>.maxTraceCount)
                            this.nodeTraceQueue.Dequeue();

                        this.currentRecurseCount += 1;
                        if (this.currentRecurseCount >= Fsm<D>.maxRecurseCount)
                        {
                            string msg = $"[FSM] possible infinite recursion detected! ({this.currentRecurseCount} recursion)\n";
                            while (this.nodeTraceQueue.Count > 0)
                            {
                                msg += "--> " + this.nodeTraceQueue.Dequeue() + "\n";
                            }
                            FsmDebug.Logger.Error(msg);
                            return (currentFlow, currentNodeState);
                        }
                    }
#endif
                    // transition is found (recurse)
                    return this.GetNextRecursive(currentFlow, currentFlow.GetNodeByName(nextNodeName));
                }
                else
                {
                    // no more transition is found (stop recursing)
                    return (currentFlow, currentNodeState);
                }
            }
            // if curent node is flow node
            else
            {
                // get next flow
                var nextFlow = ((Flow<D>.NodeFlow)currentNode).next(data);
                nextFlow.SetInitialNode(data);

                // current node of `nextFlow` shouldn't be null after `SetInitialNode()` is called
                return this.GetNextRecursive(nextFlow, nextFlow.GetCurrentNode()!);
            }
        }

        public void UpdateFsm()
        {
            // try get next node
            var nextNode = this.currentFlow.GetNextNode(data);
            if (nextNode is not null)
            {
#if FSM_DEBUG
                this.currentRecurseCount = 0;
                this.nodeTraceQueue.Enqueue(this.currentFlow.GetName(nextNode));
                if (this.nodeTraceQueue.Count > Fsm<D>.maxTraceCount)
                    this.nodeTraceQueue.Dequeue();
#endif

                var (nextFlow, nextNodeState) = this.GetNextRecursive(this.currentFlow, nextNode);
                this.currentFlow.SetCurrentNode(nextNodeState);

                // change current flow
                if (nextFlow != this.currentFlow)
                {
                    this.currentFlow.OnExit(data);
                    this.currentFlow = nextFlow;
                    this.currentFlow.OnEnter(data);
                }

                State<D> nextState = nextNodeState.state(data);
                this.currentFlow.SetState(nextState);

                // change current state
                if (nextNodeState.restart || nextState != this.currentState)
                {
#if FSM_DEBUG
                    if (this.printDebugMsg && this.currentState is not null)
                        FsmDebug.Logger.Log($"[FSM] state exit: {this.currentState.GetType()}");
#endif
                    this.currentState?.OnExit(data);
                    this.currentState = nextState;
                    this.currentState.OnEnter(data);
#if FSM_DEBUG
                    if (this.printDebugMsg)
                        FsmDebug.Logger.Log($"[FSM] state enter: {this.currentState.GetType()}");
#endif
                }
            }
        }

        public void Update()
        {
            this.currentFlow.OnUpdate(data);
        }

        public void LateUpdate()
        {
            this.currentFlow.OnLateUpdate(data);
        }

        public void FixedUpdate()
        {
            this.currentFlow.OnFixedUpdate(data);
        }
    }
}
