using System.Windows;

namespace ClockTrainer.Wpf;

public static class ThemeManager
{
    public static void Apply(string theme)
    {
        var app = Application.Current;
        var dictionaries = app.Resources.MergedDictionaries;

        var themeSource = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
            ? "Themes/DarkTheme.xaml"
            : "Themes/LightTheme.xaml";

        var newTheme = new ResourceDictionary { Source = new Uri(themeSource, UriKind.Relative) };

        var oldTheme = dictionaries.FirstOrDefault(d =>
            d.Source is not null && d.Source.OriginalString.Contains("Theme.xaml"));

        if (oldTheme is not null)
            dictionaries.Remove(oldTheme);

        dictionaries.Insert(0, newTheme);
    }
}
