using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ClockTrainer.WinForms;

public sealed class RoundedButton : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 14;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        Height = 36;
        Padding = new Padding(10, 0, 10, 0);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(0, 36);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = RoundedPath(ClientRectangle, CornerRadius);
        using var brush = new SolidBrush(BackColor);
        g.FillPath(brush, path);

        var hover = ClientRectangle.Contains(PointToClient(MousePosition)) && (MouseButtons == MouseButtons.None);
        if (hover)
        {
            using var overlay = new SolidBrush(Color.FromArgb(28, 255, 255, 255));
            g.FillPath(overlay, path);
        }

        using var textBrush = new SolidBrush(ForeColor);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(Text, Font, textBrush, ClientRectangle, format);
    }

    protected override void OnMouseMove(MouseEventArgs mevent)
    {
        base.OnMouseMove(mevent);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Invalidate();
    }

    private void UpdateRegion()
    {
        using var path = RoundedPath(ClientRectangle, CornerRadius);
        Region = new Region(path);
    }

    private static GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (rect.Width <= 0 || rect.Height <= 0)
            return path;

        var d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
