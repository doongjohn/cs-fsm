static class MonsterExample
{
    public static void Run()
    {
        // create fsm
        var monsterFsm = Monster.createFsm();

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
                    monsterFsm.data.hit = true;
                    monsterFsm.data.health -= 10;
                    if (monsterFsm.data.health < 0)
                        monsterFsm.data.health = 0;
                    Console.WriteLine("[input] hit monster -10");
                    break;

                case ConsoleKey.RightArrow:
                    monsterFsm.data.targetDistance += 10;
                    Console.WriteLine("[input] increase distance +10");
                    Console.WriteLine($"distance = {monsterFsm.data.targetDistance}");
                    break;

                case ConsoleKey.LeftArrow:
                    monsterFsm.data.targetDistance -= 10;
                    if (monsterFsm.data.targetDistance < 0)
                        monsterFsm.data.targetDistance = 0;
                    Console.WriteLine("[input] decrease distance -10");
                    Console.WriteLine($"distance = {monsterFsm.data.targetDistance}");
                    break;
            }
        } while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape);

        Console.WriteLine("app exit");
        Environment.Exit(0);
    }
}
