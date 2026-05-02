using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.Framework;

namespace MonadoBlade.AI.Hub.V3
{
    /// <summary>
    /// HermesAgent - AI agent with async communication and learning capabilities
    /// Core component of Monado Blade v3 agent fleet
    /// </summary>
    public class HermesAgent : IHELIOSService
    {
        private readonly string _agentId;
        private readonly ILogger<HermesAgent> _logger;
        private readonly IHermesAgentCommunicationBus _communicationBus;
        private readonly HermesAgentLearningEngine _learningEngine;
        
        private AgentState _state = AgentState.Ready;
        private readonly ConcurrentDictionary<string, ExecutionMetrics> _executionHistory = new();
        private readonly ConcurrentBag<AgentFeedback> _feedback = new();
        private Stopwatch _uptime = Stopwatch.StartNew();

        public AgentCapabilities Capabilities { get; private set; } = new();

        public HermesAgent(string agentId, ILogger<HermesAgent> logger, 
            IHermesAgentCommunicationBus communicationBus,
            HermesAgentLearningEngine learningEngine)
        {
            _agentId = agentId;
            _logger = logger;
            _communicationBus = communicationBus;
            _learningEngine = learningEngine;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgent {AgentId} starting", _agentId);
            _state = AgentState.Ready;
            await _communicationBus.SubscribeAsync(_agentId, "agent-tasks", HandleIncomingTaskAsync);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgent {AgentId} stopping", _agentId);
            _state = AgentState.Offline;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Initialize agent with capabilities
        /// </summary>
        public async Task InitializeAsync(AgentCapabilities capabilities)
        {
            Capabilities = capabilities;
            _logger.LogInformation("HermesAgent {AgentId} initialized with capabilities: {Capabilities}", 
                _agentId, string.Join(", ", capabilities.Specializations));
            await Task.CompletedTask;
        }

        /// <summary>
        /// Send message to another agent (request help/feedback)
        /// </summary>
        public async Task<AgentResponse> SendMessageAsync(string targetAgentId, AgentRequest request)
        {
            if (_state == AgentState.Offline)
                throw new InvalidOperationException("Agent is offline");

            try
            {
                _logger.LogInformation("Agent {From} sending message to {To}: {RequestType}", 
                    _agentId, targetAgentId, request.Type);

                _state = AgentState.Waiting;
                var stopwatch = Stopwatch.StartNew();

                var response = await _communicationBus.SendAsync<AgentResponse>(
                    _agentId, targetAgentId, request, timeoutMs: 5000);

                stopwatch.Stop();
                _logger.LogInformation("Response received in {Latency}ms", stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to {TargetAgent}", targetAgentId);
                throw;
            }
            finally
            {
                _state = AgentState.Ready;
            }
        }

        /// <summary>
        /// Handle incoming message
        /// </summary>
        public async Task<AgentResponse> HandleIncomingMessageAsync(AgentRequest request)
        {
            _logger.LogInformation("Agent {AgentId} handling incoming request: {Type}", _agentId, request.Type);

            try
            {
                // Process request based on type
                var result = await ProcessRequestAsync(request);
                
                return new AgentResponse
                {
                    Success = true,
                    Content = result,
                    SourceAgentId = _agentId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                return new AgentResponse { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Execute task with metrics collection
        /// </summary>
        public async Task<ExecutionResult> ExecuteAsync(AgentTask task)
        {
            if (_state == AgentState.Offline)
                throw new InvalidOperationException("Agent is offline");

            _state = AgentState.Busy;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Agent {AgentId} executing task: {TaskId}", _agentId, task.Id);

                // Simulate task execution
                await Task.Delay(Random.Shared.Next(100, 1000));

                stopwatch.Stop();

                var result = new ExecutionResult
                {
                    TaskId = task.Id,
                    AgentId = _agentId,
                    Success = true,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    TokensUsed = Random.Shared.Next(100, 5000),
                    OutputQuality = 0.85 + (Random.Shared.NextDouble() * 0.15),
                    Timestamp = DateTime.UtcNow
                };

                // Record metrics for learning
                _executionHistory[task.Id] = new ExecutionMetrics
                {
                    LatencyMs = result.LatencyMs,
                    TokensUsed = result.TokensUsed,
                    QualityScore = result.OutputQuality,
                    Success = result.Success
                };

                _logger.LogInformation("Task {TaskId} completed: {LatencyMs}ms, Quality: {Quality}", 
                    task.Id, result.LatencyMs, result.OutputQuality);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task execution failed: {TaskId}", task.Id);
                throw;
            }
            finally
            {
                _state = AgentState.Ready;
            }
        }

        /// <summary>
        /// Record feedback (human rating) → learn
        /// </summary>
        public void RecordFeedback(ExecutionResult result, float quality)
        {
            var feedback = new AgentFeedback
            {
                TaskId = result.TaskId,
                HumanRating = quality,
                Timestamp = DateTime.UtcNow
            };

            _feedback.Add(feedback);
            _learningEngine.RecordFeedback(_agentId, feedback);

            _logger.LogInformation("Feedback recorded for task {TaskId}: {Rating}", result.TaskId, quality);
        }

        /// <summary>
        /// Get agent metrics
        /// </summary>
        public AgentMetrics GetMetrics()
        {
            var successful = _executionHistory.Values.Count(m => m.Success);
            var avgLatency = _executionHistory.Values.Any() 
                ? _executionHistory.Values.Average(m => m.LatencyMs) 
                : 0;

            return new AgentMetrics
            {
                AgentId = _agentId,
                State = _state.ToString(),
                UptimeMinutes = (int)_uptime.Elapsed.TotalMinutes,
                ExecutionCount = _executionHistory.Count,
                SuccessCount = successful,
                SuccessRate = _executionHistory.Count > 0 ? (successful * 100.0) / _executionHistory.Count : 100,
                AverageLatencyMs = avgLatency,
                FeedbackCount = _feedback.Count,
                QualityScore = _learningEngine.GetQualityScore(_agentId),
                Timestamp = DateTime.UtcNow
            };
        }

        private async Task<string> ProcessRequestAsync(AgentRequest request)
        {
            // Simulate processing based on request type
            return await Task.FromResult($"Processed by {_agentId}: {request.Content}");
        }

        public string ServiceName => $"HermesAgent-{_agentId}";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;

        public Dictionary<string, object> GetMetricsDict()
        {
            var metrics = GetMetrics();
            return new()
            {
                { "State", metrics.State },
                { "ExecutionCount", metrics.ExecutionCount },
                { "SuccessRate", metrics.SuccessRate },
                { "AverageLatencyMs", metrics.AverageLatencyMs },
                { "QualityScore", metrics.QualityScore }
            };
        }
    }

    /// <summary>
    /// HermesAgentCommunicationBus - Pub/Sub message routing
    /// </summary>
    public interface IHermesAgentCommunicationBus
    {
        Task SubscribeAsync(string agentId, string topic, Func<AgentRequest, Task> handler);
        Task PublishAsync(string topic, AgentMessage message);
        Task<T> SendAsync<T>(string fromAgentId, string toAgentId, AgentRequest request, int timeoutMs = 5000);
    }

    public class HermesAgentCommunicationBus : IHermesAgentCommunicationBus, IHELIOSService
    {
        private readonly ILogger<HermesAgentCommunicationBus> _logger;
        private readonly ConcurrentDictionary<string, List<Func<AgentRequest, Task>>> _subscriptions = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _pendingResponses = new();

        public HermesAgentCommunicationBus(ILogger<HermesAgentCommunicationBus> logger)
        {
            _logger = logger;
        }

        public async Task SubscribeAsync(string agentId, string topic, Func<AgentRequest, Task> handler)
        {
            var subscribers = _subscriptions.GetOrAdd(topic, _ => new List<Func<AgentRequest, Task>>());
            subscribers.Add(handler);
            _logger.LogInformation("Agent {AgentId} subscribed to topic {Topic}", agentId, topic);
            await Task.CompletedTask;
        }

        public async Task PublishAsync(string topic, AgentMessage message)
        {
            if (_subscriptions.TryGetValue(topic, out var subscribers))
            {
                var tasks = subscribers.Select(s => s(new AgentRequest { Content = message.Content }));
                await Task.WhenAll(tasks);
            }
        }

        public async Task<T> SendAsync<T>(string fromAgentId, string toAgentId, AgentRequest request, int timeoutMs = 5000)
        {
            var correlationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<object>();
            
            _pendingResponses[correlationId] = tcs;

            try
            {
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                    throw new TimeoutException($"Response from {toAgentId} timed out after {timeoutMs}ms");

                var result = await tcs.Task;
                return (T)result!;
            }
            finally
            {
                _pendingResponses.TryRemove(correlationId, out _);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgentCommunicationBus started");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgentCommunicationBus stopped");
            await Task.CompletedTask;
        }

        public string ServiceName => "HermesAgentCommunicationBus";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "Topics", _subscriptions.Count },
            { "PendingResponses", _pendingResponses.Count }
        };
    }

    /// <summary>
    /// HermesAgentRegistry - Agent lifecycle and discovery
    /// </summary>
    public class HermesAgentRegistry : IHELIOSService
    {
        private readonly ILogger<HermesAgentRegistry> _logger;
        private readonly ConcurrentDictionary<string, AgentRegistration> _agents = new();
        private Timer? _healthCheckTimer;

        public HermesAgentRegistry(ILogger<HermesAgentRegistry> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgentRegistry started");
            _healthCheckTimer = new Timer(_ => CheckAgentHealth(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgentRegistry stopped");
            _healthCheckTimer?.Dispose();
            await Task.CompletedTask;
        }

        public void RegisterAgent(string agentId, HermesAgent agent, AgentCapabilities capabilities)
        {
            var registration = new AgentRegistration
            {
                Id = agentId,
                Agent = agent,
                Capabilities = capabilities,
                RegisteredAt = DateTime.UtcNow,
                Status = AgentStatus.Ready
            };

            _agents[agentId] = registration;
            _logger.LogInformation("Agent {AgentId} registered with capabilities: {Capabilities}", 
                agentId, string.Join(", ", capabilities.Specializations));
        }

        public HermesAgent GetAgent(string agentId)
        {
            if (_agents.TryGetValue(agentId, out var registration))
                return registration.Agent;

            throw new KeyNotFoundException($"Agent {agentId} not found");
        }

        public List<HermesAgent> FindAgentsByCapability(string capability)
        {
            return _agents.Values
                .Where(r => r.Capabilities.Specializations.Contains(capability))
                .Select(r => r.Agent)
                .ToList();
        }

        public AgentStatus GetAgentStatus(string agentId)
        {
            if (_agents.TryGetValue(agentId, out var registration))
                return registration.Status;

            return AgentStatus.Offline;
        }

        public Dictionary<string, AgentStatus> GetAllAgentStatuses()
        {
            return _agents.ToDictionary(x => x.Key, x => x.Value.Status);
        }

        private void CheckAgentHealth()
        {
            foreach (var registration in _agents.Values)
            {
                // Simplified health check
                var metrics = registration.Agent.GetMetrics();
                registration.Status = metrics.State == "Offline" ? AgentStatus.Offline : AgentStatus.Ready;
            }
        }

        public string ServiceName => "HermesAgentRegistry";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "RegisteredAgents", _agents.Count },
            { "ActiveAgents", _agents.Values.Count(a => a.Status == AgentStatus.Ready) }
        };
    }

    /// <summary>
    /// HermesAgentLearningEngine - Continuous learning and improvement
    /// </summary>
    public class HermesAgentLearningEngine : IHELIOSService
    {
        private readonly ILogger<HermesAgentLearningEngine> _logger;
        private readonly ConcurrentDictionary<string, AgentLearningProfile> _learningProfiles = new();

        public HermesAgentLearningEngine(ILogger<HermesAgentLearningEngine> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgentLearningEngine started");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("HermesAgentLearningEngine stopped");
            await Task.CompletedTask;
        }

        public void RecordMetrics(string agentId, string taskType, ExecutionMetrics metrics)
        {
            var profile = _learningProfiles.GetOrAdd(agentId, _ => new AgentLearningProfile { AgentId = agentId });
            profile.AddMetrics(taskType, metrics);
            
            _logger.LogInformation("Metrics recorded for agent {AgentId}, task type {TaskType}", agentId, taskType);
        }

        public float GetQualityScore(string agentId)
        {
            if (_learningProfiles.TryGetValue(agentId, out var profile))
                return profile.GetOverallQualityScore();

            return 0;
        }

        public string RecommendAgent(string taskType, List<string> availableAgents)
        {
            var bestAgent = availableAgents.OrderByDescending(a =>
            {
                if (_learningProfiles.TryGetValue(a, out var profile))
                    return profile.GetQualityScoreForTaskType(taskType);
                return 0;
            }).FirstOrDefault();

            return bestAgent ?? availableAgents.FirstOrDefault() ?? string.Empty;
        }

        public LearningHistory GetLearningHistory(string agentId)
        {
            if (_learningProfiles.TryGetValue(agentId, out var profile))
            {
                return new LearningHistory
                {
                    AgentId = agentId,
                    TotalExecutions = profile.Executions.Count,
                    SuccessRate = profile.GetSuccessRate(),
                    AverageQuality = profile.GetOverallQualityScore(),
                    TaskTypeScores = profile.TaskTypeQualityScores
                };
            }

            return new LearningHistory();
        }

        public void RecordFeedback(string agentId, AgentFeedback feedback)
        {
            var profile = _learningProfiles.GetOrAdd(agentId, _ => new AgentLearningProfile { AgentId = agentId });
            profile.AddFeedback(feedback);
        }

        public string ServiceName => "HermesAgentLearningEngine";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "TrackedAgents", _learningProfiles.Count },
            { "TotalExecutions", _learningProfiles.Values.Sum(p => p.Executions.Count) }
        };
    }

