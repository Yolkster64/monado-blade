namespace MonadoBlade.Security.Configuration;

/// <summary>
/// Configuration settings for the Security module
/// </summary>
public class SecurityConfiguration
{
    public string ContainerBasePath { get; set; } = @"D:\Monado\Containers";
    public string VaultsPath { get; set; } = @"D:\Monado\Containers\Vaults";
    public string QuarantinePath { get; set; } = @"D:\Monado\Containers\Quarantine";
    
    public VhdxSettings VhdxSettings { get; set; } = new();
    public BitLockerSettings BitLockerSettings { get; set; } = new();
    public TpmSettings TpmSettings { get; set; } = new();
    public EncryptionSettings EncryptionSettings { get; set; } = new();
}

public class VhdxSettings
{
    public long DefaultVaultSizeGb { get; set; } = 100;
    public long DefaultSandboxSizeGb { get; set; } = 50;
    public long DefaultQuarantineSizeGb { get; set; } = 50;
    public long DefaultDevDriveSizeGb { get; set; } = 100;
    
    public char VaultDrive { get; set; } = 'V';
    public char QuarantineDrive { get; set; } = 'Q';
    public char SandboxDrive { get; set; } = 'S';
    public char DevDrive { get; set; } = 'E';
    
    public int MountTimeoutSeconds { get; set; } = 10;
    public int UnmountTimeoutSeconds { get; set; } = 10;
}

public class BitLockerSettings
{
    public bool RequireTPM { get; set; } = true;
    public string PreferredEncryption { get; set; } = "AES256";
    public bool EnableRecoveryKey { get; set; } = true;
    public bool AutoEnableOnMount { get; set; } = true;
}

public class TpmSettings
{
    public bool RequireTPM20 { get; set; } = true;
    public bool AllowClear { get; set; } = false;
    public bool AutoInitialize { get; set; } = true;
    public int[] PcrIndicesToSeal { get; set; } = { 0, 1, 2, 3, 7 };
}

public class EncryptionSettings
{
    public int DefaultKeySize { get; set; } = 256;
    public string DefaultAlgorithm { get; set; } = "AES";
    public bool StoreKeysSecurely { get; set; } = true;
}
