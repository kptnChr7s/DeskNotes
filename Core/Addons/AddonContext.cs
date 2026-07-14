using DeskNotes.Abstractions;
using DeskNotes.Models;
using DeskNotes.ViewModels;
using System.Windows;

namespace DeskNotes.Core.Addons;

public sealed class AddonContext : IAddonContext
{
    private readonly EventBus _bus;
    private readonly MainViewModel _viewModel;
    private readonly List<AddonTrayMenuItem> _trayItems = [];
    private readonly List<AddonSettingsSection> _settingsSections = [];

    public AddonContext(
        EventBus bus,
        Window mainWindow,
        object? trayIcon,
        MainViewModel viewModel,
        string addonsDirectory,
        string dataDirectory)
    {
        _bus = bus;
        MainWindow = mainWindow;
        TrayIcon = trayIcon;
        _viewModel = viewModel;
        AddonsDirectory = addonsDirectory;
        DataDirectory = dataDirectory;
    }

    public object MainWindow { get; }
    public object? TrayIcon { get; }
    public string AddonsDirectory { get; }
    public string DataDirectory { get; }

    public IReadOnlyList<AddonTrayMenuItem> TrayMenuItems => _trayItems;
    public IReadOnlyList<AddonSettingsSection> SettingsSections => _settingsSections;

    public IReadOnlyList<AddonNote> GetNotes() =>
        _viewModel.Todos.Select(MapNote).ToList();

    public void Subscribe<T>(Action<T> handler) => _bus.Subscribe(handler);

    public void Publish<T>(T message) => _bus.Publish(message);

    public void RegisterTrayMenuItem(string label, Action onClick, int order = 100) =>
        _trayItems.Add(new AddonTrayMenuItem { Label = label, Action = onClick, Order = order });

    public void RegisterSettingsSection(string title, UIElement content, int order = 100) =>
        _settingsSections.Add(new AddonSettingsSection { Title = title, Content = content, Order = order });

    internal static AddonNote MapNote(TodoItem item) => new()
    {
        Id = item.Id,
        Text = item.Text,
        IsCompleted = item.IsCompleted,
        CreatedAt = item.CreatedAt,
        CompletedAt = item.CompletedAt,
        AccentColor = item.AccentColor
    };
}