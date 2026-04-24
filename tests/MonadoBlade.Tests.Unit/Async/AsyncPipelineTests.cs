using Xunit;
using MonadoBlade.Core.Async;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Tests.Unit.Async
{
    public class AsyncPipelineTests
    {
        [Fact]
        public async Task ExecuteAsync_ExecutesOperationsSequentially()
        {
            var executionOrder = new List<int>();
            var pipeline = new AsyncPipeline();

            pipeline.AddOperation(async ct => { executionOrder.Add(1); await Task.Delay(10); })
                    .AddOperation(async ct => { executionOrder.Add(2); await Task.Delay(10); })
                    .AddOperation(async ct => { executionOrder.Add(3); await Task.Delay(10); });

            await pipeline.ExecuteAsync();

            Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
        }

        [Fact]
        public async Task ExecuteParallelAsync_ExecutesOperationsInParallel()
        {
            var sw = Stopwatch.StartNew();
            var pipeline = new AsyncPipeline();

            pipeline.AddOperation(async ct => await Task.Delay(50))
                    .AddOperation(async ct => await Task.Delay(50))
                    .AddOperation(async ct => await Task.Delay(50));

            await pipeline.ExecuteParallelAsync();
            sw.Stop();

            // Parallel should be ~50ms, sequential would be ~150ms
            Assert.True(sw.ElapsedMilliseconds < 100);
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeout_ThrowsOnTimeout()
        {
            var pipeline = new AsyncPipeline(TimeSpan.FromMilliseconds(50), maxRetries: 1);
            pipeline.AddOperation(async ct => await Task.Delay(500, ct));

            await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.ExecuteAsync());
        }

        [Fact]
        public async Task ExecuteAsync_WithRetry_RetriesOnTimeout()
        {
            int attempts = 0;
            var pipeline = new AsyncPipeline(TimeSpan.FromMilliseconds(50), maxRetries: 3);

            pipeline.AddOperation(async ct =>
            {
                attempts++;
                if (attempts < 3)
                    await Task.Delay(500, ct);
                else
                    await Task.Delay(10);
            });

            await pipeline.ExecuteAsync();
            Assert.Equal(3, attempts);
        }

        [Fact]
        public async Task GenericPipeline_ExecuteAsync_ReturnsResults()
        {
            var pipeline = new AsyncPipeline<int>();
            pipeline.AddOperation(async ct => { await Task.Delay(10); return 1; })
                    .AddOperation(async ct => { await Task.Delay(10); return 2; })
                    .AddOperation(async ct => { await Task.Delay(10); return 3; });

            await pipeline.ExecuteAsync();

            Assert.Equal(new[] { 1, 2, 3 }, pipeline.Results);
        }

        [Fact]
        public async Task GenericPipeline_ExecuteFirstAsync_ReturnsFastestResult()
        {
            var results = new List<int>();
            var pipeline = new AsyncPipeline<int>();

            pipeline.AddOperation(async ct => { await Task.Delay(100); return 1; })
                    .AddOperation(async ct => { await Task.Delay(10); return 2; })
                    .AddOperation(async ct => { await Task.Delay(100); return 3; });

            var result = await pipeline.ExecuteFirstAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task AsyncLazy_DeferrsExecution()
        {
            int executionCount = 0;
            var lazy = new AsyncLazy<int>(async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return 42;
            });

            Assert.False(lazy.IsValueCreated);
            
            var value = await lazy.Value;
            
            Assert.Equal(42, value);
            Assert.True(lazy.IsValueCreated);
            Assert.Equal(1, executionCount);
        }

        [Fact]
        public async Task AsyncBatchProcessor_ProcessesInBatches()
        {
            var batches = new List<List<int>>();
            var processor = new AsyncBatchProcessor<int>(async (batch, ct) =>
            {
                batches.Add(new List<int>(batch));
                await Task.Delay(10);
            }, batchSize: 3);

            for (int i = 1; i <= 7; i++)
            {
                await processor.AddAsync(i);
            }
            await processor.FlushAsync();

            Assert.Equal(3, batches.Count);
            Assert.Equal(new[] { 1, 2, 3 }, batches[0]);
            Assert.Equal(new[] { 4, 5, 6 }, batches[1]);
            Assert.Equal(new[] { 7 }, batches[2]);
        }

        [Fact]
        public async Task AsyncBatchProcessor_RespectsBatchSize()
        {
            var batchSizes = new List<int>();
            var processor = new AsyncBatchProcessor<int>(async (batch, ct) =>
            {
                batchSizes.Add(batch.Count);
                await Task.Delay(1);
            }, batchSize: 5);

            for (int i = 1; i <= 23; i++)
            {
                await processor.AddAsync(i);
            }
            await processor.FlushAsync();

            Assert.Equal(new[] { 5, 5, 5, 5, 3 }, batchSizes);
        }

        [Fact]
        public async Task AsyncPipeline_CancellationToken_StopsExecution()
        {
            var cts = new CancellationTokenSource();
            int executedCount = 0;

            var pipeline = new AsyncPipeline();
            pipeline.AddOperation(async ct => { executedCount++; await Task.Delay(10); })
                    .AddOperation(async ct => { cts.Cancel(); await Task.Delay(10); })
                    .AddOperation(async ct => { executedCount++; await Task.Delay(10); });

            await Assert.ThrowsAsync<OperationCanceledException>(() => pipeline.ExecuteAsync(cts.Token));
            Assert.Equal(2, executedCount);
        }
    }
}
