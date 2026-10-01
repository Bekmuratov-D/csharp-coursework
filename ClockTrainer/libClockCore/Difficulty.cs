namespace ClockCore;

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

public static class DifficultyExtensions
{
    public static int StepMinutes(this Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 30,
        Difficulty.Medium => 5,
        Difficulty.Hard => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
    };
}
