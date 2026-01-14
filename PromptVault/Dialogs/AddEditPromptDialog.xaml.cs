using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PromptVault.Models;

namespace PromptVault.Dialogs
{
    public partial class AddEditPromptDialog : Window
    {
        public Prompt EditingPrompt { get; private set; }
        public bool IsEditMode { get; private set; }

        public AddEditPromptDialog()
        {
            InitializeComponent();
            IsEditMode = false;
            InitializeForAdd();
            ContentTextBox.TextChanged += ContentTextBox_TextChanged;

            // Allow custom AI providers and models
            AIProviderComboBox.IsEditable = true;
            ModelVersionComboBox.IsEditable = true;
        }

        public AddEditPromptDialog(Prompt prompt)
        {
            InitializeComponent();
            IsEditMode = true;
            EditingPrompt = prompt;
            InitializeForEdit(prompt);
            ContentTextBox.TextChanged += ContentTextBox_TextChanged;

            // Allow custom AI providers and models
            AIProviderComboBox.IsEditable = true;
            ModelVersionComboBox.IsEditable = true;
        }

        private void InitializeForAdd()
        {
            DialogTitle.Text = "➕ Add New Prompt";
            Title = "Add Prompt";

            // Clear all fields
            TitleTextBox.Text = "";
            TagsTextBox.Text = "";
            ContentTextBox.Text = "";
            FavoriteCheckBox.IsChecked = false;

            // Set defaults
            AIProviderComboBox.SelectedIndex = 0;
            UpdateModelVersions("ChatGPT");

            // Focus on title
            TitleTextBox.Focus();
        }

        private void InitializeForEdit(Prompt prompt)
        {
            DialogTitle.Text = "✏️ Edit Prompt";
            Title = "Edit Prompt";
            SaveButton.Content = "💾 Update Prompt";

            // Populate fields
            TitleTextBox.Text = prompt.Title;
            TagsTextBox.Text = string.Join(", ", prompt.Tags);
            ContentTextBox.Text = prompt.Content;
            FavoriteCheckBox.IsChecked = prompt.IsFavorite;

            // Set AI Provider
            SetComboBoxValue(AIProviderComboBox, prompt.AIProvider);
            UpdateModelVersions(prompt.AIProvider);

            // Set Model Version
            SetComboBoxValue(ModelVersionComboBox, prompt.ModelVersion);

            UpdateCharacterCount();
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                var item = comboBox.Items[i] as ComboBoxItem;
                if (item?.Content?.ToString() == value)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }

        private void AIProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelVersionComboBox == null) return;

            var selectedProvider = AIProviderComboBox.Text; // Use Text instead of SelectedItem
            if (!string.IsNullOrEmpty(selectedProvider))
            {
                UpdateModelVersions(selectedProvider);
            }
        }

        private void UpdateModelVersions(string provider)
        {
            ModelVersionComboBox.Items.Clear();

            var versions = new List<string>();

            // Handle custom providers by checking the text
            if (provider.Contains("ChatGPT") || provider == "ChatGPT")
            {
                versions = new List<string> { "GPT-4", "GPT-4 Turbo", "GPT-3.5", "GPT-4o", "o1", "o1-mini" };
            }
            else if (provider.Contains("Claude") || provider == "Claude")
            {
                versions = new List<string> { "Claude 3.5 Sonnet", "Claude 3 Opus", "Claude 3 Sonnet", "Claude 3 Haiku" };
            }
            else if (provider.Contains("Gemini") || provider == "Gemini")
            {
                versions = new List<string> { "Gemini 1.5 Pro", "Gemini Pro", "Gemini Ultra", "Gemini 2.0 Flash" };
            }
            else if (provider.Contains("Copilot") || provider == "Copilot")
            {
                versions = new List<string> { "Copilot (GPT-4)", "Copilot" };
            }
            else
            {
                // For custom providers, just add "Default" option
                versions = new List<string> { "Default", "Latest" };
            }

            foreach (var version in versions)
            {
                ModelVersionComboBox.Items.Add(new ComboBoxItem { Content = version });
            }

            // If editing and the model exists, select it
            if (IsEditMode && EditingPrompt != null)
            {
                SetComboBoxValue(ModelVersionComboBox, EditingPrompt.ModelVersion);
            }
            else
            {
                ModelVersionComboBox.SelectedIndex = 0;
            }
        }

        private void ContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCount();
        }

        private void UpdateCharacterCount()
        {
            int charCount = ContentTextBox.Text.Length;
            int estimatedTokens = (int)(charCount / 4.0); // Rough estimate: 1 token ≈ 4 characters

            CharacterCountText.Text = $"Characters: {charCount:N0} | Estimated tokens: ~{estimatedTokens:N0}";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Please enter a title for the prompt.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("Please enter the prompt content.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContentTextBox.Focus();
                return;
            }

            // Get AI Provider (support custom text or selected item)
            string aiProvider = AIProviderComboBox.Text;
            if (string.IsNullOrWhiteSpace(aiProvider))
            {
                aiProvider = (AIProviderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unknown";
            }

            // Get Model Version (support custom text or selected item)
            string modelVersion = ModelVersionComboBox.Text;
            if (string.IsNullOrWhiteSpace(modelVersion))
            {
                modelVersion = (ModelVersionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unknown";
            }

            // Create or update prompt
            if (IsEditMode)
            {
                EditingPrompt.Title = TitleTextBox.Text.Trim();
                EditingPrompt.Content = ContentTextBox.Text.Trim();
                EditingPrompt.AIProvider = aiProvider.Trim();
                EditingPrompt.ModelVersion = modelVersion.Trim();
                EditingPrompt.IsFavorite = FavoriteCheckBox.IsChecked ?? false;
                EditingPrompt.UpdatedAt = DateTime.Now;

                // Parse tags
                EditingPrompt.Tags = ParseTags(TagsTextBox.Text);
            }
            else
            {
                EditingPrompt = new Prompt
                {
                    Title = TitleTextBox.Text.Trim(),
                    Content = ContentTextBox.Text.Trim(),
                    AIProvider = aiProvider.Trim(),
                    ModelVersion = modelVersion.Trim(),
                    IsFavorite = FavoriteCheckBox.IsChecked ?? false,
                    Tags = ParseTags(TagsTextBox.Text),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }

            DialogResult = true;
            Close();
        }

        private List<string> ParseTags(string tagsText)
        {
            if (string.IsNullOrWhiteSpace(tagsText))
                return new List<string>();

            return tagsText
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct()
                .ToList();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}