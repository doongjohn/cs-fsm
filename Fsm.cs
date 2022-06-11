namespace Fsm
{
    interface IState<D>
    where D : class
    {
        void OnEnter(D data) =>
            Console.WriteLine($"[FSM] state enter: {this.GetType()}");
        void OnExit(D data) =>
            Console.WriteLine($"[FSM] state exit: {this.GetType()}");
        void OnUpdate(D data) =>
            Console.WriteLine($"[FSM] state update: {this.GetType()}");
    }

    class Flow<D>
    where D : class
    {
        public interface INode { }
        public struct NodeState : INode
        {
            public Func<D, IState<D>> state;
            public Func<D, string?> next;
        }
        public struct NodeFlow : INode
        {
            public Func<D, Flow<D>> next;
        }

        private readonly Dictionary<string, int> indexMap;
        private readonly List<(Func<D, bool> condition, INode node)> forceNodes;
        private readonly List<INode> nodes;
        private INode? currentNode;
        private IState<D>? currentState;

        public Flow()
        {
            indexMap = new();
            forceNodes = new();
            nodes = new();
        }

        public Flow<D> ForceDo(Func<D, bool> condition, Func<D, IState<D>> state, Func<D, string?> next)
        {
            forceNodes.Add((condition, new NodeState() { state = state, next = next }));
            return this;
        }
        public Flow<D> ForceTo(Func<D, bool> condition, Func<D, Flow<D>> next)
        {
            forceNodes.Add((condition, new NodeFlow() { next = next }));
            return this;
        }
        public Flow<D> Do(string name, Func<D, IState<D>> state, Func<D, string?> next)
        {
            indexMap[name] = nodes.Count;
            nodes.Add(new NodeState() { state = state, next = next });
            return this;
        }
        public Flow<D> To(string name, Func<D, Flow<D>> next)
        {
            indexMap[name] = nodes.Count;
            nodes.Add(new NodeFlow() { next = next });
            return this;
        }

        public INode GetNode(string name)
        {
            return nodes[indexMap[name]];
        }
        public NodeState GetNodeState(string name)
        {
            return (NodeState)nodes[indexMap[name]];
        }
        public NodeFlow GetNodeFlow(string name)
        {
            return (NodeFlow)nodes[indexMap[name]];
        }

        public INode? GetCurrentNode()
        {
            return currentNode;
        }
        public void SetCurrentNode(INode node)
        {
            this.currentNode = node;
        }

        public void SetInitialNode(D data)
        {
            currentNode = null;
            currentState = null;

            // check force nodes
            foreach (var forceNodeTuple in forceNodes)
            {
                var (condition, node) = forceNodeTuple;
                if (condition(data))
                {
                    currentNode = node;
                    return;
                }
            }

            // check normal nodes
            foreach (var node in nodes)
            {
                if (node is NodeState)
                {
                    var state = ((NodeState)node).state(data);
                    currentNode = node;
                    currentState = state;
                    return;
                }

                if (node is NodeFlow)
                {
                    currentNode = node;
                    return;
                }
            }
        }

        public INode? GetNextNode(D data)
        {
            foreach (var nodeTuple in forceNodes)
            {
                var (condition, node) = nodeTuple;
                if (condition(data))
                    return node;
            }

            if (currentNode is NodeState)
            {
                var nodeName = ((NodeState)currentNode).next(data);
                if (nodeName != null)
                    return this.GetNode(nodeName);
                else
                    return null;
            }

            if (currentNode is NodeFlow)
            {
                return currentNode;
            }

            return null;
        }

        public void SetState(D data, IState<D> newState)
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
                // TODO: prevent infinite loop
                // recursively find next state
                return this.RecFindNext((Flow<D>.NodeFlow)node);
            }
        }

        public void Update()
        {
            // REVIEW: this may cause an issue
            if (currentFlow.GetCurrentNode() == null)
            {
                currentFlow.SetInitialNode(data);
                currentFlow.OnEnter(data);
            }

            // try get next node
            var nextNode = currentFlow.GetNextNode(data);
            if (nextNode != null)
            {
                // change current node
                currentFlow.SetCurrentNode(nextNode);

                if (nextNode is Flow<D>.NodeState)
                {
                    // change current state
                    var nodeState = (Flow<D>.NodeState)nextNode;
                    var state = nodeState.state(data);
                    currentFlow.SetState(data, state);
                }
                else if (nextNode is Flow<D>.NodeFlow)
                {
                    var (nextFlow, nextNodeState) = this.RecFindNext((Flow<D>.NodeFlow)nextNode);

                    // change current node
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
