namespace MonadoBlade.Core.Services;

using System;
using System.Linq;
using System.Runtime.InteropServices;

/// <summary>
/// Native bridge to high-performance C++ Hermes learning kernels.
/// Falls back to managed scoring if native binary is unavailable.
/// </summary>
public sealed class NativeHermesLearningBridge
{
    private const string NativeDll = "hermes_learning_kernel";

    [DllImport(NativeDll, EntryPoint = "hermes_reward_update", CallingConvention = CallingConvention.Cdecl)]
    private static extern double HermesRewardUpdateNative(
        double quality,
        double speed,
        double costEfficiency,
        double truthScore,
        double novelty,
        double[] weights,
        nuint weightLen);

    [DllImport(NativeDll, EntryPoint = "hermes_gaussian_3d_score", CallingConvention = CallingConvention.Cdecl)]
    private static extern double HermesGaussian3DScoreNative(
        double x,
        double y,
        double z,
        double targetX,
        double targetY,
        double targetZ,
        double sigma);

    [DllImport(NativeDll, EntryPoint = "hermes_knaa_qnaa_score", CallingConvention = CallingConvention.Cdecl)]
    private static extern double HermesKnaaQnaaScoreNative(
        double[] shortValues,
        nuint shortLen,
        double[] midValues,
        nuint midLen,
        double[] longValues,
        nuint longLen,
        double truthScore,
        double rewardScore,
        double explorationRate);

    [DllImport(NativeDll, EntryPoint = "hermes_fleet_shape_score", CallingConvention = CallingConvention.Cdecl)]
    private static extern double HermesFleetShapeScoreNative(
        double activeAgents,
        double latencyMs,
        double throughputRps,
        double errorRate,
        double diversity,
        double memoryRetention);

    [DllImport(NativeDll, EntryPoint = "hermes_quantized_compression_score", CallingConvention = CallingConvention.Cdecl)]
    private static extern double HermesQuantizedCompressionScoreNative(
        double[] values,
        nuint valueLen);

    [DllImport(NativeDll, EntryPoint = "hermes_long_haul_meta_score", CallingConvention = CallingConvention.Cdecl)]
    private static extern double HermesLongHaulMetaScoreNative(
        double[] shortValues,
        nuint shortLen,
        double[] midValues,
        nuint midLen,
        double[] longValues,
        nuint longLen,
        double externalSignalScore,
        double correctionSignal,
        double truthScore,
        double gaussianAlignment);

    /// <summary>
    /// Computes reward using native C++ kernels when available.
    /// </summary>
    public double ComputeReward(
        double quality,
        double speed,
        double costEfficiency,
        double truthScore,
        double novelty,
        double[] weights)
    {
        if (weights is null || weights.Length < 5)
            throw new ArgumentException("At least 5 weights are required", nameof(weights));

        try
        {
            return HermesRewardUpdateNative(
                quality,
                speed,
                costEfficiency,
                truthScore,
                novelty,
                weights,
                (nuint)weights.Length);
        }
        catch (DllNotFoundException)
        {
            return ComputeManagedFallback(quality, speed, costEfficiency, truthScore, novelty, weights);
        }
        catch (EntryPointNotFoundException)
        {
            return ComputeManagedFallback(quality, speed, costEfficiency, truthScore, novelty, weights);
        }
    }

    private static double ComputeManagedFallback(
        double quality,
        double speed,
        double costEfficiency,
        double truthScore,
        double novelty,
        double[] weights)
    {
        var score =
            quality * weights[0] +
            speed * weights[1] +
            costEfficiency * weights[2] +
            truthScore * weights[3] +
            novelty * weights[4];

        if (truthScore < 0.68d)
            score -= (0.68d - truthScore) * 1.5d;

        return score;
    }

    public double ComputeGaussian3DScore(
        double x,
        double y,
        double z,
        double targetX,
        double targetY,
        double targetZ,
        double sigma = 0.2d)
    {
        try
        {
            return HermesGaussian3DScoreNative(x, y, z, targetX, targetY, targetZ, sigma);
        }
        catch (DllNotFoundException)
        {
            return ComputeGaussianManagedFallback(x, y, z, targetX, targetY, targetZ, sigma);
        }
        catch (EntryPointNotFoundException)
        {
            return ComputeGaussianManagedFallback(x, y, z, targetX, targetY, targetZ, sigma);
        }
    }

