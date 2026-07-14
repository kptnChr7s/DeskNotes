using DeskNotes.Abstractions;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace DeskNotes.Addon.Disco;

public sealed class DiscoAddon : IDeskNotesAddon, IDisposable
{
    private DiscoModeService? _disco;
    private IAddonContext? _context;

    public AddonManifest Manifest { get; } = AddonManifest.Create(
        "desknotes.disco",
        "Disco Mode",
        "1.0.0",
        "Aktiviere den Disco-Modus mit dem Befehl disco");

    public Task InitializeAsync(IAddonContext context)
    {
        _context = context;

        if (context.MainWindow is not Window window)
            return Task.CompletedTask;

        _disco = new DiscoModeService(window);

        if (context.TrayIcon is WinForms.NotifyIcon tray)
            _disco.AttachTray(tray);

        _disco.Bind();

        context.Subscribe<NoteInputSubmitting>(OnNoteInput);
        context.Subscribe<AppShutdown>(_ => _disco?.Dispose());

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _disco?.Dispose();
        _disco = null;
        return Task.CompletedTask;
    }

    private void OnNoteInput(NoteInputSubmitting message)
    {
        var text = message.Text.Trim().ToLowerInvariant();
        if (text is not ("disco" or "disco!"))
            return;

        _disco?.Toggle();
        message.CancelDefault = true;
        message.ClearInput = true;
    }

    public void Dispose() => _disco?.Dispose();
}