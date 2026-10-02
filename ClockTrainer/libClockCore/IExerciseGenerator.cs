namespace ClockCore;

public interface IExerciseGenerator
{
    ClockExercise Next(ExerciseKind kind, Difficulty difficulty, PromptFormat format = PromptFormat.Digits);
}
