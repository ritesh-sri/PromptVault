using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace PromptVault.Services
{
    public class KeyboardShortcutsManager
    {
        private readonly Window _window;

        public event EventHandler OnNewPromptRequested;
        public event EventHandler OnDeleteSelectedRequested;
        public event EventHandler OnSearchFocusRequested;
        public event EventHandler OnRefreshRequested;
        public event EventHandler OnToggleFavoritesRequested;
        public event EventHandler OnCopySelectedRequested;
        public event EventHandler OnEditSelectedRequested;
        public event EventHandler OnOpenSettingsRequested;
        public event EventHandler OnOpenStatisticsRequested;
        public event EventHandler OnImportRequested;
        public event EventHandler OnExportRequested;
        public event EventHandler<int> OnQuickFilterRequested;

        public KeyboardShortcutsManager(Window mainWindow)
        {
            _window = mainWindow;
            _window.PreviewKeyDown += HandleKeyDown;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            bool isInTextInput = e.OriginalSource is TextBox || e.OriginalSource is ComboBox;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                ProcessControlKey(e, isInTextInput);
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                ProcessControlShiftKey(e);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                ProcessAltKey(e);
            }
            else if (Keyboard.Modifiers == ModifierKeys.None && !isInTextInput)
            {
                ProcessSingleKey(e);
            }
        }

        private void ProcessControlKey(KeyEventArgs e, bool isInTextInput)
        {
            switch (e.Key)
            {
                case Key.N:
                    OnNewPromptRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.D:
                    if (!isInTextInput)
                    {
                        OnDeleteSelectedRequested?.Invoke(this, EventArgs.Empty);
                        e.Handled = true;
                    }
                    break;

                case Key.F:
                    OnSearchFocusRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.R:
                    OnRefreshRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.E:
                    if (!isInTextInput)
                    {
                        OnEditSelectedRequested?.Invoke(this, EventArgs.Empty);
                        e.Handled = true;
                    }
                    break;

                case Key.C:
                    if (!isInTextInput)
                    {
                        OnCopySelectedRequested?.Invoke(this, EventArgs.Empty);
                        e.Handled = true;
                    }
                    break;

                case Key.OemComma:
                    OnOpenSettingsRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.I:
                    OnImportRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.S:
                    if (!isInTextInput)
                    {
                        OnOpenStatisticsRequested?.Invoke(this, EventArgs.Empty);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void ProcessControlShiftKey(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    OnToggleFavoritesRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.E:
                    OnExportRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.S:
                    OnOpenStatisticsRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;
            }
        }

        private void ProcessAltKey(KeyEventArgs e)
        {
            int filterIndex = 0;

            switch (e.Key)
            {
                case Key.D1:
                case Key.NumPad1:
                    filterIndex = 1;
                    break;
                case Key.D2:
                case Key.NumPad2:
                    filterIndex = 2;
                    break;
                case Key.D3:
                case Key.NumPad3:
                    filterIndex = 3;
                    break;
                case Key.D4:
                case Key.NumPad4:
                    filterIndex = 4;
                    break;
                case Key.D5:
                case Key.NumPad5:
                    filterIndex = 5;
                    break;
            }

            if (filterIndex > 0)
            {
                OnQuickFilterRequested?.Invoke(this, filterIndex);
                e.Handled = true;
            }
        }

        private void ProcessSingleKey(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    OnDeleteSelectedRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.F5:
                    OnRefreshRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;

                case Key.Escape:
                    OnSearchFocusRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;
            }
        }

        public static string GetShortcutsHelp()
        {
            return @"⌨️ KEYBOARD SHORTCUTS

📝 PROMPT MANAGEMENT
• Ctrl+N          New Prompt
• Ctrl+E          Edit Selected Prompt
• Ctrl+D / Del    Delete Selected Prompt
• Ctrl+C          Copy Selected Prompt
• Ctrl+Shift+F    Toggle Favorites Filter

🔍 SEARCH & NAVIGATION
• Ctrl+F          Focus Search Box
• Escape          Clear Search
• Alt+1-5         Quick Filter (Platform 1-5)

🔄 GENERAL
• Ctrl+R / F5     Refresh Prompt List
• Ctrl+I          Open Import Wizard
• Ctrl+Shift+E    Export Prompts
• Ctrl+S          Open Statistics
• Ctrl+,          Open Settings

💡 TIP: Hover over any prompt card for a full preview!";
        }

        public void Unregister()
        {
            if (_window != null)
            {
                _window.PreviewKeyDown -= HandleKeyDown;
            }
        }
    }
}