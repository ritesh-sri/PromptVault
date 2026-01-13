using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace PromptVault
{
    public partial class MainWindow : Window
    {
        private bool isDarkMode = false;
        private GlobalHotkey globalHotkey;

        public MainWindow()
        {
            InitializeComponent();
            InitializeHotkey();
            LoadTheme();
        }

        private void InitializeHotkey()
        {
            // Register Ctrl+Shift+V to open the app
            try
            {
                globalHotkey = new GlobalHotkey(ModifierKeys.Control | ModifierKeys.Shift, Key.V, this);
                globalHotkey.Register();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register global hotkey: {ex.Message}",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadTheme()
        {
            // Load saved theme preference (placeholder for settings)
            // For now, starts with light theme
            ApplyTheme(isDarkMode);
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;
            ApplyTheme(isDarkMode);
            ThemeToggleButton.Content = isDarkMode ? "☀️" : "🌙";
        }

        private void ApplyTheme(bool darkMode)
        {
            var resources = Application.Current.Resources;

            if (darkMode)
            {
                // Apply Dark Theme
                resources["BackgroundBrush"] = resources["DarkBackground"];
                resources["SurfaceBrush"] = resources["DarkSurface"];
                resources["BorderBrush"] = resources["DarkBorder"];
                resources["TextBrush"] = resources["DarkText"];
                resources["TextSecondaryBrush"] = resources["DarkTextSecondary"];
                resources["AccentBrush"] = resources["DarkAccent"];
                resources["HoverBrush"] = resources["DarkHover"];
            }
            else
            {
                // Apply Light Theme
                resources["BackgroundBrush"] = resources["LightBackground"];
                resources["SurfaceBrush"] = resources["LightSurface"];
                resources["BorderBrush"] = resources["LightBorder"];
                resources["TextBrush"] = resources["LightText"];
                resources["TextSecondaryBrush"] = resources["LightTextSecondary"];
                resources["AccentBrush"] = resources["LightAccent"];
                resources["HoverBrush"] = resources["LightHover"];
            }
        }

        private void NewPromptButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open Add/Edit Prompt Dialog
            MessageBox.Show("Open New Prompt Dialog (To be implemented)",
                "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddFromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();
                    // TODO: Open quick add dialog with clipboard content
                    MessageBox.Show($"Clipboard content captured:\n\n{clipboardText.Substring(0, Math.Min(100, clipboardText.Length))}...",
                        "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Clipboard is empty or doesn't contain text.",
                        "PromptVault", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing clipboard: {ex.Message}",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open Import Dialog (CSV/Text files)
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Prompts",
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show($"Selected files: {string.Join(", ", dialog.FileNames)}\n\n(Import functionality to be implemented)",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open Settings Dialog
            MessageBox.Show("Settings Dialog (To be implemented)\n\n• Customize hotkeys\n• Export/Import settings\n• Database backup",
                "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            globalHotkey?.Unregister();
            base.OnClosed(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // Optional: Minimize to system tray
                // Hide();
            }
            base.OnStateChanged(e);
        }
    }

    // Global Hotkey Handler
    public class GlobalHotkey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private IntPtr handle;
        private Window window;

        public GlobalHotkey(ModifierKeys modifiers, Key key, Window window)
        {
            this.window = window;
            this.handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            var source = System.Windows.Interop.HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);
        }

        public void Register()
        {
            uint modifiers = 0;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                modifiers |= 0x0002; // MOD_CONTROL
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                modifiers |= 0x0004; // MOD_SHIFT

            // Ctrl+Shift = 0x0002 | 0x0004 = 0x0006
            // V key = 0x56
            RegisterHotKey(handle, HOTKEY_ID, 0x0006, 0x56);
        }

        public void Unregister()
        {
            UnregisterHotKey(handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // Show and activate the window
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                window.Show();
                window.Activate();
                window.Focus();

                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}