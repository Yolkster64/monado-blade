using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Scheduled task
    /// </summary>
    public class ScheduledTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public Func<Task> TaskFunc { get; set; }
        public int Priority { get; set; } // 0-10 (0=lowest, 10=highest)
        public int[] AffinityMask { get; set; } // CPU cores to use
        public long MaxMemoryBytes { get; set; }
        public ResourceConstraints Constraints { get; set; }
        public DateTime ScheduledFor { get; set; }
        public TimeSpan? RecurrenceInterval { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
    }

    public enum TaskStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Task scheduler interface
    /// </summary>
    public interface ITaskScheduler
    {
        void ScheduleTask(ScheduledTask task);
        void CancelTask(string taskId);
        ScheduledTask GetTask(string taskId);
        IEnumerable<ScheduledTask> GetScheduledTasks();
        IEnumerable<ScheduledTask> GetRunningTasks();
        void SetCpuAffinity(string taskId, int[] cores);
        void SetMemoryLimit(string taskId, long bytes);
        void SetIoPriority(string taskId, int priority);
    }

    /// <summary>
    /// Task scheduler implementation
    /// </summary>
    public class TaskScheduler : ITaskScheduler
    {
        private readonly Dictionary<string, ScheduledTask> _tasks = new();
        private readonly Dictionary<string, Task> _runningTasks = new();
        private readonly object _lock = new();
        private readonly PriorityQueue<ScheduledTask> _taskQueue;
        private readonly EventBus _eventBus;
        private bool _isRunning;

        public TaskScheduler(EventBus eventBus = null)
        {
            _eventBus = eventBus;
            _taskQueue = new PriorityQueue<ScheduledTask>();
        }

        public void ScheduleTask(ScheduledTask task)
        {
            lock (_lock)
            {
                _tasks[task.Id] = task;
                _taskQueue.Enqueue(task, task.Priority);

                _eventBus?.PublishEvent(new Event
                {
                    EventType = "TaskScheduled",
                    Data = new { TaskId = task.Id, TaskName = task.Name, Priority = task.Priority }
                });
            }
        }

        public void CancelTask(string taskId)
        {
            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.Status = TaskStatus.Cancelled;
                    _tasks.Remove(taskId);

                    _eventBus?.PublishEvent(new Event
                    {
                        EventType = "TaskCancelled",
                        Data = new { TaskId = taskId }
                    });
                }
            }
        }

        public ScheduledTask GetTask(string taskId)
        {
            lock (_lock)
            {
                _tasks.TryGetValue(taskId, out var task);
                return task;
            }
        }

        public IEnumerable<ScheduledTask> GetScheduledTasks()
        {
            lock (_lock)
            {
                return new List<ScheduledTask>(_tasks.Values);
            }
        }

        public IEnumerable<ScheduledTask> GetRunningTasks()
        {
            lock (_lock)
            {
                var running = new List<ScheduledTask>();
                foreach (var taskId in _runningTasks.Keys)
                {
                    if (_tasks.TryGetValue(taskId, out var task) && task.Status == TaskStatus.Running)
                    {
                        running.Add(task);
                    }
                }
                return running;
            }
        }

        public void SetCpuAffinity(string taskId, int[] cores)
        {
            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.AffinityMask = cores;
                }
            }
        }

        public void SetMemoryLimit(string taskId, long bytes)
        {
            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.MaxMemoryBytes = bytes;
                    if (task.Constraints == null)
                        task.Constraints = new ResourceConstraints();
                    task.Constraints.MaxMemoryBytes = bytes;
                }
            }
        }

        public void SetIoPriority(string taskId, int priority)
        {
            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    if (task.Constraints == null)
                        task.Constraints = new ResourceConstraints();
                    task.Constraints.IoPriority = priority;
                }
            }
        }

        /// <summary>
        /// Start the scheduler
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            // Scheduler loop would run in a background thread
            Task.Run(() => SchedulerLoop());
        }

        /// <summary>
        /// Stop the scheduler
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        private async Task SchedulerLoop()
        {
            while (_isRunning)
            {
                ScheduledTask nextTask = null;

                lock (_lock)
                {
                    // Get task ready to run
                    if (_taskQueue.Count > 0)
                    {
                        nextTask = _taskQueue.Peek();
                        if (DateTime.UtcNow >= nextTask.ScheduledFor)
                        {
                            _taskQueue.Dequeue();
                        }
                        else
                        {
                            nextTask = null;
                        }
                    }
                }

                if (nextTask != null)
                {
                    await ExecuteTask(nextTask);
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }

        private async Task ExecuteTask(ScheduledTask task)
        {
            try
            {
                task.Status = TaskStatus.Running;

                _eventBus?.PublishEvent(new Event
                {
                    EventType = "TaskStarted",
                    Data = new { TaskId = task.Id, TaskName = task.Name }
                });

                await task.TaskFunc();

                task.Status = TaskStatus.Completed;

                _eventBus?.PublishEvent(new Event
                {
                    EventType = "TaskCompleted",
                    Data = new { TaskId = task.Id, TaskName = task.Name }
                });

                // Reschedule if recurring
                if (task.RecurrenceInterval.HasValue)
                {
                    task.ScheduledFor = DateTime.UtcNow.Add(task.RecurrenceInterval.Value);
                    ScheduleTask(task);
                }
            }
            catch (Exception ex)
            {
                task.Status = TaskStatus.Failed;

                _eventBus?.PublishEvent(new Event
                {
                    EventType = "TaskFailed",
                    Data = new { TaskId = task.Id, TaskName = task.Name, Error = ex.Message }
                });
            }
        }
    }

    /// <summary>
    /// Simple priority queue implementation
    /// </summary>
    public class PriorityQueue<T> where T : class
    {
        private readonly SortedDictionary<int, Queue<T>> _dict = new();
        private int _count = 0;

        public int Count => _count;

        public void Enqueue(T item, int priority)
        {
            if (!_dict.ContainsKey(priority))
            {
                _dict[priority] = new Queue<T>();
            }
            _dict[priority].Enqueue(item);
            _count++;
        }

        public T Dequeue()
        {
            var lastKey = _dict.Keys.Max();
            var item = _dict[lastKey].Dequeue();
            if (_dict[lastKey].Count == 0)
            {
                _dict.Remove(lastKey);
            }
            _count--;
            return item;
        }

        public T Peek()
        {
            var lastKey = _dict.Keys.Max();
            return _dict[lastKey].Peek();
        }
    }
}
