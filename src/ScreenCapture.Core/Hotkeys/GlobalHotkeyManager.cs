using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ScreenCapture.Core.Hotkeys;

public class GlobalHotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;

    [Flags]
    public enum KeyModifiers : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    public enum VirtualKey : uint
    {
        PrintScreen = 0x2C,
        S = 0x53,
        R = 0x52,
        C = 0x43,
        Escape = 0x1B
    }

    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private readonly IntPtr _hwnd;
    private readonly HwndSource? _source;
    private int _currentId = 0;
    private bool _disposed;

    public GlobalHotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(HwndHook);
    }

    public int RegisterHotkey(KeyModifiers modifiers, VirtualKey key, Action callback)
    {
        int id = ++_currentId;

        if (RegisterHotKey(_hwnd, id, (uint)modifiers, (uint)key))
        {
            _hotkeyActions[id] = callback;
            return id;
        }

        return -1;
    }

    public bool UnregisterHotkey(int id)
    {
        if (_hotkeyActions.ContainsKey(id))
        {
            _hotkeyActions.Remove(id);
            return UnregisterHotKey(_hwnd, id);
        }
        return false;
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeyActions.Keys.ToList())
        {
            UnregisterHotKey(_hwnd, id);
        }
        _hotkeyActions.Clear();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            UnregisterAll();
            _source?.RemoveHook(HwndHook);
            _disposed = true;
        }
    }

    ~GlobalHotkeyManager()
    {
        Dispose(false);
    }
}

public class HotkeyBinding
{
    public string Name { get; set; } = string.Empty;
    public GlobalHotkeyManager.KeyModifiers Modifiers { get; set; }
    public GlobalHotkeyManager.VirtualKey Key { get; set; }
    public Action? Action { get; set; }
    public int RegisteredId { get; set; } = -1;

    public string DisplayString
    {
        get
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(GlobalHotkeyManager.KeyModifiers.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(GlobalHotkeyManager.KeyModifiers.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(GlobalHotkeyManager.KeyModifiers.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(GlobalHotkeyManager.KeyModifiers.Windows)) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join(" + ", parts);
        }
    }
}
