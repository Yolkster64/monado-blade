# Storage Architecture: Tiered Partition Strategy with Cross-Software Separation

**Hardware:** Disk 0 (900GB SSD) + Disk 1 (1.87TB HDD)  
**Strategy:** Core + Common + Cross-Software + User-Specific partitions  
**Goal:** Security isolation, zero fragmentation, efficient resource sharing  

---

## Executive Summary

This tiered architecture separates storage by usage scope:

- ✅ **Core (100GB):** OS + base utilities (everyone)
- ✅ **Common (200GB):** Large software 3-4 users (Office, Visual Studio, Email)
- ✅ **Cross (150GB):** Cross-software tools (GPU boosting, CUDA, ThX Spatial Audio, drivers)
- ✅ **User-Specific Partitions:** Gamer, Studio, Developer, Worker (no cross-user access)
- ✅ **Security Zones:** Vault + Quarantine (accessible to all)
- ✅ **Dev Drive (ReFS):** Zero fragmentation code environment

**Access Model:**
```
Each user can access:
  Core (mandatory)
  + Common (if software applies)
  + Cross (specific tools needed)
  + Their own user partition (exclusive)
  + Vault + Quarantine (both accessible)
```

---

## Disk Layout

```
DISK 0 (900GB - SSD NVMe):
├─ C:\        [150GB]  Core OS
├─ E:\        [200GB]  Common Software (Office, VS2022, Email)
├─ X:\        [150GB]  Cross Software (GPU boosting, CUDA, spatial audio, drivers)
├─ G:\        [250GB]  Dev Drive (VHDX ReFS - zero fragmentation)
├─ K:\        [100GB]  Quarantine (immutable, accessible by all)
└─ V:\        [100GB]  Vault (encrypted, accessible by all)

DISK 1 (1.87TB - HDD/SATA):
├─ UserData   [1.82TB] ONE LARGE PARTITION
│  ├─ Games/          [500GB]  Gamer user folder (locked)
│  ├─ Studio/         [400GB]  Studio user folder (locked)
│  ├─ Developer/      [350GB]  Developer user folder (locked)
│  ├─ Worker/         [200GB]  Worker user folder (locked)
│  └─ Shared/         [350GB]  Cross-user backups, templates, archives
│
└─ Recovery   [ 20GB]  Disaster recovery partition
```

**Total SSD: 950GB allocated (50GB reserved)**  
**Total HDD: 1.82TB in one UserData partition (50GB reserved)**

**Key:** All user folders are on SAME physical partition but with NTFS ACLs preventing cross-user access

---

## DISK 0: SSD Fast Storage

### C: Core OS (150GB)

**Purpose:** Windows 11 Pro, system services, universal drivers

**Contents:**
```
C:\Windows\              [System files - 50GB]
C:\Program Files\        [Reserved 5GB]
C:\Program Files (x86)\  [Legacy apps - 10GB]
C:\ProgramData\          [System apps - 15GB]
C:\Users\
  ├─ <profiles>\AppData\Roaming   [20GB, synced cross-device]
  └─ <profiles>\AppData\Local    [30GB, machine-specific]
C:\Drivers\              [Universal drivers - 5GB]
```

**Universal Content:**
- Network drivers (Ethernet, WiFi)
- USB drivers
- Storage drivers
- Base system utilities
- Accessibility tools
- Windows Sandbox (if needed)

**Access:** All users (read-only except system)

**Optimization:**
- ✅ Minimal bloat
- ✅ Weekly TRIM pass
- ✅ No user-specific documents/downloads

---

### E: Common Software (200GB)

**Purpose:** Large software used by 3-4 user profiles

**Contents:**
```
E:\Office\
├─ Microsoft Office 365   [30GB]
├─ Outlook                [10GB]
└─ OneDrive               [20GB]

E:\Development\
├─ Visual Studio 2022     [50GB]
├─ Git                    [5GB]
├─ .NET 8.0               [10GB]
└─ Build Tools            [10GB]

E:\Email\
├─ Thunderbird            [5GB]
├─ Mailbox Archives       [10GB]
└─ Plugins                [5GB]

E:\Utilities\
├─ Firefox / Edge         [10GB]
├─ VLC / Media Player     [5GB]
├─ 7-Zip / WinRAR         [2GB]
└─ System Tools           [6GB]

E:\Settings\
├─ office-config.json
├─ vs-config.json
└─ common-settings.json
```

