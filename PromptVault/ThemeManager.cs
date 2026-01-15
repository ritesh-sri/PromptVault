using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace PromptVault.Services
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private bool _isDarkMode;

        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event EventHandler<bool> ThemeChanged;

        private ThemeManager()
        {
            _isDarkMode = LoadThemePreference();
        }

        public bool IsDarkMode => _isDarkMode;

        public void ToggleTheme()
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme(_isDarkMode);
            SaveThemePreference(_isDarkMode);
            ThemeChanged?.Invoke(this, _isDarkMode);
        }

        public void SetTheme(bool isDark)
        {
            if (_isDarkMode != isDark)
            {
                _isDarkMode = isDark;
                ApplyTheme(_isDarkMode);
                SaveThemePreference(_isDarkMode);
                ThemeChanged?.Invoke(this, _isDarkMode);
            }
        }

        public void InitializeTheme()
        {
            ApplyTheme(_isDarkMode);
        }

        private void ApplyTheme(bool isDark)
        {
            var resources = Application.Current.Resources;

            if (isDark)
            {
                // Dark Theme
                SetColor(resources, "BackgroundBrush", "#1E1E1E");
                SetColor(resources, "SurfaceBrush", "#2D2D2D");
                SetColor(resources, "BorderBrush", "#3E3E3E");
                SetColor(resources, "TextBrush", "#E0E0E0");
                SetColor(resources, "TextSecondaryBrush", "#B0B0B0");
                SetColor(resources, "AccentBrush", "#4FC3F7");
                SetColor(resources, "HoverBrush", "#383838");
                SetColor(resources, "CardBrush", "#252525");
            }
            else
            {
                // Light Theme
                SetColor(resources, "BackgroundBrush", "#FFFFFF");
                SetColor(resources, "SurfaceBrush", "#F5F5F5");
                SetColor(resources, "BorderBrush", "#E0E0E0");
                SetColor(resources, "TextBrush", "#212121");
                SetColor(resources, "TextSecondaryBrush", "#757575");
                SetColor(resources, "AccentBrush", "#2196F3");
                SetColor(resources, "HoverBrush", "#EEEEEE");
                SetColor(resources, "CardBrush", "#FAFAFA");
            }

            // Update all windows
            foreach (Window window in Application.Current.Windows)
            {
                window.Background = (Brush)resources["BackgroundBrush"];
                window.UpdateLayout();
            }
        }

        private void SetColor(ResourceDictionary resources, string key, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);

            if (resources.Contains(key))
            {
                if (resources[key] is SolidColorBrush brush && brush.IsFrozen)
                {
                    resources[key] = new SolidColorBrush(color);
                }
                else if (resources[key] is SolidColorBrush mutableBrush)
                {
                    mutableBrush.Color = color;
                }
            }
            else
            {
                resources[key] = new SolidColorBrush(color);
            }
        }

        private bool LoadThemePreference()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptVault",
                    "settings.txt"
                );

                if (File.Exists(settingsPath))
                {
                    string content = File.ReadAllText(settingsPath);
                    return content.Contains("Theme=Dark");
                }
            }
            catch { }

            return false; // Default to light theme
        }

        private void SaveThemePreference(bool isDark)
        {
            try
            {
                // Declare settingsPath at the beginning of the method
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptVault",
                    "settings.txt"
                );

                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptVault"
                );

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                // Preserve existing settings
                if (File.Exists(settingsPath))
                {
                    string content = File.ReadAllText(settingsPath);
                    var lines = content.Split('\n');
                    var otherSettings = new List<string>();

                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("Theme="))
                        {
                            otherSettings.Add(line.Trim());
                        }
                    }

                    otherSettings.Add($"Theme={(isDark ? "Dark" : "Light")}");
                    File.WriteAllLines(settingsPath, otherSettings);
                }
                else
                {
                    File.WriteAllText(settingsPath, $"Theme={(isDark ? "Dark" : "Light")}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save theme: {ex.Message}");
            }
        }
    }
}