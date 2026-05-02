# MonadoBlade.Security Architecture

## System Overview

```
┌─────────────────────────────────────────────────────┐
│          MonadoBlade.Security Module                │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │  Application Layer (DI Integration)         │   │
│  │  ServiceCollectionExtensions                │   │
│  └─────────────────────────────────────────────┘   │
│           ▲                    ▲                     │
│           │                    │                     │
│  ┌────────┴──────┐  ┌──────────┴─────────┐          │
│  │  IVhdxService │  │ IBitLockerService  │          │
│  │  VhdxService  │  │ BitLockerService   │          │
│  └────────┬──────┘  └──────────┬─────────┘          │
│           │                    │                     │
│  ┌────────┴──────┐  ┌──────────┴─────────┐          │
│  │ ITpmService   │  │ IVaultService      │          │
│  │ TpmService    │  │ VaultService       │          │
│  └────────┬──────┘  └──────────┬─────────┘          │
│           │                    │                     │
│  ┌────────┴──────────────────────────────┐          │
│  │  Core Security Operations             │          │
│  │  • Encryption/Decryption              │          │
│  │  • Key Management                     │          │
│  │  • Access Control                     │          │
│  │  • Threat Quarantine                  │          │
│  └────────┬──────────────────────────────┘          │
│           │                                          │
│  ┌────────┴──────────────────────────────┐          │
│  │  Operating System Integration         │          │
│  │  • Windows API (DiskPart, BitLocker)  │          │
│  │  • TPM 2.0 Interface                  │          │
│  │  • Registry Management                │          │
│  │  • Service Control                    │          │
│  └───────────────────────────────────────┘          │
│                                                     │
└─────────────────────────────────────────────────────┘
```

## Component Hierarchy

### Layer 1: Interfaces (Contracts)
```
IVhdxService
IBitLockerService
ITpmService
IVaultService
IQuarantineService
IEncryptionService
IRegistrySecurityService
```

### Layer 2: Implementations
```
VhdxService → VHDX disk image management
BitLockerService → Full-disk encryption
TpmService → TPM 2.0 platform integration
VaultService → High-level vault orchestration
QuarantineService → Threat containment
EncryptionService → Cryptographic operations
RegistrySecurityService → Windows security hardening
```

### Layer 3: Models & Configuration
```
VhdxContainerConfig
VaultContainer
QuarantineEntry
BitLockerStatus
TpmStatus
EncryptionKey
SecurityConfiguration
```

### Layer 4: Exception Hierarchy
```
MonadoBladeSecurityException
├── VhdxOperationException
│   ├── VhdxMountException
│   ├── VhdxUnmountException
│   └── VhdxCreationException
├── BitLockerException
│   └── BitLockerEncryptionException
├── TpmException
├── VaultException
├── QuarantineException
└── EncryptionException
```

## Data Flow Diagrams

### Vault Creation and Encryption Flow

```
User Request
    │
    ▼
IVaultService.CreateVaultAsync()
    │
    ├─► Generate Vault ID
    │
    ├─► IVhdxService.CreateVhdxAsync()
    │   │
    │   ├─► Generate DiskPart script
    │   │
    │   ├─► Execute diskpart.exe
    │   │
    │   └─► Format volume
    │
    ├─► IBitLockerService.EnableBitLockerAsync()
    │   │
    │   ├─► Select encryption algorithm
    │   │
    │   ├─► Execute manage-bde.exe
    │   │
    │   └─► Enable TPM protector
    │
    ├─► Create VaultContainer object
    │
    └─► Return to caller
```

### File Quarantine Flow

```
Suspicious File Detected
    │
    ▼
IQuarantineService.QuarantineFileAsync()
    │
    ├─► Analyze threat level
    │
    ├─► Generate entry ID (GUID)
    │
    ├─► Copy to quarantine location
    │   D:\Monado\Containers\Quarantine\
    │
    ├─► Compute SHA-256 hash
    │
    ├─► Create QuarantineEntry record
    │
    ├─► Delete original file
    │
    └─► Return entry to caller
```

### Encryption/Decryption Pipeline

```
User Data
    │
    ▼
IEncryptionService.EncryptAes256Async()
    │
    ├─► Retrieve encryption key
    │
    ├─► Generate random IV
    │
    ├─► Create AES cipher (256-bit)
    │
    ├─► Encrypt data with IV prepended
    │
    ├─► Return encrypted bytes
    │
    ▼
Encrypted Data (IV + Ciphertext)
    │
    ├─► [Storage/Network Transport]
    │
    ▼
IEncryptionService.DecryptAes256Async()
    │
    ├─► Extract IV from ciphertext
    │
    ├─► Retrieve encryption key
    │
    ├─► Create AES decipher (256-bit)
    │
    ├─► Decrypt using extracted IV
    │
    └─► Return plaintext
```

