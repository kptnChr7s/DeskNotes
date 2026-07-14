using System.IO;
using System.Text.Json;

namespace DeskNotes.Addon.Confetti;

internal sealed class ConfettiSettingsStore
{
    private readonly string _filePath;

    public bool PlaySound { get; set; } = true;

    public ConfettiSettingsStore(string dataDirectory)
    {
        var folder = Path.Combine(dataDirectory, "addons");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "confetti.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var data = JsonSerializer.Deserialize<ConfettiSettingsData>(File.ReadAllText(_filePath));
            if (data != null)
                PlaySound = data.PlaySound;
        }
        catch
        {
            // optional persistence
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                new ConfettiSettingsData { PlaySound = PlaySound },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // optional persistence
        }
    }

    private sealed class ConfettiSettingsData
    {
        public bool PlaySound { get; set; } = true;
    }
}