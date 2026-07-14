using DeskNotes.Abstractions;
using System.Windows;
using System.Windows.Controls;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WinForms = System.Windows.Forms;

namespace DeskNotes.Addon.Timer;

public sealed class TimerAddon : IDeskNotesAddon, IDisposable
{
    private TimerService? _service;
    private TimerUiController? _ui;
    private TimerSettingsStore? _settings;

    public AddonManifest Manifest { get; } = AddonManifest.Create(
        "desknotes.timer",
        "Timer",
        "1.0.0",
        "Unsichtbarer Fokus-Timer mit Schnellauswahl");

    public Task InitializeAsync(IAddonContext context)
    {
        if (context.MainWindow is not Window window)
            return Task.CompletedTask;

        _settings = new TimerSettingsStore(context.DataDirectory);
        _service = new TimerService(_settings);

        var tray = context.TrayIcon as WinForms.NotifyIcon;
        _ui = new TimerUiController(window, _service, _settings, tray);

        context.RegisterTrayMenuItem("Timer starten…", () => _ui.OpenFlyout(), order: 15);
        context.RegisterTrayMenuItem("Timer stoppen", () => _service.Stop(), order: 16);

        context.Subscribe<NoteInputSubmitting>(OnNoteInput);
        context.Subscribe<AppShutdown>(_ => Dispose());
        context.RegisterSettingsSection("TIMER", BuildSettingsPanel(), order: 35);

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    private void OnNoteInput(NoteInputSubmitting message)
    {
        if (_ui == null || _service == null)
            return;

        var text = message.Text.Trim();
        if (text.Length == 0)
            return;

        var lower = text.ToLowerInvariant();

        if (lower is "timer" or "timer!")
        {
            _ui.OpenFlyout();
            message.CancelDefault = true;
            message.ClearInput = true;
            return;
        }

        if (lower.StartsWith("timer ", StringComparison.Ordinal))
        {
            var arg = lower["timer ".Length..].Trim();

            if (arg is "stop" or "stopp")
            {
                _service.Stop();
                message.CancelDefault = true;
                message.ClearInput = true;
                return;
            }

            if (TryParseMinutes(arg, out var minutes))
            {
                _ui.OpenFlyout(minutes);
                message.CancelDefault = true;
                message.ClearInput = true;
            }
        }
    }

    private static bool TryParseMinutes(string arg, out int minutes)
    {
        minutes = 0;
        arg = arg.Trim().TrimEnd('m', 'M', ' ', 'i', 'n');

        return int.TryParse(arg, out minutes) && minutes is >= 1 and <= 180;
    }

    private UIElement BuildSettingsPanel()
    {
        var panel = new StackPanel();

        panel.Children.Add(AddonUiHelper.CreateDescription(
            "Timer über Tray-Menü oder den Befehl timer in der Eingabe. Im Ruhezustand unsichtbar."));
        panel.Children.Add(AddonUiHelper.CreateFieldLabel("Sound bei Ende"));

        var soundCombo = AddonUiHelper.CreateComboBox();

        foreach (var option in GetSoundOptions())
            soundCombo.Items.Add(option);

        if (_settings != null)
        {
            foreach (WpfComboBoxItem item in soundCombo.Items)
            {
                if (item.Tag is TimerSoundProfile profile && profile == _settings.SoundProfile)
                {
                    soundCombo.SelectedItem = item;
                    break;
                }
            }
        }

        if (soundCombo.SelectedItem == null && soundCombo.Items.Count > 0)
            soundCombo.SelectedIndex = 1;

        soundCombo.SelectionChanged += (_, _) =>
        {
            if (soundCombo.SelectedItem is WpfComboBoxItem item && item.Tag is TimerSoundProfile profile)
                SetSoundProfile(profile);
        };
        panel.Children.Add(soundCombo);
        panel.Children.Add(AddonUiHelper.CreateSecondaryButton("Timer öffnen", (_, _) => _ui?.OpenFlyout()));

        return panel;
    }

    private static IEnumerable<WpfComboBoxItem> GetSoundOptions()
    {
        yield return CreateSoundOption("Aus", TimerSoundProfile.Off);
        yield return CreateSoundOption("Sanft (Windows)", TimerSoundProfile.Soft);
        yield return CreateSoundOption("Klassisch (Benachrichtigung)", TimerSoundProfile.Classic);
        yield return CreateSoundOption("Alarm", TimerSoundProfile.Alarm);
        yield return CreateSoundOption("Glocke", TimerSoundProfile.Bell);
    }

    private static WpfComboBoxItem CreateSoundOption(string label, TimerSoundProfile profile) =>
        new() { Content = label, Tag = profile };

    private void SetSoundProfile(TimerSoundProfile profile)
    {
        if (_settings == null)
            return;

        _settings.SoundProfile = profile;
        _settings.Save();
    }

    public void Dispose()
    {
        _ui?.Dispose();
        _ui = null;
        _service?.Dispose();
        _service = null;
    }
}