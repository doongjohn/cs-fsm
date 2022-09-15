using Fsm;

class MonsterData
{
    public int health = 100;
    public int targetDistance = 50;
    public bool isHit = false;
    public bool isHealing = false;
    public bool isAttackSuccess = false;

    public void Damage(int amount)
    {
        isHit = true;
        health -= amount;
        if (health < 0)
            health = 0;
    }
}

static class Monster
{
    public static Fsm<MonsterData> createFsm(MonsterData monsterData)
    {
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
                condition: data => data.isHit,
                next: data => flowHitStagger
            )
            .Do(
                name: "idle",
                state: data => idle,
                next: data =>
                    data.health <= 50 && idle.timer >= 3
                    // true
                    ? "heal"
                    : null
            )
            .Do(
                name: "heal",
                state: data => selfHeal,
                next: data =>
                    data.health >= 100
                    // true
                    ? "idle"
                    : null
            );

        flowHitStagger
            .ForceTo(
                condition: data => !data.isHit,
                next: data => flowHitResponse
            )
            .Do(
                name: "stagger",
                state: data => hitStagger,
                next: data => null
            );

        flowHitResponse
            .Do(
                name: "aggro",
                state: data => followTarget,
                next: data =>
                    data.targetDistance == 0
                    ? "attack"
                    : null
            )
            .Do(
                name: "attack",
                state: data => attackTarget,
                next: data =>
                    data.isAttackSuccess
                    ? "finish"
                    : null
            )
            .To(
                name: "finish",
                next: data =>
                {
                    data.isHit = false;
                    return flowNormal;
                }
            );

        // create fsm
        return new Fsm<MonsterData>(monsterData, flowNormal);
    }
}
