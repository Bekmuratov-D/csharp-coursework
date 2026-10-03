namespace SpyCore;

public enum ShiftAxis { Row, Column }

public sealed class GameSession
{
    private readonly Random _random;
    private (ShiftAxis Axis, int Index, int Direction)? _lastShift;

    public SuspectGrid Grid { get; }
    public SpyPlayer PlayerA { get; }
    public SpyPlayer PlayerB { get; }
    public SpyPlayer CurrentPlayer { get; private set; }
    public int TrophyGoal { get; }
    public SpyPlayer? Winner { get; private set; }
    public bool IsOver => Winner is not null;

    public event EventHandler? GridChanged;
    public event EventHandler<string>? TurnLogged;
    public event EventHandler<SpyPlayer>? GameEnded;
    public event EventHandler<(SpyPlayer Catcher, Suspect Caught)>? SuspectCaught;

    public GameSession(SuspectGrid grid, SpyPlayer playerA, SpyPlayer playerB, int trophyGoal = 3, Random? random = null)
    {
        Grid = grid;
        PlayerA = playerA;
        PlayerB = playerB;
        TrophyGoal = trophyGoal;
        _random = random ?? new Random();

        AssignNewIdentity(playerA, exclude: null);
        AssignNewIdentity(playerB, exclude: playerA.SecretIdentity);

        CurrentPlayer = _random.Next(2) == 0 ? playerA : playerB;
    }

    public SpyPlayer Opponent(SpyPlayer player) => ReferenceEquals(player, PlayerA) ? PlayerB : PlayerA;

    public GridPosition PositionOf(SpyPlayer player)
    {
        var position = Grid.Find(player.SecretIdentity);
        if (position is null)
            throw new InvalidOperationException("Личность игрока не найдена на игровом поле.");

        return position.Value;
    }

    /// <summary>
    /// Клетки, доступные игроку для Поймать/Сканировать: все до 8 соседей (включая диагонали)
    /// вокруг своей личности, плюс сама личность (можно сканировать/ловить и себя).
    /// </summary>
    public IReadOnlyList<GridPosition> LegalTargets(SpyPlayer player)
    {
        var selfPosition = PositionOf(player);
        var positions = new HashSet<GridPosition>(Grid.MooreNeighbors(selfPosition)) { selfPosition };
        return positions.Where(p => Grid.At(p).IsAlive).ToList();
    }

    public void Shift(ShiftAxis axis, int index, int direction)
    {
        EnsureNotOver();

        if (_lastShift is { } last && last.Axis == axis && last.Index == index && last.Direction == -direction)
            throw new InvalidOperationException("Нельзя сразу отменить последний сдвиг соперника.");

        if (axis == ShiftAxis.Row) Grid.ShiftRow(index, direction);
        else Grid.ShiftColumn(index, direction);

        var directionWord = axis == ShiftAxis.Row
            ? (direction > 0 ? "вправо" : "влево")
            : (direction > 0 ? "вниз" : "вверх");

        _lastShift = (axis, index, direction);
        TurnLogged?.Invoke(this,
            $"{CurrentPlayer.Name} сдвигает {(axis == ShiftAxis.Row ? "ряд" : "столбец")} {index + 1} {directionWord}.");
        GridChanged?.Invoke(this, EventArgs.Empty);
        EndTurn();
    }

    public bool Catch(GridPosition target)
    {
        EnsureNotOver();
        EnsureLegalTarget(target);

        if (target == PositionOf(CurrentPlayer))
            throw new InvalidOperationException("Нельзя стрелять в самого себя.");

        var suspect = Grid.At(target);
        var opponent = Opponent(CurrentPlayer);
        var success = ReferenceEquals(suspect, opponent.SecretIdentity);

        suspect.MarkCaught();
        GridChanged?.Invoke(this, EventArgs.Empty);

        if (success)
        {
            CurrentPlayer.Trophies++;
            TurnLogged?.Invoke(this, $"{CurrentPlayer.Name} ловит {opponent.Name} — это был(а) {suspect.Name}!");
            SuspectCaught?.Invoke(this, (CurrentPlayer, suspect));

            if (CurrentPlayer.Trophies >= TrophyGoal || !HasSpareIdentity(exclude: CurrentPlayer.SecretIdentity))
            {
                // Либо набрали нужный счёт, либо на поле физически некому больше прятаться.
                Winner = CurrentPlayer;
                GameEnded?.Invoke(this, CurrentPlayer);
                return true;
            }

            AssignNewIdentity(opponent, exclude: CurrentPlayer.SecretIdentity);
        }
        else
        {
            TurnLogged?.Invoke(this,
                $"{CurrentPlayer.Name} стреляет в {suspect.Name} — мимо, это был мирный житель. 💀");
        }

        _lastShift = null;
        EndTurn();
        return success;
    }

    /// <summary>Зона сканирования: выбранный подозреваемый и все его соседи, включая диагонали (до 9 карт).</summary>
    public IReadOnlyList<Suspect> ScanZone(GridPosition target)
    {
        var positions = new HashSet<GridPosition>(Grid.MooreNeighbors(target)) { target };
        return positions.Select(Grid.At).ToList();
    }

    public bool Scan(GridPosition target)
    {
        EnsureNotOver();
        EnsureLegalTarget(target);

        var opponent = Opponent(CurrentPlayer);
        var opponentPosition = PositionOf(opponent);
        var zone = new HashSet<GridPosition>(Grid.MooreNeighbors(target)) { target };
        var isInZone = zone.Contains(opponentPosition);

        TurnLogged?.Invoke(this,
            $"{CurrentPlayer.Name} сканирует зону вокруг {Grid.At(target).Name} ({zone.Count} карт) — " +
            $"{opponent.Name} отвечает: {(isInZone ? "да, я там" : "нет, меня там нет")}.");

        _lastShift = null;
        EndTurn();
        return isInZone;
    }

    private bool HasSpareIdentity(Suspect exclude) => Grid.AllSuspects().Any(s => s.IsAlive && s != exclude);

    private void AssignNewIdentity(SpyPlayer player, Suspect? exclude)
    {
        var candidates = Grid.AllSuspects().Where(s => s.IsAlive && s != exclude).ToList();
        if (candidates.Count == 0)
            throw new InvalidOperationException("На поле не осталось доступных личностей.");

        player.SecretIdentity = candidates[_random.Next(candidates.Count)];
    }

    private void EnsureLegalTarget(GridPosition target)
    {
        if (!LegalTargets(CurrentPlayer).Contains(target))
            throw new InvalidOperationException("Этот подозреваемый не находится рядом с вашей личностью.");
    }

    private void EnsureNotOver()
    {
        if (IsOver) throw new InvalidOperationException("Игра уже окончена.");
    }

    private void EndTurn()
    {
        if (IsOver) return;
        CurrentPlayer = Opponent(CurrentPlayer);
    }
}
