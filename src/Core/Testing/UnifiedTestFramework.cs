// ============================================================================
// MONADO BLADE OPTIMIZATION - COMPREHENSIVE TESTING FRAMEWORK
// Hour 6-7: Creates unified testing and validation utilities
// Expected consolidation: 180+ lines from test boilerplate
// ============================================================================

namespace MonadoBlade.Core.Testing;

using MonadoBlade.Core.Patterns;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Unified test framework - consolidates testing patterns across all modules.
/// CONSOLIDATION: Eliminates duplicate test setup/assertion code.
/// </summary>
public abstract class UnifiedTestBase : IDisposable, IAsyncDisposable
{
    protected readonly IUnifiedLogger Logger;
    protected readonly Stopwatch TestTimer;
    protected readonly List<string> Assertions;
    private ServiceContainer _serviceContainer;

    protected UnifiedTestBase()
    {
        Logger = new ConsoleUnifiedLogger($"Test[{GetType().Name}]");
        TestTimer = new Stopwatch();
        Assertions = new();
    }

    protected ServiceContainer Services
    {
        get
        {
            if (_serviceContainer == null)
            {
                _serviceContainer = new ServiceRegistrationBuilder()
                    .AddUnifiedLogger()
                    .AddAsyncOperationMetrics()
                    .AddMemoryPoolManager()
                    .Build();
                OnConfigureServices(_serviceContainer);
            }
            return _serviceContainer;
        }
    }

    protected virtual void OnConfigureServices(ServiceContainer services) { }

    protected void StartTest(string testName)
    {
        Logger.Information($"Starting: {testName}");
        TestTimer.Restart();
    }

    protected void EndTest(string testName)
    {
        TestTimer.Stop();
        Logger.Information($"Completed: {testName} ({TestTimer.ElapsedMilliseconds}ms)");
    }

    protected void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Assertions.Add($"FAIL: {message}");
            Logger.Error($"Assertion failed: {message}");
        }
        else
        {
            Assertions.Add($"PASS: {message}");
            Logger.Information($"Assertion passed: {message}");
        }
    }

    protected void AssertEqual<T>(T expected, T actual, string message = "")
    {
        bool equal = EqualityComparer<T>.Default.Equals(expected, actual);
        Assert(equal, $"Expected {expected}, got {actual}. {message}");
    }

    protected void AssertNull<T>(T value, string message = "") where T : class
    {
        Assert(value == null, $"Expected null. {message}");
    }

    protected void AssertNotNull<T>(T value, string message = "") where T : class
    {
        Assert(value != null, $"Expected non-null. {message}");
    }

    protected void AssertTrue(bool value, string message = "")
    {
        Assert(value, $"Expected true. {message}");
    }

    protected void AssertFalse(bool value, string message = "")
    {
        Assert(!value, $"Expected false. {message}");
    }

    protected async Task AssertThrowsAsync<TException>(Func<Task> action, string message = "")
        where TException : Exception
    {
        try
        {
            await action();
            Assert(false, $"Expected {typeof(TException).Name} to be thrown. {message}");
        }
        catch (TException)
        {
            Assert(true, $"Correctly threw {typeof(TException).Name}");
        }
        catch (Exception ex)
        {
            Assert(false, $"Expected {typeof(TException).Name}, got {ex.GetType().Name}. {message}");
        }
    }

    protected void PrintTestResults()
    {
        Console.WriteLine("\n=== TEST RESULTS ===");
        var passes = Assertions.Count(a => a.StartsWith("PASS"));
        var fails = Assertions.Count(a => a.StartsWith("FAIL"));
        
        foreach (var assertion in Assertions)
        {
            Console.WriteLine(assertion);
        }
        
        Console.WriteLine($"\nPassed: {passes}/{Assertions.Count}");
        if (fails > 0)
        {
            Console.WriteLine($"Failed: {fails}");
        }
    }

    public void Dispose()
    {
        _serviceContainer?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        _serviceContainer?.Dispose();
        await ValueTask.CompletedTask;
    }
}

/// <summary>
/// Performance benchmark framework - consolidates perf testing patterns.
/// CONSOLIDATION: Single framework for all performance measurements.
/// </summary>
public sealed class BenchmarkRunner
{
    private readonly string _benchmarkName;
    private readonly List<BenchmarkResult> _results;

    public BenchmarkRunner(string benchmarkName)
    {
        _benchmarkName = benchmarkName;
        _results = new();
    }

    public async Task<BenchmarkResult> RunAsync(
        string operationName,
        Func<Task> operation,
        int iterations = 1000)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var sw = Stopwatch.StartNew();
        var exceptions = 0;

        for (int i = 0; i < iterations; i++)
        {
            try
            {
                await operation();
            }
            catch
            {
                exceptions++;
            }
        }

        sw.Stop();

