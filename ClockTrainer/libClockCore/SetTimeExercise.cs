namespace ClockCore;

public sealed class SetTimeExercise : ClockExercise
{
    public TimeOnly Target { get; }
    public bool UseWords { get; }

    public SetTimeExercise(TimeOnly target, Difficulty difficulty, bool useWords = false) : base(difficulty)
    {
        Target = target;
        UseWords = useWords;
    }

    public override string Prompt => UseWords
        ? $"Выставьте на циферблате время: {RussianTimeWords.ToWords(Target)}"
        : $"Выставьте на циферблате время {Target:HH:mm}";

    public override string CorrectAnswerText => Target.ToString("HH:mm");

    public bool CheckAnswer(TimeOnly userHands)
    {
        var toleranceMinutes = Difficulty.StepMinutes() / 2;
        var diff = Math.Abs((userHands.ToTimeSpan() - Target.ToTimeSpan()).TotalMinutes);
        diff = Math.Min(diff, 1440 - diff);

        var correct = diff <= toleranceMinutes;
        Evaluate(correct);
        return correct;
    }
}
