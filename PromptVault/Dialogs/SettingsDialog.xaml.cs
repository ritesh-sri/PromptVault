using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PromptVault.Services;

namespace PromptVault.Dialogs
{
    public partial class SettingsDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly ImportService importService;
        private readonly ThemeManager themeManager;
        private readonly HotkeyManager hotkeyManager;
        private readonly SystemTrayManager trayManager;
        private string currentPanel = "General";

        public SettingsDialog(DatabaseService dbService, ImportService impService, HotkeyManager hkManager, SystemTrayManager sysTrayManager)
        {
            InitializeComponent();
            databaseService = dbService;
            importService = impService;
            themeManager = ThemeManager.Instance;
            hotkeyManager = hkManager;
            trayManager = sysTrayManager;

            LoadSettings();
            UpdateNavigationHighlight();
        }

        private void LoadSettings()
        {
            // Load database path
            DatabasePathText.Text = databaseService.GetDatabasePath();

            // Load theme setting
            if (themeManager.IsDarkMode)
            {
                ThemeComboBox.SelectedIndex = 1; // Dark Theme
            }
            else
            {
                ThemeComboBox.SelectedIndex = 0; // Light Theme
            }

            // Load other settings
            StartupCheckBox.IsChecked = CheckStartupEnabled();
            MinimizeToTrayCheckBox.IsChecked = trayManager.IsMinimizeToTrayEnabled;
            AutoBackupCheckBox.IsChecked = true;

            // Load hotkeys
            OpenHotkeyTextBox.Text = hotkeyManager.GetOpenHotkeyString();
            ClipboardHotkeyTextBox.Text = hotkeyManager.GetClipboardHotkeyString();

            // Subscribe to theme combo box changes
            ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox == null || !this.IsLoaded) return;