        var result = new BenchmarkResult
        {
            OperationName = operationName,
            Iterations = iterations,
            TotalMs = sw.ElapsedMilliseconds,
            AverageMs = (double)sw.ElapsedMilliseconds / iterations,
            Exceptions = exceptions
        };

        _results.Add(result);
        return result;
    }

    public void PrintResults()
    {
        Console.WriteLine($"\n=== BENCHMARK: {_benchmarkName} ===");
        Console.WriteLine($"{"Operation",-40} | {"Iterations",10} | {"Avg(ms)",10:F4} | {"Total(ms)",10} | {"Errors",6}");
        Console.WriteLine(new string('-', 90));

        foreach (var result in _results)
        {
            Console.WriteLine($"{result.OperationName,-40} | {result.Iterations,10} | {result.AverageMs,10:F4} | {result.TotalMs,10} | {result.Exceptions,6}");
        }

        var avgLatency = _results.Average(r => r.AverageMs);
        Console.WriteLine($"\nOverall average latency: {avgLatency:F4}ms");
    }
}

/// <summary>Result of a benchmark operation.</summary>
public sealed class BenchmarkResult
{
    public string OperationName { get; init; }
    public int Iterations { get; init; }
    public long TotalMs { get; init; }
    public double AverageMs { get; init; }
    public int Exceptions { get; init; }
}

/// <summary>
/// Scenario tester - consolidates end-to-end testing patterns.
/// CONSOLIDATION: Framework for realistic multi-step scenarios.
/// </summary>
public sealed class ScenarioBuilder
{
    private readonly List<(string Name, Func<Task> Action)> _steps;
    private readonly IUnifiedLogger _logger;

    public ScenarioBuilder(string scenarioName)
    {
        _logger = new ConsoleUnifiedLogger($"Scenario[{scenarioName}]");
        _steps = new();
    }

    public ScenarioBuilder Step(string stepName, Func<Task> action)
    {
        _steps.Add((stepName, action ?? throw new ArgumentNullException(nameof(action))));
        return this;
    }

    public async Task ExecuteAsync()
    {
        var sw = Stopwatch.StartNew();
        int completed = 0;

        foreach (var (name, action) in _steps)
        {
            try
            {
                _logger.Information($"Executing: {name}");
                await action();
                completed++;
                _logger.Information($"Completed: {name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed: {name}", ex);
                break;
            }
        }

        sw.Stop();
        _logger.Information($"Scenario finished: {completed}/{_steps.Count} steps in {sw.ElapsedMilliseconds}ms");
    }
}

/// <summary>
/// Test data generator - consolidates fixture creation patterns.
/// CONSOLIDATION: Standardizes test data generation.
/// </summary>
public static class TestDataGenerator
{
    private static readonly Random _random = new Random();

    public static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }

    public static int RandomInt(int min = 0, int max = 100)
    {
        return _random.Next(min, max);
    }

    public static List<T> RandomList<T>(int count, Func<T> generator)
    {
        var list = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(generator());
        }
        return list;
    }

    public static byte[] RandomBytes(int length = 1024)
    {
        var data = new byte[length];
        _random.NextBytes(data);
        return data;
    }
}

/// <summary>
/// Mock service for testing - consolidates mock patterns.
/// CONSOLIDATION: Base class for all mock implementations.
/// </summary>
public abstract class MockServiceBase<T> : UnifiedServiceBase where T : MockServiceBase<T>
{
    protected List<string> CallLog { get; }

    protected MockServiceBase(string serviceId) : base(serviceId)
    {
        CallLog = new();
    }

    protected void LogCall(string methodName, params object[] args)
    {
        CallLog.Add($"{methodName}({string.Join(", ", args)})");
    }

    public void PrintCallLog()
    {
        Console.WriteLine($"\n=== MOCK CALL LOG: {ServiceId} ===");
        foreach (var call in CallLog)
        {
            Console.WriteLine($"  - {call}");
        }
    }
}

/// <summary>Assertion helpers for async operations.</summary>
public static class AsyncAssertions
{
    public static async Task AssertResultSuccessAsync<T>(AsyncOperationResult<T> result, string message = "")
    {
        if (!result.IsSuccess)
        {
            throw new AssertionException(
                $"Expected successful result. Got error: {result.Error?.Message}. {message}");
        }
        await Task.CompletedTask;
    }

    public static async Task AssertResultFailureAsync<T>(AsyncOperationResult<T> result, string message = "")
    {
        if (result.IsSuccess)
        {
            throw new AssertionException($"Expected failed result. {message}");
        }
        await Task.CompletedTask;
    }

    public static async Task AssertCompleteWithinAsync(Task task, TimeSpan timeout, string message = "")
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
        if (completedTask != task)
        {
            throw new AssertionException($"Task did not complete within {timeout}. {message}");
        }
    }
}

/// <summary>Custom exception for test assertions.</summary>
public sealed class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
