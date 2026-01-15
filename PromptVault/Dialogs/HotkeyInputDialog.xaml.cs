using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PromptVault.Dialogs
{
    public partial class HotkeyInputDialog : Window
    {
        public ModifierKeys CapturedModifiers { get; private set; }
        public Key CapturedKey { get; private set; }

        private readonly List<Key> invalidKeys = new List<Key>
        {
            Key.LeftCtrl, Key.RightCtrl,
            Key.LeftShift, Key.RightShift,
            Key.LeftAlt, Key.RightAlt,
            Key.LWin, Key.RWin,
            Key.None, Key.System
        };

        public HotkeyInputDialog()
        {
            InitializeComponent();
            CapturedModifiers = ModifierKeys.None;
            CapturedKey = Key.None;
            this.Loaded += (s, e) => this.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var modifiers = Keyboard.Modifiers;

            // Ignore modifier-only presses
            if (invalidKeys.Contains(key))
            {
                UpdateDisplay(modifiers, Key.None, false);
                return;
            }

            // Validate hotkey
            if (!ValidateHotkey(modifiers, key))
            {
                UpdateDisplay(modifiers, key, false);
                return;
            }

            // Valid hotkey
            CapturedModifiers = modifiers;
            CapturedKey = key;
            UpdateDisplay(modifiers, key, true);
            OkButton.IsEnabled = true;
        }

        private bool ValidateHotkey(ModifierKeys modifiers, Key key)
        {
            // Must have at least one modifier
            if (modifiers == ModifierKeys.None)
            {
                StatusText.Text = "❌ Must include at least one modifier (Ctrl, Shift, Alt)";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(244, 67, 54));
                return false;
            }

            // Key must be valid
            if (key == Key.None || invalidKeys.Contains(key))
            {
                StatusText.Text = "❌ Invalid key";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(244, 67, 54));
                return false;
            }

            // Check for dangerous combinations
            if (IsDangerousCombination(modifiers, key))
            {
                StatusText.Text = "⚠️ This combination might conflict with system shortcuts";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 152, 0));
                return false;
            }

            StatusText.Text = "✅ Valid hotkey combination";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(76, 175, 80));
            return true;
        }

        private bool IsDangerousCombination(ModifierKeys modifiers, Key key)
        {
            // Common system shortcuts to avoid
            if (modifiers == ModifierKeys.Alt && key == Key.F4) return true; // Close window
            if (modifiers == ModifierKeys.Alt && key == Key.Tab) return true; // Switch windows
            if (modifiers == ModifierKeys.Control && key == Key.Escape) return true; // Task Manager
            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.Escape) return true; // Task Manager
            if (modifiers == ModifierKeys.Windows && key == Key.L) return true; // Lock screen
            if (modifiers == ModifierKeys.Windows && key == Key.D) return true; // Show desktop

            return false;
        }

        private void UpdateDisplay(ModifierKeys modifiers, Key key, bool isValid)
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

            if (key != Key.None && !invalidKeys.Contains(key))
            {
                parts.Add(GetKeyDisplayName(key));
            }

            if (parts.Count > 0)
            {
                HotkeyDisplay.Text = string.Join(" + ", parts);
            }
            else
            {
                HotkeyDisplay.Text = "Press a key...";
            }
        }

        private string GetKeyDisplayName(Key key)
        {
            // Convert some keys to more readable names
            return key switch
            {
                Key.Oem1 => ";",
                Key.OemPlus => "+",
                Key.OemComma => ",",
                Key.OemMinus => "-",
                Key.OemPeriod => ".",
                Key.Oem2 => "/",
                Key.Oem3 => "`",
                Key.Oem4 => "[",
                Key.Oem5 => "\\",
                Key.Oem6 => "]",
                Key.Oem7 => "'",
                Key.Space => "Space",
                _ => key.ToString()
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}