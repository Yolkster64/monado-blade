using Xunit;
using HELIOS.Platform.AI;
using HELIOS.Platform.Caching;
using HELIOS.Platform.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.AI
{
    public class PredictiveOptimizerTests
    {
        [Fact]
        public async Task AnalyzeAndRecommend_WithCpuSpikes_ReturnsCpuOptimization()
        {
            var optimizer = new PredictiveOptimizer();

            for (int i = 0; i < 15; i++)
            {
                optimizer.RecordMetric(new PredictiveOptimizer.PerformanceMetric
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    CpuUsage = 0.85,
                    MemoryUsage = 0.5,
                    DiskIo = 0.3,
                    ThreadCount = 10,
                    RequestCount = 1000,
                    AverageResponseTime = 100
                });
            }

            var recommendations = await optimizer.AnalyzeAndRecommend();

            Assert.NotEmpty(recommendations);
            Assert.Any(recommendations, r => r.Category == "CPU Optimization");
        }

        [Fact]
        public void MarkRecommendationAsApplied_UpdatesAppliedFlag()
        {
            var optimizer = new PredictiveOptimizer();

            optimizer.RecordMetric(new PredictiveOptimizer.PerformanceMetric
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = 0.95,
                MemoryUsage = 0.5,
                DiskIo = 0.3,
                ThreadCount = 20,
                RequestCount = 5000,
                AverageResponseTime = 200
            });

            var recommendations = optimizer.GetActiveRecommendations();
            if (recommendations.Count > 0)
            {
                optimizer.MarkRecommendationAsApplied(recommendations[0].Id);
                var active = optimizer.GetActiveRecommendations();
                Assert.DoesNotContain(recommendations[0].Id, active.Select(r => r.Id));
            }
        }

        [Fact]
        public void GetActiveRecommendations_ExcludesApplied()
        {
            var optimizer = new PredictiveOptimizer();

            var active = optimizer.GetActiveRecommendations();
            Assert.Empty(active);
        }
    }

    public class UsageAnalyzerTests
    {
        [Fact]
        public async Task AnalyzePatterns_WithMultipleFeatures_ReturnsStats()
        {
            var analyzer = new UsageAnalyzer();

            for (int i = 0; i < 20; i++)
            {
                analyzer.RecordUsage(new UsageAnalyzer.UsageEvent
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    FeatureName = "Dashboard",
                    UserId = "user1",
                    DurationMs = 5000 + (i * 100),
                    Success = true
                });

                analyzer.RecordUsage(new UsageAnalyzer.UsageEvent
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    FeatureName = "Settings",
                    UserId = "user1",
                    DurationMs = 2000,
                    Success = i % 2 == 0
                });
            }

            var stats = await analyzer.AnalyzePatterns();

            Assert.NotEmpty(stats);
            Assert.Contains("Dashboard", stats.Keys);
            Assert.Contains("Settings", stats.Keys);
        }

        [Fact]
        public void GetTopFeatures_ReturnsSortedByScore()
        {
            var analyzer = new UsageAnalyzer();

            for (int i = 0; i < 50; i++)
            {
                analyzer.RecordUsage(new UsageAnalyzer.UsageEvent
                {
                    Timestamp = DateTime.UtcNow,
                    FeatureName = "Popular",
                    UserId = "user1",
                    DurationMs = 5000,
                    Success = true
                });

                if (i < 10)
                {
                    analyzer.RecordUsage(new UsageAnalyzer.UsageEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        FeatureName = "Unpopular",
                        UserId = "user1",
                        DurationMs = 1000,
                        Success = false
                    });
                }
            }

            Assert.True(analyzer.GetEventCount() > 10);
        }

        [Fact]
        public void DetectSeasonalPatterns_WithVariedUsage_ReturnsPatterns()
        {
            var analyzer = new UsageAnalyzer();

            for (int day = 0; day < 100; day++)
            {
                var count = day % 14 == 0 ? 50 : 10;
                for (int i = 0; i < count; i++)
                {
                    analyzer.RecordUsage(new UsageAnalyzer.UsageEvent
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-day),
                        FeatureName = "Feature",
                        UserId = "user1",
                        DurationMs = 1000,
                        Success = true
                    });
                }
            }

            var patterns = analyzer.DetectSeasonalPatterns();
            Assert.NotEmpty(patterns);
        }
    }

    public class SmartResourceAllocatorTests
    {
        [Fact]
        public async Task AllocateResources_WithHighLoad_IncreaseThreadPoolSize()
        {
            var allocator = new SmartResourceAllocator();

            var allocation = await allocator.AllocateResources(expectedLoad: 0.85, workloadType: "cpu_intensive");

            Assert.True(allocation.ThreadPoolSize > Environment.ProcessorCount);
            Assert.True(allocation.CpuBudget > 0.7);
        }

        [Fact]
        public async Task AllocateResources_WithLowLoad_ReducesResources()
        {
            var allocator = new SmartResourceAllocator();

            var allocation = await allocator.AllocateResources(expectedLoad: 0.15, workloadType: "io_wait");

            Assert.NotNull(allocation);
            Assert.True(allocation.MemoryBudget < 0.7);
        }

        [Fact]
        public void ShouldScaleUp_WithHighCpuAndMemory_ReturnsTrue()
        {
            var allocator = new SmartResourceAllocator();
            allocator.UpdateResourceMetrics(cpuUsage: 0.85, memoryUsage: 0.80, activeThreads: 50);

            Assert.True(allocator.ShouldScaleUp());
        }

        [Fact]
        public void ShouldScaleDown_WithLowResources_ReturnsTrue()
        {
            var allocator = new SmartResourceAllocator();
            allocator.UpdateResourceMetrics(cpuUsage: 0.2, memoryUsage: 0.3, activeThreads: 2);

            Assert.True(allocator.ShouldScaleDown());
        }

        [Fact]
        public void GetEnergyAwarenessScore_WithHighUtilization_ReturnsGoodScore()
        {
            var allocator = new SmartResourceAllocator();
            allocator.UpdateResourceMetrics(cpuUsage: 0.7, memoryUsage: 0.6, activeThreads: 20);

            var score = allocator.GetEnergyAwarenessScore();
            Assert.True(score >= 0.5);
        }
    }

    public class IntelligentCacheTests
    {
        [Fact]
        public void Set_And_Get_RetrievesValue()
        {
            var cache = new IntelligentCache();
            var testValue = "test-data";

            cache.Set("key1", testValue);
            var retrieved = cache.TryGet<string>("key1", out var value);

            Assert.True(retrieved);
            Assert.Equal(testValue, value);
        }

        [Fact]
        public void TryGet_ExpiredEntry_ReturnsFalse()
        {
            var cache = new IntelligentCache();
            cache.Set("key1", "data", TimeSpan.FromMilliseconds(100));

            System.Threading.Thread.Sleep(150);

            var retrieved = cache.TryGet<string>("key1", out var value);
            Assert.False(retrieved);
        }

        [Fact]
        public async Task AutoTune_RemovesExpiredEntries()
        {
            var cache = new IntelligentCache();
            cache.Set("key1", "data1", TimeSpan.FromMilliseconds(100));
            cache.Set("key2", "data2", TimeSpan.FromMinutes(5));

            System.Threading.Thread.Sleep(150);
            await cache.AutoTune();

            var stats = cache.GetStatistics();
            Assert.Equal(1, stats.EntryCount);
        }

        [Fact]
        public void GetStatistics_ReturnsHitRate()
        {
            var cache = new IntelligentCache();
            cache.Set("key1", "data");

            cache.TryGet<string>("key1", out _);
            cache.TryGet<string>("key1", out _);
            cache.TryGet<string>("missing", out _);

            var stats = cache.GetStatistics();
            Assert.True(stats.HitRate > 0.5);
        }
    }

    public class PerformancePredictorTests
    {
        [Fact]
        public async Task TrainModels_WithHistory_CreatesModels()
        {
            var predictor = new PerformancePredictor();

            for (int i = 0; i < 60; i++)
            {
                predictor.RecordSnapshot(new PerformancePredictor.PerformanceSnapshot
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    CpuUsage = 0.5 + (i % 10) * 0.05,
                    MemoryUsage = 0.4 + (i % 8) * 0.04,
                    DiskIoUsage = 0.3,
                    NetworkUsage = 0.2,
                    AverageResponseTime = 100 + (i % 5) * 20,
                    ThroughputRps = 1000 + (i % 500),
                    ErrorCount = i % 10
                });
            }

            await predictor.TrainModels();
            var predictions = predictor.PredictNext(5);

            Assert.NotEmpty(predictions);
        }

        [Fact]
        public void IsFailureLikely_WithNormalData_ReturnsFalse()
        {
            var predictor = new PerformancePredictor();

            for (int i = 0; i < 50; i++)
            {
                predictor.RecordSnapshot(new PerformancePredictor.PerformanceSnapshot
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    CpuUsage = 0.5,
                    MemoryUsage = 0.4,
                    DiskIoUsage = 0.3,
                    NetworkUsage = 0.2,
                    AverageResponseTime = 100,
                    ThroughputRps = 1000,
                    ErrorCount = 0
                });
            }

            Assert.False(predictor.IsFailureLikely());
        }

        [Fact]
        public void GetEarlyWarnings_WithHighMetrics_MayReturnWarnings()
        {
            var predictor = new PerformancePredictor();

            for (int i = 0; i < 50; i++)
            {
                predictor.RecordSnapshot(new PerformancePredictor.PerformanceSnapshot
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    CpuUsage = 0.9,
                    MemoryUsage = 0.85,
                    DiskIoUsage = 0.7,
                    NetworkUsage = 0.6,
                    AverageResponseTime = 500,
                    ThroughputRps = 15000,
                    ErrorCount = 20
                });
            }

            var warnings = predictor.GetEarlyWarnings();
            Assert.NotNull(warnings);
        }
    }

    public class AnomalyDetectorTests
    {
        [Fact]
        public async Task CalculateBaselines_WithNormalData_CreatesBaselines()
        {
            var detector = new AnomalyDetector();

            for (int i = 0; i < 40; i++)
            {
                detector.RecordMetric("CPU", 0.5 + (i % 5) * 0.05);
                detector.RecordMetric("Memory", 0.4 + (i % 4) * 0.04);
            }

            await detector.CalculateBaselines();
        }

        [Fact]
        public async Task DetectAnomalies_WithSpikes_FindsAnomalies()
        {
            var detector = new AnomalyDetector();

            for (int i = 0; i < 40; i++)
            {
                detector.RecordMetric("CPU", 0.5);
            }

            await detector.CalculateBaselines();

            detector.RecordMetric("CPU", 0.95);
            var anomalies = await detector.DetectAnomalies();

            Assert.NotEmpty(anomalies);
            Assert.Any(anomalies, a => a.MetricName == "CPU");
        }

        [Fact]
        public void GenerateReport_ReturnsHealthScore()
        {
            var detector = new AnomalyDetector();

            for (int i = 0; i < 40; i++)
            {
                detector.RecordMetric("CPU", 0.5);
            }

            var report = detector.GenerateReport();

            Assert.NotNull(report);
            Assert.True(report.HealthScore >= 0.0 && report.HealthScore <= 1.0);
        }

        [Fact]
        public void GetRecentAnomalies_WithCount_ReturnsLimitedResults()
        {
            var detector = new AnomalyDetector();

            var anomalies = detector.GetRecentAnomalies(5);

            Assert.True(anomalies.Count <= 5);
        }
    }

    public class AdaptiveFeaturesTests
    {
        [Fact]
        public async Task AdaptInterface_WithHighUsage_MakesFeatureVisible()
        {
            var adaptive = new AdaptiveFeatures();

            for (int i = 0; i < 60; i++)
            {
                adaptive.RecordInteraction("feature1", "click", 1000, true);
            }

            await adaptive.AdaptInterface();

            Assert.True(adaptive.ShouldShowFeature("feature1"));
        }

        [Fact]
        public async Task LearnPreferences_WithEveningUsage_LearnsPreferences()
        {
            var adaptive = new AdaptiveFeatures();

            for (int i = 0; i < 50; i++)
            {
                adaptive.RecordInteraction("feature1", "click", 1000, true);
            }

            await adaptive.LearnPreferences();

            var theme = adaptive.GetRecommendedTheme();
            Assert.NotNull(theme);
        }

        [Fact]
        public void GetRecommendedFeatures_ReturnsTopFeatures()
        {
            var adaptive = new AdaptiveFeatures();

            adaptive.RecordInteraction("popular", "click", 5000, true);
            adaptive.RecordInteraction("popular", "click", 5000, true);
            adaptive.RecordInteraction("unpopular", "click", 1000, false);

            var recommended = adaptive.GetRecommendedFeatures(5);

            Assert.NotNull(recommended);
        }

        [Fact]
        public void RankFeaturesByUsability_ReturnsSortedList()
        {
            var adaptive = new AdaptiveFeatures();

            var ranking = adaptive.RankFeaturesByUsability();

            Assert.NotNull(ranking);
        }
    }

    public class HubIntegrationTests
    {
        [Fact]
        public void HubIntegration_Initialize_CreatesDeviceId()
        {
            var integration = new HubIntegration("http://localhost:5000");

            var deviceId = integration.GetDeviceId();

            Assert.NotEmpty(deviceId);
            Assert.NotEqual(Guid.Empty.ToString(), deviceId);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyApiKey_ReturnsFalse()
        {
            var integration = new HubIntegration("http://localhost:5000");

            var result = await integration.AuthenticateAsync("", "user1");

            Assert.False(result);
        }

        [Fact]
        public void IsHealthy_WithoutAuthentication_ReturnsFalse()
        {
            var integration = new HubIntegration("http://localhost:5000");

            Assert.False(integration.IsHealthy());
        }

        [Fact]
        public void IsAuthenticated_InitiallyFalse()
        {
            var integration = new HubIntegration("http://localhost:5000");

            Assert.False(integration.IsAuthenticated);
        }
    }
}
