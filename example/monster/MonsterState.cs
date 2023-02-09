using System;
using StateMonster = Fsm.State<MonsterData>;

class StateIdle : StateMonster
{
    public int timer = 0;

    public override void OnExit(MonsterData data)
    {
        timer = 0;
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
    }
    public override void OnExit(MonsterData data)
    {
        data.isHealing = false;
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
        Console.WriteLine($"monster starts following you...");
    }
    public override void OnUpdate(MonsterData data)
    {
        data.targetDistance -= 5;
        if (data.targetDistance < 0)
            data.targetDistance = 0;
        Console.WriteLine($"following: distance - 5 = {data.targetDistance}");
    }
}

class StateAttackTarget : StateMonster
{
    public override void OnExit(MonsterData data)
    {
        data.isAttackSuccess = false;
    }
    public override void OnUpdate(MonsterData data)
    {
        data.isAttackSuccess = true;
        Console.WriteLine($"monster hits you...");
    }
}

class StateHitStagger : StateMonster
{
    public override void OnEnter(MonsterData data)
    {
        Console.WriteLine($"monster got staggered...");
    }
    public override void OnUpdate(MonsterData data)
    {
        data.isHit = false;
    }
}
