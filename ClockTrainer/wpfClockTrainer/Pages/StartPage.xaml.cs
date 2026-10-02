using System.Windows;
using System.Windows.Controls;
using ClockCore;

namespace ClockTrainer.Wpf.Pages;

public partial class StartPage : UserControl
{
    public event EventHandler<(TrainerMode Mode, ExerciseKind Kind, Difficulty Difficulty, PromptFormat Format)>? PlayRequested;

    public StartPage(TrainerMode mode, ExerciseKind kind, Difficulty difficulty, PromptFormat format)
    {
        InitializeComponent();

        ModeLearnRadio.IsChecked = mode == TrainerMode.Learn;
        ModeTestRadio.IsChecked = mode == TrainerMode.Test;

        KindSetRadio.IsChecked = kind == ExerciseKind.SetTime;
        KindElapsedRadio.IsChecked = kind == ExerciseKind.ElapsedTime;

        DifficultyEasyRadio.IsChecked = difficulty == Difficulty.Easy;
        DifficultyMediumRadio.IsChecked = difficulty == Difficulty.Medium;
        DifficultyHardRadio.IsChecked = difficulty == Difficulty.Hard;

        FormatDigitsRadio.IsChecked = format == PromptFormat.Digits;
        FormatWordsRadio.IsChecked = format == PromptFormat.Words;
        FormatMixedRadio.IsChecked = format == PromptFormat.Mixed;

        UpdateFormatVisibility();
    }

    private void KindRadio_Checked(object sender, RoutedEventArgs e) => UpdateFormatVisibility();

    private void UpdateFormatVisibility()
    {
        FormatPanel.Visibility = KindSetRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        var mode = ModeTestRadio.IsChecked == true ? TrainerMode.Test : TrainerMode.Learn;
        var kind = KindElapsedRadio.IsChecked == true ? ExerciseKind.ElapsedTime : ExerciseKind.SetTime;
        var difficulty = DifficultyHardRadio.IsChecked == true
            ? Difficulty.Hard
            : DifficultyMediumRadio.IsChecked == true
                ? Difficulty.Medium
                : Difficulty.Easy;
        var format = FormatWordsRadio.IsChecked == true
            ? PromptFormat.Words
            : FormatMixedRadio.IsChecked == true
                ? PromptFormat.Mixed
                : PromptFormat.Digits;

        PlayRequested?.Invoke(this, (mode, kind, difficulty, format));
    }
}