    // Supporting types
    public class AgentCapabilities
    {
        public List<string> Specializations { get; set; } = new();
        public int MaxConcurrentTasks { get; set; } = 5;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class AgentRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AgentResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string SourceAgentId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AgentTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Priority { get; set; } = 5;
    }

    public class ExecutionResult
    {
        public string TaskId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public int TokensUsed { get; set; }
        public double OutputQuality { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ExecutionMetrics
    {
        public long LatencyMs { get; set; }
        public int TokensUsed { get; set; }
        public double QualityScore { get; set; }
        public bool Success { get; set; }
    }

    public class AgentMessage
    {
        public string Content { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AgentMetrics
    {
        public string AgentId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int UptimeMinutes { get; set; }
        public int ExecutionCount { get; set; }
        public int SuccessCount { get; set; }
        public double SuccessRate { get; set; }
        public double AverageLatencyMs { get; set; }
        public int FeedbackCount { get; set; }
        public float QualityScore { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AgentFeedback
    {
        public string TaskId { get; set; } = string.Empty;
        public float HumanRating { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LearningHistory
    {
        public string AgentId { get; set; } = string.Empty;
        public int TotalExecutions { get; set; }
        public double SuccessRate { get; set; }
        public float AverageQuality { get; set; }
        public Dictionary<string, float> TaskTypeScores { get; set; } = new();
    }

    internal class AgentRegistration
    {
        public string Id { get; set; } = string.Empty;
        public HermesAgent Agent { get; set; } = null!;
        public AgentCapabilities Capabilities { get; set; } = new();
        public DateTime RegisteredAt { get; set; }
        public AgentStatus Status { get; set; }
    }

    internal class AgentLearningProfile
    {
        public string AgentId { get; set; } = string.Empty;
        public List<ExecutionMetrics> Executions { get; set; } = new();
        public Dictionary<string, float> TaskTypeQualityScores { get; set; } = new();
        public List<AgentFeedback> Feedback { get; set; } = new();

        public void AddMetrics(string taskType, ExecutionMetrics metrics)
        {
            Executions.Add(metrics);
            if (!TaskTypeQualityScores.ContainsKey(taskType))
                TaskTypeQualityScores[taskType] = 0;
            TaskTypeQualityScores[taskType] = (float)metrics.QualityScore;
        }

        public void AddFeedback(AgentFeedback feedback)
        {
            Feedback.Add(feedback);
        }

        public float GetOverallQualityScore()
        {
            return Executions.Any() ? (float)Executions.Average(e => e.QualityScore) : 0;
        }

        public float GetQualityScoreForTaskType(string taskType)
        {
            return TaskTypeQualityScores.TryGetValue(taskType, out var score) ? score : 0;
        }

        public double GetSuccessRate()
        {
            if (!Executions.Any()) return 0;
            return (Executions.Count(e => e.Success) * 100.0) / Executions.Count;
        }
    }

    public enum AgentState { Ready, Busy, Waiting, Offline }
    public enum AgentStatus { Ready, Busy, Offline }
}
