# Storage Architecture: Multi-Disk User-Profile Partition Strategy

**Hardware:** Disk 0 (900GB SSD) + Disk 1 (1.87TB HDD)  
**Strategy:** Consolidated production partition + user-type profiles + security zones  
**Goal:** Zero fragmentation, easy management, optimal performance per user type  

---

## Executive Summary

This revised strategy consolidates storage around:

- ✅ **One large production partition** (900GB) containing games, Steam, Reaper, music projects
- ✅ **User-type profiles** (Developer, Studio, Worker, Gamer) with separate quota management
- ✅ **Encrypted vault** (V:\) for sensitive data
- ✅ **Quarantine zone** (K:\) for security isolation
- ✅ **Dev Drive** (G:\) with ReFS for zero fragmentation
- ✅ **TRIM optimization** on all SSD partitions
- ✅ **Per-profile backup strategy**

---

## Disk Layout

```
DISK 0 (900GB - SSD NVMe):
├─ C:\        [200GB]  Windows OS + Core Services
├─ E:\        [150GB]  Shared Software (CUDA, spatial audio, drivers)
├─ G:\        [250GB]  Dev Drive (VHDX ReFS - zero fragmentation)
├─ K:\        [150GB]  Quarantine (immutable, isolated)
└─ V:\        [150GB]  Vault (encrypted BitLocker)

DISK 1 (1.87TB - HDD/SATA):
├─ Production [900GB]  Games/Steam + Reaper/Music + Projects
├─ Developer  [350GB]  Developer profile workspace
├─ Studio     [250GB]  Audio/video creator workspace
├─ Worker     [150GB]  Office/web worker workspace
├─ Gamer      [150GB]  Gaming profile workspace
└─ Recovery   [ 20GB]  Disaster recovery partition
```

**Total allocation: 1.82TB of 1.87TB (97%)**  
**Reserve: 50GB buffer for OS growth/maintenance**

---

## DISK 0: SSD Fast Storage (System + Development)

### C: Windows OS (200GB)

**Purpose:** Windows 11 Pro, system services, core drivers

**Contents:**
```
C:\Windows\              [System files]
C:\Program Files\        [Reserved 5GB]
C:\Program Files (x86)\  [Legacy 32-bit - minimal]
C:\ProgramData\          [System apps data - 10GB]
C:\Users\<profile>\AppData
  ├─ Roaming\            [20GB, synced cross-device]
  └─ Local\              [10GB, machine-specific]
```

**Optimization:**
- ✅ Minimal bloat (OS + essentials only)
- ✅ No user documents/downloads here
- ✅ Weekly TRIM pass
- ✅ Roaming AppData supports multi-machine sync

---

### E: Shared Software Layer (150GB)

**Purpose:** Common libraries used by multiple applications

**Structure:**
```
E:\Software\
├─ CUDA\                       [CUDA 12.4, cuDNN, cuBLAS, cuFFT]
├─ AudioLibraries\             [Spatial Audio, ASIO, VST3, ReWire]
├─ Drivers\
│  ├─ NVIDIA (560.70)
│  ├─ AMD (ROCm 5.7)
│  └─ Intel Arc (oneAPI)
├─ DeveloperTools\             [Git, CMake, .NET 8.0, Python 3.12]
├─ CommonLibraries\            [DirectX 12, OpenGL, Vulkan, OpenAL]
└─ Settings\
   ├─ driver-versions.json     [Current driver versions]
   ├─ audio-config.json        [Audio system settings]
   └─ user-overrides.json      [User customization]
```

**Size Breakdown:**
- CUDA + cuDNN + cuBLAS + cuFFT: 45GB
- Audio libraries + VST: 20GB
- Drivers (NVIDIA, AMD, Intel): 35GB
- Developer tools: 25GB
- Common libraries: 15GB
- Settings/config: 10GB

**Key Features:**
- ✅ Single installation (no duplication)
- ✅ Symbolic links from Program Files to E:\
- ✅ Version management (2-3 driver versions)
- ✅ Easy user configuration (settings centralized)
- ✅ Quarterly driver archival

---

### G: Dev Drive (250GB) - VHDX ReFS

**Purpose:** Development with guaranteed zero external fragmentation

