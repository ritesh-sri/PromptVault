using System;
using System.Collections.Generic;

namespace PromptVault.Services
{
    /// <summary>
    /// Service for estimating token counts and API costs
    /// </summary>
    public class TokenEstimator
    {
        // Pricing per 1M tokens (as of 2025)
        private static readonly Dictionary<string, ModelPricing> ModelPrices = new Dictionary<string, ModelPricing>
        {
            // ChatGPT Models
            { "GPT-4", new ModelPricing { InputCost = 30.00m, OutputCost = 60.00m } },
            { "GPT-4 Turbo", new ModelPricing { InputCost = 10.00m, OutputCost = 30.00m } },
            { "GPT-4o", new ModelPricing { InputCost = 2.50m, OutputCost = 10.00m } },
            { "GPT-3.5", new ModelPricing { InputCost = 0.50m, OutputCost = 1.50m } },
            { "o1", new ModelPricing { InputCost = 15.00m, OutputCost = 60.00m } },
            { "o1-mini", new ModelPricing { InputCost = 3.00m, OutputCost = 12.00m } },
            
            // Claude Models
            { "Claude 3.5 Sonnet", new ModelPricing { InputCost = 3.00m, OutputCost = 15.00m } },
            { "Claude 3 Opus", new ModelPricing { InputCost = 15.00m, OutputCost = 75.00m } },
            { "Claude 3 Sonnet", new ModelPricing { InputCost = 3.00m, OutputCost = 15.00m } },
            { "Claude 3 Haiku", new ModelPricing { InputCost = 0.25m, OutputCost = 1.25m } },
            
            // Gemini Models
            { "Gemini 1.5 Pro", new ModelPricing { InputCost = 1.25m, OutputCost = 5.00m } },
            { "Gemini Pro", new ModelPricing { InputCost = 0.50m, OutputCost = 1.50m } },
            { "Gemini Ultra", new ModelPricing { InputCost = 3.00m, OutputCost = 10.00m } },
            { "Gemini 2.0 Flash", new ModelPricing { InputCost = 0.30m, OutputCost = 1.20m } },
            
            // Copilot (uses GPT-4)
            { "Copilot (GPT-4)", new ModelPricing { InputCost = 30.00m, OutputCost = 60.00m } },
            { "Copilot", new ModelPricing { InputCost = 10.00m, OutputCost = 30.00m } },
        };

        /// <summary>
        /// Estimate token count from text (rough approximation: 1 token ≈ 4 characters)
        /// </summary>
        public static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // More accurate estimation considering:
            // - Average English word is ~4.7 characters
            // - Average token is ~0.75 words
            // - So roughly 1 token ≈ 3.5-4 characters

            int charCount = text.Length;
            int wordCount = CountWords(text);

            // Use character-based estimation with word count adjustment
            int charBasedEstimate = (int)Math.Ceiling(charCount / 4.0);
            int wordBasedEstimate = (int)Math.Ceiling(wordCount / 0.75);

            // Return average of both methods for better accuracy
            return (charBasedEstimate + wordBasedEstimate) / 2;
        }

        /// <summary>
        /// Get detailed token breakdown
        /// </summary>
        public static TokenBreakdown GetTokenBreakdown(string text)
        {
            int tokens = EstimateTokens(text);
            int words = CountWords(text);
            int characters = text?.Length ?? 0;
            int lines = text?.Split('\n').Length ?? 0;

            return new TokenBreakdown
            {
                Tokens = tokens,
                Characters = characters,
                Words = words,
                Lines = lines,
                AverageTokensPerWord = words > 0 ? (double)tokens / words : 0
            };
        }

        /// <summary>
        /// Calculate cost for a single API call
        /// </summary>
        public static CostEstimate CalculateCost(string modelVersion, int inputTokens, int estimatedOutputTokens = 0)
        {
            if (!ModelPrices.TryGetValue(modelVersion, out var pricing))
            {
                // Default pricing if model not found
                pricing = new ModelPricing { InputCost = 5.00m, OutputCost = 15.00m };
            }

            // If no output tokens specified, assume 2x input tokens (common ratio)
            if (estimatedOutputTokens == 0)
            {
                estimatedOutputTokens = inputTokens * 2;
            }

            decimal inputCost = (inputTokens / 1_000_000m) * pricing.InputCost;
            decimal outputCost = (estimatedOutputTokens / 1_000_000m) * pricing.OutputCost;
            decimal totalCost = inputCost + outputCost;

            return new CostEstimate
            {
                InputTokens = inputTokens,
                OutputTokens = estimatedOutputTokens,
                InputCost = inputCost,
                OutputCost = outputCost,
                TotalCost = totalCost,
                ModelVersion = modelVersion
            };
        }

        /// <summary>
        /// Get pricing information for a model
        /// </summary>
        public static ModelPricing GetModelPricing(string modelVersion)
        {
            if (ModelPrices.TryGetValue(modelVersion, out var pricing))
            {
                return pricing;
            }

            // Return default pricing
            return new ModelPricing { InputCost = 5.00m, OutputCost = 15.00m };
        }

        /// <summary>
        /// Calculate total estimated spending across multiple prompts
        /// </summary>
        public static TotalCostEstimate CalculateTotalCost(List<PromptUsage> usageData)
        {
            decimal totalCost = 0;
            int totalTokens = 0;
            int totalCalls = 0;

            foreach (var usage in usageData)
            {
                var estimate = CalculateCost(usage.ModelVersion, usage.InputTokens, usage.OutputTokens);
                totalCost += estimate.TotalCost * usage.UsageCount;
                totalTokens += (usage.InputTokens + usage.OutputTokens) * usage.UsageCount;
                totalCalls += usage.UsageCount;
            }

            return new TotalCostEstimate
            {
                TotalCost = totalCost,
                TotalTokens = totalTokens,
                TotalCalls = totalCalls,
                AverageCostPerCall = totalCalls > 0 ? totalCost / totalCalls : 0
            };
        }

        /// <summary>
        /// Count words in text
        /// </summary>
        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// Format cost as currency
        /// </summary>
        public static string FormatCost(decimal cost)
        {
            if (cost < 0.01m)
                return $"${cost:F4}";
            else if (cost < 1.00m)
                return $"${cost:F3}";
            else
                return $"${cost:F2}";
        }

        /// <summary>
        /// Format tokens with thousands separator
        /// </summary>
        public static string FormatTokens(int tokens)
        {
            return tokens.ToString("N0");
        }
    }

    public class ModelPricing
    {
        public decimal InputCost { get; set; }  // Per 1M tokens
        public decimal OutputCost { get; set; } // Per 1M tokens
    }

    public class TokenBreakdown
    {
        public int Tokens { get; set; }
        public int Characters { get; set; }
        public int Words { get; set; }
        public int Lines { get; set; }
        public double AverageTokensPerWord { get; set; }
    }

    public class CostEstimate
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal InputCost { get; set; }
        public decimal OutputCost { get; set; }
        public decimal TotalCost { get; set; }
        public string ModelVersion { get; set; }
    }

    public class PromptUsage
    {
        public string ModelVersion { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int UsageCount { get; set; }
    }

    public class TotalCostEstimate
    {
        public decimal TotalCost { get; set; }
        public int TotalTokens { get; set; }
        public int TotalCalls { get; set; }
        public decimal AverageCostPerCall { get; set; }
    }
}