using System;
using StateMonster = Fsm.State<MonsterData>;

class StateIdle : StateMonster
{
    public int timer = 0;

    public override void OnExit(MonsterData data)
    {
        timer = 0;
        Console.WriteLine($"[FSM] state exit: {this.GetType()}");
    }
    public override void OnUpdate(MonsterData data)
    {
        timer += 1;
        Console.WriteLine($"idling...");
    }
}

class StateSelfHeal : StateMonster
{
    public override void OnEnter(MonsterData data)
    {
        data.isHealing = true;
        Console.WriteLine($"[FSM] state enter: {this.GetType()}");
    }
    public override void OnExit(MonsterData data)
    {
        data.isHealing = false;
        Console.WriteLine($"[FSM] state exit: {this.GetType()}");
    }
    public override void OnUpdate(MonsterData data)
    {
        data.health += 10;
        Console.WriteLine($"heal 10 : health = {data.health}");
    }
}

class StateFollowTarget : StateMonster
{
    public override void OnEnter(MonsterData data)
    {
        Console.WriteLine($"start following target...");
    }
    public override void OnUpdate(MonsterData data)
    {
        data.targetDistance -= 5;
        if (data.targetDistance < 0)
            data.targetDistance = 0;
        Console.WriteLine($"distance = {data.targetDistance}");
    }
}

class StateAttackTarget : StateMonster
{
    public override void OnUpdate(MonsterData data)
    {
        Console.WriteLine($"I punch you!");
    }
}

class StateHitStagger : StateMonster
{
    public override void OnUpdate(MonsterData data)
    {
        data.isHit = false;
        Console.WriteLine($"Oh no! I got hit!");
    }
}

class StateGoHome : StateMonster
{
    public override void OnEnter(MonsterData data)
    {
        Console.WriteLine($"I'm going home!");
    }
    public override void OnUpdate(MonsterData data)
    {
        data.targetDistance += data.targetDistance < 100 ? 5 : -5;
        Console.WriteLine($"distance = {data.targetDistance}");
    }
}
