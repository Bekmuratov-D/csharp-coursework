using System.Drawing.Drawing2D;

namespace ClockTrainer.WinForms;

public enum ClockHand
{
    Hour,
    Minute
}

/// <summary>Циферблат с часовой и минутной стрелкой. В интерактивном режиме стрелки можно тащить мышью.</summary>
public sealed class AnalogClockControl : Control
{
    private TimeOnly _time;
    private ClockHand? _draggedHand;

    public event EventHandler? TimeChanged;

    public AnalogClockControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                  ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Size = new Size(220, 220);
    }

    public TimeOnly Time
    {
        get => _time;
        set
        {
            _time = value;
            Invalidate();
        }
    }

    public bool Interactive { get; set; }

    public int SnapMinutes { get; set; } = 1;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var diameter = Math.Min(Width, Height) - 10;
        var radius = diameter / 2.0;
        var center = new PointF(Width / 2f, Height / 2f);

        using var facePen = new Pen(Color.Black, 2);
        using var numberFont = new Font(Font.FontFamily, (float)(radius * 0.14), FontStyle.Bold);
        using var numberBrush = new SolidBrush(ForeColor);

        g.FillEllipse(Brushes.White, (float)(center.X - radius), (float)(center.Y - radius), diameter, diameter);
        g.DrawEllipse(facePen, (float)(center.X - radius), (float)(center.Y - radius), diameter, diameter);

        for (var i = 1; i <= 12; i++)
        {
            var angle = i / 12.0 * 2 * Math.PI;
            var tickOuter = PointOnClock(center, radius - 4, angle);
            var tickInner = PointOnClock(center, radius - 14, angle);
            g.DrawLine(facePen, tickInner, tickOuter);

            var numberPos = PointOnClock(center, radius - 28, angle);
            var text = i.ToString();
            var textSize = g.MeasureString(text, numberFont);
            g.DrawString(text, numberFont, numberBrush, numberPos.X - textSize.Width / 2, numberPos.Y - textSize.Height / 2);
        }

        DrawHand(g, center, radius * 0.5, HourAngle(), 5, Color.Black);
        DrawHand(g, center, radius * 0.75, MinuteAngle(), 3, Color.DarkSlateGray);

        g.FillEllipse(Brushes.Black, center.X - 3, center.Y - 3, 6, 6);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Interactive) return;

        _draggedHand = PickHand(e.Location);
        UpdateHandFromMouse(e.Location, _draggedHand.Value);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!Interactive || _draggedHand is null) return;

        UpdateHandFromMouse(e.Location, _draggedHand.Value);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _draggedHand = null;
    }

    private double HourAngle() => (_time.Hour % 12 + _time.Minute / 60.0) / 12.0 * 2 * Math.PI;

    private double MinuteAngle() => _time.Minute / 60.0 * 2 * Math.PI;

    private PointF Center() => new(Width / 2f, Height / 2f);

    private ClockHand PickHand(Point mouse)
    {
        var clickAngle = AngleFromMouse(mouse);
        var hourDiff = AngleDifference(clickAngle, HourAngle());
        var minuteDiff = AngleDifference(clickAngle, MinuteAngle());
        return minuteDiff <= hourDiff ? ClockHand.Minute : ClockHand.Hour;
    }

    private void UpdateHandFromMouse(Point mouse, ClockHand hand)
    {
        var angle = AngleFromMouse(mouse);

        if (hand == ClockHand.Minute)
        {
            var minute = SnapTo((int)Math.Round(angle / (2 * Math.PI) * 60), SnapMinutes, 60);
            _time = new TimeOnly(_time.Hour, 0).AddMinutes(minute);
        }
        else
        {
            var hour = (int)Math.Round(angle / (2 * Math.PI) * 12) % 12;
            var baseHour = _time.Hour - _time.Hour % 12;
            _time = new TimeOnly((baseHour + hour) % 24, _time.Minute);
        }

        Invalidate();
        TimeChanged?.Invoke(this, EventArgs.Empty);
    }

    private double AngleFromMouse(Point mouse)
    {
        var center = Center();
        var dx = mouse.X - center.X;
        var dy = mouse.Y - center.Y;
        var angle = Math.Atan2(dx, -dy);
        return angle < 0 ? angle + 2 * Math.PI : angle;
    }

    private static int SnapTo(int value, int step, int modulo)
    {
        if (step <= 1) return value % modulo;

        var snapped = (int)Math.Round(value / (double)step) * step;
        return snapped % modulo;
    }

    private static double AngleDifference(double a, double b)
    {
        var diff = Math.Abs(a - b) % (2 * Math.PI);
        return diff > Math.PI ? 2 * Math.PI - diff : diff;
    }

    private static PointF PointOnClock(PointF center, double radius, double clockAngle) => new(
        (float)(center.X + radius * Math.Sin(clockAngle)),
        (float)(center.Y - radius * Math.Cos(clockAngle)));

    private static void DrawHand(Graphics g, PointF center, double length, double angle, float width, Color color)
    {
        var end = PointOnClock(center, length, angle);
        using var pen = new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, center, end);
    }
}
