namespace ClockCore;

public sealed class SetTimeExercise : ClockExercise
{
    public TimeOnly Target { get; }

    public SetTimeExercise(TimeOnly target, Difficulty difficulty) : base(difficulty)
    {
        Target = target;
    }

    public override string Prompt => $"Выставьте на циферблате время {Target:HH:mm}";
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
