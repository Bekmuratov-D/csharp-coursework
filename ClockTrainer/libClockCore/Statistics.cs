namespace ClockCore;

public sealed class Statistics
{
    private readonly List<ExerciseResult> _history = [];

    public IReadOnlyList<ExerciseResult> History => _history;

    public int Correct => _history.Count(r => r.IsCorrect);
    public int Incorrect => _history.Count(r => !r.IsCorrect);
    public int Total => _history.Count;
    public double Accuracy => Total == 0 ? 0 : (double)Correct / Total * 100;

    public void Add(ExerciseResult result) => _history.Add(result);

    public void Reset() => _history.Clear();
}
