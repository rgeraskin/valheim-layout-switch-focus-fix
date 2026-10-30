using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using LayoutSwitchFocusFix;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ValheimDevHarness;

namespace LayoutSwitchFocusFixHarness;

/// <summary>
/// Test commands for LayoutSwitchFocusFix. `oskey` presses keys through Windows itself (SendInput), so the system's
/// layout switch hotkey fires exactly as for a player; the harness's own `key` only reaches the Input System.
/// </summary>
[BepInPlugin("rgeraskin.LayoutSwitchFocusFixHarness", "LayoutSwitchFocusFix DevHarness", "0.0.1")]
[BepInDependency(Harness.Guid)]
[BepInDependency(Plugin.Guid)]
public class LayoutSwitchFocusFixHarness : BaseUnityPlugin
{
    void Awake()
    {
        Harness.Register("oskey", "oskey <scancode hex>[,...] [hold ms=60]: real key presses through Windows (29 = the ` key, 1E = A); the game must be in front", OsKey);
        Harness.Register("focus", "focus: the selected object, the focused text field, its text and caret", _ => Harness.Log(Focus()));
        Harness.Register("pinname", "pinname: open the big map and start naming a new pin at the player", _ =>
        {
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
            Minimap.instance.ShowPinNameInput(Player.m_localPlayer.transform.position);
        });
        Harness.Register("activate", "activate <name>: focus the visible text field whose object name contains name (WBP_Search, ...)", a =>
        {
            var name = string.Join(" ", a);
            var f = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None).FirstOrDefault(x => x.isActiveAndEnabled && x.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (f == null)
            {
                Harness.Log("! no visible text field " + name);
                return;
            }
            f.ActivateInputField();
            Harness.Log("activated " + Diagnostics.Path(f));
        });
        Harness.Register("closeui", "closeui: close the map, pin naming, sign dialog, inventory and chat without a key press", _ =>
        {
            if (Minimap.instance != null)
            {
                Minimap.instance.HidePinTextInput();
                Minimap.instance.SetMapMode(Minimap.MapMode.Small);
            }
            if (TextInput.instance != null && TextInput.IsVisible())
                TextInput.instance.Hide();
            if (InventoryGui.instance != null && InventoryGui.IsVisible())
                InventoryGui.instance.Hide();
            if (Chat.instance != null)
                Chat.instance.m_input.gameObject.SetActive(false);
            if (Console.instance != null && Console.IsVisible())
                Console.instance.m_chatWindow.gameObject.SetActive(false);
            if (Menu.IsVisible())
                Menu.instance.Hide();
            EventSystem.current?.SetSelectedGameObject(null);
        });
        Harness.Register("cfgx", "cfgx <plugin GUID> <value> <key with spaces>: like cfg, for keys with spaces (VCP's \"Fix Layout Switch Focus\")", a =>
        {
            var plugin = BepInEx.Bootstrap.Chainloader.PluginInfos.Values.FirstOrDefault(p => p.Metadata.GUID.Equals(a[0], StringComparison.OrdinalIgnoreCase));
            var key = string.Join(" ", a.Skip(2));
            var def = plugin?.Instance?.Config.Keys.FirstOrDefault(k => k.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (def == null)
            {
                Harness.Log($"! no config key '{key}' in {a[0]}");
                return;
            }
            var entry = plugin.Instance.Config[def];
            entry.BoxedValue = BepInEx.Configuration.TomlTypeConverter.ConvertToValue(a[1], entry.SettingType);
            Harness.Log($"{plugin.Metadata.Name} {def.Section}.{def.Key} = {entry.BoxedValue}");
        });
        // for clean screenshots
        Harness.Register("unequip", "unequip: put away every equipped item (no build hints, no hammer panel)", _ => Player.m_localPlayer.UnequipAllItems());
        Harness.Register("chatclear", "chatclear: empty the chat window", _ =>
        {
            Chat.instance.m_chatBuffer.Clear();
            Chat.instance.UpdateChat();
        });
        Harness.Register("caretend", "caretend <name>: put the caret of the visible text field named name at the end of its text", a =>
        {
            var name = string.Join(" ", a);
            var f = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None).FirstOrDefault(x => x.isActiveAndEnabled && x.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (f == null)
            {
                Harness.Log("! no visible text field " + name);
                return;
            }
            f.MoveTextEnd(false);
        });
        Harness.Register("fg", "fg [grab]: which window is in front (and Application.isFocused); grab brings the game's window to the front", Foreground);
        Harness.AddState(() => $"focusfix kept={KeepFocus.Kept} layout='{UnityEngine.InputSystem.Keyboard.current?.keyboardLayout}' | {Focus()}");
    }

    static string Focus()
    {
        var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var f = sel != null ? sel.GetComponent<TMP_InputField>() : null;
        if (f == null)
            return $"selected={(sel != null ? sel.name : "none")}";
        return $"field={Diagnostics.Path(f)} focused={f.isFocused} text='{f.text}' caret={f.selectionStringAnchorPosition}..{f.selectionStringFocusPosition}";
    }

    static IEnumerator OsKey(string[] a)
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer)
        {
            Harness.Log("! oskey is Windows-only");
            yield break;
        }
        var codes = a[0].Split(',').Select(s => Convert.ToUInt16(s, 16)).ToArray();
        int hold = a.Length > 1 ? Harness.I(a[1]) : 60;
        IntPtr fg = GetForegroundWindow();
        GetWindowThreadProcessId(fg, out uint pid);
        if (pid != (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
        {
            Harness.Log("! the game isn't the foreground window: Windows would send the keys elsewhere");
            yield break;
        }
        foreach (var sc in codes)
        {
            uint down = Send(sc, false);
            yield return new WaitForSecondsRealtime(hold / 1000f);
            uint up = Send(sc, true);
            yield return new WaitForSecondsRealtime(0.05f);
            if (down == 0 || up == 0)
                Harness.Log($"! SendInput refused {sc:X2} (error {Marshal.GetLastWin32Error()}): a locked screen or a higher-privilege window in front");
        }
        Harness.Log($"pressed {a[0]}");
    }

    static IEnumerator Foreground(string[] a)
    {
        Harness.Log($"foreground: {Describe(GetForegroundWindow())}, Application.isFocused={Application.isFocused}");
        if (a.Length == 0 || a[0] != "grab")
            yield break;
        uint me = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        IntPtr game = IntPtr.Zero;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out uint pid);
            if (pid != me || !IsWindowVisible(h))
                return true;
            // the process can also own BepInEx's console window
            var cls = new System.Text.StringBuilder(64);
            GetClassName(h, cls, cls.Capacity);
            if (cls.ToString() != "UnityWndClass")
                return true;
            game = h;
            return false;
        }, IntPtr.Zero);
        if (game == IntPtr.Zero)
        {
            Harness.Log("! no visible game window");
            yield break;
        }
        // Windows lets a process take the foreground right after it sent input: tap Alt first
        Send(0x38, false);
        Send(0x38, true);
        bool ok = SetForegroundWindow(game);
        yield return new WaitForSecondsRealtime(0.5f);
        Harness.Log($"SetForegroundWindow={ok}; foreground now: {Describe(GetForegroundWindow())}, Application.isFocused={Application.isFocused}");
    }

    static string Describe(IntPtr h)
    {
        if (h == IntPtr.Zero)
            return "none (locked screen or a secure desktop?)";
        GetWindowThreadProcessId(h, out uint pid);
        var title = new System.Text.StringBuilder(256);
        GetWindowText(h, title, title.Capacity);
        string proc;
        try { proc = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName; }
        catch { proc = "?"; }
        return $"{proc} (pid {pid}) '{title}'";
    }

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder s, int max);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder s, int max);

    const uint KEYEVENTF_KEYUP = 0x2, KEYEVENTF_SCANCODE = 0x8;

    static uint Send(ushort scancode, bool up)
    {
        var input = new INPUT { type = 1 };
        input.u.ki = new KEYBDINPUT { wScan = scancode, dwFlags = KEYEVENTF_SCANCODE | (up ? KEYEVENTF_KEYUP : 0) };
        return SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    // sized for the largest member (MOUSEINPUT) so INPUT has the size SendInput checks
    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