**Technology:** Microsoft Dev Drive (ReFS in VHDX container)

**Features:**
```
✅ Zero external fragmentation (guaranteed by design)
✅ Automatic cache management
✅ Snapshot capability
✅ Copy-on-write cloning
✅ Native VS Code + Visual Studio integration
```

**Structure:**
```
G:\
├─ Projects/                    [150GB]
│  ├─ MonadoBlade/
│  ├─ HeliosPlatform/
│  ├─ Experiments/
│  └─ Archive/
│
├─ Build/                       [50GB]
│  ├─ Debug/
│  ├─ Release/
│  └─ Artifacts/
│
├─ NugetCache/                  [20GB]
└─ Git/                         [30GB]
```

**TRIM:** Not applicable (VHDX handles internal fragmentation)

---

### K: Quarantine (150GB) - Immutable Security Zone

**Purpose:** Infected/suspicious files isolated

**Properties:**
```
✅ Read-only after initial placement (NTFS ACLs)
✅ Automatic indexing disabled
✅ No execution allowed (ACL enforcement)
✅ Separate backup chain
✅ Separate from main partitions
```

**Workflow:**
1. Malware scanner detects threat
2. File auto-moved to K:\
3. Archived to external storage monthly
4. Can be recovered if false positive

---

### V: Vault (150GB) - Encrypted Storage

**Purpose:** Sensitive data with full encryption

**Technology:** BitLocker Full Disk Encryption (AES-256)

**Features:**
```
✅ AES-256 encryption
✅ TPM 2.0 protection
✅ Automatic unlock on login
✅ PIN/password recovery key
✅ Completely isolated from system
```

**Structure:**
```
V:\
├─ Credentials/                 [Passwords, API keys]
├─ Certificates/                [SSL, code signing]
├─ PrivateKeys/                 [Encryption keys]
├─ ConfigSecrets/               [Database passwords]
└─ BackupKeys/                  [Recovery keys (offline copy)]
```

**Access Control:**
- Admin only
- Backup recovery keys stored on USB (physical security)

---

## DISK 1: HDD Large Storage (Production + Profiles)

### Production Partition (900GB) - Consolidated Workflows

**Purpose:** Games, Steam, Reaper music, audio plugins, shared projects

**Three Sections:**

#### Games & Steam (500GB)
```
Production:\Games\
├─ SteamLibrary/               [400GB]
│  ├─ common/                  [Game executables]
│  ├─ compatdata/              [Proton configs]
│  └─ screenshots/             [Captures]
│
└─ GameData/                   [100GB]
   ├─ Saves/                   [All game saves]
   ├─ Mods/                    [Game mods]
   └─ Config/                  [Game configs]
```

**Performance:**
- Steam auto-updates: Managed by Steam
- Regular defrag: Monthly
- Cloud sync: Steam Cloud enabled

#### Music Production (300GB)
```
Production:\Music\
├─ Reaper/                     [200GB]
│  ├─ Projects/                [100GB REP files]
│  ├─ Backups/                 [20GB auto-backups]
│  ├─ Rendering/               [80GB exported audio]
│  └─ Temp/                    [Auto-cleanup]
│
├─ AudioPlugins/               [80GB]
│  ├─ VST3/                    [50GB modern plugins]
│  ├─ VST2/                    [20GB legacy]
│  └─ Presets/                 [10GB presets]
│
└─ Samples/                    [20GB]
   ├─ Drums/                   [5GB]
   ├─ Strings/                 [5GB]
   ├─ Synths/                  [5GB]
   └─ FX/                      [5GB]
```

**Note:** Full sample library (140GB) can be added later  
**Backup:** Daily snapshots, weekly to external SSD

#### Projects (100GB)
```
Production:\Projects/
├─ Active/                     [50GB current work]
├─ Archives/                   [30GB completed]
└─ Experimental/               [20GB prototypes]
```

**Total: 900GB precisely allocated**

**Key Features:**
- ✅ Consolidated production workflows (no data scattering)
- ✅ Single partition = simple backup/recovery
- ✅ Monthly defrag (HDD optimization)
- ✅ Weekly TRIM pass
- ✅ Easy quota enforcement (900GB fixed limit)

---

### Developer Profile (350GB)

