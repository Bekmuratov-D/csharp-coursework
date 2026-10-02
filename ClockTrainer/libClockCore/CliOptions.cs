namespace ClockCore;

public sealed class CliOptions
{
    public TrainerMode? Mode { get; set; }
    public ExerciseKind? Kind { get; set; }
    public Difficulty? Difficulty { get; set; }
    public PromptFormat? Format { get; set; }
    public int Rounds { get; set; } = 5;
    public bool ShowHelp { get; set; }
}
