using PromptVault.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PromptVault.Services
{
    /// <summary>
    /// Service for calculating prompt statistics
    /// </summary>
    public class StatisticsService
    {
        private readonly DatabaseService databaseService;

        public StatisticsService(DatabaseService dbService)
        {
            databaseService = dbService;
        }

        /// <summary>
        /// Get comprehensive dashboard statistics
        /// </summary>
        public DashboardStats GetDashboardStats()
        {
            var allPrompts = databaseService.GetAllPrompts();

            return new DashboardStats
            {
                TotalPrompts = allPrompts.Count,
                FavoriteCount = allPrompts.Count(p => p.IsFavorite),
                TotalUsageCount = allPrompts.Sum(p => p.UsageCount),
                MostUsedPrompts = GetMostUsedPrompts(allPrompts, 5),
                RecentlyAdded = GetRecentlyAdded(allPrompts, 5),
                RecentlyUpdated = GetRecentlyUpdated(allPrompts, 5),
                PromptsByPlatform = GetPromptsByPlatform(allPrompts),
                PromptsByModel = GetPromptsByModel(allPrompts),
                PopularTags = GetPopularTags(allPrompts, 10),
                AveragePromptsPerDay = CalculateAveragePromptsPerDay(allPrompts),
                EstimatedTotalCost = CalculateEstimatedTotalCost(allPrompts),
                TotalTokensUsed = CalculateTotalTokens(allPrompts)
            };
        }

        /// <summary>
        /// Get most frequently used prompts
        /// </summary>
        private List<PromptStat> GetMostUsedPrompts(List<Prompt> prompts, int count)
        {
            return prompts
                .OrderByDescending(p => p.UsageCount)
                .Take(count)
                .Select(p => new PromptStat
                {
                    Title = p.Title,
                    Value = p.UsageCount,
                    AIProvider = p.AIProvider,
                    ModelVersion = p.ModelVersion,
                    PromptId = p.Id
                })
                .ToList();
        }

        /// <summary>
        /// Get recently added prompts
        /// </summary>
        private List<PromptStat> GetRecentlyAdded(List<Prompt> prompts, int count)
        {
            return prompts
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new PromptStat
                {
                    Title = p.Title,
                    Value = (int)(DateTime.Now - p.CreatedAt).TotalDays,
                    AIProvider = p.AIProvider,
                    ModelVersion = p.ModelVersion,
                    PromptId = p.Id,
                    DateInfo = p.CreatedAt.ToString("MMM dd, yyyy")
                })
                .ToList();
        }

        /// <summary>
        /// Get recently updated prompts
        /// </summary>
        private List<PromptStat> GetRecentlyUpdated(List<Prompt> prompts, int count)
        {
            return prompts
                .OrderByDescending(p => p.UpdatedAt)
                .Take(count)
                .Select(p => new PromptStat
                {
                    Title = p.Title,
                    Value = (int)(DateTime.Now - p.UpdatedAt).TotalHours,
                    AIProvider = p.AIProvider,
                    ModelVersion = p.ModelVersion,
                    PromptId = p.Id,
                    DateInfo = p.UpdatedAt.ToString("MMM dd, yyyy HH:mm")
                })
                .ToList();
        }

        /// <summary>
        /// Get prompt count by AI platform
        /// </summary>
        private Dictionary<string, int> GetPromptsByPlatform(List<Prompt> prompts)
        {
            return prompts
                .GroupBy(p => p.AIProvider)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Get prompt count by model version
        /// </summary>
        private Dictionary<string, int> GetPromptsByModel(List<Prompt> prompts)
        {
            return prompts
                .GroupBy(p => p.ModelVersion)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Get most popular tags
        /// </summary>
        private Dictionary<string, int> GetPopularTags(List<Prompt> prompts, int count)
        {
            return prompts
                .SelectMany(p => p.Tags)
                .GroupBy(tag => tag)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Calculate average prompts added per day
        /// </summary>
        private double CalculateAveragePromptsPerDay(List<Prompt> prompts)
        {
            if (prompts.Count == 0)
                return 0;

            var oldestPrompt = prompts.OrderBy(p => p.CreatedAt).First();
            var daysSinceFirstPrompt = (DateTime.Now - oldestPrompt.CreatedAt).TotalDays;

            if (daysSinceFirstPrompt < 1)
                return prompts.Count;

            return prompts.Count / daysSinceFirstPrompt;
        }

        /// <summary>
        /// Calculate estimated total cost based on usage
        /// </summary>
        private decimal CalculateEstimatedTotalCost(List<Prompt> prompts)
        {
            var usageData = prompts.Select(p => new PromptUsage
            {
                ModelVersion = p.ModelVersion,
                InputTokens = TokenEstimator.EstimateTokens(p.Content),
                OutputTokens = TokenEstimator.EstimateTokens(p.Content) * 2, // Assume 2x output
                UsageCount = p.UsageCount
            }).ToList();

            var totalEstimate = TokenEstimator.CalculateTotalCost(usageData);
            return totalEstimate.TotalCost;
        }

        /// <summary>
        /// Calculate total tokens across all prompts
        /// </summary>
        private int CalculateTotalTokens(List<Prompt> prompts)
        {
            return prompts.Sum(p =>
                TokenEstimator.EstimateTokens(p.Content) * (p.UsageCount > 0 ? p.UsageCount : 1));
        }

        /// <summary>
        /// Get usage trends over time
        /// </summary>
        public List<UsageTrend> GetUsageTrends(int days = 30)
        {
            var prompts = databaseService.GetAllPrompts();
            var trends = new List<UsageTrend>();

            for (int i = days; i >= 0; i--)
            {
                var date = DateTime.Now.Date.AddDays(-i);
                var promptsOnDate = prompts.Count(p => p.CreatedAt.Date == date);

                trends.Add(new UsageTrend
                {
                    Date = date,
                    PromptCount = promptsOnDate
                });
            }

            return trends;
        }

        /// <summary>
        /// Get cost breakdown by platform
        /// </summary>
        public Dictionary<string, decimal> GetCostByPlatform()
        {
            var prompts = databaseService.GetAllPrompts();
            var costByPlatform = new Dictionary<string, decimal>();

            foreach (var platform in prompts.Select(p => p.AIProvider).Distinct())
            {
                var platformPrompts = prompts.Where(p => p.AIProvider == platform).ToList();
                var usageData = platformPrompts.Select(p => new PromptUsage
                {
                    ModelVersion = p.ModelVersion,
                    InputTokens = TokenEstimator.EstimateTokens(p.Content),
                    OutputTokens = TokenEstimator.EstimateTokens(p.Content) * 2,
                    UsageCount = p.UsageCount
                }).ToList();

                var totalEstimate = TokenEstimator.CalculateTotalCost(usageData);
                costByPlatform[platform] = totalEstimate.TotalCost;
            }

            return costByPlatform;
        }
    }

    public class DashboardStats
    {
        public int TotalPrompts { get; set; }
        public int FavoriteCount { get; set; }
        public int TotalUsageCount { get; set; }
        public List<PromptStat> MostUsedPrompts { get; set; }
        public List<PromptStat> RecentlyAdded { get; set; }
        public List<PromptStat> RecentlyUpdated { get; set; }
        public Dictionary<string, int> PromptsByPlatform { get; set; }
        public Dictionary<string, int> PromptsByModel { get; set; }
        public Dictionary<string, int> PopularTags { get; set; }
        public double AveragePromptsPerDay { get; set; }
        public decimal EstimatedTotalCost { get; set; }
        public int TotalTokensUsed { get; set; }
    }

    public class PromptStat
    {
        public string Title { get; set; }
        public int Value { get; set; }
        public string AIProvider { get; set; }
        public string ModelVersion { get; set; }
        public int PromptId { get; set; }
        public string DateInfo { get; set; }
    }

    public class UsageTrend
    {
        public DateTime Date { get; set; }
        public int PromptCount { get; set; }
    }
}