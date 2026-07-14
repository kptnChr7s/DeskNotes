using DeskNotes.Abstractions;
using DeskNotes.ViewModels;
using System.IO;
using System.Windows;

namespace DeskNotes.Core.Addons;

public sealed class AddonHost : IDisposable
{
    private readonly EventBus _bus = new();
    private readonly AddonLoader _loader = new();
    private readonly AddonSettingsStore _settings = new();
    private readonly List<IDeskNotesAddon> _addons = [];
    private AddonContext? _context;

    public EventBus EventBus => _bus;
    public IReadOnlyList<IDeskNotesAddon> LoadedAddons => _addons;
    public AddonSettingsStore Settings => _settings;

    public async Task StartAsync(Window mainWindow, object? trayIcon, MainViewModel viewModel)
    {
        var addonsDir = Path.Combine(AppContext.BaseDirectory, "Addons");
        Directory.CreateDirectory(addonsDir);

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskNotes");

        _context = new AddonContext(_bus, mainWindow, trayIcon, viewModel, addonsDir, dataDir);

        foreach (var addon in _loader.LoadAll(addonsDir, _settings))
        {
            try
            {
                await addon.InitializeAsync(_context);
                _addons.Add(addon);
            }
            catch
            {
                // skip failing addon, keep app running
            }
        }
    }

    public async Task StopAsync()
    {
        if (_addons.Count == 0)
            return;

        _bus.Publish(new AppShutdown());

        foreach (var addon in _addons.ToArray())
        {
            try
            {
                await addon.ShutdownAsync();
            }
            catch
            {
                // ignore shutdown errors
            }
        }

        _addons.Clear();
        _context = null;
        _bus.Clear();
    }

    public IReadOnlyList<AddonTrayMenuItem> GetTrayMenuItems() =>
        _context?.TrayMenuItems.OrderBy(i => i.Order).ToList() ?? [];

    public IReadOnlyList<AddonSettingsSection> GetSettingsSections() =>
        _context?.SettingsSections.OrderBy(s => s.Order).ToList() ?? [];

    public IReadOnlyList<AddonManifest> GetManifests() =>
        _addons.Select(a => a.Manifest).ToList();

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}