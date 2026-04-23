using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonadoBlade.Week6.Interfaces
{
    /// <summary>
    /// Tracks all costs across LLM providers, cloud storage, CI/CD, and mobile data.
    /// Non-blocking async operations for production use.
    /// </summary>
    public interface ICostTracker
    {
        /// <summary>Records a cost event asynchronously (fire-and-forget).</summary>
        Task RecordCostAsync(CostEvent costEvent);

        /// <summary>Gets costs grouped by provider for current month.</summary>
        Task<Dictionary<string, decimal>> GetCostsByProviderAsync();

        /// <summary>Gets costs grouped by model type.</summary>
        Task<Dictionary<string, decimal>> GetCostsByModelAsync();

        /// <summary>Gets costs attributed to a specific user.</summary>
        Task<decimal> GetCostsByUserAsync(string userId);

        /// <summary>Gets costs by project/initiative.</summary>
        Task<Dictionary<string, decimal>> GetCostsByProjectAsync();

        /// <summary>Gets daily cost trend (last 30 days).</summary>
        Task<Dictionary<DateTime, decimal>> GetDailyTrendsAsync();

        /// <summary>Projects current month's costs to end of month.</summary>
        Task<CostProjection> GetMonthlyProjectionAsync();

        /// <summary>Detects cost trends (increasing, decreasing, stable).</summary>
        Task<TrendAnalysis> AnalyzeTrendAsync();

        /// <summary>Exports costs as CSV.</summary>
        Task<string> ExportCsvAsync(DateTime from, DateTime to);

        /// <summary>Exports costs as JSON.</summary>
        Task<string> ExportJsonAsync(DateTime from, DateTime to);

        /// <summary>Exports BI-ready report format.</summary>
        Task<BiReport> ExportBiReportAsync(DateTime from, DateTime to);
    }

    /// <summary>Represents a single cost event.</summary>
    public class CostEvent
    {
        public string Provider { get; set; } // "Claude", "GPT-4", "Hermes", "Azure"
        public string Model { get; set; }
        public string UserId { get; set; }
        public string ProjectId { get; set; }
        public decimal Amount { get; set; }
        public int TokensUsed { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } // "LLM", "Storage", "CI/CD", "Mobile"
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>Cost projection for current month.</summary>
    public class CostProjection
    {
        public decimal DailyAverage { get; set; }
        public decimal CurrentMonthCost { get; set; }
        public decimal ProjectedMonthCost { get; set; }
        public int DaysRemaining { get; set; }
        public DateTime ProjectedDate { get; set; }
    }

    /// <summary>Trend analysis results.</summary>
    public class TrendAnalysis
    {
        public TrendDirection Direction { get; set; }
        public decimal PercentageChange { get; set; }
        public string Reason { get; set; }
    }

    public enum TrendDirection { Increasing, Decreasing, Stable }

    /// <summary>BI report format for integration with Power BI, Tableau, etc.</summary>
    public class BiReport
    {
        public string ReportId { get; set; } = Guid.NewGuid().ToString();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<BiDataPoint> DataPoints { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BiDataPoint
    {
        public DateTime Date { get; set; }
        public string Dimension { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public string UserId { get; set; }
        public string ProjectId { get; set; }
        public decimal Cost { get; set; }
        public int TokensUsed { get; set; }
    }
}
