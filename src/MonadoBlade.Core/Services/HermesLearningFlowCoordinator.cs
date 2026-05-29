namespace MonadoBlade.Core.Services;

using System.Net.Http.Json;
using System.Threading.Channels;

/// <summary>
/// Non-blocking coordinator that bridges C# requests into native scoring + Python orchestration.
/// Uses a bounded channel to keep memory/CPU stable while maintaining smooth learning flow.
/// </summary>
public sealed class HermesLearningFlowCoordinator : IAsyncDisposable
{
    private readonly HermesLearningIntermediaryService _intermediary;
    private readonly HttpClient _httpClient;
    private readonly Channel<HermesFlowRequest> _queue;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private Uri? _orchestratorBaseUri;
    private int _processed;

    public HermesLearningFlowCoordinator(HermesLearningIntermediaryService intermediary, HttpClient? httpClient = null, int capacity = 256)
    {
        _intermediary = intermediary ?? throw new ArgumentNullException(nameof(intermediary));
        _httpClient = httpClient ?? new HttpClient();
        _queue = Channel.CreateBounded<HermesFlowRequest>(new BoundedChannelOptions(Math.Max(32, capacity))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        });
    }

    public void Start(Uri orchestratorBaseUri)
    {
        _orchestratorBaseUri = orchestratorBaseUri;
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => ProcessLoopAsync(_cts.Token));
    }

    public bool Enqueue(HermesFlowRequest request) => _queue.Writer.TryWrite(request);

    private async Task ProcessLoopAsync(CancellationToken cancellationToken)
    {
        if (_orchestratorBaseUri is null)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            HermesFlowRequest request;
            try
            {
                request = await _queue.Reader.ReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var score = _intermediary.ComputeIntermediaryScore(
                request.ShortValues,
                request.MidValues,
                request.LongValues,
                request.TruthScore,
                request.RewardScore,
                request.ActiveAgents,
                request.LatencyMs,
                request.ThroughputRps,
                request.ErrorRate,
                request.Diversity,
                request.MemoryRetention);

            await _intermediary.IngestCompositeSignalAsync(_orchestratorBaseUri, score, cancellationToken: cancellationToken);
            await _intermediary.CurateLearningAsync(
                _orchestratorBaseUri,
                sqlSignal: score.QuantizedCompression,
                internetSignal: score.FleetShape,
                llmSignal: score.LongHaulMeta,
                stabilityBias: request.TruthScore,
                cancellationToken: cancellationToken);

            var pulsePayload = new
            {
                specialty = request.Specialty,
                steps = request.PulseSteps,
                candidates = request.FleetCandidates,
                sql_signal = score.QuantizedCompression,
                internet_signal = score.FleetShape,
                llm_signal = score.LongHaulMeta,
                stability_bias = request.TruthScore,
            };
            using var pulseResponse = await _httpClient.PostAsJsonAsync(
                new Uri(_orchestratorBaseUri, "/learning-pulse"),
                pulsePayload,
                cancellationToken);
            pulseResponse.EnsureSuccessStatusCode();

            _processed++;
            if (_processed % 12 == 0)
            {
                await _intermediary.TriggerDedupeOptimizationAsync(_orchestratorBaseUri, cancellationToken: cancellationToken);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is null)
            return;

        _cts.Cancel();
        _queue.Writer.TryComplete();
        if (_loopTask is not null)
            await _loopTask;
        _cts.Dispose();
    }
}

public sealed record HermesFlowRequest(
    string Specialty,
    double[] ShortValues,
    double[] MidValues,
    double[] LongValues,
    double TruthScore,
    double RewardScore,
    double ActiveAgents,
    double LatencyMs,
    double ThroughputRps,
    double ErrorRate,
    double Diversity,
    double MemoryRetention,
    int PulseSteps = 80,
    int FleetCandidates = 50);
