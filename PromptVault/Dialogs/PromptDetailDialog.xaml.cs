using PromptVault.Models;
using PromptVault.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PromptVault.Dialogs
{
    public partial class PromptDetailDialog : Window
    {
        private Prompt currentPrompt;
        public bool WasEdited { get; private set; }
        public bool FavoriteToggled { get; private set; }

        public PromptDetailDialog(Prompt prompt)
        {
            InitializeComponent();
            currentPrompt = prompt;
            LoadPrompt();
        }

        private void LoadPrompt()
        {
            if (currentPrompt == null) return;

            // Title and subtitle
            TitleText.Text = currentPrompt.Title;
            SubtitleText.Text = $"Created: {currentPrompt.CreatedAt:MMM dd, yyyy} • Updated: {currentPrompt.GetRelativeDate()}";

            // Favorite indicator
            FavoriteIcon.Visibility = currentPrompt.IsFavorite ? Visibility.Visible : Visibility.Collapsed;

            // Model badge
            ModelText.Text = $"{currentPrompt.AIProvider} - {currentPrompt.ModelVersion}";
            ModelBadge.Background = GetModelColor(currentPrompt.AIProvider);

            // Usage statistics
            UsageText.Text = currentPrompt.UsageCount == 0
                ? "Never used"
                : $"Used {currentPrompt.UsageCount} {(currentPrompt.UsageCount == 1 ? "time" : "times")}";

            // Token count and cost
            int tokens = TokenEstimator.EstimateTokens(currentPrompt.Content);
            var costEstimate = TokenEstimator.CalculateCost(currentPrompt.ModelVersion, tokens);
            TokenCostText.Text = $"~{TokenEstimator.FormatTokens(tokens)} tokens • {TokenEstimator.FormatCost(costEstimate.TotalCost)}";

            // Tags
            TagsPanel.Children.Clear();
            foreach (var tag in currentPrompt.Tags)
            {
                var tagBorder = new Border
                {
                    Background = GetTagColor(tag),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 10, 10)
                };

                var tagStack = new StackPanel { Orientation = Orientation.Horizontal };

                var tagEmoji = new TextBlock
                {
                    Text = "🏷️ ",
                    FontSize = 12,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tagText = new TextBlock
                {
                    Text = tag,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };

                tagStack.Children.Add(tagEmoji);
                tagStack.Children.Add(tagText);
                tagBorder.Child = tagStack;
                TagsPanel.Children.Add(tagBorder);
            }

            // Content
            ContentTextBox.Text = currentPrompt.Content;

            // Update favorite button
            UpdateFavoriteButton();
        }

        private void UpdateFavoriteButton()
        {
            ToggleFavoriteButton.Content = currentPrompt.IsFavorite ? "☆ Remove Favorite" : "⭐ Add to Favorites";
            FavoriteIcon.Visibility = currentPrompt.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(currentPrompt.Content);
                CopyButton.Content = "✓ Copied!";

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    CopyButton.Content = "📋 Copy to Clipboard";
                    timer.Stop();
                };
                timer.Start();

                // Increment usage count
                currentPrompt.UsageCount++;
                UsageText.Text = $"Used {currentPrompt.UsageCount} {(currentPrompt.UsageCount == 1 ? "time" : "times")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            currentPrompt.IsFavorite = !currentPrompt.IsFavorite;
            UpdateFavoriteButton();
            FavoriteToggled = true;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var editDialog = new AddEditPromptDialog(currentPrompt);
            if (editDialog.ShowDialog() == true)
            {
                // Update current prompt with edited data
                currentPrompt = editDialog.EditingPrompt;
                LoadPrompt();
                WasEdited = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
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
    }
}