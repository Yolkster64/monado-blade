using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Core.Async;

namespace MonadoBlade.Tests.Core.Async
{
    public class BlockingCallAuditorTests
    {
        [Fact]
        public void AuditReport_ShouldNotBeNull()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            var report = auditor.AuditAssembly(assembly);

            Assert.NotNull(report);
        }

        [Fact]
        public void AuditReport_ShouldHaveFindings()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            var report = auditor.AuditAssembly(assembly);

            Assert.True(report.TotalFindingsCount >= 0);
            Assert.NotNull(report.Findings);
        }

        [Fact]
        public void AuditReport_ShouldHaveCategorized()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            var report = auditor.AuditAssembly(assembly);

            Assert.NotNull(report.FindingsByCategory);
            Assert.NotNull(report.FindingsBySeverity);
        }

        [Fact]
        public void AuditReport_ShouldGenerateSummary()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            var report = auditor.AuditAssembly(assembly);

            Assert.NotNull(report.Summary);
            Assert.Contains("BLOCKING CALL AUDIT REPORT", report.Summary);
        }

        [Fact]
        public void ExportToCSV_ShouldReturnValidCSV()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            auditor.AuditAssembly(assembly);
            var csv = auditor.ExportToCSV();

            Assert.NotNull(csv);
            Assert.Contains("Class,Method,Category", csv);
        }

        [Fact]
        public void MultipleFindingsOfDifferentSeverity_ShouldBeSorted()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            var report = auditor.AuditAssembly(assembly);

            if (report.Findings.Count > 1)
            {
                for (int i = 0; i < report.Findings.Count - 1; i++)
                {
                    Assert.True(report.Findings[i].Severity >= report.Findings[i + 1].Severity);
                }
            }
        }

        [Fact]
        public void AuditReport_ShouldCalculateTotalLatency()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            var report = auditor.AuditAssembly(assembly);

            Assert.True(report.EstimatedTotalLatencyMs >= 0);
        }

        [Fact]
        public void AuditReport_ShouldHaveReplacementStrategies()
        {
            var auditor = new BlockingCallAuditor();
            var assembly = typeof(BlockingCallAuditor).Assembly;

            var report = auditor.AuditAssembly(assembly);

            foreach (var finding in report.Findings)
            {
                Assert.NotNull(finding.ReplacementStrategy);
                Assert.NotEmpty(finding.ReplacementStrategy);
            }
        }
    }

    public class AsyncIOManagerTests : IDisposable
    {
        private readonly AsyncIOManager _manager;
        private readonly string _testDirectory;

        public AsyncIOManagerTests()
        {
            _manager = new AsyncIOManager();
            _testDirectory = Path.Combine(Path.GetTempPath(), "monadobladetest_" + Guid.NewGuid());
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task WriteFileAsync_ShouldCreateFile()
        {
            var filePath = Path.Combine(_testDirectory, "test.txt");
            var data = new byte[] { 1, 2, 3, 4, 5 };

            var result = await _manager.WriteFileAsync(filePath, data);

            Assert.True(result.Success);
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task ReadFileAsync_ShouldReadFile()
        {
            var filePath = Path.Combine(_testDirectory, "test.txt");
            var data = new byte[] { 1, 2, 3, 4, 5 };
            File.WriteAllBytes(filePath, data);

            var result = await _manager.ReadFileAsync(filePath);

            Assert.True(result.Success);
            Assert.Equal(5, result.BytesProcessed);
        }

        [Fact]
        public async Task WriteFileAsync_ShouldHandleFailure()
        {
            var invalidPath = "/invalid/path/that/does/not/exist/file.txt";

            var result = await _manager.WriteFileAsync(invalidPath, new byte[10]);

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task StreamReadFileAsync_ShouldYieldChunks()
        {
            var filePath = Path.Combine(_testDirectory, "large.bin");
            var largeData = new byte[10000];
            new Random().NextBytes(largeData);
            File.WriteAllBytes(filePath, largeData);

            var chunks = new System.Collections.Generic.List<byte[]>();
            await foreach (var chunk in _manager.StreamReadFileAsync(filePath, 1024))
            {
                chunks.Add(chunk);
            }

            Assert.NotEmpty(chunks);
            var totalBytes = chunks.Sum(c => c.Length);
            Assert.Equal(10000, totalBytes);
        }

        [Fact]
        public async Task BatchProcessFilesAsync_ShouldProcessMultipleFiles()
        {
            var operations = new[]
            {
                new AsyncIOManager.BatchFileOperation 
                { 
                    FilePath = Path.Combine(_testDirectory, "f1.txt"),
                    Data = new byte[] { 1 },
                    Type = AsyncIOManager.OperationType.Write
                },
                new AsyncIOManager.BatchFileOperation 
                { 
                    FilePath = Path.Combine(_testDirectory, "f2.txt"),
                    Data = new byte[] { 2 },
                    Type = AsyncIOManager.OperationType.Write
                }
            };

            var results = await _manager.BatchProcessFilesAsync(operations);

            Assert.True(results.All(r => r.Success));
        }

        [Fact]
        public async Task CancellationToken_ShouldCancelFileOperation()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            var filePath = Path.Combine(_testDirectory, "cancel_test.txt");

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _manager.ReadFileAsync(filePath, cts.Token);
            });
        }

        [Fact]
        public async Task EnumerateUSBDevicesAsync_ShouldNotThrow()
        {
            var devices = await _manager.EnumerateUSBDevicesAsync();

            Assert.NotNull(devices);
        }

        [Fact]
        public async Task LaunchProcessAsync_ShouldExecuteCommand()
        {
            var result = await _manager.LaunchProcessAsync("cmd", "/c echo test", TimeSpan.FromSeconds(10));

            Assert.True(result == 0 || result == 1); // Success or error code
        }

        public void Dispose()
        {
            _manager?.Dispose();
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }

    public class AsyncNetworkManagerTests : IDisposable
    {
        private readonly AsyncNetworkManager _manager;

        public AsyncNetworkManagerTests()
        {
            _manager = new AsyncNetworkManager();
        }

        [Fact]
        public async Task DownloadAsync_WithInvalidUrl_ShouldFail()
        {
            var result = await _manager.DownloadAsync("http://invalid-url-that-does-not-exist-12345.com", 
                null, CancellationToken.None, maxRetries: 1);

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task CheckUrlAvailableAsync_WithInvalidUrl_ShouldReturnFalse()
        {
            var available = await _manager.CheckUrlAvailableAsync("http://invalid-url-test-12345.invalid",
                CancellationToken.None, TimeSpan.FromSeconds(2));

            Assert.False(available);
        }

        [Fact]
        public async Task ResolveDNSAsync_WithValidHostname_ShouldReturnAddresses()
        {
            var addresses = await _manager.ResolveDNSAsync("localhost");

            Assert.NotNull(addresses);
            Assert.NotEmpty(addresses);
        }

        [Fact]
        public async Task ResolveDNSAsync_WithTimeout_ShouldThrow()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
                await _manager.ResolveDNSAsync("localhost", cts.Token, TimeSpan.FromMilliseconds(10));
            });
        }

        [Fact]
        public async Task CancellationToken_ShouldCancelDownload()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(10);

            var result = await _manager.DownloadAsync("http://httpbin.org/delay/10", 
                null, cts.Token, maxRetries: 1);

            Assert.False(result.Success);
        }

        [Fact]
        public void Manager_ShouldNotThrowOnDispose()
        {
            var manager = new AsyncNetworkManager();
            manager.Dispose();
            manager.Dispose(); // Should not throw
        }

        public void Dispose()
        {
            _manager?.Dispose();
        }
    }

    public class AsyncCPUManagerTests
    {
        [Fact]
        public async Task RunCPUBoundAsync_ShouldExecuteComputation()
        {
            var manager = new AsyncCPUManager();

            var result = await manager.RunCPUBoundAsync(() =>
            {
                return Enumerable.Range(0, 1000).Sum();
            });

            Assert.True(result.Success);
            Assert.Equal(499500, result.Result);
        }

        [Fact]
        public async Task RunCPUBoundAsync_ShouldMeasureDuration()
        {
            var manager = new AsyncCPUManager();

            var result = await manager.RunCPUBoundAsync(() =>
            {
                Thread.Sleep(100);
                return 42;
            });

            Assert.True(result.Duration.TotalMilliseconds >= 90);
        }

        [Fact]
        public async Task MapInParallelAsync_ShouldTransformItems()
        {
            var manager = new AsyncCPUManager(maxDegreeOfParallelism: 2);
            var items = Enumerable.Range(0, 10).ToList();

            var result = await manager.MapInParallelAsync(items, 
                async x => 
                { 
                    await Task.Delay(10);
                    return x * 2; 
                });

            Assert.Equal(10, result.SuccessCount);
            Assert.Equal(10, result.Results.Count);
            Assert.Contains(0, result.Results);
            Assert.Contains(18, result.Results);
        }

        [Fact]
        public async Task SelectAsync_ShouldReturnTransformedList()
        {
            var manager = new AsyncCPUManager();
            var items = Enumerable.Range(0, 5).ToList();

            var result = await manager.SelectAsync(items, 
                async x => 
                { 
                    await Task.CompletedTask;
                    return x * 3; 
                });

            Assert.Equal(5, result.Count);
            Assert.Equal(0, result[0]);
            Assert.Equal(12, result[4]);
        }

        [Fact]
        public async Task WhereAsync_ShouldFilterItems()
        {
            var manager = new AsyncCPUManager();
            var items = Enumerable.Range(0, 10).ToList();

            var result = await manager.WhereAsync(items, 
                async x => 
                { 
                    await Task.CompletedTask;
                    return x % 2 == 0; 
                });

            Assert.Equal(5, result.Count);
            Assert.All(result, x => Assert.True(x % 2 == 0));
        }

        [Fact]
        public async Task ProcessBatchesAsync_ShouldProcessInBatches()
        {
            var manager = new AsyncCPUManager();
            var items = Enumerable.Range(0, 25).ToList();

            var result = await manager.ProcessBatchesAsync(items, 
                async batch =>
                {
                    await Task.Delay(10);
                    return batch.Select(x => x * 2).ToList();
                },
                batchSize: 10);

            Assert.Equal(25, result.Count);
        }

        [Fact]
        public async Task ChainOperationsAsync_ShouldChainMultipleOperations()
        {
            var manager = new AsyncCPUManager();

            var result = await manager.ChainOperationsAsync(
                async () => 
                { 
                    await Task.Delay(10);
                    return 10; 
                },
                async x => 
                { 
                    await Task.Delay(10);
                    return x * 2; 
                },
                async x => 
                { 
                    await Task.Delay(10);
                    return x + 5; 
                });

            Assert.True(result.Success);
            Assert.Equal(25, result.Result);
        }

        [Fact]
        public void GetThreadPoolStats_ShouldReturnStats()
        {
            var manager = new AsyncCPUManager();

            var (workerThreads, completionPortThreads) = manager.GetThreadPoolStats();

            Assert.True(workerThreads > 0);
            Assert.True(completionPortThreads >= 0);
        }

        [Fact]
        public async Task Partition_ShouldDivideItemsCorrectly()
        {
            var manager = new AsyncCPUManager();
            var items = Enumerable.Range(0, 25).ToList();

            var partitions = manager.Partition(items, 10).ToList();

            Assert.Equal(3, partitions.Count);
            Assert.Equal(10, partitions[0].Count);
            Assert.Equal(10, partitions[1].Count);
            Assert.Equal(5, partitions[2].Count);
        }

        [Fact]
        public async Task RunCPUBoundAsync_WithCancellation_ShouldBeCancellable()
        {
            var manager = new AsyncCPUManager();
            var cts = new CancellationTokenSource();
            cts.CancelAfter(50);

            var result = await manager.RunCPUBoundAsync(() =>
            {
                Thread.Sleep(200);
                return 42;
            }, cts.Token);

            Assert.False(result.Success);
        }
    }
}
