namespace ClockTrainer.WinForms;

public readonly record struct Palette(
    Color Background,
    Color Surface,
    Color Text,
    Color Muted,
    Color Primary,
    Color Secondary,
    Color Success,
    Color Danger,
    Color ClockFace,
    Color ClockBorder,
    Color HourHand,
    Color MinuteHand);

public static class Theme
{
    public static readonly Palette Light = new(
        Background: Color.FromArgb(255, 247, 235),
        Surface: Color.White,
        Text: Color.FromArgb(40, 30, 60),
        Muted: Color.FromArgb(130, 120, 150),
        Primary: Color.FromArgb(124, 77, 255),
        Secondary: Color.FromArgb(255, 145, 77),
        Success: Color.FromArgb(6, 180, 130),
        Danger: Color.FromArgb(255, 89, 94),
        ClockFace: Color.White,
        ClockBorder: Color.FromArgb(124, 77, 255),
        HourHand: Color.FromArgb(124, 77, 255),
        MinuteHand: Color.FromArgb(255, 145, 77));

    public static readonly Palette Dark = new(
        Background: Color.FromArgb(26, 20, 46),
        Surface: Color.FromArgb(40, 32, 66),
        Text: Color.FromArgb(245, 240, 255),
        Muted: Color.FromArgb(170, 160, 200),
        Primary: Color.FromArgb(166, 130, 255),
        Secondary: Color.FromArgb(255, 170, 110),
        Success: Color.FromArgb(70, 224, 170),
        Danger: Color.FromArgb(255, 120, 125),
        ClockFace: Color.FromArgb(50, 40, 82),
        ClockBorder: Color.FromArgb(166, 130, 255),
        HourHand: Color.FromArgb(200, 170, 255),
        MinuteHand: Color.FromArgb(255, 170, 110));

    public static Palette For(string theme) =>
        theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? Dark : Light;
}
