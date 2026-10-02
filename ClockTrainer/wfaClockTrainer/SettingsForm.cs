namespace ClockTrainer.WinForms;

public sealed class SettingsForm : Form
{
    private readonly RadioButton _lightRadio;
    private readonly RadioButton _darkRadio;

    public string SelectedTheme => _darkRadio.Checked ? "Dark" : "Light";

    public SettingsForm(string currentTheme)
    {
        var palette = Theme.For(currentTheme);

        Text = "⚙️ Настройки";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(280, 170);
        Font = new Font("Segoe UI", 9.5F);
        BackColor = palette.Background;
        ForeColor = palette.Text;

        var themeGroup = new GroupBox
        {
            Text = "🎨 Тема",
            Dock = DockStyle.Top,
            Height = 100,
            Padding = new Padding(10),
            ForeColor = palette.Text
        };
        _lightRadio = new RadioButton { Text = "☀️ Светлая", Dock = DockStyle.Top, ForeColor = palette.Text };
        _darkRadio = new RadioButton { Text = "🌙 Тёмная", Dock = DockStyle.Top, ForeColor = palette.Text };
        themeGroup.Controls.Add(_darkRadio);
        themeGroup.Controls.Add(_lightRadio);

        if (currentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
            _darkRadio.Checked = true;
        else
            _lightRadio.Checked = true;

        var okButton = new RoundedButton
        {
            Text = "✅ ОК",
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Left,
            Width = 130,
            BackColor = palette.Primary,
            ForeColor = Color.White
        };
        var cancelButton = new RoundedButton
        {
            Text = "Отмена",
            DialogResult = DialogResult.Cancel,
            Dock = DockStyle.Right,
            Width = 130,
            BackColor = palette.Surface,
            ForeColor = palette.Muted
        };
        var buttonsPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8) };
        buttonsPanel.Controls.Add(okButton);
        buttonsPanel.Controls.Add(cancelButton);

        Controls.Add(themeGroup);
        Controls.Add(buttonsPanel);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
