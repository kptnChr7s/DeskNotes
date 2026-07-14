using DeskNotes.Abstractions;
using System.Windows;
using System.Windows.Controls;
using WpfApp = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
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

        var info = new TextBlock
        {
            Text = "Timer über Tray-Menü oder den Befehl timer in der Eingabe. Im Ruhezustand unsichtbar.",
            FontSize = 13,
            Foreground = (WpfBrush)WpfApp.Current.Resources["TextSecondary"],
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(info);

        panel.Children.Add(new TextBlock
        {
            Text = "Sound bei Ende",
            Style = TrySettingsStyle("Text.SectionLabel"),
            Margin = new Thickness(0, 0, 0, 6)
        });

        var soundCombo = new WpfComboBox
        {
            Height = 34,
            Margin = new Thickness(0, 0, 0, 8)
        };

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

        var openButton = new WpfButton
        {
            Content = "Timer öffnen",
            Height = 36,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        openButton.Click += (_, _) => _ui?.OpenFlyout();
        panel.Children.Add(openButton);

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

    private static Style? TrySettingsStyle(string key)
    {
        try
        {
            return (Style)WpfApp.Current.FindResource(key);
        }
        catch
        {
            return null;
        }
    }

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