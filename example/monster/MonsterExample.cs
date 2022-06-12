using System;
using System.Threading;

static class MonsterExample
{
    public static void Run()
    {
        // create data
        var monsterData = new MonsterData();

        // create fsm
        var monsterFsm = Monster.createFsm(monsterData);

        // run update every second
        new Timer(o => monsterFsm.Update(), null, 0, 1000);

        // get keyboard input
        // press escape to exit
        ConsoleKeyInfo key = new();
        do
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    monsterData.Damage(10);
                    Console.WriteLine("[input] hit monster -10");
                    break;

                case ConsoleKey.RightArrow:
                    monsterData.targetDistance += 10;
                    Console.WriteLine($"[input] distance + 10 = {monsterData.targetDistance}");
                    break;

                case ConsoleKey.LeftArrow:
                    monsterData.targetDistance -= 10;
                    if (monsterData.targetDistance < 0)
                        monsterData.targetDistance = 0;
                    Console.WriteLine($"[input] distance - 10 = {monsterData.targetDistance}");
                    break;
            }
        } while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape);

        Console.WriteLine("app exit");
        Environment.Exit(0);
    }
}
