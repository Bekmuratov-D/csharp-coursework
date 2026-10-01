using ClockCore;

namespace ClockTrainer.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var options = CliParser.Parse(args);
        if (options.ShowHelp)
        {
            MessageBox.Show(CliParser.HelpText, "Справка — Тренажёр определения времени",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm(options));
    }
}
