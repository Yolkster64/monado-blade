using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Architecture.ObjectPooling;

namespace MonadoBlade.Tests.ObjectPooling
{
    /// <summary>
    /// Comprehensive test suite for TaskDescriptorPool
    /// Tests object reuse, concurrent access, memory bounds, and performance
    /// </summary>
    public class TaskDescriptorPoolTests
    {
        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            using (var pool = new TaskDescriptorPool(10, 50))
            {
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(10, stats.PooledDescriptorCount);
                Assert.Equal(10, stats.TotalAllocations);
            }
        }

        [Fact]
        public void GetDescriptor_FromPopulatedPool_ReturnsPooledObject()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(5, 20))
            {
                // Act
                var desc1 = pool.GetDescriptor();
                var desc2 = pool.GetDescriptor();

                // Assert
                Assert.NotNull(desc1);
                Assert.NotNull(desc2);
                Assert.NotEqual(desc1.Id, desc2.Id);
                var stats = pool.GetStatistics();
                Assert.Equal(2, stats.Hits);
            }
        }

        [Fact]
        public void GetDescriptor_FromEmptyPool_CreatesNewObject()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(0, 20))
            {
                // Act
                var desc = pool.GetDescriptor();

                // Assert
                Assert.NotNull(desc);
                var stats = pool.GetStatistics();
                Assert.Equal(1, stats.Misses);
            }
        }

        [Fact]
        public void ReturnDescriptor_ReturnsObjectToPool()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(5, 20))
            {
                var desc = pool.GetDescriptor();
                desc.Name = "TestTask";
                desc.Priority = 5;

                // Act
                pool.ReturnDescriptor(desc);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(5, stats.PooledDescriptorCount);
                Assert.Equal(1, stats.Resets);
                Assert.Null(desc.Name); // State reset
                Assert.Equal(0, desc.Priority);
            }
        }

        [Fact]
        public void ReturnDescriptor_WhenPoolFull_RejectsObject()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(2, 2))
            {
                var desc = new TaskDescriptor();

                // Act
                pool.ReturnDescriptor(desc);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(2, stats.PooledDescriptorCount);
            }
        }

        [Fact]
        public void ConcurrentAccess_HandlesConcurrentGetReturn_ThreadSafely()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(10, 100))
            {
                var descriptors = new List<TaskDescriptor>();
                var lockObj = new object();

                // Act
                Parallel.For(0, 50, new ParallelOptions { MaxDegreeOfParallelism = 10 }, i =>
                {
                    var desc = pool.GetDescriptor();
                    lock (lockObj) descriptors.Add(desc);
                    
                    System.Threading.Thread.Sleep(1);
                    
                    pool.ReturnDescriptor(desc);
                });

                var stats = pool.GetStatistics();

                // Assert
                Assert.True(stats.Hits > 0);
                Assert.True(stats.Misses > 0);
                Assert.Equal(50, descriptors.Count);
            }
        }

        [Fact]
        public void HitRateCalculation_CalculatesCorrectly()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(3, 50))
            {
                // Act
                _ = pool.GetDescriptor(); // Hit
                _ = pool.GetDescriptor(); // Hit
                _ = pool.GetDescriptor(); // Hit
                _ = pool.GetDescriptor(); // Miss
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(3, stats.Hits);
                Assert.Equal(1, stats.Misses);
                Assert.Equal(75.0, stats.HitRate, 0.1);
            }
        }

        [Fact]
        public void Clear_ResetsAllStatistics()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(5, 50))
            {
                pool.GetDescriptor();
                pool.GetDescriptor();
                var statsBefore = pool.GetStatistics();

                // Act
                pool.Clear();
                var statsAfter = pool.GetStatistics();

                // Assert
                Assert.True(statsBefore.Hits > 0);
                Assert.Equal(0, statsAfter.PooledDescriptorCount);
                Assert.Equal(0, statsAfter.Hits);
            }
        }

        [Fact]
        public void Dispose_MakesPoolUnsusable()
        {
            // Arrange
            var pool = new TaskDescriptorPool(5, 50);

            // Act
            pool.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => pool.GetDescriptor());
        }

        [Fact]
        public void Prefill_IncreasePoolSize()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(0, 50))
            {
                // Act
                pool.Prefill(20);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(20, stats.PooledDescriptorCount);
            }
        }

        [Fact]
        public void CapacityPercent_CalculatesCorrectly()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(25, 100))
            {
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(25.0, stats.PoolCapacityPercent, 0.1);
            }
        }

        [Fact]
        public void TaskDescriptor_ResetClearsAllProperties()
        {
            // Arrange
            var descriptor = new TaskDescriptor
            {
                Name = "MyTask",
                Priority = 8,
                Status = TaskStatusValue.Running,
                MaxMemoryBytes = 1000000
            };

            // Act
            descriptor.Reset();

            // Assert
            Assert.NotNull(descriptor.Id);
            Assert.Null(descriptor.Name);
            Assert.Equal(0, descriptor.Priority);
            Assert.Equal(TaskStatusValue.Pending, descriptor.Status);
            Assert.Equal(0, descriptor.MaxMemoryBytes);
        }

        [Fact]
        public void TaskDescriptor_ToString_ProducesValidOutput()
        {
            // Arrange
            var descriptor = new TaskDescriptor
            {
                Name = "ImportantTask",
                Priority = 7,
                Status = TaskStatusValue.Running
            };

            // Act
            var output = descriptor.ToString();

            // Assert
            Assert.Contains("Running", output);
            Assert.Contains("ImportantTask", output);
            Assert.Contains("7", output);
        }
    }

    /// <summary>
    /// Performance and stress tests for TaskDescriptorPool
    /// </summary>
    public class TaskDescriptorPoolPerformanceTests
    {
        [Fact]
        public void HighThroughput_HandleManyAllocationsAndReturns()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(25, 250))
            {
                var iterations = 10000;

                // Act
                for (int i = 0; i < iterations; i++)
                {
                    var desc = pool.GetDescriptor();
                    desc.Name = $"Task_{i}";
                    desc.Priority = i % 10;
                    pool.ReturnDescriptor(desc);
                }

                var stats = pool.GetStatistics();

                // Assert
                Assert.True(stats.HitRate > 85); // High reuse expected
                Assert.True(stats.Hits > iterations * 0.85);
            }
        }

        [Fact]
        public async Task ConcurrentHighLoad_MaintainStability()
        {
            // Arrange
            using (var pool = new TaskDescriptorPool(50, 500))
            {
                var completedOps = 0;
                var errors = 0;

                // Act
                var tasks = new List<Task>();
                for (int t = 0; t < 20; t++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            for (int i = 0; i < 500; i++)
                            {
                                var desc = pool.GetDescriptor();
                                desc.Priority = i % 10;
                                System.Threading.Thread.Sleep(0); // Yield
                                pool.ReturnDescriptor(desc);
                                completedOps++;
                            }
                        }
                        catch
                        {
                            errors++;
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                var stats = pool.GetStatistics();

                // Assert
                Assert.Equal(0, errors);
                Assert.Equal(10000, completedOps);
                Assert.True(stats.HitRate > 70); // Should still have decent reuse under load
            }
        }
    }
}
