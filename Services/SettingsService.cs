using DeskNotes.Models;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace DeskNotes.Services;

public class SettingsService
{
    private readonly string _filePath;
    private readonly DispatcherTimer _saveTimer;
    private AppSettings _pending = new();

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskNotes");

        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "settings.json");

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            Write(_pending);
        };
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return MigrateLegacySettings();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        _pending = settings;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void SaveImmediate(AppSettings settings) => Write(settings);

    private void Write(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Speichern darf die App nicht abstürzen lassen.
        }
    }

    private AppSettings MigrateLegacySettings()
    {
        var folder = Path.GetDirectoryName(_filePath)!;
        var legacy = Path.Combine(folder, "window.json");

        if (!File.Exists(legacy))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(legacy);
            var window = JsonSerializer.Deserialize<WindowSettings>(json);
            if (window == null)
                return new AppSettings();

            return new AppSettings
            {
                Left = window.Left,
                Top = window.Top,
                Width = window.Width,
                Height = window.Height,
                TopMost = window.TopMost
            };
        }
        catch
        {
            return new AppSettings();
        }
    }
}