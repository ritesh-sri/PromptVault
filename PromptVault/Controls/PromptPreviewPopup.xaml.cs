using PromptVault.Models;
using PromptVault.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PromptVault.Controls
{
    public partial class PromptPreviewPopup : UserControl
    {
        private Storyboard fadeInStoryboard;

        public PromptPreviewPopup()
        {
            InitializeComponent();

            // Get the fade-in animation from resources
            fadeInStoryboard = this.Resources["FadeInAnimation"] as Storyboard;

            // Ensure the control starts invisible
            this.Opacity = 0;
        }

        /// <summary>
        /// Load prompt data into the preview and trigger fade-in animation
        /// </summary>
        public void LoadPrompt(Prompt prompt)
        {
            if (prompt == null) return;

            // Load all data first (while invisible)
            LoadPromptData(prompt);

            // Then trigger smooth fade-in
            if (fadeInStoryboard != null)
            {
                fadeInStoryboard.Stop();
                fadeInStoryboard.Begin(this);
            }
        }

        private void LoadPromptData(Prompt prompt)
        {
            if (prompt == null) return;

            // Title
            PreviewTitle.Text = prompt.Title;

            // Favorite indicator
            FavoriteIcon.Visibility = prompt.IsFavorite ? Visibility.Visible : Visibility.Collapsed;

            // Model badge
            ModelText.Text = $"{prompt.AIProvider} - {prompt.ModelVersion}";
            ModelBadge.Background = GetModelColor(prompt.AIProvider);

            // Usage count
            UsageCountText.Text = prompt.UsageCount == 0
                ? "Never used"
                : $"Used {prompt.UsageCount} {(prompt.UsageCount == 1 ? "time" : "times")}";

            // Tags
            TagsContainer.Children.Clear();
            foreach (var tag in prompt.Tags)
            {
                var tagBorder = new Border
                {
                    Background = GetTagColor(tag),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 10, 8)
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
                TagsContainer.Children.Add(tagBorder);
            }

            // Content
            ContentPreview.Text = prompt.Content;

            // Footer info
            DateInfo.Text = $"Created: {prompt.CreatedAt:MMM dd, yyyy} • Updated: {prompt.GetRelativeDate()}";

            // Token count
            int tokens = TokenEstimator.EstimateTokens(prompt.Content);
            var costEstimate = TokenEstimator.CalculateCost(prompt.ModelVersion, tokens);
            TokenInfo.Text = $"~{TokenEstimator.FormatTokens(tokens)} tokens • Est. cost: {TokenEstimator.FormatCost(costEstimate.TotalCost)} per call";
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