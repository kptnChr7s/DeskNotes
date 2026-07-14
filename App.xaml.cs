using Wpf.Ui.Appearance;

namespace DeskNotes;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        MainWindow window = new();
        window.Show();
    }
}