    private static double ComputeGaussianManagedFallback(
        double x,
        double y,
        double z,
        double targetX,
        double targetY,
        double targetZ,
        double sigma)
    {
        var safeSigma = Math.Max(0.000001d, sigma);
        var dx = x - targetX;
        var dy = y - targetY;
        var dz = z - targetZ;
        var dist2 = (dx * dx) + (dy * dy) + (dz * dz);
        return Math.Exp(-(dist2 / (2.0d * safeSigma * safeSigma)));
    }

    public double ComputeKnaaQnaaScore(
        double[] shortValues,
        double[] midValues,
        double[] longValues,
        double truthScore,
        double rewardScore,
        double explorationRate = 0.1d)
    {
        var safeShort = shortValues ?? Array.Empty<double>();
        var safeMid = midValues ?? Array.Empty<double>();
        var safeLong = longValues ?? Array.Empty<double>();
        try
        {
            return HermesKnaaQnaaScoreNative(
                safeShort,
                (nuint)safeShort.Length,
                safeMid,
                (nuint)safeMid.Length,
                safeLong,
                (nuint)safeLong.Length,
                truthScore,
                rewardScore,
                explorationRate);
        }
        catch (DllNotFoundException)
        {
            return ComputeKnaaQnaaManagedFallback(safeShort, safeMid, safeLong, truthScore, rewardScore, explorationRate);
        }
        catch (EntryPointNotFoundException)
        {
            return ComputeKnaaQnaaManagedFallback(safeShort, safeMid, safeLong, truthScore, rewardScore, explorationRate);
        }
    }

    public double ComputeFleetShapeScore(
        double activeAgents,
        double latencyMs,
        double throughputRps,
        double errorRate,
        double diversity,
        double memoryRetention)
    {
        try
        {
            return HermesFleetShapeScoreNative(activeAgents, latencyMs, throughputRps, errorRate, diversity, memoryRetention);
        }
        catch (DllNotFoundException)
        {
            return ComputeFleetShapeManagedFallback(activeAgents, latencyMs, throughputRps, errorRate, diversity, memoryRetention);
        }
        catch (EntryPointNotFoundException)
        {
            return ComputeFleetShapeManagedFallback(activeAgents, latencyMs, throughputRps, errorRate, diversity, memoryRetention);
        }
    }

    public double ComputeQuantizedCompressionScore(double[] values)
    {
        var safeValues = values ?? Array.Empty<double>();
        try
        {
            return HermesQuantizedCompressionScoreNative(safeValues, (nuint)safeValues.Length);
        }
        catch (DllNotFoundException)
        {
            return ComputeQuantizedCompressionManagedFallback(safeValues);
        }
        catch (EntryPointNotFoundException)
        {
            return ComputeQuantizedCompressionManagedFallback(safeValues);
        }
    }

    public double ComputeLongHaulMetaScore(
        double[] shortValues,
        double[] midValues,
        double[] longValues,
        double externalSignalScore,
        double correctionSignal,
        double truthScore,
        double gaussianAlignment)
    {
        var safeShort = shortValues ?? Array.Empty<double>();
        var safeMid = midValues ?? Array.Empty<double>();
        var safeLong = longValues ?? Array.Empty<double>();
        try
        {
            return HermesLongHaulMetaScoreNative(
                safeShort,
                (nuint)safeShort.Length,
                safeMid,
                (nuint)safeMid.Length,
                safeLong,
                (nuint)safeLong.Length,
                externalSignalScore,
                correctionSignal,
                truthScore,
                gaussianAlignment);
        }
        catch (DllNotFoundException)
        {
            return ComputeLongHaulMetaManagedFallback(safeShort, safeMid, safeLong, externalSignalScore, correctionSignal, truthScore, gaussianAlignment);
        }
        catch (EntryPointNotFoundException)
        {
            return ComputeLongHaulMetaManagedFallback(safeShort, safeMid, safeLong, externalSignalScore, correctionSignal, truthScore, gaussianAlignment);
        }
    }

