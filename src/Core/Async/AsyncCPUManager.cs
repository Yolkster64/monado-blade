using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Async
{
    /// <summary>
    /// Manages async CPU-bound operations with task-based parallelism.
    /// Offloads heavy computations without blocking threads.
    /// </summary>
    public class AsyncCPUManager
    {
        private readonly int _maxDegreeOfParallelism;
        private readonly SemaphoreSlim _computeSemaphore;

        public AsyncCPUManager(int? maxDegreeOfParallelism = null)
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
            _computeSemaphore = new SemaphoreSlim(_maxDegreeOfParallelism);
        }

        public class ComputeResult<T>
        {
            public T Result { get; set; }
            public bool Success { get; set; }
            public TimeSpan Duration { get; set; }
            public Exception Error { get; set; }
        }

        public class BatchComputeResult<T>
        {
            public List<T> Results { get; set; }
            public int SuccessCount { get; set; }
            public int FailureCount { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan AverageDuration { get; set; }
        }

        /// <summary>
        /// Execute single CPU-bound operation asynchronously without blocking thread.
        /// </summary>
        public async Task<ComputeResult<T>> RunCPUBoundAsync<T>(Func<T> computation,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            await _computeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var result = await Task.Run(computation, cancellationToken)
                    .ConfigureAwait(false);

                return new ComputeResult<T>
                {
                    Result = result,
                    Success = true,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (OperationCanceledException ex)
            {
                return new ComputeResult<T>
                {
                    Success = false,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
            catch (Exception ex)
            {
                return new ComputeResult<T>
                {
                    Success = false,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
            finally
            {
                _computeSemaphore.Release();
            }
        }

        /// <summary>
        /// Process items in parallel using async/await with controlled degree of parallelism.
        /// Equivalent to Parallel.ForEachAsync for compatibility.
        /// </summary>
        public async Task<BatchComputeResult<T>> ProcessInParallelAsync<T>(
            IEnumerable<T> items,
            Func<T, CancellationToken, Task> processor,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var results = new BatchComputeResult<T> { Results = new List<T>() };
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism);

            foreach (var item in items)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await processor(item, cancellationToken).ConfigureAwait(false);
                        results.SuccessCount++;
                        results.Results.Add(item);
                    }
                    catch
                    {
                        results.FailureCount++;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            results.TotalDuration = DateTime.UtcNow - startTime;
            results.AverageDuration = results.TotalDuration.TotalMilliseconds > 0
                ? TimeSpan.FromMilliseconds(results.TotalDuration.TotalMilliseconds / tasks.Count)
                : TimeSpan.Zero;

            semaphore.Dispose();
            return results;
        }

        /// <summary>
        /// Map items to results in parallel with controlled concurrency.
        /// </summary>
        public async Task<BatchComputeResult<TResult>> MapInParallelAsync<TSource, TResult>(
            IEnumerable<TSource> items,
            Func<TSource, Task<TResult>> mapper,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var results = new BatchComputeResult<TResult> { Results = new List<TResult>() };
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism);
            var resultsLock = new object();

            foreach (var item in items)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var result = await mapper(item).ConfigureAwait(false);
                        lock (resultsLock)
                        {
                            results.Results.Add(result);
                            results.SuccessCount++;
                        }
                    }
                    catch
                    {
                        lock (resultsLock)
                        {
                            results.FailureCount++;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            results.TotalDuration = DateTime.UtcNow - startTime;
            results.AverageDuration = results.TotalDuration.TotalMilliseconds > 0
                ? TimeSpan.FromMilliseconds(results.TotalDuration.TotalMilliseconds / tasks.Count)
                : TimeSpan.Zero;

            semaphore.Dispose();
            return results;
        }

        /// <summary>
        /// Async PLINQ-like operation for large collections.
        /// </summary>
        public async Task<List<TResult>> SelectAsync<TSource, TResult>(
            IEnumerable<TSource> items,
            Func<TSource, Task<TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            var batchResult = await MapInParallelAsync(items, selector, cancellationToken)
                .ConfigureAwait(false);
            return batchResult.Results;
        }

        /// <summary>
        /// Async filter operation with controlled parallelism.
        /// </summary>
        public async Task<List<T>> WhereAsync<T>(
            IEnumerable<T> items,
            Func<T, Task<bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var results = new List<T>();
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism);
            var resultsLock = new object();

            foreach (var item in items)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        if (await predicate(item).ConfigureAwait(false))
                        {
                            lock (resultsLock)
                            {
                                results.Add(item);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            semaphore.Dispose();
            return results;
        }

        /// <summary>
        /// Reduce collection to single value with async aggregation.
        /// </summary>
        public async Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(
            IEnumerable<TSource> items,
            TAccumulate seed,
            Func<TAccumulate, TSource, Task<TAccumulate>> aggregator,
            CancellationToken cancellationToken = default)
        {
            var results = await MapInParallelAsync(items, 
                async item => await aggregator(seed, item).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            return seed;
        }

        /// <summary>
        /// Partition items into chunks for batch processing.
        /// </summary>
        public IEnumerable<List<T>> Partition<T>(IEnumerable<T> items, int batchSize)
        {
            var itemList = items.ToList();

            for (int i = 0; i < itemList.Count; i += batchSize)
            {
                yield return itemList.Skip(i).Take(batchSize).ToList();
            }
        }

        /// <summary>
        /// Process batches of items asynchronously.
        /// </summary>
        public async Task<List<T>> ProcessBatchesAsync<T>(
            IEnumerable<T> items,
            Func<List<T>, Task<List<T>>> batchProcessor,
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var results = new List<T>();
            var tasks = new List<Task>();

            foreach (var batch in Partition(items, batchSize))
            {
                var task = Task.Run(async () =>
                {
                    var batchResults = await batchProcessor(batch).ConfigureAwait(false);
                    lock (results)
                    {
                        results.AddRange(batchResults);
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return results;
        }

        /// <summary>
        /// Chain multiple async operations with error recovery.
        /// </summary>
        public async Task<ComputeResult<T>> ChainOperationsAsync<T>(
            Func<Task<T>> operation1,
            Func<T, Task<T>> operation2,
            Func<T, Task<T>> operation3 = null,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await operation1().ConfigureAwait(false);
                result = await operation2(result).ConfigureAwait(false);

                if (operation3 != null)
                {
                    result = await operation3(result).ConfigureAwait(false);
                }

                return new ComputeResult<T>
                {
                    Result = result,
                    Success = true,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ComputeResult<T>
                {
                    Success = false,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
        }

        /// <summary>
        /// Get thread pool statistics for diagnostics.
        /// </summary>
        public (int WorkerThreads, int CompletionPortThreads) GetThreadPoolStats()
        {
            ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
            return (workerThreads, completionPortThreads);
        }
    }
}