**User Access:**
- **Developer:** YES (Office, VS2022, Git)
- **Studio:** YES (Office, Email, Utilities)
- **Worker:** YES (Office, Email, Utilities, Outlook)
- **Gamer:** YES (Office, Email, Utilities)

**Optimization:**
- ✅ Single installation (no duplication)
- ✅ Symbolic links from Program Files
- ✅ Weekly TRIM
- ✅ Quarterly updates

---

### X: Cross Software (150GB)

**Purpose:** Tools used across multiple user profiles (hyper-specific tools)

**Three Categories:**

#### GPU & AI Tools (70GB)
```
X:\GPU\
├─ NVIDIA\
│  ├─ Driver-560.70/           [30GB driver + runtime]
│  ├─ CUDA-12.4/               [20GB toolchain]
│  │  ├─ bin/
│  │  ├─ lib/
│  │  ├─ include/
│  │  └─ samples/
│  ├─ cuDNN/                   [10GB]
│  ├─ cuBLAS/                  [5GB]
│  ├─ cuFFT/                   [5GB]
│  └─ GameWorks/               [3GB, game-specific optimizations]
│
├─ AMD\
│  ├─ ROCm-5.7/                [15GB for fallback]
│  └─ RDNA-Driver/             [8GB]
│
└─ Intel\
   ├─ Arc-Driver/              [6GB]
   ├─ oneAPI-2024.1/           [8GB]
   └─ DPC++/                   [2GB]
```

**User Access:**
- **Developer:** YES (CUDA, cuDNN, toolchain)
- **Studio:** YES (CUDA for rendering, AI plugins)
- **Gamer:** YES (GameWorks, driver optimization)
- **Worker:** NO (not needed)

#### Audio & Spatial (40GB)
```
X:\Audio\
├─ WindowsSpatial/            [2GB]
├─ DolbyAtmos/                [3GB]
├─ ASIO/                      [2GB]
├─ VST3-Common/               [15GB shared plugins]
│  ├─ Native-Instruments/
│  ├─ Kontakt/
│  ├─ iZotope/
│  └─ Fabfilter/
├─ ReWire/                    [2GB]
└─ SpatialAudioThX/           [4GB THXSA spatial engine]
```

**User Access:**
- **Developer:** YES (ThX Spatial for dev testing)
- **Studio:** YES (Full audio suite, VST3, ASIO)
- **Gamer:** YES (Spatial audio, Dolby Atmos)
- **Worker:** NO (not needed)

#### Drivers & Optimization (40GB)
```
X:\Drivers\
├─ Audio/
│  ├─ Realtek-HD/
│  ├─ USB-Audio-Class/
│  └─ Virtual-Audio/
│
├─ Storage/
│  ├─ NVMe-Controller/
│  ├─ SATA-Drivers/
│  └─ Controller-Firmware/
│
├─ Network/
│  ├─ Ethernet/
│  ├─ WiFi-Cards/
│  └─ Bluetooth/
│
├─ Optimization/
│  ├─ Power-Profiles/
│  ├─ Performance-Tuning/
│  └─ Temperature-Monitoring/
│
└─ DeviceStack/
   ├─ Chipset-Driver/
   └─ System-Update/
```

**User Access:** ALL users (required for hardware functionality)

**Key Features:**
- ✅ Organized by function, not vendor
- ✅ No duplication (single version of each tool)
- ✅ Easy version management (keep 2 previous versions)
- ✅ Per-tool access control
- ✅ Quarterly archival of old versions

---

### G: Dev Drive (250GB) - VHDX ReFS

**Purpose:** Development with guaranteed zero external fragmentation

**Technology:** Microsoft Dev Drive (ReFS in VHDX container)

