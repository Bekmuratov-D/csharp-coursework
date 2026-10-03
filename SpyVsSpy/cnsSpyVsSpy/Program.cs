using SpyCore;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var suspects = SuspectNames.All.Select(n => new Suspect(n)).ToList();
var grid = new SuspectGrid(5, suspects);

var human = new SpyPlayer("Вы", isBot: false);
var bot = new SpyPlayer("Бот", isBot: true);
var botStrategy = new SimpleBotStrategy();

var session = new GameSession(grid, human, bot, trophyGoal: 3);
session.TurnLogged += (_, message) => Console.WriteLine($"  >> {message}");

Console.WriteLine("Шпион против шпиона — консольная версия");
Console.WriteLine("Поймайте тайную личность соперника 3 раза, прежде чем он поймает вас.\n");

while (!session.IsOver)
{
    PrintGrid(grid, human);
    Console.WriteLine($"Счёт — Вы: {human.Trophies}   Бот: {bot.Trophies}");

    if (session.CurrentPlayer == bot)
    {
        Console.WriteLine("Ход бота...");
        botStrategy.TakeTurn(session, bot);
        Console.WriteLine();
        continue;
    }

    PlayHumanTurn(session, human);
    Console.WriteLine();
}

Console.WriteLine($"\nИгра окончена! Победитель: {session.Winner!.Name}");

static void PrintGrid(SuspectGrid grid, SpyPlayer human)
{
    var selfPosition = grid.Find(human.SecretIdentity);
    Console.WriteLine();

    for (var row = 0; row < grid.Size; row++)
    {
        for (var col = 0; col < grid.Size; col++)
        {
            var position = new GridPosition(row, col);
            var suspect = grid.At(position);
            var label = suspect.IsAlive ? suspect.Name : $"[{suspect.Name}]";
            if (selfPosition == position) label = $"*{label}*";
            Console.Write(label.PadRight(14));
        }
        Console.WriteLine();
    }

    Console.WriteLine($"\nВаша текущая личность: {human.SecretIdentity.Name}");
}

static void PlayHumanTurn(GameSession session, SpyPlayer human)
{
    var targets = session.LegalTargets(human);

    Console.WriteLine("\nДоступные карты (соседи по всем направлениям, включая диагонали, и вы сами):");
    for (var i = 0; i < targets.Count; i++)
        Console.WriteLine($"  {i + 1}. {session.Grid.At(targets[i]).Name}");

    Console.WriteLine("\nЧто делаете?");
    Console.WriteLine("  1 — Поймать");
    Console.WriteLine("  2 — Сканировать зону вокруг выбранной карты (3х3, до 9 карт)");
    Console.WriteLine("  3 — Сдвинуть ряд/столбец");

    var choice = ReadChoice("Выбор: ", 1, 3);

    switch (choice)
    {
        case 1:
        {
            var index = ReadChoice("Кого ловим (номер)? ", 1, targets.Count) - 1;
            var caught = session.Catch(targets[index]);
            Console.WriteLine(caught ? "Поймали!" : "Мимо.");
            break;
        }
        case 2:
        {
            var index = ReadChoice("Вокруг кого сканируем (номер)? ", 1, targets.Count) - 1;
            var zone = session.ScanZone(targets[index]);
            var found = session.Scan(targets[index]);
            Console.WriteLine(found
                ? $"Бот где-то здесь: {string.Join(", ", zone.Select(s => s.Name))}."
                : "Бота нет ни в одной из этих карт.");
            break;
        }
        case 3:
        {
            Console.Write("Ряд или столбец (р/с)? ");
            var axis = Console.ReadLine()?.Trim().ToLowerInvariant() == "с" ? ShiftAxis.Column : ShiftAxis.Row;
            var index = ReadChoice($"Номер (1-{session.Grid.Size})? ", 1, session.Grid.Size) - 1;
            var directionPrompt = axis == ShiftAxis.Row ? "Направление (1 = вправо, 2 = влево)? " : "Направление (1 = вниз, 2 = вверх)? ";
            var directionChoice = ReadChoice(directionPrompt, 1, 2);
            var direction = directionChoice == 1 ? 1 : -1;

            try
            {
                session.Shift(axis, index, direction);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Нельзя: {ex.Message}");
            }
            break;
        }
    }
}

static int ReadChoice(string prompt, int min, int max)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out var value) && value >= min && value <= max)
            return value;

        Console.WriteLine("Некорректный ввод, попробуйте снова.");
    }
}
