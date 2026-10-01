using ClockCore;

namespace ClockTrainer.WinForms;

public sealed class MainForm : Form
{
    private readonly ISettingsStore _settingsStore = new JsonSettingsStore();
    private readonly AppSettings _settings;
    private readonly TrainerSession _session;

    private ComboBox _modeCombo = null!;
    private ComboBox _kindCombo = null!;
    private ComboBox _difficultyCombo = null!;
    private Label _promptLabel = null!;
    private Label _statusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _exercisePanel = null!;

    private AnalogClockControl? _setTimeClock;
    private NumericUpDown? _hoursInput;
    private NumericUpDown? _minutesInput;

    public MainForm(CliOptions startupOptions)
    {
        _settings = _settingsStore.Load();

        var mode = startupOptions.Mode ?? _settings.LastMode;
        var kind = startupOptions.Kind ?? _settings.LastKind;
        var difficulty = startupOptions.Difficulty ?? _settings.LastDifficulty;

        _session = new TrainerSession(new RandomExerciseGenerator(), mode, kind, difficulty);
        _session.ExerciseGenerated += OnExerciseGenerated;
        _session.AnswerEvaluated += OnAnswerEvaluated;
        _session.StatisticsChanged += (_, _) => UpdateStatsLabel();

        BuildLayout();
        SyncCombosFromSession();

        _modeCombo.SelectedIndexChanged += (_, _) => StartNewExercise();
        _kindCombo.SelectedIndexChanged += (_, _) => StartNewExercise();
        _difficultyCombo.SelectedIndexChanged += (_, _) => StartNewExercise();

        ApplyTheme(_settings.Theme);
        UpdateStatsLabel();
        StartNewExercise();
    }

    private void BuildLayout()
    {
        Text = "Тренажёр определения времени";
        MinimumSize = new Size(640, 480);
        Size = new Size(760, 560);
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight
        };

        _modeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        _kindCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        _difficultyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        var newExerciseButton = new Button { Text = "Новое (N)", AutoSize = true };
        var checkButton = new Button { Text = "Проверить (Enter)", AutoSize = true };
        var settingsButton = new Button { Text = "Настройки", AutoSize = true };
        var helpButton = new Button { Text = "Справка (F1)", AutoSize = true };

        _modeCombo.Items.AddRange(
        [
            new ComboItem<TrainerMode>("Обучение", TrainerMode.Learn),
            new ComboItem<TrainerMode>("Проверка знаний", TrainerMode.Test)
        ]);
        _kindCombo.Items.AddRange(
        [
            new ComboItem<ExerciseKind>("Выставь стрелки", ExerciseKind.SetTime),
            new ComboItem<ExerciseKind>("Сколько времени прошло", ExerciseKind.ElapsedTime)
        ]);
        _difficultyCombo.Items.AddRange(
        [
            new ComboItem<Difficulty>("Лёгкая", Difficulty.Easy),
            new ComboItem<Difficulty>("Средняя", Difficulty.Medium),
            new ComboItem<Difficulty>("Сложная", Difficulty.Hard)
        ]);

        newExerciseButton.Click += (_, _) => StartNewExercise();
        checkButton.Click += (_, _) => SubmitAnswer();
        settingsButton.Click += (_, _) => OpenSettings();
        helpButton.Click += (_, _) => ShowHelp();

        topPanel.Controls.Add(LabeledControl("Режим:", _modeCombo));
        topPanel.Controls.Add(LabeledControl("Упражнение:", _kindCombo));
        topPanel.Controls.Add(LabeledControl("Сложность:", _difficultyCombo));
        topPanel.Controls.Add(newExerciseButton);
        topPanel.Controls.Add(checkButton);
        topPanel.Controls.Add(settingsButton);
        topPanel.Controls.Add(helpButton);

