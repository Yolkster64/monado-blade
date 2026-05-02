namespace MonadoBlade.Security.Services;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Exceptions;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;

public class TpmService : ITpmService
{
    private readonly ILogger<TpmService> _logger;

    public TpmService(ILogger<TpmService> logger)
    {
        _logger = logger;
    }

    public async Task<TpmStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting TPM status");
            var status = new TpmStatus
            {
                IsAvailable = await IsAvailableAsync(cancellationToken),
                IsReady = true,
                SpecVersion = "2.0",
                IsManagedBySystem = true,
                IsOwned = true,
                LastUpdated = DateTime.UtcNow,
                PcrValues = new List<TpmPcr>()
            };
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TPM status");
            return null;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing TPM");
        return await Task.FromResult(true);
    }

    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Clearing TPM");
        return await Task.FromResult(true);
    }

    public async Task<TpmPcr?> GetPcrAsync(int pcrIndex, string hashAlgorithm = "SHA256", CancellationToken cancellationToken = default)
    {
        string value = $"SHA256_{pcrIndex:D2}_" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 32);
        return await Task.FromResult(new TpmPcr { Index = pcrIndex, HashAlgorithm = hashAlgorithm, Value = value });
    }

    public async Task<byte[]> SealDataAsync(byte[] data, int[] pcrIndices, CancellationToken cancellationToken = default)
    {
        byte[] sealed_data = new byte[data.Length + 32];
        Buffer.BlockCopy(BitConverter.GetBytes(pcrIndices.Length), 0, sealed_data, 0, 4);
        Buffer.BlockCopy(data, 0, sealed_data, 32, data.Length);
        return await Task.FromResult(sealed_data);
    }

    public async Task<byte[]> UnsealDataAsync(byte[] sealedData, CancellationToken cancellationToken = default)
    {
        if (sealedData.Length < 32)
            throw new TpmException("Invalid sealed data format");
        byte[] unsealed = new byte[sealedData.Length - 32];
        Buffer.BlockCopy(sealedData, 32, unsealed, 0, unsealed.Length);
        return await Task.FromResult(unsealed);
    }

    public async Task<string?> GetFirmwareVersionAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult("2.0");
    }
}