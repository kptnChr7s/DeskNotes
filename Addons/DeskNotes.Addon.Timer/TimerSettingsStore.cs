using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskNotes.Addon.Timer;

internal sealed class TimerSettingsStore
{
    private readonly string _filePath;

    public TimerSoundProfile SoundProfile { get; set; } = TimerSoundProfile.Soft;
    public int LastMinutes { get; set; } = 25;

    public bool PlaySound => SoundProfile != TimerSoundProfile.Off;

    public TimerSettingsStore(string dataDirectory)
    {
        var folder = Path.Combine(dataDirectory, "addons");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "timer.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var data = JsonSerializer.Deserialize<TimerSettingsData>(File.ReadAllText(_filePath));
            if (data == null)
                return;

            SoundProfile = ParseProfile(data);
            LastMinutes = Math.Clamp(data.LastMinutes, 1, 180);
        }
        catch
        {
            // optional persistence
        }
    }

    private static TimerSoundProfile ParseProfile(TimerSettingsData data)
    {
        if (!string.IsNullOrWhiteSpace(data.SoundProfile)
            && Enum.TryParse<TimerSoundProfile>(data.SoundProfile, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return data.PlaySound ? TimerSoundProfile.Soft : TimerSoundProfile.Off;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                new TimerSettingsData
                {
                    SoundProfile = SoundProfile.ToString(),
                    LastMinutes = LastMinutes
                },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // optional persistence
        }
    }

    private sealed class TimerSettingsData
    {
        public string? SoundProfile { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PlaySound { get; set; } = true;

        public int LastMinutes { get; set; } = 25;
    }
}