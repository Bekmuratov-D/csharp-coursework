namespace ClockCore;

public sealed class RandomExerciseGenerator : IExerciseGenerator
{
    private readonly Random _random;

    public RandomExerciseGenerator(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public ClockExercise Next(ExerciseKind kind, Difficulty difficulty) => kind switch
    {
        ExerciseKind.SetTime => GenerateSetTime(difficulty),
        ExerciseKind.ElapsedTime => GenerateElapsedTime(difficulty),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private SetTimeExercise GenerateSetTime(Difficulty difficulty)
    {
        var step = difficulty.StepMinutes();
        var totalSteps = 24 * 60 / step;
        var minutes = _random.Next(0, totalSteps) * step;
        var target = new TimeOnly(0, 0).AddMinutes(minutes);
        return new SetTimeExercise(target, difficulty);
    }

    private ElapsedTimeExercise GenerateElapsedTime(Difficulty difficulty)
    {
        var step = difficulty.StepMinutes();
        const int maxGapMinutes = 4 * 60;
        var maxBeforeHour = 24 - maxGapMinutes / 60 - 1;

        var beforeHour = _random.Next(0, maxBeforeHour + 1);
        var beforeMinuteSteps = 60 / step;
        var beforeMinute = _random.Next(0, beforeMinuteSteps) * step;
        var before = new TimeOnly(beforeHour, beforeMinute);

        var maxSteps = maxGapMinutes / step;
        var steps = _random.Next(1, maxSteps + 1);
        var after = before.AddMinutes(steps * step);

        return new ElapsedTimeExercise(before, after, difficulty);
    }
}
