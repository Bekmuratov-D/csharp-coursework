namespace ClockCore;

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