## Service Interaction Matrix

| Service | Depends On | Used By |
|---------|-----------|---------|
| VhdxService | None | VaultService |
| BitLockerService | None | VaultService |
| TpmService | None | Multiple |
| EncryptionService | None | Multiple |
| QuarantineService | None | Application |
| VaultService | VhdxService, BitLockerService | Application |
| RegistrySecurityService | None | Application |

## State Machine: Vault Container

```
┌──────────┐
│Unmounted │
└────┬─────┘
     │ OpenVault()
     ▼
┌──────────┐
│ Mounting │
└────┬─────┘
     │ Mount successful
     ▼
┌──────────┐
│ Mounted  │
└────┬─────┘
     │ CloseVault()
     ▼
┌──────────┐
│Unmounting│
└────┬─────┘
     │ Unmount successful
     ▼
┌──────────┐
│Unmounted │
└──────────┘
```

## Mount Point Configuration

```
D:\
├── Monado\
│   └── Containers\
│       ├── Vaults\
│       │   ├── vault1.vhdx → V:\
│       │   ├── vault2.vhdx → V:\
│       │   └── ...
│       ├── Quarantine\
│       │   ├── entry1_malware.exe
│       │   ├── entry2_trojan.dll
│       │   └── ...
│       ├── Sandbox\
│       │   └── sandbox.vhdx → S:\
│       └── DevDrive\
│           └── devdrive.vhdx → E:\
│
V:\ (Vault - Encrypted)
Q:\ (Quarantine - Read-Only)
S:\ (Sandbox - Session)
E:\ (DevDrive - ReFS)
```

## Security Model

### Defense in Depth

```
Layer 1: Access Control
├── User authentication
├── Permission verification
└── Role-based access

Layer 2: Encryption
├── Volume-level (BitLocker)
├── Data-level (AES-256)
└── Key management (TPM 2.0)

Layer 3: Isolation
├── VHDX containers
├── Quarantine zones
└── Sandboxed environments

Layer 4: Monitoring
├── Threat detection
├── Integrity verification
└── Compliance auditing
```

### Key Protection

```
Plaintext Key
    │
    ▼
[Generate Random IV]
    │
    ▼
[Encrypt with AES-256]
    │
    ▼
[Seal to TPM with PCR binding]
    │
    ▼
Encrypted Key (TPM-sealed)
```

## Configuration Precedence

1. **Explicit Configuration** - Application-supplied SecurityConfiguration
2. **Environment Variables** - System environment variables
3. **Default Values** - Hard-coded defaults in SecurityConfiguration

## Performance Characteristics

### VHDX Operations
- Create: ~5-10 seconds (depends on disk speed)
- Mount: 2-3 seconds
- Unmount: 1-2 seconds
- Compact: 5-30 seconds (depends on data)

### Encryption Operations
- Key Generation: < 100 ms
- AES Encryption (1 MB): < 50 ms
- AES Decryption (1 MB): < 50 ms
- SHA-256 Hash (1 MB): < 20 ms

### TPM Operations
- PCR Read: < 100 ms
- Data Seal: < 500 ms
- Data Unseal: < 500 ms

## Testing Architecture

```
MonadoBlade.Security.Tests
├── EncryptionServiceTests (9 tests)
├── VaultServiceTests (9 tests)
├── QuarantineServiceTests (6 tests)
├── BitLockerServiceTests (2 tests)
├── TpmServiceTests (5 tests)
└── RegistrySecurityServiceTests (4 tests)
```

## Logging Integration

All services use ILogger for structured logging:

```csharp
_logger.LogInformation("Vault created: {VaultId}", vaultId);
_logger.LogWarning("BitLocker status unknown: {Volume}", volume);
_logger.LogError(ex, "Failed to mount VHDX: {Path}", vhdxPath);
_logger.LogDebug("TPM status: {Status}", tpmStatus);
```

## Deployment Checklist

- ✅ .NET 10.0 SDK installed
- ✅ Administrator privileges available
- ✅ TPM 2.0 module available
- ✅ Adequate disk space on D:\ drive
- ✅ BitLocker policy enabled (if applicable)
- ✅ Windows Defender compatible
- ✅ Firewall rules configured
- ✅ Registry permissions granted

---

**Architecture Version**: 1.0  
**Last Updated**: 2026-04-23
