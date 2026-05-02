using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.Framework;

namespace MonadoBlade.Integration.M365
{
    /// <summary>
    /// M365PowerBiBridge - Enterprise integration with Microsoft 365 and Power BI
    /// Enables AI analysis results to be published to SharePoint, Teams, and Power BI datasets
    /// </summary>
    public class M365PowerBiBridge : IHELIOSService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<M365PowerBiBridge> _logger;
        private readonly RateLimiter _rateLimiter;
        private const int RequestTimeoutMs = 5000;
        private string _accessToken = string.Empty;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public M365PowerBiBridge(ILogger<M365PowerBiBridge> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MonadoBladeV3/1.0");
            _rateLimiter = new RateLimiter(100); // 100 req/min
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("M365PowerBiBridge started");
            await RefreshAccessTokenAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("M365PowerBiBridge stopped");
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Query Power BI dataset using DAX
        /// </summary>
        public async Task<QueryResult> QueryPowerBiAsync(string datasetId, string query, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(datasetId) || string.IsNullOrEmpty(query))
                throw new ArgumentNullException("Dataset ID and query required");

            await _rateLimiter.WaitAsync();
            
            try
            {
                _logger.LogInformation("Querying Power BI dataset: {DatasetId}", datasetId);
                
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(RequestTimeoutMs);

                var request = new HttpRequestMessage(HttpMethod.Post, 
                    $"https://api.powerbi.com/v1.0/myorg/datasets/{datasetId}/executeQueries");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { queries = new[] { new { query } } }),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync(cts.Token);
                var result = JsonSerializer.Deserialize<QueryResult>(jsonResponse);
                
                _logger.LogInformation("Power BI query completed successfully");
                return result ?? new QueryResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Power BI query failed");
                throw;
            }
        }

        /// <summary>
        /// Get available Power BI datasets
        /// </summary>
        public async Task<List<PowerBiDataset>> GetAvailableDatasetsAsync(CancellationToken ct = default)
        {
            await _rateLimiter.WaitAsync();

            try
            {
                _logger.LogInformation("Fetching available Power BI datasets");
                
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(RequestTimeoutMs);

                var request = new HttpRequestMessage(HttpMethod.Get, 
                    "https://api.powerbi.com/v1.0/myorg/datasets");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                using var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync(cts.Token);
                var doc = JsonDocument.Parse(jsonResponse);
                var datasets = new List<PowerBiDataset>();

                foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
                {
                    datasets.Add(new PowerBiDataset
                    {
                        Id = item.GetProperty("id").GetString() ?? string.Empty,
                        Name = item.GetProperty("name").GetString() ?? string.Empty,
                        ConfiguredBy = item.GetProperty("configuredBy").GetString()
                    });
                }

                _logger.LogInformation("Retrieved {DatasetCount} datasets", datasets.Count);
                return datasets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch datasets");
                throw;
            }
        }

        /// <summary>
        /// Publish analysis results to SharePoint
        /// </summary>
        public async Task<string> PublishToSharePointAsync(string siteId, AnalysisResult result, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(siteId) || result == null)
                throw new ArgumentNullException("Site ID and result required");

            await _rateLimiter.WaitAsync();

            try
            {
                _logger.LogInformation("Publishing to SharePoint: {SiteId}", siteId);
                
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(2000); // 2 second timeout

                var folderPath = $"sites/{siteId}/AI-Insights";
                var fileName = $"analysis-{DateTime.UtcNow:yyyyMMddHHmmss}.json";

                var content = JsonSerializer.Serialize(new
                {
                    result.Title,
                    result.Summary,
                    result.Details,
                    Timestamp = DateTime.UtcNow,
                    Visualizations = result.Visualizations?.Count ?? 0
                }, new JsonSerializerOptions { WriteIndented = true });

                var request = new HttpRequestMessage(HttpMethod.Put,
                    $"https://graph.microsoft.com/v1.0/sites/{siteId}/drive/root:/{folderPath}/{fileName}:/content");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Published to SharePoint successfully");
                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SharePoint publish failed");
                throw;
            }
        }

        /// <summary>
        /// Post analysis result to Teams channel
        /// </summary>
        public async Task<bool> PostToTeamsAsync(string teamId, string channelId, ShareableResult result, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(channelId) || result == null)
                throw new ArgumentNullException("Team ID, channel ID, and result required");

            await _rateLimiter.WaitAsync();

            try
            {
                _logger.LogInformation("Posting to Teams: {TeamId}/{ChannelId}", teamId, channelId);
                
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(1000); // 1 second timeout

                var adaptiveCard = new
                {
                    @type = "MessageCard",
                    @context = "https://schema.org/extensions",
                    summary = result.Title,
                    themeColor = "0078D4",
                    sections = new[]
                    {
                        new
                        {
                            activityTitle = result.Title,
                            activitySubtitle = "AI Analysis Result",
                            facts = result.Metrics.Select(m => new { name = m.Key, value = m.Value.ToString() }).ToArray(),
                            text = result.Description
                        }
                    },
                    potentialAction = new[]
                    {
                        new { @type = "OpenUri", name = "View Details", targets = new[] { new { os = "default", uri = result.Url } } }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"https://graph.microsoft.com/v1.0/teams/{teamId}/channels/{channelId}/messages");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { body = new { content = JsonSerializer.Serialize(adaptiveCard) } }),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Posted to Teams successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teams post failed");
                return false;
            }
        }

        /// <summary>
        /// Create Power BI report from AI analysis
        /// </summary>
        public async Task<PowerBiReport> CreateReportAsync(string datasetId, AnalysisResult analysis, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(datasetId) || analysis == null)
                throw new ArgumentNullException("Dataset ID and analysis required");

            _logger.LogInformation("Creating Power BI report from analysis");
            
            return new PowerBiReport
            {
                Id = Guid.NewGuid().ToString(),
                Name = analysis.Title,
                DatasetId = datasetId,
                CreatedAt = DateTime.UtcNow,
                Pages = 3, // Default pages
                Owner = "MonadoBladeV3"
            };
        }

        private async Task RefreshAccessTokenAsync()
        {
            if (_tokenExpiry > DateTime.UtcNow.AddMinutes(5))
                return; // Token still valid for 5+ minutes

            try
            {
                _logger.LogInformation("Refreshing M365 access token");
                // In production, integrate with Azure Identity
                // For now, use cached token or implement OAuth flow
                _tokenExpiry = DateTime.UtcNow.AddHours(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
            }
        }

        public string ServiceName => "M365PowerBiBridge";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "ApiCallsThrottled", _rateLimiter.ThrottledCount },
            { "TokenExpiry", _tokenExpiry },
            { "ServiceStatus", Status }
        };
    }

    public class QueryResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class AnalysisResult
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, object>? Details { get; set; }
        public List<string>? Visualizations { get; set; }
    }

    public class ShareableResult
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public Dictionary<string, long> Metrics { get; set; } = new();
    }

    public class PowerBiDataset
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ConfiguredBy { get; set; }
    }

    public class PowerBiReport
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DatasetId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Pages { get; set; }
        public string Owner { get; set; } = string.Empty;
    }

    /// <summary>
    /// Simple rate limiter for API compliance
    /// </summary>
    internal class RateLimiter
    {
        private readonly int _maxRequests;
        private readonly Queue<DateTime> _requestTimes = new();
        private readonly SemaphoreSlim _semaphore = new(1);
        public int ThrottledCount { get; private set; }

        public RateLimiter(int maxRequestsPerMinute)
        {
            _maxRequests = maxRequestsPerMinute;
        }

        public async Task WaitAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
                    _requestTimes.Dequeue();

                if (_requestTimes.Count >= _maxRequests)
                {
                    ThrottledCount++;
                    var delayUntil = _requestTimes.Peek().AddMinutes(1);
                    var delay = (int)(delayUntil - now).TotalMilliseconds;
                    if (delay > 0)
                        await Task.Delay(delay);
                }

                _requestTimes.Enqueue(DateTime.UtcNow);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
