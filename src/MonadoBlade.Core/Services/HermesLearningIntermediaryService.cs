namespace MonadoBlade.Core.Services;

using System.Net.Http.Json;

/// <summary>
/// Intermediary layer between native C++ Hermes kernels and Python orchestration APIs.
/// Uses native scoring locally, then pushes curated learning signals to orchestrator endpoints.
/// </summary>
public sealed class HermesLearningIntermediaryService
{
    private readonly NativeHermesLearningBridge _nativeBridge;
    private readonly HttpClient _httpClient;

    public HermesLearningIntermediaryService(NativeHermesLearningBridge nativeBridge, HttpClient? httpClient = null)
    {
        _nativeBridge = nativeBridge ?? throw new ArgumentNullException(nameof(nativeBridge));
        _httpClient = httpClient ?? new HttpClient();
    }

    public HermesIntermediaryScore ComputeIntermediaryScore(
        double[] shortValues,
        double[] midValues,
        double[] longValues,
        double truthScore,
        double rewardScore,
        double activeAgents,
        double latencyMs,
        double throughputRps,
        double errorRate,
        double diversity,
        double memoryRetention)
    {
        var knaaQnaa = _nativeBridge.ComputeKnaaQnaaScore(shortValues, midValues, longValues, truthScore, rewardScore);
        var fleetShape = _nativeBridge.ComputeFleetShapeScore(activeAgents, latencyMs, throughputRps, errorRate, diversity, memoryRetention);
        var allValues = shortValues.Concat(midValues).Concat(longValues).ToArray();
        var quantizedCompression = _nativeBridge.ComputeQuantizedCompressionScore(allValues);
        var gaussianAlignment = _nativeBridge.ComputeGaussian3DScore(
            x: truthScore,
            y: fleetShape,
            z: quantizedCompression,
            targetX: 0.85d,
            targetY: 0.80d,
            targetZ: 0.78d,
            sigma: 0.24d);
        var correctionSignal = (truthScore - 0.68d) + (rewardScore - 0.50d);
        var longHaulMeta = _nativeBridge.ComputeLongHaulMetaScore(
            shortValues,
            midValues,
            longValues,
            externalSignalScore: (knaaQnaa + fleetShape) / 2.0d,
            correctionSignal: correctionSignal,
            truthScore: truthScore,
            gaussianAlignment: gaussianAlignment);
        var composite = Math.Clamp(
            (knaaQnaa * 0.32d) +
            (fleetShape * 0.24d) +
            (quantizedCompression * 0.16d) +
            (longHaulMeta * 0.28d),
            0.0d,
            1.0d);
        return new HermesIntermediaryScore(knaaQnaa, fleetShape, quantizedCompression, longHaulMeta, composite);
    }

    public async Task<HermesCuratedLearningPlan> CurateLearningAsync(
        Uri orchestratorBaseUri,
        double sqlSignal,
        double internetSignal,
        double llmSignal,
        double stabilityBias,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            sql_signal = sqlSignal,
            internet_signal = internetSignal,
            llm_signal = llmSignal,
            stability_bias = stabilityBias,
        };

        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(orchestratorBaseUri, "/curate-learning"),
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var curated = await response.Content.ReadFromJsonAsync<HermesCuratedLearningPlan>(cancellationToken: cancellationToken);
        return curated ?? new HermesCuratedLearningPlan("sql", new Dictionary<string, double>(), stabilityBias, "focus-truth-and-retention");
    }

    public async Task IngestCompositeSignalAsync(
        Uri orchestratorBaseUri,
        HermesIntermediaryScore score,
        string source = "csharp_intermediary",
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            source,
            signal_score = score.Composite,
            payload = new
            {
                knaa_qnaa = score.KnaaQnaa,
                fleet_shape = score.FleetShape,
                quantized_compression = score.QuantizedCompression,
                long_haul_meta = score.LongHaulMeta,
                composite = score.Composite,
            },
        };

        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(orchestratorBaseUri, "/ingest-signal"),
            payload,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task TriggerDedupeOptimizationAsync(
        Uri orchestratorBaseUri,
        IEnumerable<string>? roots = null,
        int maxFileMb = 8,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            roots = roots?.ToArray() ?? new[] { "imports", "core", "src", "runtime" },
            max_file_mb = maxFileMb,
        };
        using var response = await _httpClient.PostAsJsonAsync(
            new Uri(orchestratorBaseUri, "/dedupe-optimize"),
            payload,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record HermesIntermediaryScore(
    double KnaaQnaa,
    double FleetShape,
    double QuantizedCompression,
    double LongHaulMeta,
    double Composite);

public sealed record HermesCuratedLearningPlan(
    string PrimarySource,
    Dictionary<string, double> SourceWeights,
    double StabilityBias,
    string Recommendation);
