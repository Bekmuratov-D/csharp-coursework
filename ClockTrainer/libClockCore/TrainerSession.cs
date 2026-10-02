namespace ClockCore;

public sealed class TrainerSession
{
    private readonly IExerciseGenerator _generator;

    public TrainerMode Mode { get; set; }
    public ExerciseKind Kind { get; set; }
    public Difficulty Difficulty { get; set; }
    public PromptFormat Format { get; set; }
    public ClockExercise? Current { get; private set; }
    public Statistics Stats { get; } = new();

    public int MaxRounds { get; set; } = 10;
    public int MaxMistakes { get; set; } = 3;
    public int RoundsPlayed { get; private set; }
    public int Lives { get; private set; }

    public bool IsGameOver => Mode == TrainerMode.Test && (Lives <= 0 || RoundsPlayed >= MaxRounds);

    public event EventHandler<ClockExercise>? ExerciseGenerated;
    public event EventHandler<bool>? AnswerEvaluated;
    public event EventHandler? StatisticsChanged;
    public event EventHandler<GameResult>? GameEnded;

    public TrainerSession(IExerciseGenerator generator, TrainerMode mode, ExerciseKind kind, Difficulty difficulty,
        PromptFormat format = PromptFormat.Digits)
    {
        _generator = generator;
        Mode = mode;
        Kind = kind;
        Difficulty = difficulty;
        Format = format;
        Lives = MaxMistakes;
    }

    public ClockExercise NewExercise()
    {
        Current = _generator.Next(Kind, Difficulty, Format);
        ExerciseGenerated?.Invoke(this, Current);
        return Current;
    }

    public bool Submit(object answer)
    {
        if (Current is null)
            throw new InvalidOperationException("Нет активного упражнения. Сначала вызовите NewExercise().");

        var correct = Current switch
        {
            SetTimeExercise ex when answer is TimeOnly t => ex.CheckAnswer(t),
            ElapsedTimeExercise ex when answer is TimeSpan s => ex.CheckAnswer(s),
            _ => throw new ArgumentException("Тип ответа не соответствует текущему упражнению.", nameof(answer))
        };

        if (Mode == TrainerMode.Test)
        {
            Stats.Add(new ExerciseResult(Kind, Difficulty, correct, DateTime.Now));
            RoundsPlayed++;
            if (!correct) Lives--;
            StatisticsChanged?.Invoke(this, EventArgs.Empty);
        }

        AnswerEvaluated?.Invoke(this, correct);

        if (IsGameOver)
            GameEnded?.Invoke(this, Lives <= 0 ? GameResult.Lost : GameResult.Won);

        return correct;
    }
}
