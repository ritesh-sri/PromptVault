using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using PromptVault.Services;
using PromptVault.Dialogs;
using System.Collections.Generic;
using PromptVault.Models;
using System.Windows.Controls;
using System.Linq;

namespace PromptVault
{
    public partial class MainWindow : Window
    {
        private bool isDarkMode = false;
        private GlobalHotkey globalHotkey;
        private DatabaseService databaseService;
        private ImportService importService;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            databaseService = new DatabaseService();
            importService = new ImportService(databaseService);

            // Initialize hotkey after window is loaded
            this.Loaded += MainWindow_Loaded;

            LoadTheme();
            LoadPrompts();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Register hotkey after window handle is created
            InitializeHotkey();
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
                // Show warning but don't crash the app
                MessageBox.Show($"Could not register global hotkey (Ctrl+Shift+V).\n\nThe app will still work, but you'll need to open it manually.\n\nError: {ex.Message}",
                    "PromptVault - Hotkey Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                resources["SurfaceBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E3E"));
                resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0B0B0"));
                resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4FC3F7"));
                resources["HoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383838"));
            }
            else
            {
                // Apply Light Theme
                resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                resources["SurfaceBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
                resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121"));
                resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575"));
                resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                resources["HoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE"));
            }
        }

        private void NewPromptButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditPromptDialog();
            if (dialog.ShowDialog() == true)
            {
                // Save the new prompt
                databaseService.AddPrompt(dialog.EditingPrompt);
                MessageBox.Show("Prompt saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload prompts
                LoadPrompts();
            }
        }

        private void AddFromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();

                    // Open dialog with clipboard content pre-filled
                    var dialog = new AddEditPromptDialog();
                    dialog.ContentTextBox.Text = clipboardText;
                    dialog.TitleTextBox.Text = "Clipboard Prompt - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    dialog.TitleTextBox.SelectAll();
                    dialog.TitleTextBox.Focus();

                    if (dialog.ShowDialog() == true)
                    {
                        databaseService.AddPrompt(dialog.EditingPrompt);
                        MessageBox.Show("Prompt saved from clipboard!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadPrompts();
                    }
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
            var dialog = new ImportWizardDialog(databaseService, importService);
            if (dialog.ShowDialog() == true)
            {
                LoadPrompts();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(databaseService, importService);
            dialog.ShowDialog();
        }

        private void LoadPrompts()
        {
            try
            {
                var prompts = databaseService.GetAllPrompts();

                // Update count
                PromptCountText.Text = $"({prompts.Count})";

                // Clear existing prompt cards (keep only static samples for now)
                PromptsContainer.Children.Clear();

                // Generate prompt cards dynamically
                foreach (var prompt in prompts)
                {
                    PromptsContainer.Children.Add(CreatePromptCard(prompt));
                }

                // If no prompts, show empty state
                if (prompts.Count == 0)
                {
                    var emptyText = new TextBlock
                    {
                        Text = "No prompts yet. Click '➕ New Prompt' to get started!",
                        FontSize = 16,
                        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 40, 0, 0)
                    };
                    PromptsContainer.Children.Add(emptyText);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading prompts: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private System.Windows.Controls.Border CreatePromptCard(Prompt prompt)
        {
            var card = new System.Windows.Controls.Border
            {
                Style = (Style)Application.Current.Resources["PromptCardStyle"],
                Tag = prompt // Store prompt object for later use
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Title and Favorite
            var titleGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleIcon = prompt.Title.StartsWith("📝") || prompt.Title.StartsWith("✍️") ||
                           prompt.Title.StartsWith("🔍") || prompt.Title.StartsWith("💡")
                           ? "" : "📝 ";

            var titleText = new TextBlock
            {
                Text = titleIcon + prompt.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"]
            };
            Grid.SetColumn(titleText, 0);

            var favoriteButton = new Button
            {
                Content = prompt.IsFavorite ? "⭐" : "☆",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 18,
                Cursor = Cursors.Hand,
                Tag = prompt
            };
            favoriteButton.Click += FavoriteButton_Click;
            Grid.SetColumn(favoriteButton, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(favoriteButton);
            Grid.SetRow(titleGrid, 0);

            // Row 1: Tags and Model
            var tagsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

            // Model badge
            var modelBorder = new System.Windows.Controls.Border
            {
                Background = GetModelColor(prompt.AIProvider),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var modelText = new TextBlock
            {
                Text = prompt.ModelVersion,
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.White
            };
            modelBorder.Child = modelText;
            tagsPanel.Children.Add(modelBorder);

            // Tags
            foreach (var tag in prompt.Tags.Take(3))
            {
                var tagBorder = new System.Windows.Controls.Border
                {
                    Background = GetTagColor(tag),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                var tagText = new TextBlock
                {
                    Text = "🏷️ " + tag,
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.White
                };
                tagBorder.Child = tagText;
                tagsPanel.Children.Add(tagBorder);
            }
            Grid.SetRow(tagsPanel, 1);

            // Row 2: Preview
            var previewText = new TextBlock
            {
                Text = prompt.GetPreview(150),
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(previewText, 2);

            // Row 3: Footer
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dateText = new TextBlock
            {
                Text = "📅 " + prompt.GetRelativeDate(),
                FontSize = 12,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 0);

            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // Copy button
            var copyButton = new Button
            {
                Content = "📋 Copy",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 12,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = prompt
            };
            copyButton.Click += CopyButton_Click;

            // Edit button
            var editButton = new Button
            {
                Content = "✏️",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 16,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = prompt
            };
            editButton.Click += EditButton_Click;

            // Delete button
            var deleteButton = new Button
            {
                Content = "🗑️",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 16,
                Cursor = Cursors.Hand,
                Tag = prompt
            };
            deleteButton.Click += DeleteButton_Click;

            buttonsPanel.Children.Add(copyButton);
            buttonsPanel.Children.Add(editButton);
            buttonsPanel.Children.Add(deleteButton);
            Grid.SetColumn(buttonsPanel, 1);

            footerGrid.Children.Add(dateText);
            footerGrid.Children.Add(buttonsPanel);
            Grid.SetRow(footerGrid, 3);

            grid.Children.Add(titleGrid);
            grid.Children.Add(tagsPanel);
            grid.Children.Add(previewText);
            grid.Children.Add(footerGrid);

            card.Child = grid;
            return card;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var prompt = button?.Tag as Prompt;

                if (prompt != null)
                {
                    Clipboard.SetText(prompt.Content);

                    // Update usage count
                    prompt.UsageCount++;
                    databaseService.UpdatePrompt(prompt);

                    // Show feedback
                    button.Content = "✓ Copied!";
                    var originalContent = "📋 Copy";

                    // Reset after 2 seconds
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, args) =>
                    {
                        button.Content = originalContent;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var prompt = button?.Tag as Prompt;

            if (prompt != null)
            {
                var dialog = new AddEditPromptDialog(prompt);
                if (dialog.ShowDialog() == true)
                {
                    databaseService.UpdatePrompt(dialog.EditingPrompt);
                    MessageBox.Show("Prompt updated successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPrompts();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var prompt = button?.Tag as Prompt;

            if (prompt != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this prompt?\n\n\"{prompt.Title}\"\n\nThis action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    databaseService.DeletePrompt(prompt.Id);
                    MessageBox.Show("Prompt deleted successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPrompts();
                }
            }
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var prompt = button?.Tag as Prompt;

            if (prompt != null)
            {
                prompt.IsFavorite = !prompt.IsFavorite;
                button.Content = prompt.IsFavorite ? "⭐" : "☆";
                databaseService.UpdatePrompt(prompt);
            }
        }

        private System.Windows.Media.Brush GetModelColor(string aiProvider)
        {
            return aiProvider switch
            {
                "ChatGPT" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32")),
                "Claude" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")),
                "Gemini" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B1FA2")),
                "Copilot" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575"))
            };
        }

        private System.Windows.Media.Brush GetTagColor(string tag)
        {
            // Use a hash of the tag name to get consistent colors
            int hash = tag.GetHashCode();
            var colors = new[]
            {
                "#E65100", "#C2185B", "#1976D2", "#7B1FA2", "#388E3C",
                "#F57C00", "#00796B", "#D32F2F", "#455A64", "#F57F17"
            };
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[Math.Abs(hash) % colors.Length]));
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