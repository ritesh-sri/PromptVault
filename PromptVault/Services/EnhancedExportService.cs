using PromptVault.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace PromptVault.Services
{
    /// <summary>
    /// Enhanced export service supporting multiple formats
    /// </summary>
    public class EnhancedExportService
    {
        private readonly ImportService importService;

        public EnhancedExportService(ImportService impService)
        {
            importService = impService;
        }

        public enum ExportFormat
        {
            CSV,
            JSON,
            XML,
            Markdown,
            HTML,
            PlainText,
            YAML
        }

        /// <summary>
        /// Export prompts in the specified format
        /// </summary>
        public void Export(string filePath, List<Prompt> prompts, ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.CSV:
                    ExportToCSV(filePath, prompts);
                    break;
                case ExportFormat.JSON:
                    ExportToJSON(filePath, prompts);
                    break;
                case ExportFormat.XML:
                    ExportToXML(filePath, prompts);
                    break;
                case ExportFormat.Markdown:
                    ExportToMarkdown(filePath, prompts);
                    break;
                case ExportFormat.HTML:
                    ExportToHTML(filePath, prompts);
                    break;
                case ExportFormat.PlainText:
                    ExportToPlainText(filePath, prompts);
                    break;
                case ExportFormat.YAML:
                    ExportToYAML(filePath, prompts);
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }
        }

        #region CSV Export
        private void ExportToCSV(string filePath, List<Prompt> prompts)
        {
            importService.ExportToCsv(filePath, prompts);
        }
        #endregion

        #region JSON Export
        private void ExportToJSON(string filePath, List<Prompt> prompts)
        {
            var exportData = prompts.Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                p.AIProvider,
                p.ModelVersion,
                Tags = p.Tags.ToArray(),
                p.IsFavorite,
                CreatedAt = p.CreatedAt.ToString("o"),
                UpdatedAt = p.UpdatedAt.ToString("o"),
                p.UsageCount,
                Metadata = new
                {
                    EstimatedTokens = Services.TokenEstimator.EstimateTokens(p.Content),
                    CharacterCount = p.Content.Length,
                    WordCount = p.Content.Split(new[] { ' ', '\t', '\n', '\r' },
                        StringSplitOptions.RemoveEmptyEntries).Length
                }
            }).ToList();

            var json = JsonConvert.SerializeObject(new
            {
                ExportedAt = DateTime.Now.ToString("o"),
                Version = "1.0",
                TotalPrompts = prompts.Count,
                Prompts = exportData
            }, Formatting.Indented);

            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
        #endregion

        #region XML Export
        private void ExportToXML(string filePath, List<Prompt> prompts)
        {
            var root = new XElement("PromptVaultExport",
                new XAttribute("ExportedAt", DateTime.Now.ToString("o")),
                new XAttribute("Version", "1.0"),
                new XAttribute("TotalPrompts", prompts.Count),
                new XElement("Prompts",
                    prompts.Select(p => new XElement("Prompt",
                        new XAttribute("Id", p.Id),
                        new XElement("Title", p.Title),
                        new XElement("Content", new XCData(p.Content)),
                        new XElement("AIProvider", p.AIProvider),
                        new XElement("ModelVersion", p.ModelVersion),
                        new XElement("Tags", string.Join(", ", p.Tags)),
                        new XElement("IsFavorite", p.IsFavorite),
                        new XElement("CreatedAt", p.CreatedAt.ToString("o")),
                        new XElement("UpdatedAt", p.UpdatedAt.ToString("o")),
                        new XElement("UsageCount", p.UsageCount),
                        new XElement("Metadata",
                            new XElement("EstimatedTokens", TokenEstimator.EstimateTokens(p.Content)),
                            new XElement("CharacterCount", p.Content.Length)
                        )
                    ))
                )
            );

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            doc.Save(filePath);
        }
        #endregion

        #region Markdown Export
        private void ExportToMarkdown(string filePath, List<Prompt> prompts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# PromptVault Export");
            sb.AppendLine();
            sb.AppendLine($"**Exported:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Total Prompts:** {prompts.Count}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // Table of contents
            sb.AppendLine("## Table of Contents");
            sb.AppendLine();
            for (int i = 0; i < prompts.Count; i++)
            {
                sb.AppendLine($"{i + 1}. [{prompts[i].Title}](#{SanitizeAnchor(prompts[i].Title)})");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // Prompts
            foreach (var prompt in prompts)
            {
                sb.AppendLine($"## {prompt.Title}");
                sb.AppendLine();

                // Metadata table
                sb.AppendLine("| Property | Value |");
                sb.AppendLine("|----------|-------|");
                sb.AppendLine($"| **AI Provider** | {prompt.AIProvider} |");
                sb.AppendLine($"| **Model** | {prompt.ModelVersion} |");
                sb.AppendLine($"| **Tags** | {string.Join(", ", prompt.Tags)} |");
                sb.AppendLine($"| **Favorite** | {(prompt.IsFavorite ? "⭐ Yes" : "No")} |");
                sb.AppendLine($"| **Usage Count** | {prompt.UsageCount} |");
                sb.AppendLine($"| **Created** | {prompt.CreatedAt:yyyy-MM-dd} |");
                sb.AppendLine($"| **Last Updated** | {prompt.UpdatedAt:yyyy-MM-dd} |");
                sb.AppendLine($"| **Tokens** | ~{TokenEstimator.EstimateTokens(prompt.Content)} |");
                sb.AppendLine();

                // Content
                sb.AppendLine("### Content");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(prompt.Content);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private string SanitizeAnchor(string text)
        {
            return text.ToLower()
                .Replace(" ", "-")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("'", "")
                .Replace("\"", "");
        }
        #endregion

        #region HTML Export
        private void ExportToHTML(string filePath, List<Prompt> prompts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>PromptVault Export</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine(@"
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
            background: #f5f5f5;
            color: #333;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 12px;
            margin-bottom: 30px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        .header h1 { margin: 0 0 10px 0; }
        .header p { margin: 5px 0; opacity: 0.9; }
        .prompt-card {
            background: white;
            padding: 25px;
            margin-bottom: 20px;
            border-radius: 12px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            transition: transform 0.2s, box-shadow 0.2s;
        }
        .prompt-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.15);
        }
        .prompt-title {
            font-size: 24px;
            font-weight: bold;
            color: #2c3e50;
            margin-bottom: 15px;
        }
        .metadata {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            margin-bottom: 15px;
        }
        .badge {
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
            color: white;
        }
        .badge-primary { background: #3498db; }
        .badge-success { background: #27ae60; }
        .badge-warning { background: #f39c12; }
        .badge-info { background: #9b59b6; }
        .content-box {
            background: #f8f9fa;
            border-left: 4px solid #3498db;
            padding: 20px;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            white-space: pre-wrap;
            overflow-x: auto;
            margin: 15px 0;
        }
        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 10px;
            margin-top: 15px;
            padding-top: 15px;
            border-top: 1px solid #ecf0f1;
        }
        .stat-item {
            font-size: 13px;
            color: #7f8c8d;
        }
        .stat-value {
            font-weight: bold;
            color: #2c3e50;
        }
        .favorite { color: #f39c12; font-size: 20px; }
    ");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Header
            sb.AppendLine("    <div class=\"header\">");
            sb.AppendLine("        <h1>📝 PromptVault Export</h1>");
            sb.AppendLine($"        <p>Exported: {DateTime.Now:MMMM dd, yyyy 'at' HH:mm:ss}</p>");
            sb.AppendLine($"        <p>Total Prompts: {prompts.Count}</p>");
            sb.AppendLine("    </div>");

            // Prompts
            foreach (var prompt in prompts)
            {
                sb.AppendLine("    <div class=\"prompt-card\">");
                sb.AppendLine($"        <div class=\"prompt-title\">");
                if (prompt.IsFavorite)
                    sb.AppendLine($"            <span class=\"favorite\">⭐</span> ");
                sb.AppendLine($"            {EscapeHtml(prompt.Title)}");
                sb.AppendLine($"        </div>");

                sb.AppendLine("        <div class=\"metadata\">");
                sb.AppendLine($"            <span class=\"badge badge-primary\">{EscapeHtml(prompt.AIProvider)}</span>");
                sb.AppendLine($"            <span class=\"badge badge-success\">{EscapeHtml(prompt.ModelVersion)}</span>");
                foreach (var tag in prompt.Tags)
                {
                    sb.AppendLine($"            <span class=\"badge badge-info\">🏷️ {EscapeHtml(tag)}</span>");
                }
                sb.AppendLine("        </div>");

                sb.AppendLine($"        <div class=\"content-box\">{EscapeHtml(prompt.Content)}</div>");

                sb.AppendLine("        <div class=\"stats\">");
                sb.AppendLine($"            <div class=\"stat-item\">Created: <span class=\"stat-value\">{prompt.CreatedAt:MMM dd, yyyy}</span></div>");
                sb.AppendLine($"            <div class=\"stat-item\">Updated: <span class=\"stat-value\">{prompt.UpdatedAt:MMM dd, yyyy}</span></div>");
                sb.AppendLine($"            <div class=\"stat-item\">Uses: <span class=\"stat-value\">{prompt.UsageCount}</span></div>");
                sb.AppendLine($"            <div class=\"stat-item\">Tokens: <span class=\"stat-value\">~{TokenEstimator.EstimateTokens(prompt.Content)}</span></div>");
                sb.AppendLine("        </div>");

                sb.AppendLine("    </div>");
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private string EscapeHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
        #endregion

        #region Plain Text Export
        private void ExportToPlainText(string filePath, List<Prompt> prompts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("                    PROMPTVAULT EXPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Prompts: {prompts.Count}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            foreach (var prompt in prompts)
            {
                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine($"TITLE: {prompt.Title}");
                if (prompt.IsFavorite)
                    sb.AppendLine("⭐ FAVORITE");
                sb.AppendLine("───────────────────────────────────────────────────────────────");
                sb.AppendLine();
                sb.AppendLine($"AI Provider: {prompt.AIProvider}");
                sb.AppendLine($"Model: {prompt.ModelVersion}");
                sb.AppendLine($"Tags: {string.Join(", ", prompt.Tags)}");
                sb.AppendLine($"Created: {prompt.CreatedAt:yyyy-MM-dd}");
                sb.AppendLine($"Updated: {prompt.UpdatedAt:yyyy-MM-dd}");
                sb.AppendLine($"Usage Count: {prompt.UsageCount}");
                sb.AppendLine($"Estimated Tokens: ~{TokenEstimator.EstimateTokens(prompt.Content)}");
                sb.AppendLine();
                sb.AppendLine("CONTENT:");
                sb.AppendLine(new string('─', 60));
                sb.AppendLine(prompt.Content);
                sb.AppendLine(new string('─', 60));
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("                    END OF EXPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
        #endregion

        #region YAML Export
        private void ExportToYAML(string filePath, List<Prompt> prompts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# PromptVault Export");
            sb.AppendLine($"export_date: \"{DateTime.Now:o}\"");
            sb.AppendLine($"version: \"1.0\"");
            sb.AppendLine($"total_prompts: {prompts.Count}");
            sb.AppendLine();
            sb.AppendLine("prompts:");

            foreach (var prompt in prompts)
            {
                sb.AppendLine($"  - id: {prompt.Id}");
                sb.AppendLine($"    title: \"{EscapeYaml(prompt.Title)}\"");
                sb.AppendLine($"    ai_provider: \"{prompt.AIProvider}\"");
                sb.AppendLine($"    model_version: \"{prompt.ModelVersion}\"");
                sb.AppendLine($"    is_favorite: {prompt.IsFavorite.ToString().ToLower()}");
                sb.AppendLine($"    usage_count: {prompt.UsageCount}");
                sb.AppendLine($"    created_at: \"{prompt.CreatedAt:o}\"");
                sb.AppendLine($"    updated_at: \"{prompt.UpdatedAt:o}\"");

                if (prompt.Tags.Count > 0)
                {
                    sb.AppendLine("    tags:");
                    foreach (var tag in prompt.Tags)
                    {
                        sb.AppendLine($"      - \"{EscapeYaml(tag)}\"");
                    }
                }
                else
                {
                    sb.AppendLine("    tags: []");
                }

                sb.AppendLine("    content: |");
                foreach (var line in prompt.Content.Split('\n'))
                {
                    sb.AppendLine($"      {line}");
                }

                sb.AppendLine("    metadata:");
                sb.AppendLine($"      estimated_tokens: {TokenEstimator.EstimateTokens(prompt.Content)}");
                sb.AppendLine($"      character_count: {prompt.Content.Length}");
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private string EscapeYaml(string text)
        {
            return text.Replace("\"", "\\\"").Replace("\n", "\\n");
        }
        #endregion

        /// <summary>
        /// Get file extension for export format
        /// </summary>
        public static string GetFileExtension(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => ".csv",
                ExportFormat.JSON => ".json",
                ExportFormat.XML => ".xml",
                ExportFormat.Markdown => ".md",
                ExportFormat.HTML => ".html",
                ExportFormat.PlainText => ".txt",
                ExportFormat.YAML => ".yaml",
                _ => ".txt"
            };
        }

        /// <summary>
        /// Get filter string for save dialog
        /// </summary>
        public static string GetFileFilter()
        {
            return "CSV files (*.csv)|*.csv|" +
                   "JSON files (*.json)|*.json|" +
                   "XML files (*.xml)|*.xml|" +
                   "Markdown files (*.md)|*.md|" +
                   "HTML files (*.html)|*.html|" +
                   "YAML files (*.yaml)|*.yaml|" +
                   "Text files (*.txt)|*.txt|" +
                   "All files (*.*)|*.*";
        }
    }
}