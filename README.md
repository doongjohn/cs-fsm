# Fsm

my ~~shitty~~ take on finite state machine

## References

> <img width="24px" src="https://yt3.ggpht.com/ytc/AKedOLQMDfbOQvRp6eZg9qu0v8p_iN5mgIge9D-gTAMjmA=s48-c-k-c0x00ffffff-no-rj"></img> The AI of Half-Life: Finite State Machines | AI 101\
> <https://www.youtube.com/watch?v=JyF0oyarz4U>

## Features

- it's some sort of a hierarchical finite state machine
    - state does not contain transition logic (flow does)
- single file (no dependency)
- you can use my awesome api

## Example

```csharp
// TODO: exmaple code
```

## API

```csharp
// - user defined state class must inherit this interface
// - generic D is a class that contains all shared data
interface IState<D>
where D : class
{
    // - runs once when entering this state
    void OnEnter(D data) { }

    // - runs once when exiting this state
    void OnExit(D data) { }

    // - runs every update
    void OnUpdate(D data) { }
}
```

```csharp
// - this class contains state & flow transition logics
class Flow<D>
where D : class
{
    // - runs once when entering this flow
    public Flow<D> OnEnter(Action<D> onEnter) { ... }

    // - runs once when exiting this flow
    public Flow<D> OnExit(Action<D> onEnter) { ... }

    // - this logic is always being checked on every update
    // - if the condition returns true, then it will run its `state`
    // - if the condition returns true, then it will transition to the `next`
    //   (when `next` returns null, then it will not change state)
    public Flow<D> FoceDo(
        Func<D, bool> condition,
        Func<D, IState<D>> state,
        Func<D, string?> next
    ) { ... }

    // - this logic is always being checked on every update
    // - if the condition returns true, then it will transition to the next flow
    public Flow<D> ForceTo(
        Func<D, bool> condition,
        Func<D, Flow<D>> next
    ) { ... }

    // - if next retunrs null, then it will run `state` if not transition to `next`
    // - the first `Do` will be executed when this flow is entered
    public Flow<D> Do(
        string name,
        Func<D, IState<D>> state,
        Func<D, string?> next
    ) { ... }

    // - transition to the next flow
    public Flow<D> To(
        string name,
        Func<D, Flow<D>> next
    ) { ... }

    // NOTE: every lambda parameters of `ForceDo`, `ForceTo`, `Do`, `To` must not
    // mutate outside values because it runs multiple times per frame
}
```

```csharp
class Fsm<D>
where D : class
{
    // - initialize Fsm
    public Fsm(
        D data,
        Flow<D> startingFlow
    ) { ... }

    // - execute this method to update this state machine
    public void UpdateFsm() { ... }

    // - execute this method to call `OnUpdate` callback of the current state
    public void Update() { ... }
}
```
