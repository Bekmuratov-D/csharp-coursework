using ClockCore;

var options = CliParser.Parse(args);

if (options.ShowHelp)
{
    Console.WriteLine(CliParser.HelpText);
    return;
}

var settingsStore = new JsonSettingsStore();
var settings = settingsStore.Load();

var mode = options.Mode ?? settings.LastMode;
var kind = options.Kind ?? settings.LastKind;
var difficulty = options.Difficulty ?? settings.LastDifficulty;

settings.LastMode = mode;
settings.LastKind = kind;
settings.LastDifficulty = difficulty;
settingsStore.Save(settings);

Console.WriteLine("Тренажёр определения времени — консоль");
Console.WriteLine($"Режим: {DescribeMode(mode)}   Упражнение: {DescribeKind(kind)}   Сложность: {DescribeDifficulty(difficulty)}");
Console.WriteLine("Подсказка: -h или --help покажет список параметров запуска.");
Console.WriteLine();

var session = new TrainerSession(new RandomExerciseGenerator(), mode, kind, difficulty);

session.ExerciseGenerated += (_, exercise) => Console.WriteLine(exercise.Prompt);
session.AnswerEvaluated += (_, correct) =>
{
    if (mode == TrainerMode.Learn)
        Console.WriteLine($"Правильный ответ: {session.Current!.CorrectAnswerText}");
    else
        Console.WriteLine(correct ? "Верно!" : $"Неверно. Правильный ответ: {session.Current!.CorrectAnswerText}");

    Console.WriteLine();
};

for (var round = 1; round <= options.Rounds; round++)
{
    Console.WriteLine($"--- Раунд {round}/{options.Rounds} ---");
    session.NewExercise();

    switch (kind)
    {
        case ExerciseKind.SetTime:
            session.Submit(ReadTime("Ваш ответ (чч:мм): "));
            break;
        case ExerciseKind.ElapsedTime:
            session.Submit(ReadDuration("Сколько прошло времени (чч:мм): "));
            break;
    }
}

if (mode == TrainerMode.Test)
{
    Console.WriteLine("=== Итоги ===");
    Console.WriteLine($"Верно: {session.Stats.Correct}  Неверно: {session.Stats.Incorrect}  Точность: {session.Stats.Accuracy:F0}%");
}

static TimeOnly ReadTime(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        if (TimeOnly.TryParseExact(input, "HH:mm", out var result))
            return result;

        Console.WriteLine("Не понял формат, нужно чч:мм, например 15:45");
    }
}

static TimeSpan ReadDuration(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        var parts = input?.Split(':', 2);
        if (parts is { Length: 2 } &&
            int.TryParse(parts[0], out var hours) &&
            int.TryParse(parts[1], out var minutes) &&
            hours is >= 0 and < 24 && minutes is >= 0 and < 60)
        {
            return new TimeSpan(hours, minutes, 0);
        }

        Console.WriteLine("Не понял формат, нужно чч:мм, например 01:15");
    }
}

static string DescribeMode(TrainerMode mode) => mode switch
{
    TrainerMode.Learn => "обучение",
    TrainerMode.Test => "проверка знаний",
    _ => mode.ToString()
};

static string DescribeKind(ExerciseKind kind) => kind switch
{
    ExerciseKind.SetTime => "выставь стрелки",
    ExerciseKind.ElapsedTime => "сколько времени прошло",
    _ => kind.ToString()
};

static string DescribeDifficulty(Difficulty difficulty) => difficulty switch
{
    Difficulty.Easy => "лёгкая",
    Difficulty.Medium => "средняя",
    Difficulty.Hard => "сложная",
    _ => difficulty.ToString()
};
