namespace MonadoBlade.Security.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Configuration;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Services;

/// <summary>
/// Extension methods for dependency injection setup
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Security services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddMonadoBladeSecurityServices(
        this IServiceCollection services,
        SecurityConfiguration? configuration = null)
    {
        configuration ??= new SecurityConfiguration();

        // Add configuration
        services.AddSingleton(configuration);
        services.AddSingleton(configuration.VhdxSettings);
        services.AddSingleton(configuration.BitLockerSettings);
        services.AddSingleton(configuration.TpmSettings);
        services.AddSingleton(configuration.EncryptionSettings);

        // Add services
        services.AddSingleton<IVhdxService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<VhdxService>>();
            return new VhdxService(logger, configuration.ContainerBasePath);
        });

        services.AddSingleton<IBitLockerService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<BitLockerService>>();
            return new BitLockerService(logger);
        });

        services.AddSingleton<ITpmService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TpmService>>();
            return new TpmService(logger);
        });

        services.AddSingleton<IEncryptionService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EncryptionService>>();
            return new EncryptionService(logger);
        });

        services.AddSingleton<IVaultService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<VaultService>>();
            var vhdxService = sp.GetRequiredService<IVhdxService>();
            var bitLockerService = sp.GetRequiredService<IBitLockerService>();
            return new VaultService(logger, vhdxService, bitLockerService);
        });

        services.AddSingleton<IQuarantineService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<QuarantineService>>();
            return new QuarantineService(logger, configuration.QuarantinePath);
        });

        services.AddSingleton<IRegistrySecurityService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RegistrySecurityService>>();
            return new RegistrySecurityService(logger);
        });

        return services;
    }
}
