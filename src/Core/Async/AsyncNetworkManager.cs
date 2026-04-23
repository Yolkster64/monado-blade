using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Async
{
    /// <summary>
    /// Manages asynchronous network operations with cancellation token support.
    /// Replaces synchronous HTTP and DNS operations with async alternatives.
    /// </summary>
    public class AsyncNetworkManager : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _downloadSemaphore;
        private readonly int _maxConcurrentDownloads;
        private bool _disposed;

        public AsyncNetworkManager(int maxConcurrentDownloads = 5, TimeSpan? timeout = null)
        {
            _maxConcurrentDownloads = maxConcurrentDownloads;
            _downloadSemaphore = new SemaphoreSlim(maxConcurrentDownloads);

            _httpClient = new HttpClient(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            })
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            };
        }

        public class DownloadResult
        {
            public bool Success { get; set; }
            public string Url { get; set; }
            public byte[] Data { get; set; }
            public int BytesDownloaded { get; set; }
            public TimeSpan Duration { get; set; }
            public Exception Error { get; set; }
            public int HttpStatusCode { get; set; }
        }

        public class DownloadProgress
        {
            public long BytesReceived { get; set; }
            public long? TotalBytesToReceive { get; set; }
            public int PercentComplete => TotalBytesToReceive.HasValue && TotalBytesToReceive.Value > 0
                ? (int)((BytesReceived * 100) / TotalBytesToReceive.Value)
                : 0;
        }

        /// <summary>
        /// Async download single URL with automatic retry and timeout.
        /// </summary>
        public async Task<DownloadResult> DownloadAsync(string url, 
            IProgress<DownloadProgress> progress = null,
            CancellationToken cancellationToken = default,
            int maxRetries = 3)
        {
            var startTime = DateTime.UtcNow;

            await _downloadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        using (var response = await _httpClient.GetAsync(url, 
                            HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                            .ConfigureAwait(false))
                        {
                            if (!response.IsSuccessStatusCode && i < maxRetries - 1)
                            {
                                var delayMs = (int)Math.Pow(2, i) * 100;
                                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            response.EnsureSuccessStatusCode();
                            var totalBytes = response.Content.Headers.ContentLength;

                            using (var contentStream = await response.Content.ReadAsStreamAsync()
                                .ConfigureAwait(false))
                            {
                                var buffer = new byte[8192];
                                var data = new List<byte>();
                                int bytesRead;
                                long totalRead = 0;

                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                    .ConfigureAwait(false)) > 0)
                                {
                                    data.AddRange(buffer[..bytesRead]);
                                    totalRead += bytesRead;

                                    progress?.Report(new DownloadProgress
                                    {
                                        BytesReceived = totalRead,
                                        TotalBytesToReceive = totalBytes
                                    });
                                }

                                return new DownloadResult
                                {
                                    Success = true,
                                    Url = url,
                                    Data = data.ToArray(),
                                    BytesDownloaded = data.Count,
                                    HttpStatusCode = (int)response.StatusCode,
                                    Duration = DateTime.UtcNow - startTime
                                };
                            }
                        }
                    }
                    catch (HttpRequestException) when (i < maxRetries - 1)
                    {
                        var delayMs = (int)Math.Pow(2, i) * 100;
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                return new DownloadResult
                {
                    Success = false,
                    Url = url,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
            catch (Exception ex)
            {
                return new DownloadResult
                {
                    Success = false,
                    Url = url,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
            finally
            {
                _downloadSemaphore.Release();
            }

            return null;
        }

        /// <summary>
        /// Async batch download multiple URLs with controlled concurrency.
        /// </summary>
        public async Task<List<DownloadResult>> BatchDownloadAsync(
            IEnumerable<string> urls,
            CancellationToken cancellationToken = default)
        {
            var results = new List<DownloadResult>();
            var tasks = new List<Task>();

            foreach (var url in urls)
            {
                var task = DownloadAsync(url, null, cancellationToken)
                    .ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            results.Add(t.Result);
                        }
                    }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return results;
        }

        /// <summary>
        /// Async stream download for large files with memory efficiency.
        /// </summary>
        public async IAsyncEnumerable<byte[]> StreamDownloadAsync(string url,
            int chunkSize = 81920,
            CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                using (var contentStream = await response.Content.ReadAsStreamAsync()
                    .ConfigureAwait(false))
                {
                    var buffer = new byte[chunkSize];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, chunkSize, cancellationToken)
                        .ConfigureAwait(false)) > 0)
                    {
                        yield return buffer[..bytesRead];
                    }
                }
            }
        }

        /// <summary>
        /// Async DNS lookup with timeout handling.
        /// </summary>
        public async Task<IPAddress[]> ResolveDNSAsync(string hostname,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null)
        {
            try
            {
                var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);

                var task = Dns.GetHostAddressesAsync(hostname);
                var completedTask = await Task.WhenAny(
                    task,
                    Task.Delay(effectiveTimeout, cancellationToken)
                ).ConfigureAwait(false);

                if (completedTask == task)
                {
                    return task.Result;
                }

                throw new OperationCanceledException($"DNS lookup for {hostname} timed out");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"DNS resolution failed for {hostname}", ex);
            }
        }

        /// <summary>
        /// Async HTTP POST request with automatic retry.
        /// </summary>
        public async Task<DownloadResult> PostAsync(string url, HttpContent content,
            CancellationToken cancellationToken = default,
            int maxRetries = 3)
        {
            var startTime = DateTime.UtcNow;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var response = await _httpClient.PostAsync(url, content, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        var responseData = await response.Content.ReadAsByteArrayAsync()
                            .ConfigureAwait(false);

                        return new DownloadResult
                        {
                            Success = response.IsSuccessStatusCode,
                            Url = url,
                            Data = responseData,
                            BytesDownloaded = responseData.Length,
                            HttpStatusCode = (int)response.StatusCode,
                            Duration = DateTime.UtcNow - startTime
                        };
                    }
                }
                catch (HttpRequestException) when (i < maxRetries - 1)
                {
                    var delayMs = (int)Math.Pow(2, i) * 100;
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return new DownloadResult
                    {
                        Success = false,
                        Url = url,
                        Duration = DateTime.UtcNow - startTime,
                        Error = ex
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Async HTTP HEAD request to check URL availability without downloading.
        /// </summary>
        public async Task<bool> CheckUrlAvailableAsync(string url,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null)
        {
            try
            {
                var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(effectiveTimeout);

                using (var response = await _httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, url),
                    cancellationToken: cts.Token)
                    .ConfigureAwait(false))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Async get current IPv4 address by connecting to external service.
        /// </summary>
        public async Task<string> GetPublicIPAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await DownloadAsync("https://api.ipify.org?format=text", 
                    null, cancellationToken, maxRetries: 2)
                    .ConfigureAwait(false);

                if (result.Success && result.Data != null)
                {
                    var ipString = System.Text.Encoding.UTF8.GetString(result.Data).Trim();
                    if (IPAddress.TryParse(ipString, out _))
                    {
                        return ipString;
                    }
                }

                throw new InvalidOperationException("Failed to retrieve public IP");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to determine public IP address", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _httpClient?.Dispose();
            _downloadSemaphore?.Dispose();
            _disposed = true;
        }
    }
}
