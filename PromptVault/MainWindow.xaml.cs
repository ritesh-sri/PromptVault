using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
        private HotkeyManager hotkeyManager;
        private SystemTrayManager trayManager;
        private DatabaseService databaseService;
        private ImportService importService;
        private ThemeManager themeManager;
        private List<Prompt> allPrompts;
        private string currentAIFilter = "All";
        private string currentModelFilter = "All";
        private string currentTagFilter = "All";
        private bool showFavoritesOnly = false;
        private bool hasShownFirstMinimizeTip = false;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            databaseService = new DatabaseService();
            importService = new ImportService(databaseService);
            themeManager = ThemeManager.Instance;
            trayManager = new SystemTrayManager(this);
            hotkeyManager = new HotkeyManager(this);

            // Initialize theme
            themeManager.InitializeTheme();
            UpdateThemeButton();

            // Subscribe to theme changes
            themeManager.ThemeChanged += (s, isDark) =>
            {
                UpdateThemeButton();
                if (allPrompts != null && allPrompts.Count > 0)
                {
                    ApplyFiltersAndDisplay();
                }
            };

            // Subscribe to tray events
            trayManager.OpenRequested += (s, e) => RestoreFromTray();
            trayManager.ExitRequested += (s, e) => ExitApplication();

            // Load settings
            LoadTraySettings();

            // Window events
            this.Loaded += MainWindow_Loaded;
            this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;

            LoadPrompts();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize hotkeys after window handle is created
            hotkeyManager.Initialize();

            // Subscribe to hotkey events
            hotkeyManager.OpenApplicationRequested += (s, ev) =>
            {
                Dispatcher.Invoke(() => RestoreFromTray());
            };

            hotkeyManager.QuickCaptureRequested += (s, ev) =>
            {
                Dispatcher.Invoke(() => QuickCaptureFromClipboard());
            };

            SetupFilters();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (trayManager.IsMinimizeToTrayEnabled)
                {
                    trayManager.HandleWindowStateChange(WindowState);

                    if (!hasShownFirstMinimizeTip)
                    {
                        trayManager.ShowBalloonTip(
                            "PromptVault",
                            $"Running in background. Press {hotkeyManager.GetOpenHotkeyString()} to open."
                        );
                        hasShownFirstMinimizeTip = true;
                    }
                }
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // If minimize to tray is enabled, just minimize instead of closing
            if (trayManager.IsMinimizeToTrayEnabled)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                Hide();
                trayManager.ShowInTray();
            }
        }

        private void LoadTraySettings()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptVault",
                    "settings.txt"
                );

                if (System.IO.File.Exists(settingsPath))
                {
                    var lines = System.IO.File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("MinimizeToTray="))
                        {
                            bool enabled = line.Contains("True");
                            trayManager.SetMinimizeToTray(enabled);
                            if (enabled)
                            {
                                trayManager.ShowInTray();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void RestoreFromTray()
        {
            trayManager.RestoreWindow();
        }

        private void ExitApplication()
        {
            trayManager.SetMinimizeToTray(false); // Disable to allow actual exit
            Application.Current.Shutdown();
        }

        private void QuickCaptureFromClipboard()
        {
            RestoreFromTray();
            AddFromClipboardButton_Click(null, null);
        }

        private void UpdateThemeButton()
        {
            ThemeToggleButton.Content = themeManager.IsDarkMode ? "☀️" : "🌙";
            ThemeToggleButton.ToolTip = themeManager.IsDarkMode ?
                "Switch to Light Mode" : "Switch to Dark Mode";
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            themeManager.ToggleTheme();

            if (allPrompts != null && allPrompts.Count > 0)
            {
                ApplyFiltersAndDisplay();
            }
        }

        private void NewPromptButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditPromptDialog();
            if (dialog.ShowDialog() == true)
            {
                databaseService.AddPrompt(dialog.EditingPrompt);
                MessageBox.Show("Prompt saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

                    var dialog = new AddEditPromptDialog();
                    dialog.ContentTextBox.Text = clipboardText;
                    dialog.TitleTextBox.Text = "Clipboard Prompt - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    dialog.TitleTextBox.SelectAll();
                    dialog.TitleTextBox.Focus();

                    if (dialog.ShowDialog() == true)
                    {
                        databaseService.AddPrompt(dialog.EditingPrompt);

                        // Show notification
                        if (trayManager.IsMinimizeToTrayEnabled)
                        {
                            trayManager.ShowBalloonTip("Prompt Saved", "Clipboard content saved successfully!");
                        }

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
            var dialog = new SettingsDialog(databaseService, importService, hotkeyManager, trayManager);
            dialog.ShowDialog();
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StatisticsDialog(databaseService);
            dialog.ShowDialog();
        }

        private void LoadPrompts()
        {
            try
            {
                allPrompts = databaseService.GetAllPrompts();
                PromptCountText.Text = $"({allPrompts.Count})";
                PopulateFilters();
                ApplyFiltersAndDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading prompts: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupFilters()
        {
            AIPlatformList.SelectionChanged += (s, e) =>
            {
                var selected = AIPlatformList.SelectedItem as ListBoxItem;
                currentAIFilter = selected?.Content?.ToString() ?? "All";
                ApplyFiltersAndDisplay();
            };

            ModelVersionList.SelectionChanged += (s, e) =>
            {
                var selected = ModelVersionList.SelectedItem as ListBoxItem;
                currentModelFilter = selected?.Content?.ToString() ?? "All";
                ApplyFiltersAndDisplay();
            };

            TagsList.SelectionChanged += (s, e) =>
            {
                var selected = TagsList.SelectedItem as ListBoxItem;
                currentTagFilter = selected?.Content?.ToString() ?? "All";
                ApplyFiltersAndDisplay();
            };

            FavoritesOnly.Checked += (s, e) =>
            {
                showFavoritesOnly = true;
                ApplyFiltersAndDisplay();
            };

            FavoritesOnly.Unchecked += (s, e) =>
            {
                showFavoritesOnly = false;
                ApplyFiltersAndDisplay();
            };

            SearchBox.TextChanged += (s, e) =>
            {
                ApplyFiltersAndDisplay();
            };

            SearchBox.GotFocus += (s, e) =>
            {
                if (SearchBox.Text == "Search prompts...")
                {
                    SearchBox.Text = "";
                    SearchBox.Foreground = (Brush)Application.Current.Resources["TextBrush"];
                }
            };

            SearchBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    SearchBox.Text = "Search prompts...";
                    SearchBox.Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"];
                }
            };
        }

        private void PopulateFilters()
        {
            var aiProviders = allPrompts.Select(p => p.AIProvider).Distinct().OrderBy(p => p).ToList();
            AIPlatformList.Items.Clear();
            AIPlatformList.Items.Add(new ListBoxItem { Content = "All", IsSelected = true });
            foreach (var provider in aiProviders)
            {
                AIPlatformList.Items.Add(new ListBoxItem { Content = provider });
            }

            var modelVersions = allPrompts.Select(p => p.ModelVersion).Distinct().OrderBy(m => m).ToList();
            ModelVersionList.Items.Clear();
            ModelVersionList.Items.Add(new ListBoxItem { Content = "All", IsSelected = true });
            foreach (var model in modelVersions)
            {
                ModelVersionList.Items.Add(new ListBoxItem { Content = model });
            }

            var allTags = allPrompts.SelectMany(p => p.Tags).Distinct().OrderBy(t => t).ToList();
            TagsList.Items.Clear();
            TagsList.Items.Add(new ListBoxItem { Content = "All", IsSelected = true });
            foreach (var tag in allTags)
            {
                TagsList.Items.Add(new ListBoxItem { Content = tag });
            }
        }

        private void ApplyFiltersAndDisplay()
        {
            if (allPrompts == null) return;

            var filtered = allPrompts.AsEnumerable();

            string searchText = SearchBox?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(searchText) && searchText != "Search prompts...")
            {
                filtered = filtered.Where(p =>
                    p.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Content.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Tags.Any(t => t.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    p.AIProvider.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.ModelVersion.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (currentAIFilter != "All")
                filtered = filtered.Where(p => p.AIProvider == currentAIFilter);

            if (currentModelFilter != "All")
                filtered = filtered.Where(p => p.ModelVersion == currentModelFilter);

            if (currentTagFilter != "All")
                filtered = filtered.Where(p => p.Tags.Contains(currentTagFilter));

            if (showFavoritesOnly)
                filtered = filtered.Where(p => p.IsFavorite);

            DisplayPrompts(filtered.ToList());
        }

        private void DisplayPrompts(List<Prompt> prompts)
        {
            PromptsContainer.Children.Clear();

            if (prompts.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = currentAIFilter != "All" || currentModelFilter != "All" || currentTagFilter != "All" || showFavoritesOnly
                        ? "No prompts match the current filters.\nTry adjusting your filters."
                        : "No prompts yet. Click '➕ New Prompt' to get started!",
                    FontSize = 16,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                PromptsContainer.Children.Add(emptyText);
            }
            else
            {
                foreach (var prompt in prompts)
                {
                    PromptsContainer.Children.Add(CreatePromptCard(prompt));
                }
            }

            PromptCountText.Text = $"({prompts.Count})";
        }

        private Border CreatePromptCard(Prompt prompt)
        {
            var card = new Border
            {
                Style = (Style)Application.Current.Resources["CardStyle"],
                Margin = new Thickness(0, 0, 0, 12),
                Tag = prompt
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "📝 " + prompt.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextBrush"]
            };
            Grid.SetColumn(titleText, 0);

            var favoriteButton = new Button
            {
                Content = prompt.IsFavorite ? "⭐" : "☆",
                Style = (Style)Application.Current.Resources["IconButtonStyle"],
                Width = 32,
                Height = 32,
                FontSize = 18,
                Tag = prompt
            };
            favoriteButton.Click += FavoriteButton_Click;
            Grid.SetColumn(favoriteButton, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(favoriteButton);
            Grid.SetRow(titleGrid, 0);

            var tagsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

            var modelBorder = new Border
            {
                Background = GetModelColor(prompt.AIProvider),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var modelText = new TextBlock
            {
                Text = prompt.ModelVersion,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            modelBorder.Child = modelText;
            tagsPanel.Children.Add(modelBorder);

            foreach (var tag in prompt.Tags.Take(3))
            {
                var tagBorder = new Border
                {
                    Background = GetTagColor(tag),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                var tagText = new TextBlock
                {
                    Text = "🏷️ " + tag,
                    FontSize = 12,
                    Foreground = Brushes.White
                };
                tagBorder.Child = tagText;
                tagsPanel.Children.Add(tagBorder);
            }
            Grid.SetRow(tagsPanel, 1);

            var previewText = new TextBlock
            {
                Text = prompt.GetPreview(150),
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(previewText, 2);

            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dateText = new TextBlock
            {
                Text = "📅 " + prompt.GetRelativeDate(),
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 0);

            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var copyButton = new Button
            {
                Content = "📋 Copy",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 7, 14, 7),
                FontSize = 12,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = prompt
            };
            var copyButtonTemplate = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            copyButtonTemplate.VisualTree = borderFactory;
            copyButton.Template = copyButtonTemplate;
            copyButton.Click += CopyButton_Click;

            var editButton = new Button
            {
                Content = "✏️",
                Style = (Style)Application.Current.Resources["IconButtonStyle"],
                Width = 32,
                Height = 32,
                FontSize = 16,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = prompt
            };
            editButton.Click += EditButton_Click;

            var deleteButton = new Button
            {
                Content = "🗑️",
                Style = (Style)Application.Current.Resources["IconButtonStyle"],
                Width = 32,
                Height = 32,
                FontSize = 16,
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
                    prompt.UsageCount++;
                    databaseService.UpdatePrompt(prompt);

                    button.Content = "✓ Copied!";
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, args) =>
                    {
                        button.Content = "📋 Copy";
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

        private Brush GetModelColor(string aiProvider)
        {
            return aiProvider switch
            {
                "ChatGPT" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10a37f")),
                "Claude" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC785C")),
                "Gemini" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a73e8")),
                "Copilot" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078d4")),
                "Glm" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B1FA2")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575"))
            };
        }

        private Brush GetTagColor(string tag)
        {
            int hash = tag.GetHashCode();
            var colors = new[]
            {
                "#E91E63", "#9C27B0", "#673AB7", "#3F51B5", "#2196F3",
                "#00BCD4", "#009688", "#4CAF50", "#FF9800", "#FF5722"
            };
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[Math.Abs(hash) % colors.Length]));
        }

        protected override void OnClosed(EventArgs e)
        {
            hotkeyManager?.Dispose();
            trayManager?.Dispose();
            base.OnClosed(e);
        }
    }
}