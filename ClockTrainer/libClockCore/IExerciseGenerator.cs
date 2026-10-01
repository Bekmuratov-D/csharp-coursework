namespace ClockCore;

public interface IExerciseGenerator
{
    ClockExercise Next(ExerciseKind kind, Difficulty difficulty);
}
