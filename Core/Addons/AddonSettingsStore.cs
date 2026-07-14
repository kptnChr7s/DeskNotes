using System.IO;
using System.Text.Json;

namespace DeskNotes.Core.Addons;

public sealed class AddonSettingsStore
{
    private readonly string _filePath;
    private AddonSettingsData _data = new();

    public AddonSettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskNotes");

        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "addon-settings.json");
        _data = Load();
    }

    public bool IsEnabled(string addonId) =>
        !_data.Disabled.Contains(addonId, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Disabled => _data.Disabled;

    public void SetEnabled(string addonId, bool enabled)
    {
        if (enabled)
            _data.Disabled.RemoveAll(id => id.Equals(addonId, StringComparison.OrdinalIgnoreCase));
        else if (!_data.Disabled.Contains(addonId, StringComparer.OrdinalIgnoreCase))
            _data.Disabled.Add(addonId);

        Save();
    }

    private AddonSettingsData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new AddonSettingsData();

            return JsonSerializer.Deserialize<AddonSettingsData>(File.ReadAllText(_filePath))
                   ?? new AddonSettingsData();
        }
        catch
        {
            return new AddonSettingsData();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // optional persistence
        }
    }

    private sealed class AddonSettingsData
    {
        public List<string> Disabled { get; set; } = [];
    }
}