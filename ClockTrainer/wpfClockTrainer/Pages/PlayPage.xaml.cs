using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ClockCore;
using ClockTrainer.Wpf.Controls;

namespace ClockTrainer.Wpf.Pages;

public partial class PlayPage : UserControl
{
    private readonly TrainerSession _session;

    public event EventHandler? ReturnToMenuRequested;
    public event EventHandler<(TrainerMode Mode, ExerciseKind Kind, Difficulty Difficulty, PromptFormat Format)>? PlayAgainRequested;

    private AnalogClockControl? _setTimeClock;
    private RadioButton? _amRadio;
    private RadioButton? _pmRadio;
    private bool _suppressPeriodEvent;

    private int _elapsedHours;
    private int _elapsedMinutes;
    private TextBlock? _hoursValueText;
    private TextBlock? _minutesValueText;

    public PlayPage(TrainerSession session)
    {
        InitializeComponent();

        _session = session;
        _session.ExerciseGenerated += OnExerciseGenerated;
        _session.AnswerEvaluated += OnAnswerEvaluated;
        _session.StatisticsChanged += (_, _) => UpdateStats();
        _session.GameEnded += (_, result) => ShowGameOver(result == GameResult.Won);

        UpdateStats();
        _session.NewExercise();
    }

    public void SubmitAnswer()
    {
        if (_session.Mode == TrainerMode.Learn || _session.IsGameOver) return;

        switch (_session.Current)
        {
            case SetTimeExercise when _setTimeClock is not null:
                _session.Submit(_setTimeClock.Time);
                break;
            case ElapsedTimeExercise:
                _session.Submit(new TimeSpan(_elapsedHours, _elapsedMinutes, 0));
                break;
        }
    }

    public void NewExercise()
    {
        if (_session.IsGameOver) return;
        _session.NewExercise();
    }

    public void TogglePeriod()
    {
        if (_setTimeClock is null) return;
        var time = _setTimeClock.Time;
        _setTimeClock.Time = new TimeOnly((time.Hour + 12) % 24, time.Minute);
    }

    private void OnExerciseGenerated(object? sender, ClockExercise exercise)
    {
        StatusText.Text = string.Empty;
        PromptText.Text = BuildPromptText(exercise);
        CheckButton.Visibility = _session.Mode == TrainerMode.Learn ? Visibility.Collapsed : Visibility.Visible;
        UpdateHud();

        _setTimeClock = null;
        _amRadio = null;
        _pmRadio = null;
        _hoursValueText = null;
        _minutesValueText = null;

        ExerciseHost.Content = exercise switch
        {
            SetTimeExercise setTime => BuildSetTimeView(setTime),
            ElapsedTimeExercise elapsed => BuildElapsedTimeView(elapsed),
            _ => throw new InvalidOperationException("Неизвестный тип упражнения.")
        };
    }

    private string BuildPromptText(ClockExercise exercise)
    {
        if (_session.Mode != TrainerMode.Learn)
            return $"🕐 {exercise.Prompt}";

        return exercise switch
        {
            SetTimeExercise st => $"🕐 Вот время {st.Target:HH:mm}: «{RussianTimeWords.ToWords(st.Target)}»",
            ElapsedTimeExercise et => $"🕐 До: {et.Before:HH:mm}   После: {et.After:HH:mm}   Прошло: {et.CorrectAnswerText}",
            _ => exercise.Prompt
        };
    }

    private void OnAnswerEvaluated(object? sender, bool correct)
    {
        var exercise = _session.Current!;
        UpdateHud();

        if (_session.Mode == TrainerMode.Learn)
        {
            StatusText.Text = $"💡 Правильный ответ: {exercise.CorrectAnswerText}";
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
        }
        else
        {
            StatusText.Text = correct ? "✅ Верно!" : $"❌ Неверно. Правильный ответ: {exercise.CorrectAnswerText}";
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, correct ? "SuccessBrush" : "DangerBrush");
        }

