using System.Windows;

namespace SpyVsSpy.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(a => a is "-h" or "--help" or "-?" or "/?"))
        {
            MessageBox.Show(
                "Шпион против шпиона\n\n" +
                "Параметры командной строки:\n" +
                "  --trophies=N   сколько поимок нужно для победы (по умолчанию 3)\n" +
                "  -h, --help     показать эту справку",
                "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var trophyGoal = 3;
        foreach (var arg in e.Args)
        {
            var parts = arg.Split('=', 2);
            if (parts.Length == 2 && parts[0].TrimStart('-').Equals("trophies", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out var parsed) && parsed > 0)
            {
                trophyGoal = parsed;
            }
        }

        var window = new MainWindow(trophyGoal);
        window.Show();
    }
}
