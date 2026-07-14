using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DeskNotes.Services;

public class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 9000;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkSpace = 0x20;
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _source;
    private IntPtr _hwnd;
    private bool _isRegistered;

    public event Action? HotkeyPressed;

    public bool IsRegistered => _isRegistered;
    public string HotkeyDisplay { get; } = "Strg + Alt + Leertaste";
    public string? RegistrationError { get; private set; }

    public bool Register(Window window)
    {
        Unregister();

        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;

        if (_hwnd == IntPtr.Zero)
        {
            RegistrationError = "Fenster-Handle nicht verfügbar.";
            return false;
        }

        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(HwndHook);

        _isRegistered = RegisterHotKey(_hwnd, HotkeyId, ModControl | ModAlt, VkSpace);

        if (!_isRegistered)
        {
            var error = Marshal.GetLastWin32Error();
            RegistrationError = error switch
            {
                1409 => "Hotkey wird bereits von einer anderen Anwendung verwendet.",
                _ => $"Hotkey konnte nicht registriert werden (Fehler {error})."
            };
        }
        else
        {
            RegistrationError = null;
        }

        return _isRegistered;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void Unregister()
    {
        if (_isRegistered && _hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hwnd, HotkeyId);
            _isRegistered = false;
        }

        if (_source != null)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
        }
    }

    public void Dispose() => Unregister();
}