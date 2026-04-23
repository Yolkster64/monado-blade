using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonadoBlade.Week6.Interfaces;

namespace MonadoBlade.Week6.Services
{
    /// <summary>
    /// Budget enforcement with hard/soft limits and alerts.
    /// </summary>
    public class BudgetManagerService : IBudgetManager
    {
        private decimal _monthlyBudget = 10000m;
        private BudgetLimitType _monthlyLimitType = BudgetLimitType.Soft;
        private decimal _dailyBudget = 500m;
        private BudgetLimitType _dailyLimitType = BudgetLimitType.Soft;
        private decimal _currentMonthSpent = 0m;
        private decimal _currentDaySpent = 0m;
        private DateTime _lastResetDate = DateTime.UtcNow;

        private readonly Dictionary<string, ProviderBudgetInfo> _providerBudgets = new();
        private readonly Dictionary<string, UserBudgetInfo> _userBudgets = new();
        private readonly List<BudgetAlert> _alerts = new();
        private Action<BudgetAlert> _alertHandler;

        public async Task SetMonthlyBudgetAsync(decimal amount, BudgetLimitType limitType)
        {
            _monthlyBudget = amount;
            _monthlyLimitType = limitType;
            await Task.CompletedTask;
        }

        public async Task SetDailyBudgetAsync(decimal amount, BudgetLimitType limitType)
        {
            _dailyBudget = amount;
            _dailyLimitType = limitType;
            await Task.CompletedTask;
        }

        public async Task SetProviderBudgetAsync(string provider, decimal amount, BudgetLimitType limitType)
        {
            _providerBudgets[provider] = new ProviderBudgetInfo
            {
                Provider = provider,
                Budget = amount,
                Spent = _providerBudgets.ContainsKey(provider) ? _providerBudgets[provider].Spent : 0
            };
            await Task.CompletedTask;
        }

        public async Task SetUserBudgetAsync(string userId, decimal amount, BudgetLimitType limitType)
        {
            _userBudgets[userId] = new UserBudgetInfo
            {
                UserId = userId,
                Budget = amount,
                Spent = _userBudgets.ContainsKey(userId) ? _userBudgets[userId].Spent : 0
            };
            await Task.CompletedTask;
        }

        public async Task<BudgetCheckResult> CanMakeRequestAsync(string provider, decimal estimatedCost, string userId)
        {
            // Check reset
            if (DateTime.UtcNow.Date > _lastResetDate.Date)
            {
                _currentDaySpent = 0;
                _lastResetDate = DateTime.UtcNow;
            }

            var result = new BudgetCheckResult { Allowed = true, RemainingBudget = _monthlyBudget - _currentMonthSpent };

            // Daily check
            if (_currentDaySpent + estimatedCost > _dailyBudget)
            {
                if (_dailyLimitType == BudgetLimitType.Hard)
                {
                    result.Allowed = false;
                    result.Reason = $"Daily budget exceeded. Daily: ${_dailyBudget}, Spent: ${_currentDaySpent}";
                }
                else
                {
                    result.ApproachingLimit = true;
                }
            }

            // Monthly check
            if (_currentMonthSpent + estimatedCost > _monthlyBudget)
            {
                if (_monthlyLimitType == BudgetLimitType.Hard)
                {
                    result.Allowed = false;
                    result.Reason = $"Monthly budget would be exceeded. Budget: ${_monthlyBudget}, Current: ${_currentMonthSpent}";
                }
                else
                {
                    result.ApproachingLimit = true;
                }
            }

            // Check at thresholds
            var monthlyUsagePercent = (_currentMonthSpent + estimatedCost) / _monthlyBudget * 100;
            if (monthlyUsagePercent >= 90)
            {
                var alert = new BudgetAlert
                {
                    Level = AlertLevel.Approaching90,
                    Message = $"Approaching monthly budget limit: {monthlyUsagePercent:F1}%",
                    AlertType = BudgetAlertType.MonthlyWarning,
                    CurrentSpend = _currentMonthSpent + estimatedCost,
                    Budget = _monthlyBudget
                };
                _alertHandler?.Invoke(alert);
                _alerts.Add(alert);
            }

            return await Task.FromResult(result);
        }

        public async Task<BudgetStatus> GetBudgetStatusAsync()
        {
            // Auto-reset daily
            if (DateTime.UtcNow.Date > _lastResetDate.Date)
            {
                _currentDaySpent = 0;
                _lastResetDate = DateTime.UtcNow;
            }

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

            var dailyAverage = _currentMonthSpent / Math.Max(1, (now - monthStart).Days + 1);
            var remainingDays = daysInMonth - now.Day;
            var projectedMonthCost = _currentMonthSpent + (dailyAverage * remainingDays);
            var daysUntilExceeded = remainingDays > 0 && dailyAverage > 0 
                ? (int)((_monthlyBudget - _currentMonthSpent) / dailyAverage) 
                : -1;

            return await Task.FromResult(new BudgetStatus
            {
                Period = now,
                MonthlyBudget = _monthlyBudget,
                DailyBudget = _dailyBudget,
                SpentThisMonth = _currentMonthSpent,
                SpentToday = _currentDaySpent,
                RemainingMonth = _monthlyBudget - _currentMonthSpent,
                RemainingToday = _dailyBudget - _currentDaySpent,
                MonthlyLimitType = _monthlyLimitType,
                DailyLimitType = _dailyLimitType,
                ProviderBudgets = _providerBudgets.Values.ToList(),
                UserBudgets = _userBudgets.Values.ToList(),
                BurnRate = dailyAverage,
                DaysUntilExceeded = daysUntilExceeded
            });
        }

        public async Task<BudgetStatus> GetHistoricalBudgetAsync(DateTime month)
        {
            // In real implementation, fetch from database
            return await GetBudgetStatusAsync();
        }

        public async Task ResetBudgetAsync(BudgetResetType type)
        {
            switch (type)
            {
                case BudgetResetType.Daily:
                    _currentDaySpent = 0;
                    _lastResetDate = DateTime.UtcNow;
                    break;
                case BudgetResetType.Monthly:
                    _currentMonthSpent = 0;
                    _currentDaySpent = 0;
                    break;
            }
            await Task.CompletedTask;
        }

        public async Task<bool> OverrideLimitAsync(decimal temporaryIncrease, string reason)
        {
            _monthlyBudget += temporaryIncrease;
            var alert = new BudgetAlert
            {
                Level = AlertLevel.Warning50,
                Message = $"Budget override: +${temporaryIncrease}. Reason: {reason}",
                AlertType = BudgetAlertType.MonthlyWarning,
                Timestamp = DateTime.UtcNow
            };
            _alerts.Add(alert);
            return await Task.FromResult(true);
        }

        public async Task<List<BudgetAlert>> GetAlertsAsync()
        {
            return await Task.FromResult(_alerts.OrderByDescending(a => a.Timestamp).ToList());
        }

        public void SubscribeToBudgetAlerts(Action<BudgetAlert> handler)
        {
            _alertHandler = handler;
        }

        // Method to update spent amounts (would be called by cost tracker)
        public void RecordSpend(decimal amount)
        {
            _currentMonthSpent += amount;
            _currentDaySpent += amount;
        }
    }
}
