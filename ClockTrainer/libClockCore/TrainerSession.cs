namespace ClockCore;

public sealed class TrainerSession
{
    private readonly IExerciseGenerator _generator;

    public TrainerMode Mode { get; set; }
    public ExerciseKind Kind { get; set; }
    public Difficulty Difficulty { get; set; }
    public ClockExercise? Current { get; private set; }
    public Statistics Stats { get; } = new();

    public event EventHandler<ClockExercise>? ExerciseGenerated;
    public event EventHandler<bool>? AnswerEvaluated;
    public event EventHandler? StatisticsChanged;

    public TrainerSession(IExerciseGenerator generator, TrainerMode mode, ExerciseKind kind, Difficulty difficulty)
    {
        _generator = generator;
        Mode = mode;
        Kind = kind;
        Difficulty = difficulty;
    }

    public ClockExercise NewExercise()
    {
        Current = _generator.Next(Kind, Difficulty);
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
            StatisticsChanged?.Invoke(this, EventArgs.Empty);
        }

        AnswerEvaluated?.Invoke(this, correct);
        return correct;
    }
}
