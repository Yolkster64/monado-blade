using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonadoBlade.Week6.Interfaces;

namespace MonadoBlade.Week6.Services
{
    /// <summary>
    /// Schedules maintenance tasks with cron-like syntax.
    /// </summary>
    public class MaintenanceScheduler : IMaintenanceScheduler
    {
        private readonly List<MaintenanceTask> _scheduledTasks = new();
        private readonly List<MaintenanceWindow> _maintenanceWindows = new();
        private readonly List<MaintenanceExecutionResult> _executionHistory = new();
        private MaintenanceTask _currentTask;

        public async Task DefineMaintenanceWindowAsync(MaintenanceWindow window)
        {
            _maintenanceWindows.Add(window);
            await Task.CompletedTask;
        }

        public async Task<string> ScheduleTaskAsync(MaintenanceTask task)
        {
            task.Id = Guid.NewGuid().ToString();
            task.NextRunTime = CalculateNextRunTime(task.CronExpression);
            _scheduledTasks.Add(task);
            return await Task.FromResult(task.Id);
        }

        public async Task<List<MaintenanceTask>> GetScheduledTasksAsync()
        {
            return await Task.FromResult(_scheduledTasks.OrderBy(t => t.NextRunTime).ToList());
        }

        public async Task<DateTime> GetNextMaintenanceAsync()
        {
            var nextTask = _scheduledTasks.OrderBy(t => t.NextRunTime).FirstOrDefault();
            return await Task.FromResult(nextTask?.NextRunTime ?? DateTime.MaxValue);
        }

        public async Task<bool> CancelTaskAsync(string taskId)
        {
            var task = _scheduledTasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                _scheduledTasks.Remove(task);
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }

        public async Task<MaintenanceExecutionResult> RunNowAsync(string taskId)
        {
            var task = _scheduledTasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
                return null;

            var result = new MaintenanceExecutionResult
            {
                TaskId = taskId,
                TaskName = task.Name,
                StartTime = DateTime.UtcNow
            };

            try
            {
                _currentTask = task;
                task.Status = MaintenanceTaskStatus.Running;

                if (task.ExecutionFunction != null)
                {
                    result = await task.ExecutionFunction();
                }

                task.Status = MaintenanceTaskStatus.Completed;
                task.LastRunTime = DateTime.UtcNow;
                task.NextRunTime = CalculateNextRunTime(task.CronExpression);
                result.Success = true;
                result.Status = "Completed";
            }
            catch (Exception ex)
            {
                task.Status = MaintenanceTaskStatus.Failed;
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Status = "Failed";
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                _executionHistory.Add(result);
                _currentTask = null;
            }

            return result;
        }

        public async Task<List<MaintenanceExecutionResult>> GetExecutionHistoryAsync(string taskId, int lastN = 10)
        {
            return await Task.FromResult(_executionHistory
                .Where(e => e.TaskId == taskId)
                .OrderByDescending(e => e.StartTime)
                .Take(lastN)
                .ToList());
        }

        public async Task SendMaintenanceNotificationAsync(MaintenanceNotification notification)
        {
            // In production, would send email/in-app notifications
            Console.WriteLine($"[NOTIFICATION] {notification.TaskName}: {notification.Impact}");
            await Task.CompletedTask;
        }

        public async Task<bool> DeferMaintenanceAsync(string taskId, TimeSpan duration)
        {
            var task = _scheduledTasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && task.CanBeDeferredByUsers)
            {
                task.Status = MaintenanceTaskStatus.Deferred;
                task.NextRunTime = DateTime.UtcNow.Add(duration);
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }

        public async Task<MaintenanceStatus> GetCurrentStatusAsync()
        {
            var status = new MaintenanceStatus
            {
                MaintenanceInProgress = _currentTask != null,
                CurrentTaskName = _currentTask?.Name,
                NextScheduledMaintenance = _scheduledTasks.OrderBy(t => t.NextRunTime).FirstOrDefault()?.NextRunTime ?? DateTime.MaxValue,
                RecentExecutions = _executionHistory.OrderByDescending(e => e.StartTime).Take(5).ToList(),
                FailedTasksLastWeek = _executionHistory
                    .Where(e => !e.Success && e.StartTime > DateTime.UtcNow.AddDays(-7))
                    .Count(),
                SuccessfulTasksLastWeek = _executionHistory
                    .Where(e => e.Success && e.StartTime > DateTime.UtcNow.AddDays(-7))
                    .Count()
            };

            if (_currentTask != null)
            {
                status.CurrentTaskStartTime = _executionHistory
                    .OrderByDescending(e => e.StartTime)
                    .FirstOrDefault()?.StartTime ?? DateTime.UtcNow;
                status.EstimatedTimeRemaining = TimeSpan.FromMinutes(Math.Max(0, _currentTask.EstimatedDurationMinutes - 5));
            }

            return await Task.FromResult(status);
        }

        private DateTime CalculateNextRunTime(string cronExpression)
        {
            // Simple cron parsing (production would use Cronos or similar)
            if (cronExpression == "0 3 * * ?") // 3 AM daily
                return DateTime.UtcNow.AddDays(1).Date.AddHours(3);
            if (cronExpression == "0 2 ? * 0") // 2 AM Sunday
                return DateTime.UtcNow.AddDays((7 - (int)DateTime.UtcNow.DayOfWeek) % 7).Date.AddHours(2);
            
            return DateTime.UtcNow.AddHours(1); // Default: 1 hour
        }
    }
}
