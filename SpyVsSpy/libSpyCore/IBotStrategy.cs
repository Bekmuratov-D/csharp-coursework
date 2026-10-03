namespace SpyCore;

/// <summary>
/// Стратегия поведения бота. Реализация обязана принимать решения только на основе
/// информации, легально раскрытой через действия сессии (результаты Catch/Interrogate),
/// и не должна напрямую читать SecretIdentity соперника — это было бы подглядыванием.
/// </summary>
public interface IBotStrategy
{
    void TakeTurn(GameSession session, SpyPlayer self);
}
