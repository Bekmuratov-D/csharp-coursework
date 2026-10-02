using System.Windows;

namespace ClockTrainer.Wpf;

public partial class SettingsWindow : Window
{
    public string SelectedTheme => DarkRadio.IsChecked == true ? "Dark" : "Light";

    public SettingsWindow(string currentTheme)
    {
        InitializeComponent();

        DarkRadio.IsChecked = currentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
        LightRadio.IsChecked = !DarkRadio.IsChecked.GetValueOrDefault();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
