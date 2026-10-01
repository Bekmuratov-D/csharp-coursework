namespace ClockTrainer.WinForms;

public sealed class SettingsForm : Form
{
    private readonly RadioButton _lightRadio;
    private readonly RadioButton _darkRadio;

    public string SelectedTheme => _darkRadio.Checked ? "Dark" : "Light";

    public SettingsForm(string currentTheme)
    {
        Text = "Настройки";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(260, 150);

        var themeGroup = new GroupBox { Text = "Тема", Dock = DockStyle.Top, Height = 90, Padding = new Padding(8) };
        _lightRadio = new RadioButton { Text = "Светлая", Dock = DockStyle.Top };
        _darkRadio = new RadioButton { Text = "Тёмная", Dock = DockStyle.Top };
        themeGroup.Controls.Add(_darkRadio);
        themeGroup.Controls.Add(_lightRadio);

        if (currentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
            _darkRadio.Checked = true;
        else
            _lightRadio.Checked = true;

        var okButton = new Button { Text = "ОК", DialogResult = DialogResult.OK, Dock = DockStyle.Left, Width = 120 };
        var cancelButton = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 120 };
        var buttonsPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        buttonsPanel.Controls.Add(okButton);
        buttonsPanel.Controls.Add(cancelButton);

        Controls.Add(themeGroup);
        Controls.Add(buttonsPanel);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
