using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonadoBlade.Week6.Interfaces
{
    /// <summary>
    /// Manages budgets with hard/soft limits, enforcement, and recovery mechanisms.
    /// </summary>
    public interface IBudgetManager
    {
        /// <summary>Sets a monthly budget with hard or soft limit.</summary>
        Task SetMonthlyBudgetAsync(decimal amount, BudgetLimitType limitType);

        /// <summary>Sets a daily budget to prevent spending spikes.</summary>
        Task SetDailyBudgetAsync(decimal amount, BudgetLimitType limitType);

        /// <summary>Sets budget for a specific provider (e.g., GPT-4).</summary>
        Task SetProviderBudgetAsync(string provider, decimal amount, BudgetLimitType limitType);

        /// <summary>Sets budget for a specific user (for chargeback/attribution).</summary>
        Task SetUserBudgetAsync(string userId, decimal amount, BudgetLimitType limitType);

        /// <summary>Checks if a new request can be made (respects hard limits).</summary>
        Task<BudgetCheckResult> CanMakeRequestAsync(string provider, decimal estimatedCost, string userId);

        /// <summary>Gets current budget status and remaining funds.</summary>
        Task<BudgetStatus> GetBudgetStatusAsync();

        /// <summary>Gets historical budget data for a past month.</summary>
        Task<BudgetStatus> GetHistoricalBudgetAsync(DateTime month);

        /// <summary>Manually reset budget (admin override).</summary>
        Task ResetBudgetAsync(BudgetResetType type);

        /// <summary>Temporarily override hard limit (admin only).</summary>
        Task<bool> OverrideLimitAsync(decimal temporaryIncrease, string reason);

        /// <summary>Returns alerts at different thresholds.</summary>
        Task<List<BudgetAlert>> GetAlertsAsync();

        /// <summary>Subscribes to budget alerts.</summary>
        void SubscribeToBudgetAlerts(Action<BudgetAlert> handler);
    }

    public enum BudgetLimitType { Soft, Hard }
    public enum BudgetResetType { Daily, Monthly, Custom }
    public enum AlertLevel { Warning50, Approaching90, Exceeded, Critical }

    /// <summary>Result of a budget check before making a request.</summary>
    public class BudgetCheckResult
    {
        public bool Allowed { get; set; }
        public string? Reason { get; set; }
        public decimal RemainingBudget { get; set; }
        public bool ApproachingLimit { get; set; }
    }

    /// <summary>Current budget status.</summary>
    public class BudgetStatus
    {
        public DateTime Period { get; set; }
        public decimal MonthlyBudget { get; set; }
        public decimal DailyBudget { get; set; }
        public decimal SpentThisMonth { get; set; }
        public decimal SpentToday { get; set; }
        public decimal RemainingMonth { get; set; }
        public decimal RemainingToday { get; set; }
        public BudgetLimitType MonthlyLimitType { get; set; }
        public BudgetLimitType DailyLimitType { get; set; }
        public List<ProviderBudgetInfo> ProviderBudgets { get; set; } = new();
        public List<UserBudgetInfo> UserBudgets { get; set; } = new();
        public double BurnRate { get; set; } // $/day
        public int DaysUntilExceeded { get; set; } // -1 if won't exceed
    }

    public class ProviderBudgetInfo
    {
        public string Provider { get; set; }
        public decimal Budget { get; set; }
        public decimal Spent { get; set; }
        public decimal Remaining { get; set; }
    }

    public class UserBudgetInfo
    {
        public string UserId { get; set; }
        public decimal Budget { get; set; }
        public decimal Spent { get; set; }
        public decimal Remaining { get; set; }
    }

    /// <summary>Budget alert notification.</summary>
    public class BudgetAlert
    {
        public AlertLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public BudgetAlertType AlertType { get; set; }
        public decimal CurrentSpend { get; set; }
        public decimal Budget { get; set; }
        public string Recipient { get; set; } // user email or manager email
    }

    public enum BudgetAlertType { MonthlyWarning, DailyWarning, ProviderExceeded, UserExceeded }
}
