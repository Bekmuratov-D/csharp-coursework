namespace ClockCore;

public sealed record ExerciseResult(ExerciseKind Kind, Difficulty Difficulty, bool IsCorrect, DateTime Timestamp);