**Structure:**
```
G:\
├─ Projects/                  [150GB]
│  ├─ MonadoBlade/
│  ├─ HeliosPlatform/
│  ├─ Experiments/
│  └─ Archive/
│
├─ Build/                     [50GB]
│  ├─ Debug/
│  ├─ Release/
│  └─ Artifacts/
│
├─ NugetCache/                [30GB]
└─ Git/                       [20GB]
```

**Access:** Developer profile only (read-only reference for others)

**Optimization:**
- ✅ Zero external fragmentation (guaranteed by ReFS)
- ✅ Snapshot capability
- ✅ Copy-on-write cloning
- ✅ Native VS Code/Visual Studio integration

---

### K: Quarantine (100GB) - Immutable Security

**Purpose:** Infected/suspicious files isolated

**Properties:**
```
✅ Read-only after initial placement (NTFS ACLs)
✅ Accessible by ALL users (admin moves files here)
✅ Automatic indexing disabled
✅ No execution allowed
✅ Separate backup chain
```

**Workflow:**
1. Malware scanner detects threat
2. File auto-moved to K:\ (automatic, no user prompt)
3. Archived to external storage monthly
4. Can be recovered if false positive (admin only)

---

### V: Vault (100GB) - Encrypted Storage

**Purpose:** Sensitive data accessible by authorized users

**Technology:** BitLocker Full Disk Encryption (AES-256)

**Access Control:**
- Admin: Full access
- Developer: Read access (development certificates/keys)
- Others: No access (unless specifically authorized)

**Structure:**
```
V:\
├─ AdminOnly/
│  ├─ System-Credentials/
│  ├─ Domain-Passwords/
│  └─ License-Keys/
│
├─ Shared/
│  ├─ CodeSigningCerts/       [Dev uses for signing]
│  ├─ APIKeys/                [Dev uses for APIs]
│  └─ SSL-Certificates/       [Dev uses for HTTPS]
│
└─ BackupKeys/                [Offline emergency recovery]
```

**Recovery:** TPM 2.0 + PIN/password + offline recovery key (USB in safe)

---

## DISK 1: HDD Large Storage (Single UserData Partition with User-Locked Folders)

### UserData Partition (1.82TB) - All Users with Layer Access Control

**Purpose:** One large partition containing all user-specific content with NTFS ACL folder locking

**Key Principle:** Same physical partition, but NTFS ACLs prevent cross-user access to folders

**Structure:**
```
UserData:\
├─ Games/                [500GB]  ← Gamer user ONLY
│  ├─ SteamLibrary/
│  │  ├─ common/
│  │  ├─ compatdata/
│  │  └─ screenshots/
│  ├─ GameSaves/
│  │  ├─ Steam-Cloud/
│  │  ├─ Local-Saves/
│  │  └─ Backups/
│  └─ Mods/
│
├─ Studio/               [400GB]  ← Studio user ONLY
│  ├─ Reaper/
│  │  ├─ Projects/      [120GB REP files]
│  │  ├─ Backups/       [30GB]
│  │  ├─ Rendering/     [80GB]
│  │  └─ Temp/          [20GB]
│  ├─ Plugins/          [80GB]
│  ├─ Samples/          [50GB]
│  └─ Masters/          [20GB]
│
├─ Developer/            [350GB]  ← Developer user ONLY
│  ├─ Projects/         [180GB]
│  │  ├─ MonadoBlade/
│  │  ├─ HeliosPlatform/
│  │  ├─ Experiments/
│  │  └─ Archive/
│  ├─ Build/            [100GB]
│  │  ├─ Debug/
│  │  ├─ Release/
│  │  └─ Artifacts/
│  ├─ Workspace/        [50GB]
│  └─ Archive/          [20GB]
│
├─ Worker/              [200GB]   ← Worker user ONLY
│  ├─ Documents/        [80GB]
│  ├─ Spreadsheets/     [40GB]
│  ├─ Presentations/    [30GB]
│  ├─ Email-Archive/    [30GB]
│  └─ Cloud-Sync/       [20GB]
│
└─ Shared/              [350GB]   ← ALL users (read-only)
   ├─ Templates/        [30GB]
   │  ├─ Office-Templates/
   │  ├─ Video-Templates/
   │  ├─ Audio-Templates/
   │  └─ Code-Templates/
   ├─ Libraries/        [100GB]
   │  ├─ Sample-Footage/
   │  ├─ Stock-Audio/
   │  ├─ UI-Frameworks/
   │  └─ Code-Libraries/
   ├─ Documentation/    [50GB]
   ├─ Backups/          [100GB]
   │  ├─ Daily-Snapshots/
   │  ├─ Weekly-Archives/
   │  └─ Monthly-Checkpoints/
   └─ Tools/            [70GB]
      ├─ Build-Scripts/
      ├─ Deployment-Tools/
      ├─ Analysis-Tools/
      └─ Utilities/
```

