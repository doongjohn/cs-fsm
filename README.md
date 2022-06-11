# Fsm

my ~~shitty~~ take on finite state machine

## Features

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
    // runs once when entering this state
    void OnEnter(D data) { }

    // runs once when exiting this state
    void OnExit(D data) { }

    // runs every update
    void OnUpdate(D data) { }
}
```

```csharp
// - this class contains state & flow transition logics
class Flow<D>
where D : class
{
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

    // if next retunrs null, then it will run `state` if not transition to `next`
    // the first `Do` will be executed when this flow is entered
    public Flow<D> Do(
        string name,
        Func<D, IState<D>> state,
        Func<D, string?> next
    ) { ... }

    // transition to the next flow
    public Flow<D> To(
        string name,
        Func<D, Flow<D>> next
    ) { ... }

    // NOTE: every lambda parameters of `ForceDo`, `ForceTo`, `Do`, `To` must be a pure function
    // because it runs multiple times per update
}
```

```csharp
class Fsm<D>
where D : class
{
    // initialize Fsm
    public Fsm(
        D data,
        Flow<D> startingFlow
    ) { ... }

    // execute this method to update current flow
    public void Update() { ... }
}
```
