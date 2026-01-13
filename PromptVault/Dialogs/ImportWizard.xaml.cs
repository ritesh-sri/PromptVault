using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using PromptVault.Models;
using PromptVault.Services;

namespace PromptVault.Dialogs
{
    public partial class ImportWizardDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly ImportService importService;

        private int currentStep = 1;
        private string selectedFilePath;
        private string[] selectedTextFiles;
        private ImportType importType;
        private ImportResult importResult;
        private List<Prompt> previewPrompts;

        private enum ImportType
        {
            None,
            CSV,
            TextFiles
        }

        public ImportWizardDialog(DatabaseService dbService, ImportService impService)
        {
            InitializeComponent();
            databaseService = dbService;
            importService = impService;
        }

        private void SelectCSVImport_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Select CSV File",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog() == true)
            {
                selectedFilePath = openDialog.FileName;
                importType = ImportType.CSV;
                LoadCSVPreview();
                MoveToStep(2);
            }
        }

        private void SelectTextImport_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Select Text Files",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = true,
                CheckFileExists = true
            };

            if (openDialog.ShowDialog() == true)
            {
                selectedTextFiles = openDialog.FileNames;
                importType = ImportType.TextFiles;
                LoadTextFilesPreview();
                MoveToStep(2);
            }
        }

        private void LoadCSVPreview()
        {
            try
            {
                // Load CSV and create preview
                using (var reader = new StreamReader(selectedFilePath))
                using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<PromptImportItem>().Take(5).ToList();
                    previewPrompts = records.Select(r => r.ToPrompt()).ToList();

                    // Update UI
                    SelectedFileText.Text = Path.GetFileName(selectedFilePath);

                    // Count all records
                    reader.BaseStream.Position = 0;
                    reader.DiscardBufferedData();
                    csv.Context.Reader.Read();
                    csv.Context.Reader.ReadHeader();
                    int totalCount = 0;
                    while (csv.Context.Reader.Read())
                        totalCount++;

                    PromptsCountText.Text = totalCount.ToString();

                    // Show preview
                    var previewData = records.Select(r => new
                    {
                        Title = r.Title?.Substring(0, Math.Min(30, r.Title?.Length ?? 0)),
                        AIProvider = r.AIProvider,
                        ModelVersion = r.ModelVersion,
                        Tags = r.Tags,
                        ContentPreview = r.Content?.Substring(0, Math.Min(50, r.Content?.Length ?? 0)) + "..."
                    }).ToList();

                    PreviewDataGrid.ItemsSource = previewData;
                }

                Step2Description.Text = "Review the CSV data before importing";
                NextButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load CSV file: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MoveToStep(1);
            }
        }

        private void LoadTextFilesPreview()
        {
            try
            {
                // Create preview from text files
                previewPrompts = new List<Prompt>();
                var previewData = new List<object>();

                int count = 0;
                foreach (var filePath in selectedTextFiles.Take(5))
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string content = File.ReadAllText(filePath);

                    var prompt = new Prompt
                    {
                        Title = fileName,
                        Content = content,
                        AIProvider = AIProvider.Unknown,
                        ModelVersion = ModelVersion.Unknown
                    };

                    previewPrompts.Add(prompt);

                    previewData.Add(new
                    {
                        Title = fileName.Substring(0, Math.Min(30, fileName.Length)),
                        AIProvider = "Unknown",
                        ModelVersion = "Unknown",
                        ContentPreview = content.Substring(0, Math.Min(50, content.Length)) + "..."
                    });

                    count++;
                }

                // Update UI
                SelectedFileText.Text = $"{selectedTextFiles.Length} text files selected";
                PromptsCountText.Text = selectedTextFiles.Length.ToString();
                PreviewDataGrid.ItemsSource = previewData;
                Step2Description.Text = "Review the text files before importing";
                NextButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load text files: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MoveToStep(1);
            }
        }

        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save CSV Template",
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = "promptvault_template.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    importService.CreateCsvTemplate(saveDialog.FileName);
                    MessageBox.Show($"Template saved to:\n{saveDialog.FileName}\n\nYou can now fill it with your prompts and import it.",
                        "Template Created", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create template: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep == 2)
            {
                // Start import
                PerformImport();
                MoveToStep(3);
            }
        }

        private void PerformImport()
        {
            try
            {
                NextButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                if (importType == ImportType.CSV)
                {
                    importResult = importService.ImportFromCsv(selectedFilePath);
                }
                else if (importType == ImportType.TextFiles)
                {
                    importResult = importService.ImportFromTextFiles(selectedTextFiles);
                }

                // Update results UI
                TotalProcessedText.Text = importResult.TotalProcessed.ToString();
                SuccessfulText.Text = importResult.SuccessCount.ToString();
                FailedText.Text = importResult.FailedCount.ToString();

                if (importResult.HasErrors)
                {
                    ResultIcon.Text = "⚠️";
                    ErrorDetailsPanel.Visibility = Visibility.Visible;
                    ErrorDetailsText.Text = string.Join("\n", importResult.Errors);
                }
                else
                {
                    ResultIcon.Text = "✅";
                    ErrorDetailsPanel.Visibility = Visibility.Collapsed;
                }

                NextButton.Content = "✓ Finish";
                NextButton.IsEnabled = true;
                NextButton.Click -= NextButton_Click;
                NextButton.Click += FinishButton_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MoveToStep(1);
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep > 1)
            {
                MoveToStep(currentStep - 1);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep == 3)
            {
                // Import already completed
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }
            Close();
        }

        private void MoveToStep(int step)
        {
            currentStep = step;

            // Update step indicators
            Step1Indicator.Background = step >= 1 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            Step2Indicator.Background = step >= 2 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            Step3Indicator.Background = step >= 3 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));

            // Show/hide panels
            Step1Panel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

            // Update buttons
            BackButton.Visibility = step > 1 && step < 3 ? Visibility.Visible : Visibility.Collapsed;

            if (step == 1)
            {
                NextButton.Content = "Next →";
                NextButton.IsEnabled = false;
                CancelButton.Content = "Cancel";
            }
            else if (step == 2)
            {
                NextButton.Content = "Import →";
                NextButton.IsEnabled = true;
                CancelButton.Content = "Cancel";
            }
            else if (step == 3)
            {
                NextButton.Content = "✓ Finish";
                NextButton.IsEnabled = true;
                CancelButton.Content = "Close";
            }
        }
    }
}