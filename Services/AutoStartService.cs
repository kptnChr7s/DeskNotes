using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace DeskNotes.Services;

public class AutoStartService
{
    private const string AppName = "DeskNotes";
    private readonly string _executablePath;

    public AutoStartService()
    {
        _executablePath = ResolveExecutablePath();
    }

    public string ExecutablePath => _executablePath;

    public bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue(AppName) != null;
    }

    public bool SetAutoStart(bool enable)
    {
        if (string.IsNullOrWhiteSpace(_executablePath) || !File.Exists(_executablePath))
            return false;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (key == null)
                return false;

            if (enable)
                key.SetValue(AppName, $"\"{_executablePath}\"");
            else
                key.DeleteValue(AppName, false);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) &&
            !processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            return processPath;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "DeskNotes.exe"),
            Path.Combine(AppContext.BaseDirectory, "DeskNotes.dll")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return processPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? string.Empty;
    }
}