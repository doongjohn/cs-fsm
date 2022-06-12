using Fsm;

class MonsterData
{
    public int health = 100;
    public bool hit = false;
    public bool isHealing = false;
    public int targetDistance = 100;

    public void Damage(int amount)
    {
        hit = true;
        health -= amount;
        if (health < 0)
            health = 0;
    }
}

static class Monster
{
    public static Fsm<MonsterData> createFsm()
    {
        var monsterData = new MonsterData();

        var idle = new StateIdle();
        var selfHeal = new StateSelfHeal();
        var hitStagger = new StateHitStagger();
        var followTarget = new StateFollowTarget();
        var attackTarget = new StateAttackTarget();
        var goHome = new StateGoHome();

        var flowNormal = new Flow<MonsterData>();
        var flowHitStagger = new Flow<MonsterData>();
        var flowHitResponse = new Flow<MonsterData>();

        flowNormal
            .ForceTo(
                condition: data => data.hit,
                next: data => flowHitStagger
            )
            .Do(
                name: "idle",
                state: data => idle,
                next: data =>
                {
                    if (data.health <= 50 && idle.timer >= 3)
                        return "heal";

                    return null;
                }
            )
            .Do(
                name: "heal",
                state: data => selfHeal,
                next: data =>
                {
                    if (data.health >= 100)
                        return "idle";

                    return null;
                }
            );

        flowHitStagger
            .ForceTo(
                condition: data => !data.hit,
                next: data => flowNormal
            )
            .Do(
                name: "stagger",
                state: data => hitStagger,
                next: data => null
            );

        // create fsm
        return new Fsm<MonsterData>(monsterData, flowNormal);
    }
}