            // Apply theme immediately when changed
            bool isDark = ThemeComboBox.SelectedIndex == 1;
            themeManager.SetTheme(isDark);
        }

        private bool CheckStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue("PromptVault") != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        string exePath = Process.GetCurrentProcess().MainModule.FileName;
                        key?.SetValue("PromptVault", $"\"{exePath}\"");
                    }
                    else
                    {
                        key?.DeleteValue("PromptVault", false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update startup settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Navigation methods
        private void NavigateToGeneral_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("General");
        }

        private void NavigateToHotkeys_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("Hotkeys");
        }

        private void NavigateToData_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("Data");
        }

        private void NavigateToAbout_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("About");
        }

        private void ShowPanel(string panelName)
        {
            currentPanel = panelName;

            // Hide all panels
            GeneralPanel.Visibility = Visibility.Collapsed;
            HotkeysPanel.Visibility = Visibility.Collapsed;
            DataPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;

            // Show selected panel
            switch (panelName)
            {
                case "General":
                    GeneralPanel.Visibility = Visibility.Visible;
                    break;
                case "Hotkeys":
                    HotkeysPanel.Visibility = Visibility.Visible;
                    break;
                case "Data":
                    DataPanel.Visibility = Visibility.Visible;
                    break;
                case "About":
                    AboutPanel.Visibility = Visibility.Visible;
                    break;
            }

            UpdateNavigationHighlight();
        }

        private void UpdateNavigationHighlight()
        {
            // Reset all buttons
            ResetButtonStyle(GeneralNavButton);
            ResetButtonStyle(HotkeysNavButton);
            ResetButtonStyle(DataNavButton);
            ResetButtonStyle(AboutNavButton);

            // Highlight active button
            Button activeButton = currentPanel switch
            {
                "General" => GeneralNavButton,
                "Hotkeys" => HotkeysNavButton,
                "Data" => DataNavButton,
                "About" => AboutNavButton,
                _ => GeneralNavButton
            };

            HighlightButton(activeButton);
        }

        private void ResetButtonStyle(Button button)
        {
            button.Opacity = 0.7;
            button.FontWeight = FontWeights.Normal;
        }

        private void HighlightButton(Button button)
        {
            button.Opacity = 1.0;
            button.FontWeight = FontWeights.SemiBold;
        }

        private void ChangeOpenHotkey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HotkeyInputDialog();
            if (dialog.ShowDialog() == true)
            {
                if (hotkeyManager.SetOpenHotkey(dialog.CapturedModifiers, dialog.CapturedKey))
                {
                    OpenHotkeyTextBox.Text = hotkeyManager.GetOpenHotkeyString();
                    MessageBox.Show("✅ Hotkey Updated!\n\nYour new hotkey has been set successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("❌ Failed to Set Hotkey\n\n" +
                                   "This key combination might already be in use by another application.\n" +
                                   "Please try a different combination.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ChangeClipboardHotkey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HotkeyInputDialog();
            if (dialog.ShowDialog() == true)
            {
                if (hotkeyManager.SetClipboardHotkey(dialog.CapturedModifiers, dialog.CapturedKey))
                {
                    ClipboardHotkeyTextBox.Text = hotkeyManager.GetClipboardHotkeyString();
                    MessageBox.Show("✅ Hotkey Updated!\n\nYour new hotkey has been set successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("❌ Failed to Set Hotkey\n\n" +
                                   "This key combination might already be in use by another application.\n" +
                                   "Please try a different combination.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OpenDatabaseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dbPath = databaseService.GetDatabasePath();
                string folderPath = Path.GetDirectoryName(dbPath);

                if (Directory.Exists(folderPath))
                {
                    Process.Start("explorer.exe", folderPath);
                }
                else
                {
                    MessageBox.Show("Database folder not found.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Backup Database",
                    Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
                    FileName = $"promptvault_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                    DefaultExt = ".db"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string sourcePath = databaseService.GetDatabasePath();
                    File.Copy(sourcePath, saveDialog.FileName, true);

                    MessageBox.Show($"✅ Backup Complete!\n\n" +
                                   $"Your database has been backed up to:\n" +
                                   $"{saveDialog.FileName}\n\n" +
                                   $"Keep this file safe!",
                        "Backup Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to backup database: {ex.Message}",
                    "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreDatabase_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ Warning: Restore Database\n\n" +
                "Restoring a backup will REPLACE your current database.\n" +
                "All current prompts will be lost!\n\n" +
                "Would you like to create a backup of your current database first?",
                "Restore Database",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
            {
                BackupDatabase_Click(sender, e);
            }

            try
            {
                var openDialog = new OpenFileDialog
                {
                    Title = "Select Database Backup",
                    Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (openDialog.ShowDialog() == true)
                {
                    string targetPath = databaseService.GetDatabasePath();
                    File.Copy(openDialog.FileName, targetPath, true);

                    MessageBox.Show("✅ Restore Complete!\n\n" +
                                   "Database restored successfully!\n\n" +
                                   "Please restart PromptVault to see your restored prompts.",
                        "Restore Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restore database: {ex.Message}",
                    "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportAllCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export All Prompts to CSV",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"promptvault_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var allPrompts = databaseService.GetAllPrompts();
                    importService.ExportToCsv(saveDialog.FileName, allPrompts);

                    MessageBox.Show($"✅ Export Complete!\n\n" +
                                   $"Successfully exported {allPrompts.Count} prompts to:\n" +
                                   $"{saveDialog.FileName}",
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export prompts: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportAllTXT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Select Export Folder",
                    FileName = "Select this folder",
                    Filter = "Folder Selection|*.folder",
                    CheckPathExists = true
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string folderPath = Path.GetDirectoryName(saveDialog.FileName);

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    var allPrompts = databaseService.GetAllPrompts();
                    importService.ExportToTextFiles(folderPath, allPrompts);

                    MessageBox.Show($"✅ Export Complete!\n\n" +
                                   $"Successfully exported {allPrompts.Count} prompts to:\n" +
                                   $"{folderPath}",
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start("explorer.exe", folderPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export prompts: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAllData_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ DANGER: Delete All Data\n\n" +
                "This will permanently delete ALL your prompts!\n" +
                "This action CANNOT be undone!\n\n" +
                "Are you absolutely sure?",
                "Confirm Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var confirmResult = MessageBox.Show(
                    "⚠️ FINAL WARNING\n\n" +
                    "Click YES to permanently delete everything.\n" +
                    "Click NO to cancel.",
                    "Final Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Stop);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        string dbPath = databaseService.GetDatabasePath();

                        string backupPath = Path.Combine(
                            Path.GetDirectoryName(dbPath),
                            $"pre_delete_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                        );
                        File.Copy(dbPath, backupPath, true);

                        File.Delete(dbPath);

                        MessageBox.Show("✅ All Data Deleted\n\n" +
                                       "All prompts have been deleted.\n\n" +
                                       $"A backup was created at:\n{backupPath}\n\n" +
                                       "Please restart PromptVault.",
                            "Data Cleared", MessageBoxButton.OK, MessageBoxImage.Information);

                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to clear data: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OpenDocumentation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/yourusername/promptvault/wiki",
                    UseShellExecute = true
                });
            }
            catch
            {
                Clipboard.SetText("https://github.com/yourusername/promptvault/wiki");
                MessageBox.Show("📋 Link copied to clipboard!\n\n" +
                               "https://github.com/yourusername/promptvault/wiki",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ReportBug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/yourusername/promptvault/issues/new",
                    UseShellExecute = true
                });
            }
            catch
            {
                Clipboard.SetText("https://github.com/yourusername/promptvault/issues");
                MessageBox.Show("📋 Link copied to clipboard!\n\n" +
                               "https://github.com/yourusername/promptvault/issues",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void StarOnGitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/yourusername/promptvault",
                    UseShellExecute = true
                });
            }
            catch
            {
                Clipboard.SetText("https://github.com/yourusername/promptvault");
                MessageBox.Show("📋 Link copied to clipboard!\n\n" +
                               "https://github.com/yourusername/promptvault",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🔄 Check for Updates\n\n" +
                           "You are using version 0.1.1\n\n" +
                           "Automatic update checking is coming in version 0.2.0!\n\n" +
                           "For now, visit our GitHub page to check for updates.",
                "Check for Updates", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset all settings to defaults?\n\n" +
                "This will not affect your saved prompts.",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ThemeComboBox.SelectedIndex = 0; // Light theme
                StartupCheckBox.IsChecked = false;
                MinimizeToTrayCheckBox.IsChecked = false;
                AutoBackupCheckBox.IsChecked = true;
                OpenHotkeyTextBox.Text = "Ctrl + Shift + V";
                ClipboardHotkeyTextBox.Text = "Ctrl + Shift + C";

                MessageBox.Show("✅ Settings Reset\n\n" +
                               "All settings have been reset to defaults.",
                    "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Apply startup setting
                SetStartup(StartupCheckBox.IsChecked ?? false);

                // Apply tray setting
                trayManager.SetMinimizeToTray(MinimizeToTrayCheckBox.IsChecked ?? false);
                SaveTraySettings();

                // Theme is already applied by ThemeComboBox_SelectionChanged
                // Hotkeys are already applied when changed

                MessageBox.Show("✅ Settings Saved!\n\n" +
                               "Your preferences have been saved successfully.",
                    "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveTraySettings()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptVault",
                    "settings.txt"
                );

                var lines = new List<string>();

                // Preserve existing settings
                if (System.IO.File.Exists(settingsPath))
                {
                    var existingLines = System.IO.File.ReadAllLines(settingsPath);
                    foreach (var line in existingLines)
                    {
                        if (!line.StartsWith("MinimizeToTray="))
                        {
                            lines.Add(line);
                        }
                    }
                }

                // Add tray setting
                lines.Add($"MinimizeToTray={MinimizeToTrayCheckBox.IsChecked ?? false}");

                string appDataPath = System.IO.Path.GetDirectoryName(settingsPath);
                if (!System.IO.Directory.Exists(appDataPath))
                {
                    System.IO.Directory.CreateDirectory(appDataPath);
                }

                System.IO.File.WriteAllLines(settingsPath, lines);
            }
            catch { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}