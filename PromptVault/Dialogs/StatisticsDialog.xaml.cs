using PromptVault.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PromptVault.Dialogs
{
    public partial class StatisticsDialog : Window
    {
        private readonly StatisticsService statisticsService;
        private readonly DatabaseService databaseService;

        public StatisticsDialog(DatabaseService dbService)
        {
            InitializeComponent();
            databaseService = dbService;
            statisticsService = new StatisticsService(dbService);
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                var stats = statisticsService.GetDashboardStats();

                // Overview Statistics
                TotalPromptsText.Text = stats.TotalPrompts.ToString("N0");
                FavoriteCountText.Text = stats.FavoriteCount.ToString("N0");
                TotalUsageText.Text = stats.TotalUsageCount.ToString("N0");
                AvgPerDayText.Text = stats.AveragePromptsPerDay.ToString("F1");

                // Cost & Tokens
                EstimatedCostText.Text = TokenEstimator.FormatCost(stats.EstimatedTotalCost);
                TotalTokensText.Text = TokenEstimator.FormatTokens(stats.TotalTokensUsed);

                // Most Used Prompts
                PopulateMostUsed(stats.MostUsedPrompts);

                // Recently Added
                PopulateRecentlyAdded(stats.RecentlyAdded);

                // Platform Breakdown
                PopulatePlatformBreakdown(stats.PromptsByPlatform);

                // Model Breakdown
                PopulateModelBreakdown(stats.PromptsByModel);

                // Popular Tags
                PopularTagsPanel.Children.Clear();
                foreach (var tag in stats.PopularTags.Take(8))
                {
                    var tagBorder = CreateTagBadge(tag.Key, tag.Value);
                    PopularTagsPanel.Children.Add(tagBorder);
                }

                // Cost by Platform Chart
                PopulateCostByPlatform();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateMostUsed(List<PromptStat> prompts)
        {
            MostUsedPanel.Children.Clear();

            if (prompts.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No usage data yet",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                MostUsedPanel.Children.Add(emptyText);
                return;
            }

            foreach (var prompt in prompts)
            {
                var card = CreatePromptStatCard(prompt, $"{prompt.Value} uses");
                MostUsedPanel.Children.Add(card);
            }
        }

        private void PopulateRecentlyAdded(List<PromptStat> prompts)
        {
            RecentlyAddedPanel.Children.Clear();

            if (prompts.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No prompts yet",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                RecentlyAddedPanel.Children.Add(emptyText);
                return;
            }

            foreach (var prompt in prompts)
            {
                var card = CreatePromptStatCard(prompt, prompt.DateInfo);
                RecentlyAddedPanel.Children.Add(card);
            }
        }

        private Border CreatePromptStatCard(PromptStat prompt, string subtitle)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();

            var titleText = new TextBlock
            {
                Text = prompt.Title.Length > 40 ? prompt.Title.Substring(0, 40) + "..." : prompt.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var subtitleText = new TextBlock
            {
                Text = subtitle,
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575")),
                Margin = new Thickness(0, 2, 0, 0)
            };

            titleStack.Children.Add(titleText);
            titleStack.Children.Add(subtitleText);
            Grid.SetColumn(titleStack, 0);

            var modelBadge = new Border
            {
                Background = GetModelColor(prompt.AIProvider),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };

            var modelText = new TextBlock
            {
                Text = prompt.AIProvider,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            modelBadge.Child = modelText;
            Grid.SetColumn(modelBadge, 1);

            grid.Children.Add(titleStack);
            grid.Children.Add(modelBadge);
            card.Child = grid;

            return card;
        }

        private void PopulatePlatformBreakdown(Dictionary<string, int> platforms)
        {
            PlatformBreakdownPanel.Children.Clear();

            var total = platforms.Values.Sum();

            foreach (var platform in platforms.OrderByDescending(p => p.Value))
            {
                var percentage = total > 0 ? (platform.Value * 100.0 / total) : 0;

                var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

                var nameText = new TextBlock
                {
                    Text = platform.Key,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameText, 0);

                var progressBar = new Border
                {
                    Height = 20,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(8, 0, 8, 0)
                };

                var progressFill = new Border
                {
                    Background = GetModelColor(platform.Key),
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = percentage * 2.5 // Scale to fit nicely
                };

                progressBar.Child = progressFill;
                Grid.SetColumn(progressBar, 1);

                var countText = new TextBlock
                {
                    Text = $"{platform.Value} ({percentage:F0}%)",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575")),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(countText, 2);

                grid.Children.Add(nameText);
                grid.Children.Add(progressBar);
                grid.Children.Add(countText);

                PlatformBreakdownPanel.Children.Add(grid);
            }
        }

        private void PopulateModelBreakdown(Dictionary<string, int> models)
        {
            ModelBreakdownPanel.Children.Clear();

            foreach (var model in models.Take(5))
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameText = new TextBlock
                {
                    Text = model.Key,
                    FontSize = 12
                };
                Grid.SetColumn(nameText, 0);

                var countBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 2, 8, 2)
                };

                var countText = new TextBlock
                {
                    Text = model.Value.ToString(),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                };

                countBadge.Child = countText;
                Grid.SetColumn(countBadge, 1);

                grid.Children.Add(nameText);
                grid.Children.Add(countBadge);

                ModelBreakdownPanel.Children.Add(grid);
            }
        }

        private void PopulateCostByPlatform()
        {
            CostByPlatformPanel.Children.Clear();

            var costByPlatform = statisticsService.GetCostByPlatform();

            if (costByPlatform.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No cost data available",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                CostByPlatformPanel.Children.Add(emptyText);
                return;
            }

            foreach (var platform in costByPlatform.OrderByDescending(p => p.Value))
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var nameText = new TextBlock
                {
                    Text = platform.Key,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameText, 0);

                var costText = new TextBlock
                {
                    Text = TokenEstimator.FormatCost(platform.Value),
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(costText, 1);

                grid.Children.Add(nameText);
                grid.Children.Add(costText);

                CostByPlatformPanel.Children.Add(grid);
            }
        }

        private Border CreateTagBadge(string tag, int count)
        {
            var badge = new Border
            {
                Background = GetTagColor(tag),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 8, 8)
            };

            var text = new TextBlock
            {
                Text = $"{tag} ({count})",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            badge.Child = text;
            return badge;
        }

        private Brush GetModelColor(string aiProvider)
        {
            return aiProvider switch
            {
                "ChatGPT" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10a37f")),
                "Claude" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC785C")),
                "Gemini" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a73e8")),
                "Copilot" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078d4")),
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
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                colors[Math.Abs(hash) % colors.Length]));
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}