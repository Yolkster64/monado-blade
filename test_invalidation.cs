using System;
using MonadoBlade.Core.Caching;

class TestInvalidation
{
    static void Main()
    {
        var tracker = new DependencyTracker();
        for (int i = 0; i < 100; i++)
            tracker.RegisterDependency($"key{i}", "key0");

        var result = tracker.InvalidateKey("key0", totalCacheSize: 500);

        Console.WriteLine($"Keys affected: {result.KeysToInvalidate.Count}");
        Console.WriteLine($"Should clear all: {result.ShouldClearAll}");
        Console.WriteLine($"Ratio: {result.InvalidationRatio:P}");
        Console.WriteLine($"Threshold: 10%");
        Console.WriteLine($"Calculation: {result.KeysToInvalidate.Count} / 500 = {result.InvalidationRatio}");
        Console.WriteLine($"Expected: TRUE (20% > 10%)");
    }
}
