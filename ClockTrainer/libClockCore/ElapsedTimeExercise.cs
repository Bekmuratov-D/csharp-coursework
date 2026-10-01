namespace ClockCore;

public sealed class ElapsedTimeExercise : ClockExercise
{
    public TimeOnly Before { get; }
    public TimeOnly After { get; }
    public TimeSpan Expected { get; }

    public ElapsedTimeExercise(TimeOnly before, TimeOnly after, Difficulty difficulty) : base(difficulty)
    {
        Before = before;
        After = after;
        Expected = after.ToTimeSpan() - before.ToTimeSpan();
    }

    public override string Prompt => $"До: {Before:HH:mm}   После: {After:HH:mm}. Сколько времени прошло?";
    public override string CorrectAnswerText => $"{(int)Expected.TotalHours} ч {Expected.Minutes} мин";

    public bool CheckAnswer(TimeSpan userAnswer)
    {
        var correct = userAnswer == Expected;
        Evaluate(correct);
        return correct;
    }
}
