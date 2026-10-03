using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SpyCore;

namespace SpyVsSpy.Wpf.Controls;

public partial class SuspectCardControl : UserControl
{
    private static readonly Color[] AvatarPalette =
    [
        Color.FromRgb(0xC0, 0x39, 0x2B), Color.FromRgb(0x2E, 0x86, 0xAB), Color.FromRgb(0x6A, 0x4C, 0x93),
        Color.FromRgb(0x1E, 0x8A, 0x6E), Color.FromRgb(0xD4, 0xA8, 0x2A), Color.FromRgb(0xA3, 0x3E, 0x6B),
        Color.FromRgb(0x4B, 0x6A, 0x3B), Color.FromRgb(0x7D, 0x5A, 0x3C)
    ];

    private bool _isSelectable;

    public GridPosition Position { get; set; }

    public event EventHandler<GridPosition>? CardClicked;

    public SuspectCardControl()
    {
        InitializeComponent();
    }

    public void Render(Suspect suspect, bool isSelf, bool isSelectable, bool isSelected)
    {
        NameText.Text = suspect.Name;
        _isSelectable = isSelectable && suspect.IsAlive;

        AvatarInitials.Text = Initials(suspect.Name);
        AvatarEllipse.Fill = new SolidColorBrush(AvatarPalette[Math.Abs(suspect.Name.GetHashCode()) % AvatarPalette.Length]);

        DeadMark.Visibility = suspect.IsAlive ? Visibility.Collapsed : Visibility.Visible;
        AvatarEllipse.Opacity = suspect.IsAlive ? 1.0 : 0.15;
        AvatarInitials.Opacity = suspect.IsAlive ? 1.0 : 0.0;
        NameText.Opacity = suspect.IsAlive ? 1.0 : 0.35;
        CardBorder.Background = (Brush)FindResource(suspect.IsAlive ? "CardBrush" : "CardDeadBrush");
        SelfBadge.Visibility = isSelf && suspect.IsAlive ? Visibility.Visible : Visibility.Collapsed;

        CardBorder.BorderBrush = isSelected
            ? (Brush)FindResource("AccentBrush")
            : Brushes.Transparent;

        CardBorder.Cursor = _isSelectable ? Cursors.Hand : Cursors.Arrow;
        CardBorder.Opacity = isSelectable || !suspect.IsAlive ? 1.0 : 0.75;
    }

    private static string Initials(string name) => name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();

    private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelectable) return;

        var pop = new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 }
        };
        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);

        CardClicked?.Invoke(this, Position);
    }
}