        _promptLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold)
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
        };

        _exercisePanel = new Panel { Dock = DockStyle.Fill };

        _statsLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(_exercisePanel);
        Controls.Add(_statusLabel);
        Controls.Add(_promptLabel);
        Controls.Add(_statsLabel);
        Controls.Add(topPanel);
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                SubmitAnswer();
                e.Handled = true;
                break;
            case Keys.N:
                StartNewExercise();
                e.Handled = true;
                break;
            case Keys.F1:
                ShowHelp();
                e.Handled = true;
                break;
            case Keys.Oemcomma when e.Control:
                OpenSettings();
                e.Handled = true;
                break;
        }
    }

    private void StartNewExercise()
    {
        _session.Mode = ((ComboItem<TrainerMode>)_modeCombo.SelectedItem!).Value;
        _session.Kind = ((ComboItem<ExerciseKind>)_kindCombo.SelectedItem!).Value;
        _session.Difficulty = ((ComboItem<Difficulty>)_difficultyCombo.SelectedItem!).Value;

        _settings.LastMode = _session.Mode;
        _settings.LastKind = _session.Kind;
        _settings.LastDifficulty = _session.Difficulty;
        _settingsStore.Save(_settings);

        UpdateStatsLabel();
        _session.NewExercise();
    }

    private void SubmitAnswer()
    {
        switch (_session.Current)
        {
            case SetTimeExercise when _setTimeClock is not null:
                _session.Submit(_setTimeClock.Time);
                break;
            case ElapsedTimeExercise when _hoursInput is not null && _minutesInput is not null:
                _session.Submit(new TimeSpan((int)_hoursInput.Value, (int)_minutesInput.Value, 0));
                break;
        }
    }

    private void OnExerciseGenerated(object? sender, ClockExercise exercise)
    {
        _statusLabel.Text = string.Empty;
        _promptLabel.Text = exercise.Prompt;

        foreach (Control control in _exercisePanel.Controls)
            control.Dispose();
        _exercisePanel.Controls.Clear();

        _setTimeClock = null;
        _hoursInput = null;
        _minutesInput = null;

        Control view = exercise switch
        {
            SetTimeExercise setTime => BuildSetTimeView(setTime),
            ElapsedTimeExercise elapsed => BuildElapsedTimeView(elapsed),
            _ => throw new InvalidOperationException("Неизвестный тип упражнения.")
        };
        _exercisePanel.Controls.Add(view);
    }

    private void OnAnswerEvaluated(object? sender, bool correct)
    {
        var exercise = _session.Current!;

        if (_session.Mode == TrainerMode.Learn)
        {
            _statusLabel.Text = $"Правильный ответ: {exercise.CorrectAnswerText}";
            _statusLabel.ForeColor = Color.DarkBlue;
        }
        else
        {
            _statusLabel.Text = correct ? "Верно!" : $"Неверно. Правильный ответ: {exercise.CorrectAnswerText}";
            _statusLabel.ForeColor = correct ? Color.Green : Color.Firebrick;
        }
    }

    private Control BuildSetTimeView(SetTimeExercise exercise)
    {
        _setTimeClock = new AnalogClockControl
        {
            Interactive = true,
            SnapMinutes = exercise.Difficulty.StepMinutes(),
            Time = new TimeOnly(12, 0),
            Size = new Size(260, 260)
        };

        var container = new Panel { Dock = DockStyle.Fill };
        container.Controls.Add(_setTimeClock);

        void Reposition()
        {
            _setTimeClock.Left = (container.ClientSize.Width - _setTimeClock.Width) / 2;
            _setTimeClock.Top = (container.ClientSize.Height - _setTimeClock.Height) / 2;
        }

        container.Resize += (_, _) => Reposition();
        Reposition();

        return container;
    }

    private Control BuildElapsedTimeView(ElapsedTimeExercise exercise)
    {
        var beforeClock = new AnalogClockControl { Interactive = false, Time = exercise.Before, Size = new Size(180, 180) };
        var afterClock = new AnalogClockControl { Interactive = false, Time = exercise.After, Size = new Size(180, 180) };

        var clocksLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        clocksLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        clocksLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        clocksLayout.Controls.Add(BuildLabeledClock("До", beforeClock), 0, 0);
        clocksLayout.Controls.Add(BuildLabeledClock("После", afterClock), 1, 0);

        _hoursInput = new NumericUpDown { Minimum = 0, Maximum = 23, Width = 60 };
        _minutesInput = new NumericUpDown { Minimum = 0, Maximum = 59, Width = 60 };

        var answerPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8) };
        answerPanel.Controls.Add(new Label { Text = "Прошло: часов", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        answerPanel.Controls.Add(_hoursInput);
        answerPanel.Controls.Add(new Label { Text = "минут", AutoSize = true, Margin = new Padding(8, 8, 4, 0) });
        answerPanel.Controls.Add(_minutesInput);

        var container = new Panel { Dock = DockStyle.Fill };
        container.Controls.Add(clocksLayout);
        container.Controls.Add(answerPanel);

        return container;
    }

    private static Control BuildLabeledClock(string title, AnalogClockControl clock)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(panel.Font, FontStyle.Bold)
        };

        panel.Controls.Add(clock);
        panel.Controls.Add(label);

        void Reposition()
        {
            clock.Left = (panel.ClientSize.Width - clock.Width) / 2;
            clock.Top = label.Height + (panel.ClientSize.Height - label.Height - clock.Height) / 2;
        }

        panel.Resize += (_, _) => Reposition();
        Reposition();

        return panel;
    }

    private static Control LabeledControl(string text, Control control)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown };
        panel.Controls.Add(new Label { Text = text, AutoSize = true });
        panel.Controls.Add(control);
        return panel;
    }

    private void SyncCombosFromSession()
    {
        foreach (ComboItem<TrainerMode> item in _modeCombo.Items)
            if (item.Value == _session.Mode) { _modeCombo.SelectedItem = item; break; }

        foreach (ComboItem<ExerciseKind> item in _kindCombo.Items)
            if (item.Value == _session.Kind) { _kindCombo.SelectedItem = item; break; }

        foreach (ComboItem<Difficulty> item in _difficultyCombo.Items)
            if (item.Value == _session.Difficulty) { _difficultyCombo.SelectedItem = item; break; }
    }

    private void UpdateStatsLabel()
    {
        _statsLabel.Text = _session.Mode == TrainerMode.Test
            ? $"Верно: {_session.Stats.Correct}   Неверно: {_session.Stats.Incorrect}   Точность: {_session.Stats.Accuracy:F0}%"
            : "Режим обучения — статистика не ведётся";
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settings.Theme);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _settings.Theme = dialog.SelectedTheme;
        _settingsStore.Save(_settings);
        ApplyTheme(_settings.Theme);
    }

    private static void ShowHelp()
    {
        MessageBox.Show(
            "Управление:\n" +
            "  Enter — проверить ответ\n" +
            "  N — новое упражнение\n" +
            "  Ctrl+, — настройки\n" +
            "  F1 — эта справка\n" +
            "  Мышь — перетащите стрелку часов, чтобы выставить время\n\n" +
            CliParser.HelpText,
            "Справка — Тренажёр определения времени",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ApplyTheme(string theme)
    {
        var dark = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
        BackColor = dark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
        ForeColor = dark ? Color.White : SystemColors.ControlText;

        foreach (Control control in Controls)
            ApplyThemeRecursive(control, dark);
    }

    private static void ApplyThemeRecursive(Control control, bool dark)
    {
        control.BackColor = dark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
        control.ForeColor = dark ? Color.White : SystemColors.ControlText;

        foreach (Control child in control.Controls)
            ApplyThemeRecursive(child, dark);
    }

    private readonly record struct ComboItem<T>(string Text, T Value)
    {
        public override string ToString() => Text;
    }
}
