using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ClockCore;
using ClockTrainer.Wpf.Pages;

namespace ClockTrainer.Wpf;

public partial class MainWindow : Window
{
    private readonly ISettingsStore _settingsStore = new JsonSettingsStore();
    private readonly AppSettings _settings;
    private readonly CliOptions _startupOptions;

    private PlayPage? _playPage;

    public MainWindow(CliOptions startupOptions)
    {
        InitializeComponent();

        _startupOptions = startupOptions;
        _settings = _settingsStore.Load();

        if (startupOptions.Mode is not null || startupOptions.Kind is not null || startupOptions.Difficulty is not null || startupOptions.Format is not null)
        {
            StartSession(
                startupOptions.Mode ?? _settings.LastMode,
                startupOptions.Kind ?? _settings.LastKind,
                startupOptions.Difficulty ?? _settings.LastDifficulty,
                startupOptions.Format ?? _settings.LastPromptFormat);
        }
        else
        {
            ShowStartPage();
        }
    }

    private void ShowStartPage()
    {
        var page = new StartPage(_settings.LastMode, _settings.LastKind, _settings.LastDifficulty, _settings.LastPromptFormat);
        page.PlayRequested += (_, selection) => StartSession(selection.Mode, selection.Kind, selection.Difficulty, selection.Format);
        NavigateTo(page, showBack: false, title: "🕐 Тренажёр определения времени");
    }

    private void StartSession(TrainerMode mode, ExerciseKind kind, Difficulty difficulty, PromptFormat format)
    {
        _settings.LastMode = mode;
        _settings.LastKind = kind;
        _settings.LastDifficulty = difficulty;
        _settings.LastPromptFormat = format;
        _settingsStore.Save(_settings);

        var session = new TrainerSession(new RandomExerciseGenerator(), mode, kind, difficulty, format);
        _playPage = new PlayPage(session);
        _playPage.ReturnToMenuRequested += (_, _) => { _playPage = null; ShowStartPage(); };
        _playPage.PlayAgainRequested += (_, selection) => StartSession(selection.Mode, selection.Kind, selection.Difficulty, selection.Format);
        NavigateTo(_playPage, showBack: true, title: "🕐 Тренажёр — игра");
    }

    private void NavigateTo(UserControl page, bool showBack, string title)
    {
        BackButton.Visibility = showBack ? Visibility.Visible : Visibility.Collapsed;
        TitleText.Text = title;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
        fadeOut.Completed += (_, _) =>
        {
            PageHost.Content = page;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            PageHost.BeginAnimation(OpacityProperty, fadeIn);
        };
        PageHost.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _playPage = null;
        ShowStartPage();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void HelpButton_Click(object sender, RoutedEventArgs e) => ShowHelp();

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_settings.Theme) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        _settings.Theme = dialog.SelectedTheme;
        _settingsStore.Save(_settings);
        ThemeManager.Apply(_settings.Theme);
    }

    private static void ShowHelp()
    {
        MessageBox.Show(
            "Управление:\n" +
            "  Enter — проверить ответ\n" +
            "  N — новое упражнение\n" +
            "  Ctrl+, — настройки\n" +
            "  F1 — эта справка\n" +
            "  Пробел — переключить АМ/ПМ (00–11 / 12–23) в упражнении \"Выставь стрелки\"\n" +
            "  Мышь — перетащите стрелку часов, чтобы выставить время\n\n" +
            CliParser.HelpText,
            "Справка — Тренажёр определения времени",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                _playPage?.SubmitAnswer();
                e.Handled = true;
                break;
            case Key.N:
                _playPage?.NewExercise();
                e.Handled = true;
                break;
            case Key.F1:
                ShowHelp();
                e.Handled = true;
                break;
            case Key.OemComma when Keyboard.Modifiers == ModifierKeys.Control:
                OpenSettings();
                e.Handled = true;
                break;
            case Key.Space:
                _playPage?.TogglePeriod();
                e.Handled = true;
                break;
        }
    }
}