**Total: 1.82TB in ONE partition (UserData:\)**

---

### Access Control via NTFS ACLs

**Gamer User:**
```
UserData:\Games/     → RW (full access)
UserData:\Studio/    → DENY (no access)
UserData:\Developer/ → DENY (no access)
UserData:\Worker/    → DENY (no access)
UserData:\Shared/    → R  (read-only)
```

**Studio User:**
```
UserData:\Games/     → DENY (no access)
UserData:\Studio/    → RW (full access)
UserData:\Developer/ → DENY (no access)
UserData:\Worker/    → DENY (no access)
UserData:\Shared/    → R  (read-only)
```

**Developer User:**
```
UserData:\Games/     → DENY (no access)
UserData:\Studio/    → DENY (no access)
UserData:\Developer/ → RW (full access)
UserData:\Worker/    → DENY (no access)
UserData:\Shared/    → R  (read-only)
```

**Worker User:**
```
UserData:\Games/     → DENY (no access)
UserData:\Studio/    → DENY (no access)
UserData:\Developer/ → DENY (no access)
UserData:\Worker/    → RW (full access)
UserData:\Shared/    → R  (read-only)
```

**Admin:**
```
UserData:\*          → RW (all access)
```

---

### NTFS ACL Configuration Script

```powershell
# Set NTFS permissions on user folders

# Remove inherited permissions
icacls "UserData:\Games" /inheritance:r
icacls "UserData:\Studio" /inheritance:r
icacls "UserData:\Developer" /inheritance:r
icacls "UserData:\Worker" /inheritance:r
icacls "UserData:\Shared" /inheritance:r

# Grant user-specific RW permissions
icacls "UserData:\Games" /grant "Gamer:(OI)(CI)F"
icacls "UserData:\Studio" /grant "Studio:(OI)(CI)F"
icacls "UserData:\Developer" /grant "Developer:(OI)(CI)F"
icacls "UserData:\Worker" /grant "Worker:(OI)(CI)F"

# Grant read-only on Shared
icacls "UserData:\Shared" /grant "Gamer:(OI)(CI)R"
icacls "UserData:\Shared" /grant "Studio:(OI)(CI)R"
icacls "UserData:\Shared" /grant "Developer:(OI)(CI)R"
icacls "UserData:\Shared" /grant "Worker:(OI)(CI)R"

# Grant admin full access
icacls "UserData:\Games" /grant "Administrators:(OI)(CI)F"
icacls "UserData:\Studio" /grant "Administrators:(OI)(CI)F"
icacls "UserData:\Developer" /grant "Administrators:(OI)(CI)F"
icacls "UserData:\Worker" /grant "Administrators:(OI)(CI)F"
icacls "UserData:\Shared" /grant "Administrators:(OI)(CI)F"
```

---

### Benefits of Single Partition Approach

```
✅ ONE logical volume (easy backup/restore)
✅ LOWER overhead (one filesystem, not multiple)
✅ BETTER space utilization (no wasted partition boundaries)
✅ SIMPLER management (quota on one partition)
✅ IDENTICAL performance (same disk, same speed)
✅ SECURITY via ACLs (not physical separation)
✅ USER isolation without partition overhead
✅ FLEXIBLE growth (add/remove user folders)
```

---

### Quota Enforcement (Single Partition)

```powershell
# Set quota on entire UserData partition
fsutil quota modify UserData /limit:1820000000000  # 1.82TB hard limit

# Soft warning at 90%
fsutil quota modify UserData /threshold:1638000000000

# Per-user soft limits (monitored, not enforced by filesystem)
# Gamer: 500GB
# Studio: 400GB
# Developer: 350GB
# Worker: 200GB
# Shared: 350GB

# Use event logs to alert when users exceed soft limits
```

