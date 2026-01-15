using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace PromptVault.Services
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int OPEN_HOTKEY_ID = 9000;
        private const int CLIPBOARD_HOTKEY_ID = 9001;

        private IntPtr windowHandle;
        private HwndSource source;
        private Window window;

        public event EventHandler OpenApplicationRequested;
        public event EventHandler QuickCaptureRequested;

        public ModifierKeys OpenModifiers { get; private set; }
        public Key OpenKey { get; private set; }
        public ModifierKeys ClipboardModifiers { get; private set; }
        public Key ClipboardKey { get; private set; }

        private bool isOpenHotkeyRegistered = false;
        private bool isClipboardHotkeyRegistered = false;

        public HotkeyManager(Window mainWindow)
        {
            window = mainWindow;
            LoadHotkeys();
        }

        public void Initialize()
        {
            windowHandle = new WindowInteropHelper(window).Handle;
            source = HwndSource.FromHwnd(windowHandle);
            source.AddHook(HwndHook);

            RegisterAllHotkeys();
        }

        private void LoadHotkeys()
        {
            try
            {
                string settingsPath = GetSettingsPath();
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("OpenHotkey="))
                        {
                            ParseHotkey(line.Substring("OpenHotkey=".Length), out var mods, out var key);
                            OpenModifiers = mods;
                            OpenKey = key;
                        }
                        else if (line.StartsWith("ClipboardHotkey="))
                        {
                            ParseHotkey(line.Substring("ClipboardHotkey=".Length), out var mods, out var key);
                            ClipboardModifiers = mods;
                            ClipboardKey = key;
                        }
                    }
                }
                else
                {
                    // Set defaults
                    OpenModifiers = ModifierKeys.Control | ModifierKeys.Shift;
                    OpenKey = Key.V;
                    ClipboardModifiers = ModifierKeys.Control | ModifierKeys.Shift;
                    ClipboardKey = Key.C;
                }
            }
            catch
            {
                // Default fallback
                OpenModifiers = ModifierKeys.Control | ModifierKeys.Shift;
                OpenKey = Key.V;
                ClipboardModifiers = ModifierKeys.Control | ModifierKeys.Shift;
                ClipboardKey = Key.C;
            }
        }

        public void SaveHotkeys()
        {
            try
            {
                string settingsPath = GetSettingsPath();
                var lines = new List<string>();

                // Preserve theme setting if it exists
                if (File.Exists(settingsPath))
                {
                    var existingLines = File.ReadAllLines(settingsPath);
                    foreach (var line in existingLines)
                    {
                        if (line.StartsWith("Theme="))
                        {
                            lines.Add(line);
                        }
                    }
                }

                lines.Add($"OpenHotkey={FormatHotkey(OpenModifiers, OpenKey)}");
                lines.Add($"ClipboardHotkey={FormatHotkey(ClipboardModifiers, ClipboardKey)}");

                string appDataPath = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                File.WriteAllLines(settingsPath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save hotkeys: {ex.Message}");
            }
        }

        public bool SetOpenHotkey(ModifierKeys modifiers, Key key)
        {
            // Unregister old hotkey
            if (isOpenHotkeyRegistered)
            {
                UnregisterHotKey(windowHandle, OPEN_HOTKEY_ID);
                isOpenHotkeyRegistered = false;
            }

            // Try to register new hotkey
            uint mod = GetModifierCode(modifiers);
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            if (RegisterHotKey(windowHandle, OPEN_HOTKEY_ID, mod, vk))
            {
                OpenModifiers = modifiers;
                OpenKey = key;
                isOpenHotkeyRegistered = true;
                SaveHotkeys();
                return true;
            }

            // If failed, re-register old hotkey
            uint oldMod = GetModifierCode(OpenModifiers);
            uint oldVk = (uint)KeyInterop.VirtualKeyFromKey(OpenKey);
            RegisterHotKey(windowHandle, OPEN_HOTKEY_ID, oldMod, oldVk);
            isOpenHotkeyRegistered = true;

            return false;
        }

        public bool SetClipboardHotkey(ModifierKeys modifiers, Key key)
        {
            // Unregister old hotkey
            if (isClipboardHotkeyRegistered)
            {
                UnregisterHotKey(windowHandle, CLIPBOARD_HOTKEY_ID);
                isClipboardHotkeyRegistered = false;
            }

            // Try to register new hotkey
            uint mod = GetModifierCode(modifiers);
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            if (RegisterHotKey(windowHandle, CLIPBOARD_HOTKEY_ID, mod, vk))
            {
                ClipboardModifiers = modifiers;
                ClipboardKey = key;
                isClipboardHotkeyRegistered = true;
                SaveHotkeys();
                return true;
            }

            // If failed, re-register old hotkey
            uint oldMod = GetModifierCode(ClipboardModifiers);
            uint oldVk = (uint)KeyInterop.VirtualKeyFromKey(ClipboardKey);
            RegisterHotKey(windowHandle, CLIPBOARD_HOTKEY_ID, oldMod, oldVk);
            isClipboardHotkeyRegistered = true;

            return false;
        }

        private void RegisterAllHotkeys()
        {
            // Register Open hotkey
            uint openMod = GetModifierCode(OpenModifiers);
            uint openVk = (uint)KeyInterop.VirtualKeyFromKey(OpenKey);
            isOpenHotkeyRegistered = RegisterHotKey(windowHandle, OPEN_HOTKEY_ID, openMod, openVk);

            // Register Clipboard hotkey
            uint clipMod = GetModifierCode(ClipboardModifiers);
            uint clipVk = (uint)KeyInterop.VirtualKeyFromKey(ClipboardKey);
            isClipboardHotkeyRegistered = RegisterHotKey(windowHandle, CLIPBOARD_HOTKEY_ID, clipMod, clipVk);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == OPEN_HOTKEY_ID)
                {
                    OpenApplicationRequested?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (id == CLIPBOARD_HOTKEY_ID)
                {
                    QuickCaptureRequested?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private uint GetModifierCode(ModifierKeys modifiers)
        {
            uint code = 0;
            if (modifiers.HasFlag(ModifierKeys.Control))
                code |= 0x0002; // MOD_CONTROL
            if (modifiers.HasFlag(ModifierKeys.Shift))
                code |= 0x0004; // MOD_SHIFT
            if (modifiers.HasFlag(ModifierKeys.Alt))
                code |= 0x0001; // MOD_ALT
            if (modifiers.HasFlag(ModifierKeys.Windows))
                code |= 0x0008; // MOD_WIN
            return code;
        }

        private void ParseHotkey(string hotkeyString, out ModifierKeys modifiers, out Key key)
        {
            modifiers = ModifierKeys.None;
            key = Key.None;

            var parts = hotkeyString.Split('+').Select(p => p.Trim()).ToArray();

            foreach (var part in parts)
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= ModifierKeys.Control;
                }
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= ModifierKeys.Shift;
                }
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= ModifierKeys.Alt;
                }
                else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                         part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= ModifierKeys.Windows;
                }
                else
                {
                    // Try to parse as key
                    if (Enum.TryParse<Key>(part, true, out Key parsedKey))
                    {
                        key = parsedKey;
                    }
                }
            }
        }

        private string FormatHotkey(ModifierKeys modifiers, Key key)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(key.ToString());

            return string.Join(" + ", parts);
        }

        public string GetOpenHotkeyString()
        {
            return FormatHotkey(OpenModifiers, OpenKey);
        }

        public string GetClipboardHotkeyString()
        {
            return FormatHotkey(ClipboardModifiers, ClipboardKey);
        }

        private string GetSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PromptVault",
                "settings.txt"
            );
        }

        public void Dispose()
        {
            if (isOpenHotkeyRegistered)
            {
                UnregisterHotKey(windowHandle, OPEN_HOTKEY_ID);
                isOpenHotkeyRegistered = false;
            }

            if (isClipboardHotkeyRegistered)
            {
                UnregisterHotKey(windowHandle, CLIPBOARD_HOTKEY_ID);
                isClipboardHotkeyRegistered = false;
            }

            source?.RemoveHook(HwndHook);
        }
    }
}