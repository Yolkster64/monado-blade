using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Architecture;
using HELIOS.Platform.Architecture.ObjectPooling;

namespace MonadoBlade.Tests.ObjectPooling
{
    /// <summary>
    /// Comprehensive test suite for MessagePool
    /// Tests object reuse, concurrent access, memory bounds, and performance
    /// </summary>
    public class MessagePoolTests
    {
        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            using (var pool = new MessagePool(10, 50))
            {
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(10, stats.PooledObjectCount);
                Assert.Equal(10, stats.TotalAllocations);
                Assert.Equal(0, stats.PoolHits);
                Assert.Equal(0, stats.PoolMisses);
            }
        }

        [Fact]
        public void GetMessage_FromPopulatedPool_ReturnsPooledObject()
        {
            // Arrange
            using (var pool = new MessagePool(5, 20))
            {
                // Act
                var msg1 = pool.GetMessage();
                var msg2 = pool.GetMessage();

                // Assert
                Assert.NotNull(msg1);
                Assert.NotNull(msg2);
                Assert.NotEqual(msg1.Id, msg2.Id); // Different objects
                var stats = pool.GetStatistics();
                Assert.Equal(2, stats.PoolHits);
            }
        }

        [Fact]
        public void GetMessage_FromEmptyPool_CreatesNewObject()
        {
            // Arrange
            using (var pool = new MessagePool(0, 20))
            {
                // Act
                var msg = pool.GetMessage();

                // Assert
                Assert.NotNull(msg);
                var stats = pool.GetStatistics();
                Assert.Equal(1, stats.PoolMisses);
                Assert.Equal(1, stats.TotalAllocations);
            }
        }

        [Fact]
        public void ReturnMessage_ReturnsObjectToPool()
        {
            // Arrange
            using (var pool = new MessagePool(5, 20))
            {
                var msg = pool.GetMessage();
                msg.FromService = "TestService";
                msg.MessageType = "TestType";

                // Act
                pool.ReturnMessage(msg);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(5, stats.PooledObjectCount); // Back to original + returned
                Assert.Equal(1, stats.ResetCount);
                Assert.Null(msg.FromService); // State should be reset
                Assert.Null(msg.MessageType);
            }
        }

        [Fact]
        public void ReturnMessage_WhenPoolFull_RejectsObject()
        {
            // Arrange
            using (var pool = new MessagePool(2, 2))
            {
                var msg = new ServiceMessage();

                // Act
                pool.ReturnMessage(msg);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(2, stats.PooledObjectCount); // Should not grow beyond max
            }
        }

        [Fact]
        public void ConcurrentAccess_HandlesConcurrentGetReturn_ThreadSafely()
        {
            // Arrange
            using (var pool = new MessagePool(10, 100))
            {
                var messages = new List<ServiceMessage>();
                var lockObj = new object();

                // Act
                Parallel.For(0, 50, new ParallelOptions { MaxDegreeOfParallelism = 10 }, i =>
                {
                    var msg = pool.GetMessage();
                    lock (lockObj) messages.Add(msg);
                    
                    System.Threading.Thread.Sleep(1); // Simulate work
                    
                    pool.ReturnMessage(msg);
                });

                var stats = pool.GetStatistics();

                // Assert
                Assert.True(stats.PoolHits > 0); // Some should have hit the pool
                Assert.True(stats.PoolMisses > 0); // Some should have missed
                Assert.NotNull(messages);
                Assert.Equal(50, messages.Count);
            }
        }

        [Fact]
        public void MemoryTracking_TracksMemoryUsage()
        {
            // Arrange
            using (var pool = new MessagePool(5, 50))
            {
                var stats1 = pool.GetStatistics();

                // Act
                pool.Prefill(30);
                var stats2 = pool.GetStatistics();

                // Assert
                Assert.True(stats2.CurrentMemoryUsageBytes > stats1.CurrentMemoryUsageBytes);
                Assert.True(stats2.PeakMemoryUsageBytes >= stats2.CurrentMemoryUsageBytes);
            }
        }

