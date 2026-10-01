namespace ClockCore;

public abstract class ClockExercise
{
    public Difficulty Difficulty { get; }
    public bool IsAnswered { get; private set; }
    public bool IsCorrect { get; private set; }

    public event EventHandler? Answered;

    protected ClockExercise(Difficulty difficulty)
    {
        Difficulty = difficulty;
    }

    public abstract string Prompt { get; }
    public abstract string CorrectAnswerText { get; }

    protected void Evaluate(bool correct)
    {
        IsAnswered = true;
        IsCorrect = correct;
        Answered?.Invoke(this, EventArgs.Empty);
    }
}
