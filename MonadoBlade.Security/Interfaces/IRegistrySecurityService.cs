namespace MonadoBlade.Security.Interfaces;

/// <summary>
/// Interface for Windows Registry security policy management
/// </summary>
public interface IRegistrySecurityService
{
    /// <summary>
    /// Applies a security policy to the Windows Registry
    /// </summary>
    Task<bool> ApplySecurityPolicyAsync(string policyName, Dictionary<string, object> policySettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current value of a registry security policy
    /// </summary>
    Task<object?> GetPolicyValueAsync(string policyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces UAC (User Account Control) restrictions
    /// </summary>
    Task<bool> EnforceUacAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables unnecessary Windows services for security
    /// </summary>
    Task<bool> HardenServiceConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables Windows Defender Real-time Protection
    /// </summary>
    Task<bool> EnableDefenderProtectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures firewall rules
    /// </summary>
    Task<bool> ConfigureFirewallAsync(Dictionary<string, object> firewallRules, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current security policy configuration
    /// </summary>
    Task<Dictionary<string, object>> GetCurrentSecurityConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Audits security configuration compliance
    /// </summary>
    Task<Dictionary<string, bool>> AuditSecurityComplianceAsync(CancellationToken cancellationToken = default);
}
