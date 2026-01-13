using System;
using System.Collections.Generic;

namespace PromptVault.Models
{
    public class Prompt
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string AIProvider { get; set; } // ChatGPT, Claude, Gemini, etc.
        public string ModelVersion { get; set; } // GPT-4, Claude 3.5, etc.
        public List<string> Tags { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UsageCount { get; set; }

        public Prompt()
        {
            Tags = new List<string>();
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            UsageCount = 0;
            IsFavorite = false;
        }

        // Helper method to get a preview of the content
        public string GetPreview(int maxLength = 150)
        {
            if (string.IsNullOrEmpty(Content))
                return string.Empty;

            if (Content.Length <= maxLength)
                return Content;

            return Content.Substring(0, maxLength) + "...";
        }

        // Helper method to get formatted date
        public string GetRelativeDate()
        {
            var timeSpan = DateTime.Now - UpdatedAt;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";

            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }
    }

    public class AIProvider
    {
        public static readonly string ChatGPT = "ChatGPT";
        public static readonly string Claude = "Claude";
        public static readonly string Gemini = "Gemini";
        public static readonly string Copilot = "Copilot";
        public static readonly string Unknown = "Unknown";

        public static List<string> GetAll()
        {
            return new List<string>
            {
                ChatGPT,
                Claude,
                Gemini,
                Copilot,
                Unknown
            };
        }
    }

    public class ModelVersion
    {
        // ChatGPT versions
        public static readonly string GPT4 = "GPT-4";
        public static readonly string GPT4Turbo = "GPT-4 Turbo";
        public static readonly string GPT35 = "GPT-3.5";

        // Claude versions
        public static readonly string Claude35Sonnet = "Claude 3.5 Sonnet";
        public static readonly string Claude3Opus = "Claude 3 Opus";
        public static readonly string Claude3Sonnet = "Claude 3 Sonnet";
        public static readonly string Claude3Haiku = "Claude 3 Haiku";

        // Gemini versions
        public static readonly string GeminiPro = "Gemini Pro";
        public static readonly string Gemini15Pro = "Gemini 1.5 Pro";

        // Copilot versions
        public static readonly string CopilotGPT4 = "Copilot (GPT-4)";

        public static readonly string Unknown = "Unknown";

        public static List<string> GetAll()
        {
            return new List<string>
            {
                GPT4,
                GPT4Turbo,
                GPT35,
                Claude35Sonnet,
                Claude3Opus,
                Claude3Sonnet,
                Claude3Haiku,
                GeminiPro,
                Gemini15Pro,
                CopilotGPT4,
                Unknown
            };
        }
    }

    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }

        public static List<Tag> GetDefaultTags()
        {
            return new List<Tag>
            {
                new Tag { Name = "Coding", Color = "#E65100" },
                new Tag { Name = "Writing", Color = "#C2185B" },
                new Tag { Name = "Analysis", Color = "#1976D2" },
                new Tag { Name = "Email", Color = "#7B1FA2" },
                new Tag { Name = "Creative", Color = "#388E3C" },
                new Tag { Name = "Review", Color = "#F57C00" },
                new Tag { Name = "SEO", Color = "#F57F17" },
                new Tag { Name = "Debug", Color = "#D32F2F" },
                new Tag { Name = "Documentation", Color = "#455A64" },
                new Tag { Name = "Testing", Color = "#00796B" }
            };
        }
    }

    public class PromptImportItem
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string AIProvider { get; set; }
        public string ModelVersion { get; set; }
        public string Tags { get; set; } // Comma-separated tags

        public Prompt ToPrompt()
        {
            var prompt = new Prompt
            {
                Title = string.IsNullOrWhiteSpace(Title) ? "Untitled Prompt" : Title,
                Content = Content ?? string.Empty,
                AIProvider = string.IsNullOrWhiteSpace(AIProvider) ? Models.AIProvider.Unknown : AIProvider,
                ModelVersion = string.IsNullOrWhiteSpace(ModelVersion) ? Models.ModelVersion.Unknown : ModelVersion
            };

            if (!string.IsNullOrWhiteSpace(Tags))
            {
                var tagArray = Tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tagArray)
                {
                    prompt.Tags.Add(tag.Trim());
                }
            }

            return prompt;
        }
    }
}