---

## Access Control Matrix

| User | Core | Common | Cross | UserData:\Games | UserData:\Studio | UserData:\Developer | UserData:\Worker | UserData:\Shared | Vault | Quarantine |
|------|------|--------|-------|---|---|---|---|---|---|---|
| **Gamer** | R | R | R (GPU) | RW | DENY | DENY | DENY | R | - | Admin |
| **Studio** | R | R | R (Audio/GPU) | DENY | RW | DENY | DENY | R | - | Admin |
| **Developer** | R | R | R (GPU/CUDA) | DENY | DENY | RW | DENY | R | R | Admin |
| **Worker** | R | R | - | DENY | DENY | DENY | RW | R | - | Admin |
| **Admin** | RW | RW | RW | RW | RW | RW | RW | RW | RW | RW |

**Legend:**
- R = Read-only
- RW = Read-write
- DENY = No access (NTFS ACL blocks)
- Admin = Admin-only access

**Security Model:**
- ✅ Users can only access their own UserData:\ folder
- ✅ All users can read Shared/ and Core/Common/Cross
- ✅ Admin can access everything
- ✅ NTFS ACLs prevent cross-user escape
- ✅ No symlinks between user folders allowed

---

## Size Allocation Summary

```
DISK 0 (900GB SSD):
├─ C: Core OS           150GB
├─ E: Common Software   200GB
├─ X: Cross Software    150GB
├─ G: Dev Drive ReFS    250GB
├─ K: Quarantine        100GB
└─ V: Vault             100GB
Total: 950GB (50GB buffer)

DISK 1 (1.87TB HDD) - ONE UserData Partition:
├─ Games/      500GB  ← Gamer user (NTFS ACL locked)
├─ Studio/     400GB  ← Studio user (NTFS ACL locked)
├─ Developer/  350GB  ← Developer user (NTFS ACL locked)
├─ Worker/     200GB  ← Worker user (NTFS ACL locked)
└─ Shared/     350GB  ← ALL users read-only

Total in UserData:\: 1.82TB (50GB buffer for growth)
Recovery: 20GB (separate partition)
```

**Key Advantages:**
- ✅ ONE filesystem = simple backup/restore
- ✅ Better space utilization (no wasted partition space)
- ✅ Security via NTFS ACLs (not physical partitions)
- ✅ Easier management (one quota, one defrag schedule)
- ✅ Flexible growth (add/remove folders within partition)

---

## TRIM & Fragmentation Prevention

### SSD (Disk 0) - Weekly TRIM

```powershell
# Enable TRIM
fsutil behavior set DisableDeleteNotify 0

# Weekly optimization (Sunday 2 AM)
Optimize-Volume -DriveLetter C -Defrag -Verbose
Optimize-Volume -DriveLetter E -Defrag -Verbose
Optimize-Volume -DriveLetter X -Defrag -Verbose
Optimize-Volume -DriveLetter K -Defrag -Verbose
Optimize-Volume -DriveLetter V -Defrag -Verbose
```

### Dev Drive (G:\) - Zero Fragmentation

```csharp
// ReFS handles fragmentation automatically
// No manual TRIM needed - guaranteed 0% external fragmentation
```

### HDD (Disk 1) - Monthly Defrag

```powershell
# Monthly full defragmentation
Optimize-Volume -DriveLetter Games -Defrag -Verbose
Optimize-Volume -DriveLetter Studio -Defrag -Verbose
Optimize-Volume -DriveLetter Developer -Defrag -Verbose
Optimize-Volume -DriveLetter Worker -Defrag -Verbose
Optimize-Volume -DriveLetter Production -Defrag -Verbose
```

---

## Maintenance Schedule

