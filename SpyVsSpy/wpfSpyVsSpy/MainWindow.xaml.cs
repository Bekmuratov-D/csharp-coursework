using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SpyCore;
using SpyVsSpy.Wpf.Controls;

namespace SpyVsSpy.Wpf;

public partial class MainWindow : Window
{
    private readonly int _trophyGoal;
    private readonly SpyPlayer _human = new("Вы", isBot: false);
    private readonly SpyPlayer _bot = new("Бот", isBot: true);
    private readonly SimpleBotStrategy _botStrategy = new();

    private GameSession _session = null!;
    private SuspectCardControl[,] _cards = null!;
    private readonly SuspectCardControl _selfCard = new();
    private GridPosition? _selected;
    private bool _busy;

    public MainWindow(int trophyGoal)
    {
        InitializeComponent();
        _trophyGoal = trophyGoal;
        SelfCardHost.Content = _selfCard;

        StartNewGame();
    }

    private void StartNewGame()
    {
        var suspects = SuspectNames.All.Select(n => new Suspect(n)).ToList();
        var grid = new SuspectGrid(5, suspects);

        _session = new GameSession(grid, _human, _bot, _trophyGoal);
        _session.TurnLogged += (_, message) => AppendLog(message);
        _session.GameEnded += (_, winner) => ShowGameOver(winner);
        _session.SuspectCaught += (_, info) => AddTrophyChip(info.Catcher, info.Caught);

        _selected = null;
        _busy = false;
        LogPanel.Children.Clear();
        HumanTrophyPanel.Items.Clear();
        BotTrophyPanel.Items.Clear();
        GameOverOverlay.Visibility = Visibility.Collapsed;

        BuildBoard();
        RefreshAll();
        _ = AdvanceIfBotTurnAsync();
    }

