using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ClockTrainer.Wpf.Controls;

public partial class AnalogClockControl : UserControl
{
    private enum Hand { Hour, Minute }

    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(320);

    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
        nameof(Time), typeof(TimeOnly), typeof(AnalogClockControl),
        new FrameworkPropertyMetadata(default(TimeOnly), OnTimeChanged));

    public static readonly DependencyProperty InteractiveProperty = DependencyProperty.Register(
        nameof(Interactive), typeof(bool), typeof(AnalogClockControl), new PropertyMetadata(false));

    public static readonly DependencyProperty SnapMinutesProperty = DependencyProperty.Register(
        nameof(SnapMinutes), typeof(int), typeof(AnalogClockControl), new PropertyMetadata(1));

    public event EventHandler? TimeChanged;

    private Hand? _draggedHand;
    private bool _isDragging;
    private TimeOnly _pendingTime;
    private string _lastPeriodText = string.Empty;

    private readonly Ellipse[] _minuteDots = new Ellipse[60];
    private readonly TextBlock[] _hourNumbers = new TextBlock[12];
    private Color _mutedColor;
    private Color _activeColor;

    public AnalogClockControl()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BuildFaceMarkings();
            RefreshHands(animate: false);
        };
    }

    public TimeOnly Time
    {
        get => (TimeOnly)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public bool Interactive
    {
        get => (bool)GetValue(InteractiveProperty);
        set => SetValue(InteractiveProperty, value);
    }

    public int SnapMinutes
    {
        get => (int)GetValue(SnapMinutesProperty);
        set => SetValue(SnapMinutesProperty, value);
    }

    private static void OnTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AnalogClockControl)d;
        if (!control.IsLoaded) return;

        control.RefreshHands(animate: !control._isDragging);
        control.TimeChanged?.Invoke(control, EventArgs.Empty);
    }

    private void BuildFaceMarkings()
    {
        var center = new Point(110, 110);
        _mutedColor = ResourceColor("MutedBrush");
        _activeColor = ResourceColor("SuccessBrush");
        var textColor = ResourceColor("TextBrush");

        for (var minute = 0; minute < 60; minute++)
        {
            var angle = minute / 60.0 * 2 * Math.PI;
            var isMajor = minute % 5 == 0;
            var pos = PointOnClock(center, 80, angle);
            var size = isMajor ? 7.0 : 4.0;

            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Opacity = isMajor ? 0.55 : 0.35,
                Fill = new SolidColorBrush(_mutedColor)
            };
            Canvas.SetLeft(dot, pos.X - size / 2);
            Canvas.SetTop(dot, pos.Y - size / 2);
            RootCanvas.Children.Insert(1, dot);
            _minuteDots[minute] = dot;
        }

        for (var i = 1; i <= 12; i++)
        {
            var angle = i / 12.0 * 2 * Math.PI;

            var tickOuter = PointOnClock(center, 100, angle);
            var tickInner = PointOnClock(center, 92, angle);
            var tick = new Line
            {
                X1 = tickInner.X, Y1 = tickInner.Y, X2 = tickOuter.X, Y2 = tickOuter.Y,
                StrokeThickness = 2.5,
                Opacity = 0.45
            };
            tick.SetResourceReference(Line.StrokeProperty, "ClockBorderBrush");
            RootCanvas.Children.Insert(1, tick);

            var numberPos = PointOnClock(center, 64, angle);
            var number = new TextBlock
            {
                Text = i.ToString(),
                FontFamily = new FontFamily("Segoe UI Semibold"),
                FontSize = 17,
                Width = 26,
                Height = 22,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(textColor)
            };
            Canvas.SetLeft(number, numberPos.X - 13);
            Canvas.SetTop(number, numberPos.Y - 11);
            RootCanvas.Children.Insert(1, number);
            _hourNumbers[i - 1] = number;
        }
    }

    private Color ResourceColor(string key) => ((SolidColorBrush)FindResource(key)).Color;

    private void UpdateHighlights()
    {
        if (!Interactive) return;

        var reference = _draggedHand is not null ? _pendingTime : Time;

        for (var i = 0; i < _minuteDots.Length; i++)
        {
            var brush = (SolidColorBrush)_minuteDots[i].Fill;
            var isActive = i == reference.Minute;
            brush.Color = isActive ? _activeColor : _mutedColor;
            _minuteDots[i].Opacity = isActive ? 1.0 : (i % 5 == 0 ? 0.55 : 0.35);
        }

        var hour12 = reference.Hour % 12;
        var activeHourIndex = hour12 == 0 ? 11 : hour12 - 1;

        for (var i = 0; i < _hourNumbers.Length; i++)
        {
            var brush = (SolidColorBrush)_hourNumbers[i].Foreground;
            brush.Color = i == activeHourIndex ? _activeColor : ResourceColor("TextBrush");
            _hourNumbers[i].FontWeight = i == activeHourIndex ? FontWeights.Black : FontWeights.Bold;
        }
    }

    private void RefreshHands(bool animate)
    {
        var hourTarget = HourAngleDegrees(Time);
        var minuteTarget = MinuteAngleDegrees(Time);

        if (animate)
        {
            AnimateHand(HourRotate, hourTarget);
            AnimateHand(MinuteRotate, minuteTarget);
        }
        else
        {
            HourRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            MinuteRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            HourRotate.Angle = hourTarget;
            MinuteRotate.Angle = minuteTarget;
        }

        UpdatePeriodBadge();
        UpdateHighlights();
    }

    private static void AnimateHand(RotateTransform transform, double targetMod360)
    {
        var current = transform.Angle;
        var currentMod = ((current % 360) + 360) % 360;
        var delta = targetMod360 - currentMod;
        if (delta > 180) delta -= 360;
        if (delta < -180) delta += 360;

        var animation = new DoubleAnimation(current, current + delta, AnimationDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        transform.BeginAnimation(RotateTransform.AngleProperty, animation);
    }

    private void UpdatePeriodBadge()
    {
        var text = Time.Hour < 12 ? "AM" : "PM";
        if (text == _lastPeriodText) return;
        _lastPeriodText = text;
        PeriodBadgeText.Text = text;

        var pop = new DoubleAnimation(0.7, 1.0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 }
        };
        PeriodBadgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        PeriodBadgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }

    private void RootCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!Interactive) return;

        RootCanvas.CaptureMouse();
        var point = e.GetPosition(RootCanvas);
        _draggedHand = PickHand(point);
        _isDragging = true;
        _pendingTime = Time;
        ApplyAngle(AngleFromPoint(point), _draggedHand.Value);
    }

    private void RootCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!Interactive || _draggedHand is null) return;

        var angle = AngleFromPoint(e.GetPosition(RootCanvas));
        ApplyAngle(angle, _draggedHand.Value);
    }

    private void RootCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var hadDrag = _draggedHand is not null;
        _draggedHand = null;
        _isDragging = false;
        RootCanvas.ReleaseMouseCapture();

        // Commit the snapped value now, after the hand has already been
        // tracking the mouse continuously — this animates a short "settle"
        // into place instead of jumping during the drag itself.
        if (hadDrag)
            Time = _pendingTime;
    }

    private void ApplyAngle(double angleDegrees, Hand hand)
    {
        if (hand == Hand.Minute)
        {
            MinuteRotate.Angle = angleDegrees;

            var minute = SnapTo((int)Math.Round(angleDegrees / 360.0 * 60), SnapMinutes, 60);
            _pendingTime = new TimeOnly(_pendingTime.Hour, 0).AddMinutes(minute);
        }
        else
        {
            HourRotate.Angle = angleDegrees;

            var hour = (int)Math.Round(angleDegrees / 360.0 * 12) % 12;
            var baseHour = _pendingTime.Hour - _pendingTime.Hour % 12;
            _pendingTime = new TimeOnly((baseHour + hour) % 24, _pendingTime.Minute);
        }

        UpdateHighlights();
    }

    private Hand PickHand(Point clickPoint)
    {
        var center = new Point(110, 110);
        var hourEnd = PointOnClock(center, 50, HourAngleDegrees(Time) * Math.PI / 180.0);
        var minuteEnd = PointOnClock(center, 75, MinuteAngleDegrees(Time) * Math.PI / 180.0);

        var hourDist = DistanceToSegment(clickPoint, center, hourEnd);
        var minuteDist = DistanceToSegment(clickPoint, center, minuteEnd);

        return minuteDist <= hourDist ? Hand.Minute : Hand.Hour;
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var lengthSquared = abx * abx + aby * aby;

        var t = lengthSquared == 0 ? 0 : ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lengthSquared;
        t = Math.Clamp(t, 0, 1);

        var closestX = a.X + t * abx;
        var closestY = a.Y + t * aby;
        var dx = p.X - closestX;
        var dy = p.Y - closestY;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double HourAngleDegrees(TimeOnly time) => (time.Hour % 12 + time.Minute / 60.0) / 12.0 * 360.0;

    private static double MinuteAngleDegrees(TimeOnly time) => time.Minute / 60.0 * 360.0;

    private static double AngleFromPoint(Point point)
    {
        var dx = point.X - 110;
        var dy = point.Y - 110;
        var angle = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        return angle < 0 ? angle + 360 : angle;
    }

    private static int SnapTo(int value, int step, int modulo)
    {
        if (step <= 1) return ((value % modulo) + modulo) % modulo;

        var snapped = (int)Math.Round(value / (double)step) * step;
        return ((snapped % modulo) + modulo) % modulo;
    }

    private static Point PointOnClock(Point center, double radius, double angleRadians) => new(
        center.X + radius * Math.Sin(angleRadians),
        center.Y - radius * Math.Cos(angleRadians));
}
