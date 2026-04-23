using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonadoBlade.Week6.Interfaces
{
    /// <summary>
    /// Handles automatic cleanup, defragmentation, and maintenance.
    /// Isolated and safe - cannot crash main system.
    /// </summary>
    public interface ICleanupAutomationFramework
    {
        /// <summary>Deletes cache items older than specified days.</summary>
        Task<CleanupResult> CacheCleanupAsync(int daysOld = 7);

        /// <summary>Clears temporary files from temp partition.</summary>
        Task<CleanupResult> TempFileCleanupAsync();

        /// <summary>Archives old backups to cold storage and deletes local copies.</summary>
        Task<CleanupResult> BackupArchivalAsync(int daysOld = 30);

        /// <summary>Compresses logs older than daysOld, deletes logs older than 1 year.</summary>
        Task<CleanupResult> LogCleanupAsync(int daysOld = 7);

        /// <summary>Removes unused Docker images.</summary>
        Task<CleanupResult> DockerCleanupAsync();

        /// <summary>Cleans unused NuGet packages from cache.</summary>
        Task<CleanupResult> NuGetCacheCleanupAsync();

        /// <summary>Defragments NTFS partitions.</summary>
        Task<DefragResult> DefragmentNtfsAsync(string driveLetter = "C:");

        /// <summary>Optimizes ReFS (DevDrive) partitions.</summary>
        Task<DefragResult> OptimizeRefsAsync(string driveLetter = "D:");

        /// <summary>Rebuilds database indexes for performance.</summary>
        Task<DbMaintenanceResult> RebuildDatabaseIndexesAsync();

        /// <summary>Restarts unhealthy services (auto-recovery).</summary>
        Task<ServiceHealthResult> RestartUnhealthyServicesAsync();

        /// <summary>Removes orphaned processes and connections.</summary>
        Task<CleanupResult> RemoveOrphanedProcessesAsync();

        /// <summary>Verifies all scheduled tasks are running.</summary>
        Task<ScheduledTasksStatus> VerifyScheduledTasksAsync();

        /// <summary>Identifies large unused files.</summary>
        Task<List<LargeFile>> IdentifyLargeUnusedFilesAsync(long minSizeBytes = 100 * 1024 * 1024);

        /// <summary>Finds duplicate files by hash.</summary>
        Task<List<DuplicateFileSet>> FindDuplicateFilesAsync();

        /// <summary>Reports storage usage trends over time.</summary>
        Task<StorageUsageTrend> AnalyzeStorageUsageTrendAsync();
    }

    public class CleanupResult
    {
        public bool Success { get; set; }
        public string Operation { get; set; }
        public int ItemsProcessed { get; set; }
        public int ItemsDeleted { get; set; }
        public long BytesFreed { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ErrorMessage { get; set; }
    }

    public class DefragResult
    {
        public bool Success { get; set; }
        public string Drive { get; set; }
        public double FragmentationBefore { get; set; }
        public double FragmentationAfter { get; set; }
        public TimeSpan Duration { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class DbMaintenanceResult
    {
        public bool Success { get; set; }
        public int IndexesRebuilt { get; set; }
        public long StatisticsUpdated { get; set; }
        public TimeSpan Duration { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ServiceHealthResult
    {
        public bool Success { get; set; }
        public List<ServiceRestartInfo> RestartedServices { get; set; } = new();
        public List<ServiceHealthInfo> HealthyServices { get; set; } = new();
        public List<string> FailedServices { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ServiceRestartInfo
    {
        public string ServiceName { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public bool Successful { get; set; }
    }

    public class ServiceHealthInfo
    {
        public string ServiceName { get; set; }
        public string Status { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public int Restarts { get; set; }
        public DateTime LastRestart { get; set; }
    }

    public class ScheduledTasksStatus
    {
        public int TotalTasks { get; set; }
        public int RunningTasks { get; set; }
        public int FailedTasks { get; set; }
        public List<TaskInfo> FailedTaskDetails { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class TaskInfo
    {
        public string TaskName { get; set; }
        public DateTime LastRunTime { get; set; }
        public string LastStatus { get; set; }
        public DateTime NextRunTime { get; set; }
    }

    public class LargeFile
    {
        public string FilePath { get; set; }
        public long SizeBytes { get; set; }
        public DateTime LastAccessTime { get; set; }
        public int DaysSinceAccess { get; set; }
        public string Recommendation { get; set; }
    }

    public class DuplicateFileSet
    {
        public string FileHash { get; set; }
        public int Count { get; set; }
        public long TotalSizeBytes { get; set; }
        public List<string> FilePaths { get; set; } = new();
        public long RecoverableBytes { get; set; } // size - one copy
    }

    public class StorageUsageTrend
    {
        public string Drive { get; set; }
        public long TotalCapacity { get; set; }
        public long CurrentUsage { get; set; }
        public long AvailableSpace { get; set; }
        public List<StorageSnapshot> History { get; set; } = new(); // last 30 days
        public double GrowthRatePerDay { get; set; }
        public int DaysUntilFull { get; set; }
    }

    public class StorageSnapshot
    {
        public DateTime Date { get; set; }
        public long UsedBytes { get; set; }
    }
}
