using IStateMonster = Fsm.IState<MonsterData>;

class StateIdle : IStateMonster
{
    public int timer = 0;

    public void OnExit(MonsterData data)
    {
        timer = 0;
        Console.WriteLine($"[FSM] state exit: {this.GetType()}");
    }
    public void OnUpdate(MonsterData data)
    {
        timer += 1;
        Console.WriteLine($"idle waiting...");
    }
}

class StateSelfHeal : IStateMonster
{
    public void OnEnter(MonsterData data)
    {
        data.isHealing = true;
        Console.WriteLine($"[FSM] state enter: {this.GetType()}");
    }
    public void OnExit(MonsterData data)
    {
        data.isHealing = false;
        Console.WriteLine($"[FSM] state exit: {this.GetType()}");
    }
    public void OnUpdate(MonsterData data)
    {
        data.health += 10;
        Console.WriteLine($"heal 10 : health = {data.health}");
    }
}

class StateFollowTarget : IStateMonster
{
    public void OnUpdate(MonsterData data)
    {
        Console.WriteLine($"follow target");
        data.targetDistance -= 5;
        if (data.targetDistance < 0) data.targetDistance = 0;
        Console.WriteLine($"distance = {data.targetDistance}");
    }
}

class StateAttackTarget : IStateMonster
{
    public void OnUpdate(MonsterData data)
    {
        Console.WriteLine($"attack target");
    }
}

class StateHitStagger : IStateMonster
{
    public void OnEnter(MonsterData data)
    {
        Console.WriteLine($"Oh no! I got hit!");
    }
    public void OnExit(MonsterData data)
    {
    }
    public void OnUpdate(MonsterData data)
    {
        data.hit = false;
    }
}

class StateGoHome : IStateMonster
{
    public void OnUpdate(MonsterData data)
    {
        Console.WriteLine($"I'm going home");
        data.targetDistance += data.targetDistance < 100 ? 5 : -5;
        Console.WriteLine($"distance = {data.targetDistance}");
    }
}