    private void BuildBoard()
    {
        BoardHost.Children.Clear();
        BoardHost.RowDefinitions.Clear();
        BoardHost.ColumnDefinitions.Clear();

        var size = _session.Grid.Size;
        const int rowOffset = 2; // 0 = номера столбцов, 1 = стрелки сверху, 2.. = карточки
        const int colOffset = 2; // 0 = номера рядов, 1 = стрелки слева, 2.. = карточки

        BoardHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        BoardHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var i = 0; i < size; i++)
            BoardHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(112) });
        BoardHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        BoardHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        BoardHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (var i = 0; i < size; i++)
            BoardHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) });
        BoardHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _cards = new SuspectCardControl[size, size];

        for (var row = 0; row < size; row++)
        {
            var rowLabel = CreateIndexLabel(row + 1);
            Grid.SetRow(rowLabel, row + rowOffset);
            Grid.SetColumn(rowLabel, 0);
            BoardHost.Children.Add(rowLabel);

            var leftArrow = CreateArrow("◀");
            var capturedRow = row;
            leftArrow.Click += (_, _) => TryShift(ShiftAxis.Row, capturedRow, -1);
            Grid.SetRow(leftArrow, row + rowOffset);
            Grid.SetColumn(leftArrow, 1);
            BoardHost.Children.Add(leftArrow);

            var rightArrow = CreateArrow("▶");
            rightArrow.Click += (_, _) => TryShift(ShiftAxis.Row, capturedRow, 1);
            Grid.SetRow(rightArrow, row + rowOffset);
            Grid.SetColumn(rightArrow, size + colOffset);
            BoardHost.Children.Add(rightArrow);
        }

        for (var col = 0; col < size; col++)
        {
            var colLabel = CreateIndexLabel(col + 1);
            Grid.SetRow(colLabel, 0);
            Grid.SetColumn(colLabel, col + colOffset);
            BoardHost.Children.Add(colLabel);

            var upArrow = CreateArrow("▲");
            var capturedCol = col;
            upArrow.Click += (_, _) => TryShift(ShiftAxis.Column, capturedCol, -1);
            Grid.SetRow(upArrow, 1);
            Grid.SetColumn(upArrow, col + colOffset);
            BoardHost.Children.Add(upArrow);

            var downArrow = CreateArrow("▼");
            downArrow.Click += (_, _) => TryShift(ShiftAxis.Column, capturedCol, 1);
            Grid.SetRow(downArrow, size + rowOffset);
            Grid.SetColumn(downArrow, col + colOffset);
            BoardHost.Children.Add(downArrow);
        }

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                var card = new SuspectCardControl { Position = new GridPosition(row, col), Margin = new Thickness(4) };
                card.CardClicked += OnCardClicked;
                Grid.SetRow(card, row + rowOffset);
                Grid.SetColumn(card, col + colOffset);
                BoardHost.Children.Add(card);
                _cards[row, col] = card;
            }
        }
    }

    private static TextBlock CreateIndexLabel(int number) => new()
    {
        Text = number.ToString(),
        FontFamily = new FontFamily("Segoe UI Semibold"),
        FontSize = 13,
        Foreground = (Brush)Application.Current.Resources["MutedTextBrush"],
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Width = 22
    };

    private static Button CreateArrow(string glyph) => new()
    {
        Content = glyph,
        Style = (Style)Application.Current.Resources["ShiftArrowButton"],
        Margin = new Thickness(2)
    };

    private void OnCardClicked(object? sender, GridPosition position)
    {
        _selected = _selected == position ? null : position;
        RefreshBoard();
        UpdateActionButtons();
    }

    private void TryShift(ShiftAxis axis, int index, int direction)
    {
        if (_busy || _session.IsOver || _session.CurrentPlayer != _human) return;

        try
        {
            _session.Shift(axis, index, direction);
        }
        catch (InvalidOperationException ex)
        {
            AppendLog($"⚠ {ex.Message}");
            return;
        }

        _selected = null;
        RefreshAll();
        _ = AdvanceIfBotTurnAsync();
    }

    private void CatchButton_Click(object sender, RoutedEventArgs e) => ResolveHumanAction(isCatch: true);

    private void ScanButton_Click(object sender, RoutedEventArgs e) => ResolveHumanAction(isCatch: false);

    private void ResolveHumanAction(bool isCatch)
    {
        if (_busy || _session.IsOver || _session.CurrentPlayer != _human || _selected is not { } target) return;

        if (isCatch)
        {
            var caught = _session.Catch(target);
            AppendLog(caught ? "✅ Попадание!" : "❌ Мимо.");
        }
        else
        {
            var zone = _session.ScanZone(target);
            var found = _session.Scan(target);
            AppendLog(found
                ? $"📡 Бот где-то здесь: {string.Join(", ", zone.Select(s => s.Name))}."
                : "📡 Бота нет ни в одной из просканированных карт.");
        }

        _selected = null;
        RefreshAll();
        _ = AdvanceIfBotTurnAsync();
    }

    private async Task AdvanceIfBotTurnAsync()
    {
        if (_session.IsOver || _session.CurrentPlayer != _bot) return;

        _busy = true;
        UpdateActionButtons();
        await Task.Delay(700);

        _botStrategy.TakeTurn(_session, _bot);

        _busy = false;
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshBoard();
        UpdateActionButtons();
        ScoreText.Text = $"Вы: {_human.Trophies} 🏆   Бот: {_bot.Trophies} 🏆";

        _selfCard.Position = _session.PositionOf(_human);
        _selfCard.Render(_human.SecretIdentity, isSelf: true, isSelectable: false, isSelected: false);

        IdentityText.Text = _human.SecretIdentity.Name;
        TurnText.Text = _session.IsOver
            ? "Игра окончена"
            : _session.CurrentPlayer == _human ? "Ваш ход" : "Ход бота...";
    }

    private void RefreshBoard()
    {
        var size = _session.Grid.Size;
        var legalTargets = _session.IsOver || _session.CurrentPlayer != _human
            ? []
            : _session.LegalTargets(_human).ToArray();

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                var position = new GridPosition(row, col);
                var suspect = _session.Grid.At(position);
                var isSelf = suspect == _human.SecretIdentity;
                var isSelectable = legalTargets.Contains(position);
                var isSelected = _selected == position;
                _cards[row, col].Render(suspect, isSelf, isSelectable, isSelected);
            }
        }
    }

    private void UpdateActionButtons()
    {
        var canAct = !_busy && !_session.IsOver && _session.CurrentPlayer == _human && _selected is not null;
        var isSelf = _selected is { } p && p == _session.PositionOf(_human);

        CatchButton.IsEnabled = canAct && !isSelf;
        ScanButton.IsEnabled = canAct;

        if (_selected is { } position)
        {
            var name = _session.Grid.At(position).Name;
            CatchButton.Content = isSelf ? "🔍 Поймать (нельзя себя)" : $"🔍 Поймать: {name}";
            ScanButton.Content = $"📡 Сканировать вокруг: {name}";
        }
        else
        {
            CatchButton.Content = "🔍 Поймать";
            ScanButton.Content = "📡 Сканировать зону";
        }
    }

    private void AddTrophyChip(SpyPlayer catcher, Suspect caught)
    {
        var panel = catcher == _human ? HumanTrophyPanel : BotTrophyPanel;
        panel.Items.Add(CreateTrophyChip(caught.Name));
    }

    private static Border CreateTrophyChip(string name) => new()
    {
        Background = (Brush)Application.Current.Resources["AccentDarkBrush"],
        CornerRadius = new CornerRadius(5),
        Padding = new Thickness(8, 3, 8, 3),
        Margin = new Thickness(0, 0, 6, 0),
        Child = new TextBlock
        {
            Text = name,
            FontSize = 11,
            FontFamily = new FontFamily("Segoe UI Semibold"),
            Foreground = (Brush)Application.Current.Resources["TextBrush"]
        }
    };

    private void AppendLog(string message)
    {
        LogPanel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextBrush"],
            Margin = new Thickness(0, 0, 0, 8)
        });
        LogScroll.ScrollToEnd();
    }

    private void ShowGameOver(SpyPlayer winner)
    {
        var youWon = winner == _human;
        GameOverText.Text = youWon ? "🎉 Вы поймали бота!" : "💀 Бот поймал вас!";
        GameOverOverlay.Visibility = Visibility.Visible;
        RefreshAll();
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e) => StartNewGame();

    private static void ShowHelp()
    {
        MessageBox.Show(
            "Правила:\n" +
            "На поле 5×5 — подозреваемые. У вас и у бота есть тайная личность (ваша показана сверху).\n\n" +
            "Можно выбрать любую из 8 карт вокруг себя (включая диагонали) либо саму себя — номера рядов и столбцов подписаны по краям поля.\n\n" +
            "Каждый ход выберите ОДНО действие:\n" +
            "  • «Поймать» — угадайте личность соперника среди выбранной карты (себя ловить нельзя). Промах убивает мирного жителя — карта выбывает навсегда!\n" +
            "  • «Сканировать зону» — узнайте, находится ли соперник среди выбранной карты и всех её соседей (до 9 карт, включая диагонали и себя). Безопасно, никто не погибает.\n" +
            "  • Сдвиньте ряд/столбец стрелочками по краям поля, чтобы изменить расположение.\n\n" +
            "Первый, кто поймает соперника 3 раза, побеждает.\n\n" +
            "Горячие клавиши: N — новая игра, F1 — эта справка.",
            "Правила — Шпион против шпиона",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e) => ShowHelp();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.N:
                StartNewGame();
                e.Handled = true;
                break;
            case Key.F1:
                ShowHelp();
                e.Handled = true;
                break;
        }
    }
}
