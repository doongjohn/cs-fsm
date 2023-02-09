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
        monsterFsm.printDebugMsg = true; // you must define FSM_DEBUG_CONSOLE to use this

        // run update every second
        // (this creates a new thread so... my code is not thread safe lol)
        new Timer(o =>
            {
                monsterFsm.UpdateFsm();
                monsterFsm.Update();
            },
            null, 0, 1000);

        Console.WriteLine("* press enter to attack.");
        Console.WriteLine("* press left/right arraw to move.");
        Console.WriteLine("* press escape to exit.");

        // get keyboard input
        ConsoleKeyInfo key = new();
        do
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    monsterData.DamageHp(10);
                    Console.WriteLine($"[input] attack: monster hp - 10 = {monsterData.health}");
                    break;

                case ConsoleKey.RightArrow:
                    monsterData.targetDistance += 10;
                    Console.WriteLine($"[input] move: distance + 10 = {monsterData.targetDistance}");
                    break;

                case ConsoleKey.LeftArrow:
                    monsterData.targetDistance -= 10;
                    monsterData.targetDistance = Math.Max(monsterData.targetDistance, 0);
                    Console.WriteLine($"[input] move: distance - 10 = {monsterData.targetDistance}");
                    break;
            }
        } while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape);

        Console.WriteLine("app exit");
        Environment.Exit(0);
    }
}
