namespace DeskNotes.Abstractions;

public interface IAddonContext
{
    object MainWindow { get; }
    object? TrayIcon { get; }
    string AddonsDirectory { get; }
    string DataDirectory { get; }

    IReadOnlyList<AddonNote> GetNotes();

    void Subscribe<T>(Action<T> handler);
    void Publish<T>(T message);

    void RegisterTrayMenuItem(string label, Action onClick, int order = 100);
    void RegisterSettingsSection(string title, System.Windows.UIElement content, int order = 100);
}