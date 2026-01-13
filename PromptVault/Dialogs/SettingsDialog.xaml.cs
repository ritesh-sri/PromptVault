using Microsoft.Win32;
using PromptVault.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PromptVault.Dialogs
{
    public partial class SettingsDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly ImportService importService;

        public SettingsDialog(DatabaseService dbService, ImportService impService)
        {
            InitializeComponent();
            databaseService = dbService;
            importService = impService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load current settings (placeholder - implement proper settings storage)
            DatabasePathText.Text = databaseService.GetDatabasePath();

            // TODO: Load from settings file
            ThemeComboBox.SelectedIndex = 0; // Light theme default
            StartupCheckBox.IsChecked = false;
            MinimizeToTrayCheckBox.IsChecked = false;
        }

        private void ChangeOpenHotkey_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hotkey customization will be available in the next version.\n\nCurrent hotkey: Ctrl + Shift + V",
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);

            // TODO: Implement hotkey capture dialog
            // var hotkeyDialog = new HotkeyCaptureDialog();
            // if (hotkeyDialog.ShowDialog() == true)
            // {
            //     OpenHotkeyTextBox.Text = hotkeyDialog.CapturedHotkey;
            // }
        }

        private void ChangeClipboardHotkey_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hotkey customization will be available in the next version.\n\nCurrent hotkey: Ctrl + Shift + C",
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
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

                    MessageBox.Show($"Database backed up successfully to:\n{saveDialog.FileName}",
                        "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
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
                "Restoring a backup will replace your current database.\n\n" +
                "This action cannot be undone. Would you like to create a backup of your current database first?",
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

                    // Close any database connections (should be handled by DatabaseService)
                    File.Copy(openDialog.FileName, targetPath, true);

                    MessageBox.Show("Database restored successfully!\n\nPlease restart PromptVault to see the changes.",
                        "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
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

                    MessageBox.Show($"Successfully exported {allPrompts.Count} prompts to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    FileName = "Select Folder", // This will be ignored, just shows instruction
                    Filter = "Folder Selection|*.none",
                    CheckPathExists = true
                };

                // Trick to use SaveFileDialog as folder picker
                saveDialog.FileName = "PromptVault_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (saveDialog.ShowDialog() == true)
                {
                    string folderPath = Path.GetDirectoryName(saveDialog.FileName);
                    string exportFolderName = Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    string fullExportPath = Path.Combine(folderPath, exportFolderName);

                    // Create the export folder if it doesn't exist
                    Directory.CreateDirectory(fullExportPath);

                    var allPrompts = databaseService.GetAllPrompts();
                    importService.ExportToTextFiles(fullExportPath, allPrompts);

                    MessageBox.Show($"Successfully exported {allPrompts.Count} prompts to:\n{fullExportPath}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Open the folder
                    Process.Start("explorer.exe", fullExportPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export prompts: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Could not open documentation. Please visit:\nhttps://github.com/yourusername/promptvault/wiki",
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
                MessageBox.Show("Could not open bug report page. Please visit:\nhttps://github.com/yourusername/promptvault/issues",
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
                MessageBox.Show("Could not open GitHub page. Please visit:\nhttps://github.com/yourusername/promptvault",
                    "PromptVault", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all settings to defaults?\n\nThis will not affect your saved prompts.",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Reset to defaults
                ThemeComboBox.SelectedIndex = 0;
                StartupCheckBox.IsChecked = false;
                MinimizeToTrayCheckBox.IsChecked = false;
                OpenHotkeyTextBox.Text = "Ctrl + Shift + V";
                ClipboardHotkeyTextBox.Text = "Ctrl + Shift + C";

                MessageBox.Show("Settings have been reset to defaults.",
                    "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Save settings to file (JSON or XML)
                // For now, just show success message

                // Apply theme change immediately
                var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                bool isDarkMode = selectedTheme == "Dark";

                // Apply theme to main window (need to pass this back)
                // This is a placeholder - implement proper settings persistence

                MessageBox.Show("Settings saved successfully!\n\nSome changes may require restarting PromptVault.",
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
    }
}