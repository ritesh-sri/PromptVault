using PromptVault.Services;
using System.Windows;

namespace PromptVault.Dialogs
{
    public partial class ExportFormatDialog : Window
    {
        public EnhancedExportService.ExportFormat SelectedFormat { get; private set; }

        public ExportFormatDialog()
        {
            InitializeComponent();
            SelectedFormat = EnhancedExportService.ExportFormat.CSV;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Determine which format is selected
            if (CSVRadio.IsChecked == true)
                SelectedFormat = EnhancedExportService.ExportFormat.CSV;
            else if (JSONRadio.IsChecked == true)
                SelectedFormat = EnhancedExportService.ExportFormat.JSON;
            else if (XMLRadio.IsChecked == true)
                SelectedFormat = EnhancedExportService.ExportFormat.XML;
            else if (MarkdownRadio.IsChecked == true)
                SelectedFormat = EnhancedExportService.ExportFormat.Markdown;
            else if (HTMLRadio.IsChecked == true)
                SelectedFormat = EnhancedExportService.ExportFormat.HTML;
            else if (YAMLRadio.IsChecked == true)
                SelectedFormat = EnhancedExportService.ExportFormat.YAML;
            else if (PlainTextRadio.IsChecked == true)
                SelectedFormat = EnhancedExportService.ExportFormat.PlainText;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}