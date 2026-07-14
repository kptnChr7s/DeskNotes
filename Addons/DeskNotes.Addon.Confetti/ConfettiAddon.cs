using DeskNotes.Abstractions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DeskNotes.Addon.Confetti;

public sealed class ConfettiAddon : IDeskNotesAddon, IDisposable
{
    private ConfettiService? _service;
    private ConfettiSettingsStore? _settings;
    private Window? _window;

    public AddonManifest Manifest { get; } = AddonManifest.Create(
        "desknotes.confetti",
        "Confetti",
        "1.0.0",
        "Konfetti und Sound wenn eine Notiz erledigt wird");

    public Task InitializeAsync(IAddonContext context)
    {
        if (context.MainWindow is not Window window)
            return Task.CompletedTask;

        _window = window;
        _settings = new ConfettiSettingsStore(context.DataDirectory);

        try
        {
            _service = new ConfettiService(window, _settings);
        }
        catch
        {
            return Task.CompletedTask;
        }

        context.Subscribe<NoteCompleted>(OnNoteCompleted);
        context.Subscribe<AppShutdown>(_ => _service.Dispose());
        context.RegisterSettingsSection("CONFETTI", BuildSettingsPanel(), order: 40);

        return Task.CompletedTask;
    }

    private void OnNoteCompleted(NoteCompleted message)
    {
        if (_service == null || _window == null)
            return;

        var note = message.Note;
        _window.Dispatcher.BeginInvoke(
            () => _service.Burst(note),
            DispatcherPriority.Loaded);
    }

    public Task ShutdownAsync()
    {
        _service?.Dispose();
        _service = null;
        return Task.CompletedTask;
    }

    private UIElement BuildSettingsPanel()
    {
        var panel = new StackPanel();

        var info = new TextBlock
        {
            Text = "Feier-Moment beim Abhaken einer Notiz.",
            FontSize = 13,
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"],
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(info);

        var soundCheck = new CheckBox
        {
            Content = "Sound abspielen",
            IsChecked = _settings?.PlaySound ?? true,
            Margin = new Thickness(0, 0, 0, 8)
        };
        soundCheck.Checked += (_, _) => SetPlaySound(true);
        soundCheck.Unchecked += (_, _) => SetPlaySound(false);
        panel.Children.Add(soundCheck);

        return panel;
    }

    private void SetPlaySound(bool value)
    {
        if (_settings == null) return;
        _settings.PlaySound = value;
        _settings.Save();
    }

    public void Dispose() => _service?.Dispose();
}