    private static double ComputeKnaaQnaaManagedFallback(
        double[] shortValues,
        double[] midValues,
        double[] longValues,
        double truthScore,
        double rewardScore,
        double explorationRate)
    {
        static double Mean(double[] values) => values.Length == 0 ? 0.0d : values.Average();
        var shortAvg = Mean(shortValues);
        var midAvg = Mean(midValues);
        var longAvg = Mean(longValues);
        var knaa = (shortAvg * 0.42d) + (midAvg * 0.34d) + (longAvg * 0.24d);
        var qnaa =
            (Sigmoid((truthScore - 0.5d) * 7.0d) * 0.62d) +
            (Sigmoid((rewardScore - 0.5d) * 5.0d) * 0.38d);
        var explore = Math.Clamp(explorationRate, 0.0d, 1.0d) * 0.14d;
        return Math.Clamp((knaa * 0.64d) + (qnaa * 0.36d) + explore, 0.0d, 1.0d);
    }

    private static double ComputeFleetShapeManagedFallback(
        double activeAgents,
        double latencyMs,
        double throughputRps,
        double errorRate,
        double diversity,
        double memoryRetention)
    {
        var agentFactor = Sigmoid((activeAgents - 8.0d) * 0.28d);
        var latencyFactor = Sigmoid((220.0d - latencyMs) / 45.0d);
        var throughputFactor = Sigmoid((throughputRps - 140.0d) / 30.0d);
        var reliability = 1.0d - Math.Clamp(errorRate, 0.0d, 1.0d);
        var diversityFactor = Math.Clamp(diversity, 0.0d, 1.0d);
        var retentionFactor = Math.Clamp(memoryRetention, 0.0d, 1.0d);

        return Math.Clamp(
            (agentFactor * 0.20d) +
            (latencyFactor * 0.22d) +
            (throughputFactor * 0.22d) +
            (reliability * 0.18d) +
            (diversityFactor * 0.08d) +
            (retentionFactor * 0.10d),
            0.0d,
            1.0d);
    }

    private static double ComputeQuantizedCompressionManagedFallback(double[] values)
    {
        if (values.Length == 0)
            return 0.0d;

        var minV = values.Min();
        var maxV = values.Max();
        var range = Math.Max(0.000001d, maxV - minV);
        var deq = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var norm = Math.Clamp((values[i] - minV) / range, 0.0d, 1.0d);
            var q = Math.Round(norm * 255.0d);
            deq[i] = minV + ((q / 255.0d) * range);
        }

        var mse = 0.0d;
        for (var i = 0; i < values.Length; i++)
        {
            var d = values[i] - deq[i];
            mse += d * d;
        }

        mse /= values.Length;
        var fidelity = 1.0d / (1.0d + (mse * 200.0d));
        var compressionRatio = 1.0d - (8.0d / 64.0d);
        return Math.Clamp((fidelity * 0.72d) + (compressionRatio * 0.28d), 0.0d, 1.0d);
    }

    private static double ComputeLongHaulMetaManagedFallback(
        double[] shortValues,
        double[] midValues,
        double[] longValues,
        double externalSignalScore,
        double correctionSignal,
        double truthScore,
        double gaussianAlignment)
    {
        static double Mean(double[] values) => values.Length == 0 ? 0.0d : values.Average();
        var shortAvg = Mean(shortValues);
        var midAvg = Mean(midValues);
        var longAvg = Mean(longValues);
        var horizonStability = (shortAvg * 0.20d) + (midAvg * 0.33d) + (longAvg * 0.47d);
        var signalQuality =
            (Math.Clamp(externalSignalScore, 0.0d, 1.0d) * 0.34d) +
            (Math.Clamp(truthScore, 0.0d, 1.0d) * 0.41d) +
            (Math.Clamp(gaussianAlignment, 0.0d, 1.0d) * 0.25d);
        var correction = 0.5d + (Math.Clamp(correctionSignal, -1.0d, 1.0d) * 0.5d);
        return Math.Clamp((horizonStability * 0.58d) + (signalQuality * 0.30d) + (correction * 0.12d), 0.0d, 1.0d);
    }

    private static double Sigmoid(double value) => 1.0d / (1.0d + Math.Exp(-value));
}
