using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonadoBlade.Week6.Interfaces
{
    /// <summary>
    /// Schedules maintenance tasks with cron-like syntax and dependency handling.
    /// </summary>
    public interface IMaintenanceScheduler
    {
        /// <summary>Defines a maintenance window (preferred time and duration).</summary>
        Task DefineMaintenanceWindowAsync(MaintenanceWindow window);

        /// <summary>Schedules a maintenance task with cron expression.</summary>
        Task<string> ScheduleTaskAsync(MaintenanceTask task);

        /// <summary>Gets all scheduled maintenance tasks.</summary>
        Task<List<MaintenanceTask>> GetScheduledTasksAsync();

        /// <summary>Gets the next maintenance event.</summary>
        Task<DateTime> GetNextMaintenanceAsync();

        /// <summary>Cancels a scheduled maintenance task.</summary>
        Task<bool> CancelTaskAsync(string taskId);

        /// <summary>Runs a maintenance task immediately (bypass schedule).</summary>
        Task<MaintenanceExecutionResult> RunNowAsync(string taskId);

        /// <summary>Gets execution history of a maintenance task.</summary>
        Task<List<MaintenanceExecutionResult>> GetExecutionHistoryAsync(string taskId, int lastN = 10);

        /// <summary>Notifies users about upcoming maintenance.</summary>
        Task SendMaintenanceNotificationAsync(MaintenanceNotification notification);

        /// <summary>Allows users to defer low-priority maintenance.</summary>
        Task<bool> DeferMaintenanceAsync(string taskId, TimeSpan duration);

        /// <summary>Gets current maintenance status.</summary>
        Task<MaintenanceStatus> GetCurrentStatusAsync();
    }

    public class MaintenanceWindow
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TimeSpan StartTime { get; set; } // e.g., 03:00 (3 AM)
        public TimeSpan Duration { get; set; } // e.g., 1 hour
        public List<DayOfWeek> ApplicableDays { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }

    public class MaintenanceTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public string CronExpression { get; set; } // "0 3 * * ?" = 3 AM daily
        public MaintenanceTaskType Type { get; set; }
        public decimal EstimatedDurationMinutes { get; set; }
        public bool IsCritical { get; set; }
        public List<string> DependsOnTaskIds { get; set; } = new();
        public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Scheduled;
        public DateTime LastRunTime { get; set; }
        public DateTime NextRunTime { get; set; }
        public bool CanBeDeferredByUsers { get; set; }
        public int MaxRetries { get; set; } = 3;
        public Func<Task<MaintenanceExecutionResult>> ExecutionFunction { get; set; }
    }

    public enum MaintenanceTaskType
    {
        CacheCleanup,
        TempFileCleanup,
        BackupArchival,
        LogCleanup,
        DockerCleanup,
        NuGetCleanup,
        Defragmentation,
        DatabaseMaintenance,
        ServiceHealthCheck,
        ResourceMonitoring
    }

    public enum MaintenanceTaskStatus { Scheduled, Running, Completed, Failed, Deferred }

    public class MaintenanceExecutionResult
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
        public string Output { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
    }

    public class MaintenanceNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TaskName { get; set; }
        public DateTime ScheduledTime { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public string Impact { get; set; } // "None", "Some services unavailable", "Full maintenance"
        public List<string> AffectedSystems { get; set; } = new();
        public bool CanDefer { get; set; }
        public string NotificationType { get; set; } // "Email", "InApp", "Both"
        public int HoursBeforeNotification { get; set; } = 24;
    }

    public class MaintenanceStatus
    {
        public bool MaintenanceInProgress { get; set; }
        public string CurrentTaskName { get; set; }
        public DateTime CurrentTaskStartTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public DateTime NextScheduledMaintenance { get; set; }
        public List<MaintenanceExecutionResult> RecentExecutions { get; set; } = new();
        public int FailedTasksLastWeek { get; set; }
        public int SuccessfulTasksLastWeek { get; set; }
    }
}
