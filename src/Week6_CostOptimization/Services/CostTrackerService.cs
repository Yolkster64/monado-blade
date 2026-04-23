using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonadoBlade.Week6.Interfaces;

namespace MonadoBlade.Week6.Services
{
    /// <summary>
    /// In-memory implementation of cost tracker (production would use database).
    /// Non-blocking async operations.
    /// </summary>
    public class CostTrackerService : ICostTracker
    {
        private readonly List<CostEvent> _costEvents = new();
        private readonly object _lockObj = new();

        public async Task RecordCostAsync(CostEvent costEvent)
        {
            // Fire-and-forget with proper async handling
            await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    _costEvents.Add(costEvent);
                }
            });
        }

        public async Task<Dictionary<string, decimal>> GetCostsByProviderAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    return _costEvents
                        .GroupBy(e => e.Provider)
                        .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
                }
            });
        }

        public async Task<Dictionary<string, decimal>> GetCostsByModelAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    return _costEvents
                        .GroupBy(e => e.Model)
                        .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
                }
            });
        }

        public async Task<decimal> GetCostsByUserAsync(string userId)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    return _costEvents
                        .Where(e => e.UserId == userId)
                        .Sum(e => e.Amount);
                }
            });
        }

        public async Task<Dictionary<string, decimal>> GetCostsByProjectAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    return _costEvents
                        .GroupBy(e => e.ProjectId)
                        .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
                }
            });
        }

        public async Task<Dictionary<DateTime, decimal>> GetDailyTrendsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    return _costEvents
                        .GroupBy(e => e.Timestamp.Date)
                        .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
                }
            });
        }

        public async Task<CostProjection> GetMonthlyProjectionAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    var now = DateTime.UtcNow;
                    var monthStart = new DateTime(now.Year, now.Month, 1);
                    var monthCosts = _costEvents
                        .Where(e => e.Timestamp >= monthStart && e.Timestamp <= now)
                        .Sum(e => e.Amount);

                    var days = (int)(now - monthStart).TotalDays + 1;
                    var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                    var dailyAverage = monthCosts / days;
                    var projectedMonthCost = dailyAverage * daysInMonth;

                    return new CostProjection
                    {
                        DailyAverage = dailyAverage,
                        CurrentMonthCost = monthCosts,
                        ProjectedMonthCost = projectedMonthCost,
                        DaysRemaining = daysInMonth - days,
                        ProjectedDate = new DateTime(now.Year, now.Month, daysInMonth)
                    };
                }
            });
        }

        public async Task<TrendAnalysis> AnalyzeTrendAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (_costEvents.Count < 2)
                        return new TrendAnalysis { Direction = TrendDirection.Stable };

                    var now = DateTime.UtcNow;
                    var week1 = _costEvents.Where(e => e.Timestamp > now.AddDays(-14) && e.Timestamp <= now.AddDays(-7)).Sum(e => e.Amount);
                    var week2 = _costEvents.Where(e => e.Timestamp > now.AddDays(-7) && e.Timestamp <= now).Sum(e => e.Amount);

                    var change = week2 - week1;
                    var percentChange = week1 > 0 ? (change / week1) * 100 : 0;

                    var direction = percentChange > 5 ? TrendDirection.Increasing :
                                  percentChange < -5 ? TrendDirection.Decreasing :
                                  TrendDirection.Stable;

                    return new TrendAnalysis
                    {
                        Direction = direction,
                        PercentageChange = (decimal)percentChange,
                        Reason = direction switch
                        {
                            TrendDirection.Increasing => "Costs increasing - check usage patterns",
                            TrendDirection.Decreasing => "Costs decreasing - optimization effective",
                            _ => "Costs stable"
                        }
                    };
                }
            });
        }

        public async Task<string> ExportCsvAsync(DateTime from, DateTime to)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    var events = _costEvents.Where(e => e.Timestamp >= from && e.Timestamp <= to).ToList();
                    var csv = "Provider,Model,UserId,ProjectId,Amount,TokensUsed,Category,Timestamp\n";
                    foreach (var e in events)
                    {
                        csv += $"{e.Provider},{e.Model},{e.UserId},{e.ProjectId},{e.Amount},{e.TokensUsed},{e.Category},{e.Timestamp:o}\n";
                    }
                    return csv;
                }
            });
        }

        public async Task<string> ExportJsonAsync(DateTime from, DateTime to)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    var events = _costEvents.Where(e => e.Timestamp >= from && e.Timestamp <= to).ToList();
                    var json = System.Text.Json.JsonSerializer.Serialize(events, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    return json;
                }
            });
        }

        public async Task<BiReport> ExportBiReportAsync(DateTime from, DateTime to)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    var report = new BiReport();
                    var events = _costEvents.Where(e => e.Timestamp >= from && e.Timestamp <= to).ToList();
                    
                    foreach (var e in events)
                    {
                        report.DataPoints.Add(new BiDataPoint
                        {
                            Date = e.Timestamp,
                            Dimension = $"{e.Provider}:{e.Model}",
                            Provider = e.Provider,
                            Model = e.Model,
                            UserId = e.UserId,
                            ProjectId = e.ProjectId,
                            Cost = e.Amount,
                            TokensUsed = e.TokensUsed
                        });
                    }
                    
                    return report;
                }
            });
        }
    }
}