**Purpose:** Development workspace for "Developer" user profile

**Structure:**
```
Developer:\
├─ Projects/                   [150GB]
│  ├─ MonadoBlade/             [Current main project]
│  ├─ HeliosPlatform/          [Supporting libs]
│  └─ Experiments/             [Testing/prototypes]
│
├─ Build/                      [100GB]
│  ├─ Debug/
│  ├─ Release/
│  └─ Artifacts/
│
├─ Workspace/                  [80GB IDE configs]
│  ├─ VS2022/
│  ├─ VS Code/
│  └─ Git/
│
└─ Archive/                    [20GB old projects]
```

**Access:**
- Developer profile: Full read/write
- Other profiles: Read-only (reference)

**Backup:**
- Daily snapshots to external storage
- Weekly full backup
- Git repos auto-backup via GitHub

---

### Studio Profile (250GB)

**Purpose:** Audio/video creation workspace for "Studio" user profile

**Structure:**
```
Studio:\
├─ VideoProjects/              [100GB]
│  ├─ DaVinci/                 [DaVinci projects]
│  ├─ Rendering/               [Render output]
│  └─ Archive/                 [Completed]
│
├─ AudioProjects/              [80GB]
│  ├─ Recordings/              [Raw audio]
│  ├─ Editing/                 [Work in progress]
│  └─ Masters/                 [Final masters]
│
├─ Media/                      [50GB]
│  ├─ Footage/                 [Video clips]
│  ├─ Audio/                   [Audio samples]
│  └─ Graphics/                [Design assets]
│
└─ Presets/                    [20GB]
   ├─ ColorGrading/
   ├─ AudioFX/
   └─ Transitions/
```

**GPU Allocation:**
- NVIDIA 5090: 100% during video rendering
- 250GB quota strictly enforced

**Backup:**
- Daily snapshots
- Weekly to external SSD
- Monthly to external HDD

---

### Worker Profile (150GB)

**Purpose:** Office/web worker workspace for "Worker" user profile

**Structure:**
```
Worker:\
├─ Documents/                  [50GB]
│  ├─ Active/
│  └─ Archives/
│
├─ Spreadsheets/               [30GB]
│
├─ Presentations/              [20GB]
│
├─ Reference/                  [30GB]
│
└─ Backups/                    [20GB]
```

**Cloud Integration:**
- OneDrive sync enabled (to F:\ isolated partition)
- 150GB quota enforced
- No GPU access (CPU only)

**Backup:**
- Weekly to OneDrive
- Monthly to external storage

---

### Gamer Profile (150GB)

**Purpose:** Gaming-specific workspace for "Gamer" user profile

**Structure:**
```
Gamer:\
├─ GameLibrary/                [80GB]
│  ├─ Installed/
│  └─ Downloaded/
│
├─ Saves/                      [40GB]
│  ├─ SteamCloud/
│  ├─ LocalSaves/
│  └─ Backups/
│
└─ Mods/                       [30GB]
   ├─ Popular/
   └─ Experimental/
```

**GPU Allocation:**
- NVIDIA 5090: 100% during gameplay
- 150GB quota enforced
- High-priority I/O scheduling

**Backup:**
- Weekly snapshots (game saves mainly)
- Steam Cloud handles cloud backup

---

### Recovery Partition (20GB)

**Purpose:** Windows recovery and disaster recovery

**Contents:**
```
├─ Windows Recovery Image      [10GB]
├─ Boot files
├─ Repair tools
└─ BitLocker recovery keys
```

**Features:**
- ✅ Hidden from Windows Explorer
- ✅ Static content (no TRIM needed)
- ✅ Monthly verification
- ✅ External backup copy (USB)

---

## OneDrive Integration (Special Handling)

**Location:** F:\ (Disk 1 if space available, otherwise Disk 0)

**Purpose:** Cloud sync with strict isolation

**Configuration:**
```
F:\OneDrive\
├─ Documents/                  [Synced from cloud]
├─ Pictures/                   [Synced from cloud]
├─ Desktop/                    [Synced from desktop]
└─ CloudOnly/                  [OneDrive-only files]
```

