using ClockCore;

namespace ClockTrainer.WinForms;

public sealed class MainForm : Form
{
    private readonly ISettingsStore _settingsStore = new JsonSettingsStore();
    private readonly AppSettings _settings;
    private readonly TrainerSession _session;

    private FlowLayoutPanel _topPanel = null!;
    private ComboBox _modeCombo = null!;
    private ComboBox _kindCombo = null!;
    private ComboBox _difficultyCombo = null!;
    private RoundedButton _newExerciseButton = null!;
    private RoundedButton _checkButton = null!;
    private RoundedButton _settingsButton = null!;
    private RoundedButton _helpButton = null!;
    private Label _promptLabel = null!;
    private Label _statusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _exercisePanel = null!;

    private AnalogClockControl? _setTimeClock;
    private NumericUpDown? _hoursInput;
    private NumericUpDown? _minutesInput;
    private RoundedButton? _amButton;
    private RoundedButton? _pmButton;

    public MainForm(CliOptions startupOptions)
    {
        _settings = _settingsStore.Load();

        var mode = startupOptions.Mode ?? _settings.LastMode;
        var kind = startupOptions.Kind ?? _settings.LastKind;
        var difficulty = startupOptions.Difficulty ?? _settings.LastDifficulty;
        var format = startupOptions.Format ?? _settings.LastPromptFormat;

        _session = new TrainerSession(new RandomExerciseGenerator(), mode, kind, difficulty, format);
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
        Text = "🕐 Тренажёр определения времени";
        MinimumSize = new Size(680, 520);
        Size = new Size(800, 600);
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;
        Font = new Font("Segoe UI", 9.5F);

        _topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12, 10, 12, 10),
            FlowDirection = FlowDirection.LeftToRight
        };

        _modeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, FlatStyle = FlatStyle.Flat };
        _kindCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, FlatStyle = FlatStyle.Flat };
        _difficultyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, FlatStyle = FlatStyle.Flat };

        _newExerciseButton = new RoundedButton { Text = "🔄 Новое (N)" };
        _checkButton = new RoundedButton { Text = "✅ Проверить (Enter)" };
        _settingsButton = new RoundedButton { Text = "⚙️ Настройки" };
        _helpButton = new RoundedButton { Text = "❓ Справка (F1)" };

        _modeCombo.Items.AddRange(
        [
            new ComboItem<TrainerMode>("🎓 Обучение", TrainerMode.Learn),
            new ComboItem<TrainerMode>("🏆 Проверка знаний", TrainerMode.Test)
        ]);
        _kindCombo.Items.AddRange(
        [
            new ComboItem<ExerciseKind>("👉 Выставь стрелки", ExerciseKind.SetTime),
            new ComboItem<ExerciseKind>("⏳ Сколько времени прошло", ExerciseKind.ElapsedTime)
        ]);
        _difficultyCombo.Items.AddRange(
        [
            new ComboItem<Difficulty>("🙂 Лёгкая", Difficulty.Easy),
            new ComboItem<Difficulty>("😐 Средняя", Difficulty.Medium),
            new ComboItem<Difficulty>("🔥 Сложная", Difficulty.Hard)
        ]);

        _newExerciseButton.Click += (_, _) => StartNewExercise();
        _checkButton.Click += (_, _) => SubmitAnswer();
        _settingsButton.Click += (_, _) => OpenSettings();
        _helpButton.Click += (_, _) => ShowHelp();

        _topPanel.Controls.Add(LabeledControl("Режим:", _modeCombo));
        _topPanel.Controls.Add(LabeledControl("Упражнение:", _kindCombo));
        _topPanel.Controls.Add(LabeledControl("Сложность:", _difficultyCombo));
        _topPanel.Controls.Add(PadTop(_newExerciseButton));
        _topPanel.Controls.Add(PadTop(_checkButton));
        _topPanel.Controls.Add(PadTop(_settingsButton));
        _topPanel.Controls.Add(PadTop(_helpButton));

        _promptLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 13, FontStyle.Bold)
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        _exercisePanel = new Panel { Dock = DockStyle.Fill };

        _statsLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };

        Controls.Add(_exercisePanel);
        Controls.Add(_statusLabel);
        Controls.Add(_promptLabel);
        Controls.Add(_statsLabel);
        Controls.Add(_topPanel);
    }

    private static Control PadTop(Control control)
    {
        control.Margin = new Padding(6, 18, 0, 0);
        return control;
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
            case Keys.Space when _session.Current is SetTimeExercise:
                TogglePeriod();
                e.Handled = true;
                break;
        }
    }

    private void TogglePeriod()
    {
        if (_setTimeClock is null) return;
        var time = _setTimeClock.Time;
        _setTimeClock.Time = new TimeOnly((time.Hour + 12) % 24, time.Minute);
    }

    private void SetPeriod(bool isPm)
    {
        if (_setTimeClock is null) return;
        var time = _setTimeClock.Time;
        _setTimeClock.Time = new TimeOnly(time.Hour % 12 + (isPm ? 12 : 0), time.Minute);
    }

    private void UpdatePeriodButtons()
    {
        if (_setTimeClock is null || _amButton is null || _pmButton is null) return;

        var palette = Theme.For(_settings.Theme);
        var isPm = _setTimeClock.Time.Hour >= 12;

        StylePeriodButton(_amButton, selected: !isPm, palette);
        StylePeriodButton(_pmButton, selected: isPm, palette);
    }

    private static void StylePeriodButton(RoundedButton button, bool selected, Palette palette)
    {
        button.BackColor = selected ? palette.Primary : palette.Surface;
        button.ForeColor = selected ? Color.White : palette.Muted;
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
        _promptLabel.Text = $"🕐 {exercise.Prompt}";

        foreach (Control control in _exercisePanel.Controls)
            control.Dispose();
        _exercisePanel.Controls.Clear();

        _setTimeClock = null;
        _hoursInput = null;
        _minutesInput = null;
        _amButton = null;
        _pmButton = null;

        Control view = exercise switch
        {
            SetTimeExercise setTime => BuildSetTimeView(setTime),
            ElapsedTimeExercise elapsed => BuildElapsedTimeView(elapsed),
            _ => throw new InvalidOperationException("Неизвестный тип упражнения.")
        };
        _exercisePanel.Controls.Add(view);
        ApplyExercisePalette();
    }

    private void OnAnswerEvaluated(object? sender, bool correct)
    {
        var exercise = _session.Current!;
        var palette = Theme.For(_settings.Theme);

        if (_session.Mode == TrainerMode.Learn)
        {
            _statusLabel.Text = $"💡 Правильный ответ: {exercise.CorrectAnswerText}";
            _statusLabel.ForeColor = palette.Primary;
        }
        else
        {
            _statusLabel.Text = correct
                ? "✅ Верно!"
                : $"❌ Неверно. Правильный ответ: {exercise.CorrectAnswerText}";
            _statusLabel.ForeColor = correct ? palette.Success : palette.Danger;
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

        _amButton = new RoundedButton { Text = "AM", Width = 80 };
        _pmButton = new RoundedButton { Text = "PM", Width = 80 };

        _amButton.Click += (_, _) => SetPeriod(isPm: false);
        _pmButton.Click += (_, _) => SetPeriod(isPm: true);
        _setTimeClock.TimeChanged += (_, _) => UpdatePeriodButtons();
        UpdatePeriodButtons();

        var switchPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        switchPanel.Controls.Add(_amButton, 0, 0);
        switchPanel.Controls.Add(_pmButton, 1, 0);

        var hintLabel = new Label
        {
            Text = "подсказка: пробел тоже переключает АМ/ПМ",
            AutoSize = true,
            Font = new Font("Segoe UI", 8, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8)
        };
        bottomPanel.Controls.Add(switchPanel);
        bottomPanel.Controls.Add(hintLabel);

        void CenterBottomPanel()
        {
            switchPanel.Left = (bottomPanel.ClientSize.Width - switchPanel.Width) / 2;
            hintLabel.Left = (bottomPanel.ClientSize.Width - hintLabel.Width) / 2;
        }

        bottomPanel.Resize += (_, _) => CenterBottomPanel();

        var clockArea = new Panel { Dock = DockStyle.Fill };
        clockArea.Controls.Add(_setTimeClock);

        var container = new Panel { Dock = DockStyle.Fill };
        container.Controls.Add(clockArea);
        container.Controls.Add(bottomPanel);

        void Reposition()
        {
            _setTimeClock.Left = (clockArea.ClientSize.Width - _setTimeClock.Width) / 2;
            _setTimeClock.Top = (clockArea.ClientSize.Height - _setTimeClock.Height) / 2;
        }

        clockArea.Resize += (_, _) => Reposition();
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

        _hoursInput = new NumericUpDown { Minimum = 0, Maximum = 23, Width = 60, Font = new Font("Segoe UI", 10) };
        _minutesInput = new NumericUpDown { Minimum = 0, Maximum = 59, Width = 60, Font = new Font("Segoe UI", 10) };

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
            Height = 26,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
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
        panel.Controls.Add(new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", 8) });
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
            ? $"📊 Верно: {_session.Stats.Correct}   Неверно: {_session.Stats.Incorrect}   Точность: {_session.Stats.Accuracy:F0}%"
            : "🎓 Режим обучения — статистика не ведётся";
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
            "  Пробел — переключить АМ/ПМ (00–11 / 12–23) в упражнении \"Выставь стрелки\"\n" +
            "  Мышь — перетащите стрелку часов, чтобы выставить время\n\n" +
            CliParser.HelpText,
            "Справка — Тренажёр определения времени",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ApplyTheme(string theme)
    {
        var palette = Theme.For(theme);

        BackColor = palette.Background;
        ForeColor = palette.Text;

        _topPanel.BackColor = palette.Surface;
        StyleLabelTree(_topPanel, palette);

        _modeCombo.BackColor = palette.Surface;
        _modeCombo.ForeColor = palette.Text;
        _kindCombo.BackColor = palette.Surface;
        _kindCombo.ForeColor = palette.Text;
        _difficultyCombo.BackColor = palette.Surface;
        _difficultyCombo.ForeColor = palette.Text;

        _newExerciseButton.BackColor = palette.Secondary;
        _newExerciseButton.ForeColor = Color.White;
        _checkButton.BackColor = palette.Primary;
        _checkButton.ForeColor = Color.White;
        _settingsButton.BackColor = palette.Surface;
        _settingsButton.ForeColor = palette.Primary;
        _helpButton.BackColor = palette.Surface;
        _helpButton.ForeColor = palette.Primary;

        _promptLabel.ForeColor = palette.Primary;
        _statsLabel.ForeColor = palette.Muted;
        _exercisePanel.BackColor = palette.Background;

        ApplyExercisePalette();
    }

    private static void StyleLabelTree(Control root, Palette palette)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Label label)
                label.ForeColor = palette.Text;

            if (child.Controls.Count > 0)
                StyleLabelTree(child, palette);
        }
    }

    private void ApplyExercisePalette()
    {
        var palette = Theme.For(_settings.Theme);
        ApplyExercisePaletteRecursive(_exercisePanel, palette);
        UpdatePeriodButtons();
    }

    private void ApplyExercisePaletteRecursive(Control root, Palette palette)
    {
        foreach (Control child in root.Controls)
        {
            switch (child)
            {
                case AnalogClockControl clock:
                    clock.ApplyPalette(palette);
                    break;
                case RoundedButton button when button != _amButton && button != _pmButton:
                    break;
                case Label label:
                    label.ForeColor = palette.Muted;
                    break;
                case NumericUpDown numeric:
                    numeric.BackColor = palette.Surface;
                    numeric.ForeColor = palette.Text;
                    break;
                case Panel or FlowLayoutPanel or TableLayoutPanel:
                    child.BackColor = palette.Background;
                    break;
            }

            if (child.Controls.Count > 0)
                ApplyExercisePaletteRecursive(child, palette);
        }
    }

    private readonly record struct ComboItem<T>(string Text, T Value)
    {
        public override string ToString() => Text;
    }
}