| Frequency | Task | Partitions |
|-----------|------|-----------|
| Weekly | TRIM pass | C, E, X, K, V |
| Weekly | Cleanup temp | C:\Temp, X:\Temp |
| Monthly | Full defrag | Games, Studio, Developer, Worker, Production |
| Monthly | Archive old | K: (quarantine) |
| Monthly | Verify Recovery | Recovery partition |
| Quarterly | Driver cleanup | X:\Drivers (keep 2 versions) |
| Quarterly | Backup verify | All external backups |
| Yearly | Full backup | All user partitions |

---

## Quota Enforcement

```powershell
# NTFS Quotas per user partition
fsutil quota modify Games /limit:500000000000
fsutil quota modify Studio /limit:400000000000
fsutil quota modify Developer /limit:350000000000
fsutil quota modify Worker /limit:200000000000

# Alerts when reaching 90% quota
fsutil quota modify Games /threshold:450000000000
```

---

## Performance Targets

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| OS Boot | 30s | 12s | -60% |
| App Launch | 5s | 1.5s | -70% |
| File Access | 150ms | 50ms | -67% |
| Defrag Status | 40-60% | <5% | -93% |
| Build Time | 45s | 30s | -33% |

---

## Integration with Phase 10 + GPU

**Developer Workflow:**
```
Code → G:\ Dev Drive (ReFS, zero fragmentation)
  ↓
Compile → Developer:\ Build/
  ↓
GPU Compute ← X:\GPU (CUDA 12.4)
  ↓
Results → Production:\ (shared)
```

**Studio Workflow:**
```
Reaper Projects → Studio:\ (250GB workspace)
  ↓
Plugins ← X:\Audio (VST3, ASIO)
  ↓
Rendering ← NVIDIA 5090 (X:\GPU\NVIDIA)
  ↓
Masters → Studio:\ Masters/
```

**Gamer Workflow:**
```
Steam Library ← Games:\ (500GB)
  ↓
GameWorks ← X:\GPU\NVIDIA\GameWorks
  ↓
Play → NVIDIA 5090 (100%)
```

---

## Security Isolation

**Cross-User Access Prevention:**
```
Games:\     → Gamer only
Studio:\    → Studio only
Developer:\ → Developer only
Worker:\    → Worker only
Production:\→ Everyone (read-only)

Core:\      → Everyone
Common:\    → Everyone
X:\         → Everyone (function-specific)
V:\         → Admin + authorized (encrypted)
K:\         → Admin (quarantine)
```

**No symlinks allowed between user partitions** (NTFS ACLs prevent escape)

---

## Installation Order

### Phase 1: Core + Common (SSD - 2h)
1. Windows 11 Pro install (C:\)
2. Common software (E:\) - Office, VS, Email, browsers
3. Cross software (X:\) - Drivers, CUDA, audio

### Phase 2: Dev Environment (2h)
1. Dev Drive VHDX setup (G:\)
2. Git repositories
3. Build tools

### Phase 3: Security (30m)
1. Quarantine partition (K:\)
2. Enable BitLocker on Vault (V:\)
3. NTFS ACL configuration

### Phase 4: UserData Partition (1.5h)
1. Format UserData partition (1.82TB)
2. Create folders: Games/, Studio/, Developer/, Worker/, Shared/
3. Set NTFS ACLs (user-specific locking)
4. Set quota limits

### Phase 5: Backup Infrastructure (1h)
1. External SSD setup (daily backups)
2. External HDD setup (weekly/archive)
3. Backup script deployment
4. Recovery testing

---

## Conclusion

This tiered architecture with consolidated UserData partition optimizes for:

✅ **Security** (NTFS ACL user isolation, no cross-access, encrypted vault)  
✅ **Efficiency** (shared Core/Common/Cross, no duplication, one partition)  
✅ **Performance** (SSD for OS/dev, HDD for storage, zero fragmentation)  
✅ **Manageability** (clear folder structure, centralized settings, one backup point)  
✅ **Scalability** (easy to add users, expand folders, adjust quotas)  
✅ **Production-ready** (integrated with Phase 10 + GPU optimization)  

**Storage Model:**
- Core + Common + Cross are shared infrastructure
- UserData\ is one large partition with user-locked subfolders
- Vault + Quarantine are accessible by all (security zones)
- Dev Drive is zero-fragmentation development environment

**Status:** ✅ Ready for implementation
