namespace SpyCore;

/// <summary>
/// Эвристический бот: ведёт список возможных личностей соперника (пересечение
/// результатов сканирований). Пока список кандидатов есть — ловит только из
/// тех, кто сейчас доступен И входит в этот список; если таких нет — ищет
/// дальше (сканирует или перемещается), вместо того чтобы гадать вслепую.
/// Перемещение честное: бот никогда не читает настоящую позицию соперника,
/// а лишь едет к среднему положению своих собственных подозреваемых.
/// </summary>
public sealed class SimpleBotStrategy : IBotStrategy
{
    private readonly Random _random;
    private HashSet<Suspect>? _candidates;

    public SimpleBotStrategy(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public void TakeTurn(GameSession session, SpyPlayer self)
    {
        _candidates?.RemoveWhere(s => !s.IsAlive);
        if (_candidates is { Count: 0 }) _candidates = null;

        var selfPosition = session.PositionOf(self);
        var options = session.LegalTargets(self)
            .Select(p => (Position: p, Suspect: session.Grid.At(p)))
            .ToList();

        if (options.Count == 0)
        {
            Reposition(session, self);
            return;
        }

        // Себя ловить нельзя — выстрел считается только среди соседей.
        var catchable = options.Where(o => o.Position != selfPosition).ToList();

        if (_candidates is not null)
        {
            var known = catchable.Where(o => _candidates.Contains(o.Suspect)).ToList();
            if (known.Count > 0)
            {
                Attempt(session, known[_random.Next(known.Count)]);
                return;
            }

            // Никого из текущего списка подозреваемых нет рядом — едем туда, где они есть.
            Reposition(session, self);
            return;
        }

        // Без единой зацепки стреляем вслепую, только если выбора вообще не остаётся —
        // иначе сперва сканируем, чтобы не выкашивать мирных жителей почём зря.
        if (catchable.Count == 1)
        {
            Attempt(session, catchable[0]);
            return;
        }

        var scanTarget = options[_random.Next(options.Count)];
        var zone = session.ScanZone(scanTarget.Position);
        var isInZone = session.Scan(scanTarget.Position);
        ApplyScanResult(session, zone, isInZone);
    }

    private void Attempt(GameSession session, (GridPosition Position, Suspect Suspect) target)
    {
        var caught = session.Catch(target.Position);
        if (caught)
            Forget();
        else
            _candidates?.Remove(target.Suspect);
    }

    private void ApplyScanResult(GameSession session, IReadOnlyList<Suspect> zone, bool isInZone)
    {
        if (isInZone)
        {
            var zoneSet = new HashSet<Suspect>(zone);
            _candidates = _candidates is null ? zoneSet : new HashSet<Suspect>(_candidates.Intersect(zoneSet));
        }
        else
        {
            _candidates ??= session.Grid.AllSuspects().Where(s => s.IsAlive).ToHashSet();
            foreach (var suspect in zone)
                _candidates.Remove(suspect);
        }
    }

    /// <summary>
    /// Выбирает лучший сдвиг своего ряда/столбца: если уже есть подозреваемые
    /// (из сканирований) — едем ближе к их "центру тяжести"; если информации ещё
    /// нет — едем туда, где вокруг больше живых подозреваемых вообще.
    /// </summary>
    private void Reposition(GameSession session, SpyPlayer self)
    {
        var position = session.PositionOf(self);
        var moves = new (ShiftAxis Axis, int Direction)[]
        {
            (ShiftAxis.Row, 1), (ShiftAxis.Row, -1),
            (ShiftAxis.Column, 1), (ShiftAxis.Column, -1)
        };

        var target = EstimateCandidatesCenter(session);

        // Небольшая доля случайности — без неё два бота, оказавшись в одном ряду/
        // столбце, иногда двигались синхронно и никогда не сокращали расстояние
        // (детерминированное зацикливание). Случайный выбор гарантированно его рвёт.
        if (_random.Next(5) == 0)
        {
            var randomMove = moves[_random.Next(moves.Length)];
            var randomIndex = randomMove.Axis == ShiftAxis.Row ? position.Row : position.Col;
            try
            {
                session.Shift(randomMove.Axis, randomIndex, randomMove.Direction);
                return;
            }
            catch (InvalidOperationException)
            {
                // запрещённый ход — просто продолжаем обычным ранжированием ниже
            }
        }

        var ranked = moves
            .Select(m => (Move: m, Score: ScoreShift(session, self, m.Axis, m.Axis == ShiftAxis.Row ? position.Row : position.Col, m.Direction, target)))
            .OrderByDescending(x => x.Score);

        foreach (var (move, _) in ranked)
        {
            var index = move.Axis == ShiftAxis.Row ? position.Row : position.Col;
            try
            {
                session.Shift(move.Axis, index, move.Direction);
                return;
            }
            catch (InvalidOperationException)
            {
                // Этот конкретный сдвиг сейчас запрещён (отмена хода соперника) —
                // пробуем следующий по качеству вариант, а не произвольный.
            }
        }
    }

    private GridPosition? EstimateCandidatesCenter(GameSession session)
    {
        if (_candidates is null || _candidates.Count == 0) return null;

        var positions = _candidates
            .Select(session.Grid.Find)
            .Where(p => p is not null)
            .Select(p => p!.Value)
            .ToList();

        if (positions.Count == 0) return null;

        var avgRow = (int)Math.Round(positions.Average(p => p.Row));
        var avgCol = (int)Math.Round(positions.Average(p => p.Col));
        return new GridPosition(avgRow, avgCol);
    }

    private static double ScoreShift(
        GameSession session, SpyPlayer self, ShiftAxis axis, int index, int direction, GridPosition? target)
    {
        if (axis == ShiftAxis.Row) session.Grid.ShiftRow(index, direction);
        else session.Grid.ShiftColumn(index, direction);

        var newPosition = session.PositionOf(self);
        double score = target is { } t
            ? -(CircularDistance(newPosition.Row, t.Row, session.Grid.Size) + CircularDistance(newPosition.Col, t.Col, session.Grid.Size))
            : session.Grid.MooreNeighbors(newPosition).Count(p => session.Grid.At(p).IsAlive);

        if (axis == ShiftAxis.Row) session.Grid.ShiftRow(index, -direction);
        else session.Grid.ShiftColumn(index, -direction);

        return score;
    }

    /// <summary>Расстояние с учётом того, что сдвиг поля закольцован (0 и Size-1 — соседи).</summary>
    private static int CircularDistance(int a, int b, int size)
    {
        var diff = Math.Abs(a - b);
        return Math.Min(diff, size - diff);
    }

    private void Forget()
    {
        _candidates = null;
    }
}
