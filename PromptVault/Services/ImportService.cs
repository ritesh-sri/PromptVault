using CsvHelper;
using CsvHelper.Configuration;
using PromptVault.Models;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PromptVault.Services
{
    public class ImportService
    {
        private readonly DatabaseService databaseService;

        public ImportService(DatabaseService dbService)
        {
            databaseService = dbService;
        }

        /// <summary>
        /// Import prompts from CSV file
        /// Expected columns: Title, Content, AIProvider, ModelVersion, Tags
        /// </summary>
        public ImportResult ImportFromCsv(string filePath)
        {
            var result = new ImportResult();

            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    BadDataFound = null
                }))
                {
                    var records = csv.GetRecords<PromptImportItem>().ToList();

                    foreach (var record in records)
                    {
                        try
                        {
                            var prompt = record.ToPrompt();
                            databaseService.AddPrompt(prompt);
                            result.SuccessCount++;
                        }
                        catch (Exception ex)
                        {
                            result.FailedCount++;
                            result.Errors.Add($"Row: {record.Title ?? "Unknown"} - Error: {ex.Message}");
                        }
                    }

                    result.TotalProcessed = records.Count;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to read CSV file: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Import prompts from plain text file
        /// Creates a single prompt with the file content
        /// </summary>
        public ImportResult ImportFromTextFile(string filePath)
        {
            var result = new ImportResult();

            try
            {
                string content = File.ReadAllText(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                var prompt = new Prompt
                {
                    Title = fileName,
                    Content = content,
                    AIProvider = AIProvider.Unknown,
                    ModelVersion = ModelVersion.Unknown
                };

                databaseService.AddPrompt(prompt);
                result.SuccessCount = 1;
                result.TotalProcessed = 1;
            }
            catch (Exception ex)
            {
                result.FailedCount = 1;
                result.TotalProcessed = 1;
                result.Errors.Add($"Failed to import text file: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Import multiple text files at once
        /// </summary>
        public ImportResult ImportFromTextFiles(string[] filePaths)
        {
            var result = new ImportResult();

            foreach (var filePath in filePaths)
            {
                var fileResult = ImportFromTextFile(filePath);
                result.SuccessCount += fileResult.SuccessCount;
                result.FailedCount += fileResult.FailedCount;
                result.TotalProcessed += fileResult.TotalProcessed;
                result.Errors.AddRange(fileResult.Errors);
            }

            return result;
        }

        /// <summary>
        /// Export all prompts to CSV file
        /// </summary>
        public void ExportToCsv(string filePath, List<Prompt> prompts)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var exportItems = prompts.Select(p => new PromptExportItem
                {
                    Title = p.Title,
                    Content = p.Content,
                    AIProvider = p.AIProvider,
                    ModelVersion = p.ModelVersion,
                    Tags = string.Join(", ", p.Tags),
                    IsFavorite = p.IsFavorite,
                    CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = p.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    UsageCount = p.UsageCount
                }).ToList();

                csv.WriteRecords(exportItems);
            }
        }

        /// <summary>
        /// Export prompts to individual text files
        /// </summary>
        public void ExportToTextFiles(string folderPath, List<Prompt> prompts)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            foreach (var prompt in prompts)
            {
                // Create safe filename
                string safeFileName = GetSafeFileName(prompt.Title);
                string filePath = Path.Combine(folderPath, $"{safeFileName}.txt");

                // Add metadata header
                StringBuilder content = new StringBuilder();
                content.AppendLine($"Title: {prompt.Title}");
                content.AppendLine($"AI Provider: {prompt.AIProvider}");
                content.AppendLine($"Model: {prompt.ModelVersion}");
                content.AppendLine($"Tags: {string.Join(", ", prompt.Tags)}");
                content.AppendLine($"Created: {prompt.CreatedAt:yyyy-MM-dd}");
                content.AppendLine(new string('-', 50));
                content.AppendLine();
                content.AppendLine(prompt.Content);

                File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);
            }
        }

        /// <summary>
        /// Create a CSV template file for users to fill
        /// </summary>
        public void CreateCsvTemplate(string filePath)
        {
            var template = new List<PromptImportItem>
            {
                new PromptImportItem
                {
                    Title = "Example Prompt Title",
                    Content = "Your prompt content goes here. You can use multiple lines.",
                    AIProvider = "ChatGPT",
                    ModelVersion = "GPT-4",
                    Tags = "coding, example"
                },
                new PromptImportItem
                {
                    Title = "Another Example",
                    Content = "Another prompt content. Leave AIProvider and ModelVersion empty for 'Unknown'.",
                    AIProvider = "",
                    ModelVersion = "",
                    Tags = "writing"
                }
            };

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(template);
            }
        }

        private string GetSafeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = fileName.ToCharArray()
                .Select(c => invalid.Contains(c) ? '_' : c)
                .ToArray();

            string result = new string(safe);

            // Limit length
            if (result.Length > 100)
                result = result.Substring(0, 100);

            return result;
        }
    }

    public class ImportResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; }

        public ImportResult()
        {
            Errors = new List<string>();
        }

        public bool HasErrors => FailedCount > 0;

        public string GetSummary()
        {
            var summary = $"Import completed:\n" +
                         $"• Total processed: {TotalProcessed}\n" +
                         $"• Successful: {SuccessCount}\n" +
                         $"• Failed: {FailedCount}";

            if (HasErrors)
            {
                summary += "\n\nErrors:\n" + string.Join("\n", Errors.Take(5));
                if (Errors.Count > 5)
                    summary += $"\n... and {Errors.Count - 5} more errors";
            }

            return summary;
        }
    }

    public class PromptExportItem
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string AIProvider { get; set; }
        public string ModelVersion { get; set; }
        public string Tags { get; set; }
        public bool IsFavorite { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public int UsageCount { get; set; }
    }
}