        [Fact]
        public void HitRateCalculation_CalculatesCorrectly()
        {
            // Arrange
            using (var pool = new MessagePool(3, 50))
            {
                // Act
                _ = pool.GetMessage(); // Hit
                _ = pool.GetMessage(); // Hit
                _ = pool.GetMessage(); // Hit
                _ = pool.GetMessage(); // Miss
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(3, stats.PoolHits);
                Assert.Equal(1, stats.PoolMisses);
                Assert.Equal(75.0, stats.HitRate, 0.1);
            }
        }

        [Fact]
        public void GetMetrics_ReturnsPooledMetricsObject()
        {
            // Arrange
            using (var pool = new MessagePool(5, 50))
            {
                // Act
                var metrics1 = pool.GetMetrics();
                metrics1.MessageType = "Test";
                var metrics2 = pool.GetMetrics();

                // Assert
                Assert.NotNull(metrics1);
                Assert.NotNull(metrics2);
            }
        }

        [Fact]
        public void ReturnMetrics_ResetState()
        {
            // Arrange
            using (var pool = new MessagePool(5, 50))
            {
                var metrics = pool.GetMetrics();
                metrics.MessageType = "Test";
                metrics.ProcessingTimeMs = 100;

                // Act
                pool.ReturnMetrics(metrics);

                // Assert
                Assert.Null(metrics.MessageType);
                Assert.Equal(0, metrics.ProcessingTimeMs);
            }
        }

        [Fact]
        public void Clear_ResetsAllStatistics()
        {
            // Arrange
            using (var pool = new MessagePool(5, 50))
            {
                pool.GetMessage();
                pool.GetMessage();
                var statsBefore = pool.GetStatistics();

                // Act
                pool.Clear();
                var statsAfter = pool.GetStatistics();

                // Assert
                Assert.True(statsBefore.PoolHits > 0);
                Assert.Equal(0, statsAfter.PooledObjectCount);
                Assert.Equal(0, statsAfter.PoolHits);
                Assert.Equal(0, statsAfter.TotalAllocations);
            }
        }

        [Fact]
        public void Dispose_MakesPoolUnsusable()
        {
            // Arrange
            var pool = new MessagePool(5, 50);

            // Act
            pool.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => pool.GetMessage());
        }

        [Fact]
        public void Prefill_IncreasePoolSize()
        {
            // Arrange
            using (var pool = new MessagePool(0, 50))
            {
                // Act
                pool.Prefill(20);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(20, stats.PooledObjectCount);
            }
        }

        [Fact]
        public void CapacityPercent_CalculatesCorrectly()
        {
            // Arrange
            using (var pool = new MessagePool(25, 100))
            {
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(25.0, stats.PoolCapacityPercent, 0.1);
            }
        }
    }

    /// <summary>
    /// Integration tests for MessagePool with EventBus
    /// </summary>
    public class MessagePoolIntegrationTests
    {
        [Fact]
        public void MessagePool_WithEventBus_ReducesAllocations()
        {
            // Arrange
            using (var pool = new MessagePool(50, 200))
            using (var eventBus = new EventBus())
            {
                var statsBefore = pool.GetStatistics();

                // Act: Simulate message publishing with pooling
                for (int i = 0; i < 100; i++)
                {
                    var msg = pool.GetMessage();
                    msg.FromService = "Service1";
                    msg.ToService = "Service2";
                    msg.MessageType = "TestType";
                    pool.ReturnMessage(msg);
                }

                var statsAfter = pool.GetStatistics();

                // Assert
                Assert.True(statsAfter.PoolHits > statsBefore.PoolHits);
                Assert.True(statsAfter.HitRate > 50); // Should have high reuse
            }
        }

        [Fact]
        public async Task ConcurrentMessagePooling_WithAsyncHandlers_MaintainThreadSafety()
        {
            // Arrange
            using (var pool = new MessagePool(20, 100))
            {
                var completedMessages = 0;

                // Act
                var tasks = new List<Task>();
                for (int i = 0; i < 50; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var msg = pool.GetMessage();
                        msg.MessageType = "AsyncType";
                        System.Threading.Thread.Sleep(5);
                        pool.ReturnMessage(msg);
                        completedMessages++;
                    }));
                }

                await Task.WhenAll(tasks);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(50, completedMessages);
                Assert.True(stats.HitRate > 50);
            }
        }
    }
}