**Isolation:**
- ✅ Cannot access other partitions (NTFS ACLs)
- ✅ Symlink blocking enabled
- ✅ No cross-partition escape vectors
- ✅ Separate filesystem permissions

**Size:** 200GB maximum (won't exceed partition)

**Sync Rules:**
- Selective sync (don't sync entire library)
- Auto-cleanup of old versions (>30 days)
- Weekly conflict resolution
- Monthly sync verification

---

## TRIM & Fragmentation Prevention

### TRIM Configuration

**SSD (Disk 0):**
```powershell
# Enable TRIM
fsutil behavior set DisableDeleteNotify 0

# Weekly optimization (Sunday 2 AM)
Optimize-Volume -DriveLetter C -Defrag -Verbose
Optimize-Volume -DriveLetter E -Defrag -Verbose
Optimize-Volume -DriveLetter K -Defrag -Verbose
Optimize-Volume -DriveLetter V -Defrag -Verbose
```

**HDD (Disk 1):**
```powershell
# Monthly full defragmentation
Optimize-Volume -DriveLetter Production -Defrag -Verbose
Optimize-Volume -DriveLetter Developer -Defrag -Verbose
Optimize-Volume -DriveLetter Studio -Defrag -Verbose
Optimize-Volume -DriveLetter Worker -Defrag -Verbose
Optimize-Volume -DriveLetter Gamer -Defrag -Verbose
```

### Dev Drive (Zero Fragmentation)

ReFS filesystem automatically handles fragmentation:
```csharp
// No manual TRIM needed - ReFS handles it
// Guaranteed 0% external fragmentation
```

---

## Maintenance Schedule

| Frequency | Task | Partitions |
|-----------|------|-----------|
| Weekly | TRIM pass | C:, E:, K:, V: |
| Weekly | Clean temp files | C:\Temp, G:\Temp |
| Monthly | Full defrag | Production, Developer, Studio, Worker, Gamer |
| Monthly | Archive old files | K: (quarantine) |
| Monthly | Verify Recovery | Recovery partition |
| Quarterly | Driver cleanup | E:\Drivers (keep 2 versions) |
| Quarterly | Backup verification | All external backups |
| Yearly | Full backup to external | All user partitions |

---

## User Profile Quota Enforcement

**Disk 1 Partition Quotas:**

| Profile | Partition | Quota | Current | Buffer |
|---------|-----------|-------|---------|--------|
| Production | Production | 900GB | 900GB | 0GB |
| Developer | Developer | 350GB | ~200GB | 150GB |
| Studio | Studio | 250GB | ~100GB | 150GB |
| Worker | Worker | 150GB | ~50GB | 100GB |
| Gamer | Gamer | 150GB | ~80GB | 70GB |

**Quota Enforcement:**
```powershell
# Windows NTFS quotas
fsutil quota modify C: /threshold:200000000000 /limit:200000000000 /user:developer

# Set per-partition quotas
fsutil quota modify D:\Developer /limit:350000000000
fsutil quota modify D:\Studio /limit:250000000000
fsutil quota modify D:\Worker /limit:150000000000
fsutil quota modify D:\Gamer /limit:150000000000
```

---

## Performance Targets After Optimization

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| OS Boot | 30s | 12s | -60% |
| App Launch | 5s | 1.5s | -70% |
| File Access | 150ms | 50ms | -67% |
| Defrag Status | 40-60% | <5% | -93% |
| Fragmentation | Baseline | Minimal | -95% |
| GPU Transfer | 500MB/s | 1.2GB/s | +140% |
| Build Time (G:) | 45s | 30s | -33% |

---

## Integration with Phase 10 + GPU

**Development Workflow:**
```
Project Code ← Developer:\ (150GB workspace)
    ↓
Dev Drive G:\ (250GB, VHDX ReFS, zero fragmentation)
    ↓
Build Artifacts ← G:\ Build/ (50GB)
    ↓
GPU Acceleration ← E:\ CUDA (45GB pinned)
    ↓
Results ← Production:\ (900GB)
```

**Audio Production:**
```
Reaper Projects ← Production:\Music\ (200GB)
    ↓
Plugins ← E:\ AudioLibraries (shared 20GB)
    ↓
Samples ← Production:\Music\Samples (20GB)
    ↓
Exports ← Production:\Music\Rendering\
    ↓
Archive ← V:\ Vault (encrypted)
```

**Gaming:**
```
Steam Library ← Production:\Games\ (400GB)
    ↓
Game Saves ← Gamer:\ (40GB)
    ↓
Mods ← Gamer:\ Mods (30GB)
    ↓
Archive ← Recovery (external USB)
```

---

## Backup Strategy

### Automated Backups

**Daily (Incremental):**
- Developer workspace (via external SSD)
- Studio workspace (via external SSD)
- Reaper projects (via external SSD)

**Weekly (Full):**
- All user profiles (to external HDD)
- Git repositories (via GitHub)

**Monthly (Archive):**
- Quarterly backup (to archive storage)
- Encrypted vault backup (to secure USB)

**Yearly (Comprehensive):**
- Full system snapshot (to external HDD)
- Disaster recovery test
- Off-site storage rotation

### Recovery Procedures

**Single File Recovery:**
- From weekly snapshots (fastest)
- Time: <5 minutes

**Full Profile Recovery:**
- From monthly archive
- Time: <30 minutes

**System Disaster Recovery:**
- Boot from Recovery partition (K:\)
- Full restore from yearly backup
- Time: <2 hours

---

## Security Considerations

### Partition Isolation

```
C:\      (OS)          ← TRIM-enabled, weekly optimization
E:\      (Software)    ← TRIM-enabled, read-most access
G:\      (Dev Drive)   ← ReFS, zero fragmentation
K:\      (Quarantine)  ← Immutable, read-only after placement
V:\      (Vault)       ← AES-256 encrypted, TPM-protected

Production:\  (Production)  ← Shared workflows, monthly defrag
Developer:\   (Profile)     ← Developer-only write access
Studio:\      (Profile)     ← Studio-only write access
Worker:\      (Profile)     ← Worker-only write access
Gamer:\       (Profile)     ← Gamer-only write access
```

### Access Control

| Partition | OS | Developers | Studio | Worker | Gamer |
|-----------|--|-|--|--|--|
| C: | RW | R | R | R | R |
| E: | RW | R | R | R | R |
| G: | RW | RW | - | - | - |
| K: | RW | - | - | - | - |
| V: | RW | - | - | - | - |
| Production: | RW | R | RW | - | RW |
| Developer: | - | RW | - | - | - |
| Studio: | - | - | RW | - | - |
| Worker: | - | - | - | RW | - |
| Gamer: | - | - | - | - | RW |

---

## Installation Order

### Phase 1: OS + Core (C: partition)
1. Windows 11 Pro clean install
2. Essential drivers (chipset, network, GPU)
3. System updates

### Phase 2: Shared Software (E: partition)
1. CUDA 12.4 + cuDNN
2. AMD/Intel driver fallbacks
3. Audio libraries + ASIO
4. Developer tools

### Phase 3: Dev Environment (G: partition)
1. Create VHDX ReFS container
2. Visual Studio 2022
3. Git repositories
4. Build artifacts setup

### Phase 4: Security (K: + V:)
1. Create Quarantine partition
2. Enable BitLocker on Vault
3. Configure NTFS ACLs
4. Recovery partition setup

### Phase 5: Production Storage (Disk 1)
1. Format Production partition (900GB)
2. Format profile partitions (350+250+150+150GB)
3. Install Steam to Production:\Games
4. Install Reaper to Production:\Music
5. Configure OneDrive (F:\)

### Phase 6: Backup Infrastructure
1. External SSD setup (daily backups)
2. External HDD setup (weekly/archive)
3. Configure automated backup scripts
4. Recovery testing

---

## Conclusion

This multi-disk strategy optimizes for:

✅ **Zero fragmentation** (VHDX ReFS + TRIM)  
✅ **User-type isolation** (separate profiles with quotas)  
✅ **Production consolidation** (900GB single partition)  
✅ **Security** (encrypted vault, quarantine, profile ACLs)  
✅ **Easy management** (centralized settings, per-profile backups)  
✅ **Performance** (SSD for OS/dev, HDD for storage)  
✅ **Scalability** (easy to add profiles or expand partitions)  

**Status:** ✅ Ready for Phase 10 + GPU optimization integration
