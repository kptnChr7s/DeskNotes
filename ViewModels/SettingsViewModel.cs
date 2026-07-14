using CommunityToolkit.Mvvm.ComponentModel;
using DeskNotes.Abstractions;
using DeskNotes.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace DeskNotes.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AutoStartService _autoStartService = new();
    private readonly Action<bool>? _onTopMostChanged;
    private readonly Action<bool>? _onAutoStartChanged;
    private bool _suppressCallbacks;

    [ObservableProperty]
    private bool topMost;

    [ObservableProperty]
    private bool autoStart;

    [ObservableProperty]
    private bool autoStartAvailable;

    [ObservableProperty]
    private string autoStartStatus = string.Empty;

    [ObservableProperty]
    private bool isHotkeyActive;

    [ObservableProperty]
    private string hotkeyStatus = string.Empty;

    public string HotkeyDisplay { get; }
    public string Version { get; }
    public ObservableCollection<AddonManifest> LoadedAddons { get; } = [];

    public SettingsViewModel(
        bool topMost,
        bool hotkeyRegistered,
        string? hotkeyError,
        IReadOnlyList<AddonManifest> loadedAddons,
        Action<bool>? onTopMostChanged = null,
        Action<bool>? onAutoStartChanged = null)
    {
        _onTopMostChanged = onTopMostChanged;
        _onAutoStartChanged = onAutoStartChanged;

        HotkeyDisplay = "Strg + Alt + Leertaste";
        Version = $"DeskNotes {GetAppVersion()}";

        IsHotkeyActive = hotkeyRegistered;
        HotkeyStatus = hotkeyRegistered
            ? "Aktiv — funktioniert global"
            : hotkeyError ?? "Nicht verfügbar";

        AutoStartAvailable = !string.IsNullOrWhiteSpace(_autoStartService.ExecutablePath) &&
                             File.Exists(_autoStartService.ExecutablePath);

        foreach (var addon in loadedAddons)
            LoadedAddons.Add(addon);

        _suppressCallbacks = true;
        TopMost = topMost;
        AutoStart = _autoStartService.IsAutoStartEnabled();
        _suppressCallbacks = false;

        UpdateAutoStartStatus();
    }

    partial void OnTopMostChanged(bool value)
    {
        if (_suppressCallbacks)
            return;

        _onTopMostChanged?.Invoke(value);
    }

    partial void OnAutoStartChanged(bool value)
    {
        if (_suppressCallbacks)
            return;

        if (!AutoStartAvailable)
        {
            _suppressCallbacks = true;
            AutoStart = _autoStartService.IsAutoStartEnabled();
            _suppressCallbacks = false;
            UpdateAutoStartStatus();
            return;
        }

        var success = _autoStartService.SetAutoStart(value);
        if (!success)
        {
            _suppressCallbacks = true;
            AutoStart = _autoStartService.IsAutoStartEnabled();
            _suppressCallbacks = false;
        }
        else
        {
            _onAutoStartChanged?.Invoke(value);
        }

        UpdateAutoStartStatus();
    }

    private void UpdateAutoStartStatus()
    {
        if (!AutoStartAvailable)
        {
            AutoStartStatus = "Autostart nicht verfügbar (EXE-Pfad nicht gefunden).";
            return;
        }

        AutoStartStatus = AutoStart
            ? "DeskNotes startet mit Windows."
            : "DeskNotes startet nicht automatisch mit Windows.";
    }

    private static string GetAppVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "1.0" : $"{version.Major}.{version.Minor}";
    }
}