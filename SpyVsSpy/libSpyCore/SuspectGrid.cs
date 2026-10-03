namespace SpyCore;

public sealed class SuspectGrid
{
    private readonly Suspect[,] _cells;

    public int Size { get; }

    public SuspectGrid(int size, IReadOnlyList<Suspect> suspects)
    {
        if (suspects.Count != size * size)
            throw new ArgumentException($"Нужно ровно {size * size} подозреваемых для поля {size}x{size}.", nameof(suspects));

        Size = size;
        _cells = new Suspect[size, size];

        var index = 0;
        for (var row = 0; row < size; row++)
            for (var col = 0; col < size; col++)
                _cells[row, col] = suspects[index++];
    }

    public Suspect At(GridPosition position) => _cells[position.Row, position.Col];

    public GridPosition? Find(Suspect suspect)
    {
        for (var row = 0; row < Size; row++)
            for (var col = 0; col < Size; col++)
                if (ReferenceEquals(_cells[row, col], suspect))
                    return new GridPosition(row, col);

        return null;
    }

    /// <summary>Все до 8 клеток вокруг позиции, включая диагонали (для сканирования зоны).</summary>
    public IEnumerable<GridPosition> MooreNeighbors(GridPosition position)
    {
        for (var dRow = -1; dRow <= 1; dRow++)
        {
            for (var dCol = -1; dCol <= 1; dCol++)
            {
                if (dRow == 0 && dCol == 0) continue;

                var row = position.Row + dRow;
                var col = position.Col + dCol;
                if (row >= 0 && row < Size && col >= 0 && col < Size)
                    yield return new GridPosition(row, col);
            }
        }
    }

    public void ShiftRow(int row, int direction)
    {
        var buffer = new Suspect[Size];
        for (var col = 0; col < Size; col++)
            buffer[Wrap(col + direction)] = _cells[row, col];
        for (var col = 0; col < Size; col++)
            _cells[row, col] = buffer[col];
    }

    public void ShiftColumn(int col, int direction)
    {
        var buffer = new Suspect[Size];
        for (var row = 0; row < Size; row++)
            buffer[Wrap(row + direction)] = _cells[row, col];
        for (var row = 0; row < Size; row++)
            _cells[row, col] = buffer[row];
    }

    public IEnumerable<Suspect> AllSuspects()
    {
        for (var row = 0; row < Size; row++)
            for (var col = 0; col < Size; col++)
                yield return _cells[row, col];
    }

    private int Wrap(int value) => ((value % Size) + Size) % Size;
}
