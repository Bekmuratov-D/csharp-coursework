using System.Windows;
using ClockCore;

namespace ClockTrainer.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var options = CliParser.Parse(e.Args);
        if (options.ShowHelp)
        {
            MessageBox.Show(CliParser.HelpText, "Справка — Тренажёр определения времени",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var settingsStore = new JsonSettingsStore();
        var settings = settingsStore.Load();
        ThemeManager.Apply(settings.Theme);

        var window = new MainWindow(options);
        window.Show();
    }
}
