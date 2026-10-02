namespace ClockCore;

public static class CliParser
{
    public const string HelpText =
        """
        Тренажёр определения времени — консольный режим

        Параметры командной строки:
          --mode=learn|test               режим: обучение (показывает ответ) или проверка знаний
          --exercise=set|elapsed          тип упражнения: выставить стрелки / посчитать разницу времени
          --difficulty=easy|medium|hard   уровень сложности (шаг 30 / 5 / 1 минута)
          --format=digits|words|mixed     как показывать время в упражнении "выставь стрелки"
          --rounds=N                      количество раундов (по умолчанию 5)
          -h, --help                      показать эту справку

        Пример:
          cnsClockTrainer --mode=test --exercise=elapsed --difficulty=hard --rounds=10
        """;

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        foreach (var arg in args)
        {
            if (arg is "-h" or "--help" or "-?" or "/?")
            {
                options.ShowHelp = true;
                continue;
            }

            var parts = arg.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].TrimStart('-').ToLowerInvariant();
            var value = parts[1].ToLowerInvariant();

            switch (key)
            {
                case "mode":
                    options.Mode = value switch
                    {
                        "learn" => TrainerMode.Learn,
                        "test" => TrainerMode.Test,
                        _ => options.Mode
                    };
                    break;
                case "exercise":
                    options.Kind = value switch
                    {
                        "set" => ExerciseKind.SetTime,
                        "elapsed" => ExerciseKind.ElapsedTime,
                        _ => options.Kind
                    };
                    break;
                case "difficulty":
                    options.Difficulty = value switch
                    {
                        "easy" => Difficulty.Easy,
                        "medium" => Difficulty.Medium,
                        "hard" => Difficulty.Hard,
                        _ => options.Difficulty
                    };
                    break;
                case "format":
                    options.Format = value switch
                    {
                        "digits" => PromptFormat.Digits,
                        "words" => PromptFormat.Words,
                        "mixed" => PromptFormat.Mixed,
                        _ => options.Format
                    };
                    break;
                case "rounds":
                    if (int.TryParse(value, out var rounds) && rounds > 0)
                        options.Rounds = rounds;
                    break;
            }
        }

        return options;
    }
}
