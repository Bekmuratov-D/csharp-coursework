namespace ClockCore;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Light";
    public Difficulty LastDifficulty { get; set; } = Difficulty.Medium;
    public ExerciseKind LastKind { get; set; } = ExerciseKind.SetTime;
    public TrainerMode LastMode { get; set; } = TrainerMode.Learn;
}