        var pop = new DoubleAnimation(0.85, 1.0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.7 }
        };
        StatusScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, pop);
        StatusScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, pop);
    }

    private FrameworkElement BuildSetTimeView(SetTimeExercise exercise)
    {
        var isLearn = _session.Mode == TrainerMode.Learn;

        _setTimeClock = new AnalogClockControl
        {
            Interactive = !isLearn,
            SnapMinutes = exercise.Difficulty.StepMinutes(),
            Time = isLearn ? exercise.Target : new TimeOnly(12, 0),
            Width = 280,
            Height = 280
        };

        if (isLearn)
        {
            var learnLayout = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            learnLayout.Children.Add(_setTimeClock);
            return learnLayout;
        }

        _setTimeClock.TimeChanged += (_, _) => SyncPeriodRadios();

        _amRadio = new RadioButton { GroupName = "Period", Style = (Style)FindResource("ChipToggleButton"), Content = "AM" };
        _pmRadio = new RadioButton { GroupName = "Period", Style = (Style)FindResource("ChipToggleButton"), Content = "PM" };
        _amRadio.Checked += (_, _) => SetPeriod(isPm: false);
        _pmRadio.Checked += (_, _) => SetPeriod(isPm: true);
        SyncPeriodRadios();

        var periodRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        periodRow.Children.Add(_amRadio);
        periodRow.Children.Add(_pmRadio);

        var hint = new TextBlock
        {
            Text = "подсказка: пробел тоже переключает АМ/ПМ",
            FontSize = 11, FontStyle = FontStyles.Italic,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");

        var layout = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        layout.Children.Add(_setTimeClock);
        layout.Children.Add(periodRow);
        layout.Children.Add(hint);
        return layout;
    }

    private void SetPeriod(bool isPm)
    {
        if (_setTimeClock is null || _suppressPeriodEvent) return;
        var time = _setTimeClock.Time;
        _setTimeClock.Time = new TimeOnly(time.Hour % 12 + (isPm ? 12 : 0), time.Minute);
    }

    private void SyncPeriodRadios()
    {
        if (_setTimeClock is null || _amRadio is null || _pmRadio is null) return;

        _suppressPeriodEvent = true;
        var isPm = _setTimeClock.Time.Hour >= 12;
        _amRadio.IsChecked = !isPm;
        _pmRadio.IsChecked = isPm;
        _suppressPeriodEvent = false;
    }

    private FrameworkElement BuildElapsedTimeView(ElapsedTimeExercise exercise)
    {
        var clocksRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        clocksRow.Children.Add(BuildLabeledClock("До", exercise.Before));
        clocksRow.Children.Add(new Border { Width = 24 });
        clocksRow.Children.Add(BuildLabeledClock("После", exercise.After));

        if (_session.Mode == TrainerMode.Learn)
        {
            var learnLayout = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            learnLayout.Children.Add(clocksRow);
            return learnLayout;
        }

        _elapsedHours = 0;
        _elapsedMinutes = 0;

        _hoursValueText = new TextBlock { Text = "0", FontSize = 20, FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold"), Width = 36, TextAlignment = TextAlignment.Center };
        _hoursValueText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        _minutesValueText = new TextBlock { Text = "0", FontSize = 20, FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold"), Width = 36, TextAlignment = TextAlignment.Center };
        _minutesValueText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        var answerRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 24, 0, 0) };
        answerRow.Children.Add(LabeledStack("часов", BuildStepper(_hoursValueText, delta => _elapsedHours = Wrap(_elapsedHours + delta, 24))));
        answerRow.Children.Add(new Border { Width = 24 });
        answerRow.Children.Add(LabeledStack("минут", BuildStepper(_minutesValueText, delta => _elapsedMinutes = Wrap(_elapsedMinutes + delta, 60))));

        var layout = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        layout.Children.Add(clocksRow);
        layout.Children.Add(answerRow);
        return layout;
    }

    private static int Wrap(int value, int modulo) => ((value % modulo) + modulo) % modulo;

    private FrameworkElement BuildStepper(TextBlock valueText, Action<int> apply)
    {
        var minus = new Button { Content = "−", Style = (Style)FindResource("GhostButton"), Width = 34, Padding = new Thickness(0) };
        var plus = new Button { Content = "+", Style = (Style)FindResource("GhostButton"), Width = 34, Padding = new Thickness(0) };

        minus.Click += (_, _) =>
        {
            apply(-1);
            valueText.Text = CurrentValueOf(valueText);
        };
        plus.Click += (_, _) =>
        {
            apply(1);
            valueText.Text = CurrentValueOf(valueText);
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(minus);
        row.Children.Add(valueText);
        row.Children.Add(plus);
        return row;
    }

    private string CurrentValueOf(TextBlock valueText) =>
        valueText == _hoursValueText ? _elapsedHours.ToString("00") : _elapsedMinutes.ToString("00");

    private static FrameworkElement LabeledStack(string label, FrameworkElement content)
    {
        var caption = new TextBlock { Text = label, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) };
        caption.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");

        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(caption);
        stack.Children.Add(content);
        return stack;
    }

    private static FrameworkElement BuildLabeledClock(string title, TimeOnly time)
    {
        var caption = new TextBlock { Text = title, FontSize = 13, FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
        caption.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");

        var clock = new AnalogClockControl { Interactive = false, Time = time, Width = 180, Height = 180 };

        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(caption);
        stack.Children.Add(clock);
        return stack;
    }

    private void UpdateStats()
    {
        StatsText.Text = _session.Mode == TrainerMode.Test
            ? $"📊 Верно: {_session.Stats.Correct}   Неверно: {_session.Stats.Incorrect}   Точность: {_session.Stats.Accuracy:F0}%"
            : "🎓 Режим обучения — статистика не ведётся";
    }

    private void UpdateHud()
    {
        if (_session.Mode != TrainerMode.Test)
        {
            HudPanel.Visibility = Visibility.Collapsed;
            return;
        }

        HudPanel.Visibility = Visibility.Visible;
        RoundCounterText.Text = $"Раунд {Math.Min(_session.RoundsPlayed + 1, _session.MaxRounds)}/{_session.MaxRounds}";

        var hearts = new[] { Heart1, Heart2, Heart3 };
        for (var i = 0; i < hearts.Length; i++)
            hearts[i].SetResourceReference(TextBlock.ForegroundProperty, i < _session.Lives ? "DangerBrush" : "MutedBrush");
    }

    private void ShowGameOver(bool won)
    {
        PromptText.Text = string.Empty;
        ActionButtonsPanel.Visibility = Visibility.Collapsed;
        ExerciseHost.Content = BuildGameOverView(won);
    }

    private FrameworkElement BuildGameOverView(bool won)
    {
        var heading = new TextBlock
        {
            Text = won ? "🎉 Все раунды пройдены!" : "💔 Три ошибки — игра окончена",
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold"),
            FontSize = 24,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        heading.SetResourceReference(TextBlock.ForegroundProperty, won ? "SuccessBrush" : "DangerBrush");

        var statsLine = new TextBlock
        {
            Text = $"Верно: {_session.Stats.Correct}   Неверно: {_session.Stats.Incorrect}   Точность: {_session.Stats.Accuracy:F0}%",
            FontSize = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 24)
        };
        statsLine.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");

        var againButton = new Button { Content = "🔄 Играть снова", Style = (Style)FindResource("PillButton") };
        var menuButton = new Button { Content = "⬅ В меню", Style = (Style)FindResource("GhostButton"), Margin = new Thickness(10, 0, 0, 0) };
        againButton.Click += (_, _) => PlayAgainRequested?.Invoke(this, (_session.Mode, _session.Kind, _session.Difficulty, _session.Format));
        menuButton.Click += (_, _) => ReturnToMenuRequested?.Invoke(this, EventArgs.Empty);

        var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        buttonsRow.Children.Add(againButton);
        buttonsRow.Children.Add(menuButton);

        var layout = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        layout.Children.Add(heading);
        layout.Children.Add(statsLine);
        layout.Children.Add(buttonsRow);
        return layout;
    }

    private void NewExerciseButton_Click(object sender, RoutedEventArgs e) => NewExercise();

    private void CheckButton_Click(object sender, RoutedEventArgs e) => SubmitAnswer();
}
