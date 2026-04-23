# Storage Optimization Strategy: Multi-Disk Partition Architecture

**Hardware:** Disk 0 (900GB) + Disk 1 (1.87TB)  
**Goals:** Zero fragmentation, TRIM optimization, efficient software placement, easy management  
**Strategy:** Logical partitioning by use case + shared common layer  

---

## Executive Summary

This document defines an optimized multi-disk partition strategy that:

- ✅ Eliminates fragmentation (TRIM-optimized)
- ✅ Separates concerns (OS, software, data, dev, vault)
- ✅ Maximizes shared library usage (CUDA, spatial audio, common drivers)
- ✅ Provides easy user configuration (settings centralization)
- ✅ Supports production workflows (Reaper, gaming, development)
- ✅ Maintains security (quarantine, sandbox, vault)

---

## Disk Layout Overview

```
DISK 0 (900GB - SSD NVMe):
├─ C:\        [200GB]  Windows OS + Core Services
├─ E:\        [150GB]  Software (CUDA, spatial audio, common drivers)
├─ G:\        [250GB]  Dev Drive (VHDX virtual filesystem - no fragmentation)
├─ K:\        [150GB]  Quarantine (immutable, isolated)
└─ Z:\        [150GB]  Sandbox (ephemeral, auto-cleanup)

DISK 1 (1.87TB - HDD/SATA):
├─ D:\        [700GB]  Data (documents, documents, projects)
├─ S:\        [400GB]  Steam/Games
├─ R:\        [400GB]  Reaper + Audio Plugins
├─ P:\        [ 50GB]  Personal (archives, private)
├─ F:\        [200GB]  OneDrive (synced, contained)
└─ Recovery   [ 20GB]  Recovery partition (hidden)
```

---

## Detailed Partition Specifications

### DISK 0: SSD (Fast, Transient, Core Operations)

#### C: Windows OS Partition (200GB)

**Purpose:** Windows OS, system services, core drivers

**Contents:**
```
C:\Windows\
├─ System32 (core system files)
├─ SysWOW64 (32-bit compatibility)
├─ Drivers (hardware drivers)
└─ System32\drivers\etc (network configs)

C:\Program Files (x86)\ (legacy 32-bit apps - minimal)
C:\Program Files\ (reserved for OS use - 5GB max)
C:\ProgramData\ (shared application data - 10GB max)
C:\Users\<profile>\AppData (roaming - 20GB, local - 10GB)
```

**Optimization:**
- ✅ Minimal bloat (OS + essentials only)
- ✅ No user documents/downloads
- ✅ Roaming AppData: 20GB (synced across devices)
- ✅ Local AppData: 10GB (machine-specific)
- ✅ TRIM-enabled: Weekly optimization

**VRAM Allocation:** 32GB (pinned for GPU transfers)

---

#### E: Shared Software Layer (150GB)

**Purpose:** Common software, libraries, drivers used by multiple applications

**Structure:**
```
E:\Software\
├─ CUDA\
│  ├─ 12.4\
│  │  ├─ bin\           [CUDA runtime executables]
│  │  ├─ lib\           [CUDA libraries]
│  │  └─ include\       [CUDA headers]
│  ├─ cuDNN\            [Deep learning libraries]
│  ├─ cuBLAS\           [Linear algebra]
│  └─ cuFFT\            [FFT operations]
│
├─ AudioLibraries\
│  ├─ SpatialAudio\     [Windows Sonic, Dolby Atmos]
│  ├─ ASIO\             [Audio streaming I/O]
│  ├─ VST3\             [Plugin format - shared]
│  └─ ReWire\           [Inter-app audio routing]
│
├─ Drivers\
│  ├─ NVIDIA\
│  │  ├─ Driver 560.70
│  │  ├─ GameWorks
│  │  └─ FXAA, DLSS
│  ├─ AMD\
│  │  ├─ ROCm 5.7
│  │  └─ RDNA Driver
│  ├─ Intel\
│  │  ├─ Arc Driver
│  │  └─ oneAPI 2024.1
│  └─ Audio\
│      ├─ Realtek HD
│      └─ USB audio class
│
├─ DeveloperTools\
│  ├─ Git\              [Version control]
│  ├─ CMake\            [Build system]
│  ├─ .NET\             [8.0 runtime]
│  └─ Python\           [3.12 runtime]
│
├─ CommonLibraries\
│  ├─ DirectX 12\       [Game/graphics framework]
│  ├─ OpenGL\           [Cross-platform graphics]
│  ├─ Vulkan\           [High-performance graphics]
│  └─ OpenAL\           [3D audio]
│
└─ Settings\
   ├─ nvidia-settings.ini
   ├─ audio-config.json
   ├─ driver-versions.txt
   └─ user-overrides.json
```

**Size Breakdown (150GB total):**
- CUDA + cuDNN + cuBLAS + cuFFT: 45GB
- Audio libraries + VST: 20GB
- Drivers (NVIDIA + AMD + Intel): 35GB
- Developer tools: 25GB
- Common libraries: 15GB
- Settings/config: 10GB

**Key Features:**
- ✅ Single installation of each library (no duplication)
- ✅ Environment variables point to E:\Software\
- ✅ Symbolic links from Program Files to E:\ (for compatibility)
- ✅ Version management (multiple CUDA versions side-by-side)
- ✅ Easy for users to enable/disable driver features
- ✅ Quarterly updates (new driver versions archived)

**TRIM Strategy:**
- Weekly TRIM on unused files
- Monthly full optimization
- Quarterly archive old drivers

---

#### G: Dev Drive (250GB) - VHDX Virtual Filesystem

**Purpose:** Development projects with guaranteed zero fragmentation

**Technology:** ReFS filesystem inside VHDX container (Microsoft Dev Drive)

**Features:**
```
✅ Zero external fragmentation (guaranteed by design)
✅ Automatic cache management
✅ Snapshot capability
✅ Copy-on-write cloning
✅ Integration with VS Code, Visual Studio
```

**Structure:**
```
G:\
├─ Projects\
│  ├─ MonadoBlade\       [Main development]
│  ├─ HeliosPlatform\    [Supporting platform]
│  ├─ Experiments\       [Prototype/testing]
│  └─ Archive\           [Prior versions]
│
├─ Build\
│  ├─ Debug\             [Debug binaries]
│  ├─ Release\           [Release binaries]
│  └─ Artifacts\         [Built packages]
│
├─ Nuget\
│  └─ [Local package cache]
│
└─ Git\
   └─ [Git repositories]
```

**Size:** 250GB allows:
- MonadoBlade repo (50GB)
- HeliosPlatform repo (100GB)
- Build artifacts (50GB)
- Experiments/testing (30GB)
- Reserve (20GB)

**TRIM:** Not applicable (VHDX handles fragmentation internally)

---

#### K: Quarantine Partition (150GB) - Immutable Security Zone

**Purpose:** Isolated storage for infected/suspicious files

**Properties:**
```
✅ Read-only after initial placement
✅ Separate from main OS
✅ Automatic indexing disabled
✅ No execution allowed
✅ Separate backup chain
```

**Usage Pattern:**
1. Malware scanner detects threat
2. File auto-moved to K:\
3. Cannot be executed (NTFS ACLs)
4. Encrypted (BitLocker optional)
5. Archived to external storage monthly

**Recovery:** If false positive, can recover from backup

---

#### Z: Sandbox Partition (150GB) - Ephemeral Testing

**Purpose:** Temporary storage for risky operations, auto-cleanup

**Configuration:**
```
✅ Auto-mounted at boot
✅ 30-day auto-cleanup (files older than 30 days deleted)
✅ No backup (explicitly temporary)
✅ Fast I/O (SSD backed)
✅ Separate ACLs (isolated from C:\)
```

**Usage:**
- Download directory for untrusted files
- Malware scanner analysis area
- GPU model inference temporary outputs
- Build cache (can be recreated)

**Auto-cleanup script:**
```powershell
# Run weekly
Get-ChildItem Z:\ -Recurse -File | 
  Where-Object {$_.LastAccessTime -lt (Get-Date).AddDays(-30)} | 
  Remove-Item -Force
```

---

### DISK 1: HDD/SATA (Capacity, Long-term Storage)

#### D: Data Partition (700GB)

**Purpose:** User documents, projects, archives

**Structure:**
```
D:\
├─ Documents\
│  ├─ Active Projects\   [Current work]
│  ├─ Archives\          [Completed projects]
│  └─ Reference\         [Research, references]
│
├─ OneDrive (SYMLINK to F:\)
│  └─ [Cloud sync folder]
│
└─ Backups\
   ├─ Weekly\            [Latest week]
   ├─ Monthly\           [Monthly checkpoints]
   └─ Yearly/            [Yearly archives]
```

**Size:** 700GB allows:
- Active projects: 300GB
- Archives: 250GB
- Backups: 150GB

**TRIM Strategy:**
- Weekly maintenance pass
- Automatic defrag during idle (Windows 10+ built-in)
- Manual TRIM on OS's schedule

---

#### S: Steam & Games (400GB)

**Purpose:** Gaming library and game data

**Structure:**
```
S:\
├─ SteamLibrary\
│  ├─ common\           [Game executables]
│  ├─ compatdata\       [Proton/compatibility data]
│  └─ screenshots\      [Game captures]
│
├─ GameData\            [Saves, mods, configs]
│  ├─ Save Games\
│  ├─ Mods\
│  └─ Config Files\
│
└─ Installers\          [Backup installers]
```

**Optimization:**
- ✅ Separate from OS (faster downloads, no OS fragmentation)
- ✅ TRIM-friendly (Windows TRIM on HDD)
- ✅ Easy backup/restore
- ✅ Regular defrag (Windows Optimize-Volume)

---

#### R: Reaper + Audio Production (400GB)

**Purpose:** Professional audio workstation content

**Structure:**
```
R:\
├─ Reaper\
│  ├─ Projects\         [REP files, 100GB]
│  ├─ Backups\          [Auto-backups]
│  ├─ Rendering\        [Exported audio files]
│  └─ Temp/             [Recording buffer, auto-cleanup]
│
├─ Plugins\
│  ├─ VST2/             [Legacy plugins]
│  ├─ VST3/             [Modern plugins, 50GB]
│  ├─ CLAP/             [New plugin format]
│  └─ Presets/          [Plugin presets, 10GB]
│
├─ Samples\
│  ├─ Drums\            [Drum samples, 50GB]
│  ├─ Strings/          [String samples, 40GB]
│  ├─ Synths/           [Synth samples, 30GB]
│  └─ FX/               [Effects/ambience, 20GB]
│
├─ Libraries/
│  └─ [Third-party libraries]
│
└─ AudioData/
   ├─ Masters/          [Master recordings]
   └─ Archives/         [Old projects]
```

**Size:** 400GB breakdown:
- Reaper projects: 100GB
- VST plugins: 50GB
- Samples: 140GB
- Libraries: 60GB
- Archives: 50GB

**TRIM/Optimization:**
- Weekly TRIM on plugin cache
- Automatic sample index rebuild (Reaper)
- Monthly project backup
- Quarterly archive old versions

---

#### P: Personal Archive (50GB)

**Purpose:** Private documents, archives, sensitive data

**Structure:**
```
P:\
├─ Personal\            [Private documents]
├─ Financial/           [Tax returns, statements]
├─ Medical/             [Medical records]
├─ Legal/               [Contracts, agreements]
└─ Archive/             [Old projects]
```

**Security:**
- ✅ Optional encryption (BitLocker)
- ✅ Separate backup (external drive)
- ✅ TRIM-enabled
- ✅ No cloud sync

---

#### F: OneDrive Integration (200GB) - Contained Sync

**Purpose:** Cloud synchronization (Microsoft OneDrive)

**Key Constraint:** Physically isolated to prevent cross-partition escape

**Configuration:**
```
F:\OneDrive\
├─ Documents\           [Synced documents]
├─ Pictures/            [Synced pictures]
├─ Desktop/             [Synced desktop]
└─ Cloud Only/          [OneDrive-only files]
```

**NTFS Settings:**
- ✅ Separate filesystem permissions (cannot access D:\ or other partitions)
- ✅ Symlink blocking enabled
- ✅ TRIM-enabled
- ✅ Encryption optional (OneDrive handles)

**Sync Strategy:**
- ✅ Selective sync (don't sync everything)
- ✅ 200GB limit (won't exceed partition size)
- ✅ Automatic TRIM on old versions
- ✅ Weekly cleanup of sync conflicts

---

#### Recovery Partition (20GB) - Disk 1

**Purpose:** Windows recovery and disaster recovery

**Contents:**
```
├─ Windows Recovery Image (10GB)
├─ Boot files
├─ Repair tools
└─ Bitlocker recovery keys
```

**Features:**
- ✅ Hidden from Windows Explorer
- ✅ Not TRIM-optimized (static content)
- ✅ Monthly verification
- ✅ External backup copy

---

## Optimization Strategy

### TRIM Configuration

**For SSD (Disk 0):**
```powershell
# Enable TRIM
fsutil behavior set DisableDeleteNotify 0

# Weekly optimization
Optimize-Volume -DriveLetter C -Defrag -Verbose
Optimize-Volume -DriveLetter E -Defrag -Verbose
Optimize-Volume -DriveLetter K -Defrag -Verbose
Optimize-Volume -DriveLetter Z -Defrag -Verbose
```

**For HDD (Disk 1):**
```powershell
# TRIM is less effective on HDD, but still enable
fsutil behavior set DisableDeleteNotify 0

# Monthly defrag
Optimize-Volume -DriveLetter D -Defrag -Verbose
Optimize-Volume -DriveLetter S -Defrag -Verbose
Optimize-Volume -DriveLetter R -Defrag -Verbose
Optimize-Volume -DriveLetter P -Defrag -Verbose
Optimize-Volume -DriveLetter F -Defrag -Verbose
```

### Fragmentation Prevention

**Strategy 1: VHDX Dev Drive (Zero Fragmentation)**
```csharp
// Create VHDX container for development
// ReFS filesystem automatically handles fragmentation
fsutil file createnew G:\devdrive 250000000000  // 250GB
// Mount with ReFS

// Benefits:
// - Guaranteed 0% fragmentation
// - Snapshot capability
// - Copy-on-write cloning
// - Integrated with VS Code
```

**Strategy 2: Partition Separation**
- Separate partitions = separate allocation tables
- Prevents cross-partition fragmentation
- Easy per-partition optimization

**Strategy 3: Automatic Maintenance**
```powershell
# Windows Task Scheduler - Weekly
New-ScheduledTask -TaskName "StorageOptimization" `
  -Trigger (New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 2am) `
  -Action (New-ScheduledTaskAction -Execute "PowerShell.exe" `
    -Argument "-NoProfile -File C:\Scripts\Optimize-Storage.ps1")
```

### Settings Centralization

**E:\Software\Settings\ - User Configuration**

```json
// E:\Software\Settings\driver-versions.json
{
  "nvidia": {
    "version": "560.70",
    "cudaVersion": "12.4",
    "driverPath": "E:\\Software\\Drivers\\NVIDIA\\560.70"
  },
  "amd": {
    "version": "24.3.1",
    "rocmVersion": "5.7",
    "driverPath": "E:\\Software\\Drivers\\AMD\\24.3.1"
  },
  "intel": {
    "arcVersion": "1.3.25",
    "oneapiVersion": "2024.1",
    "driverPath": "E:\\Software\\Drivers\\Intel\\1.3.25"
  }
}

// E:\Software\Settings\audio-config.json
{
  "spatialAudio": "enabled",
  "asioDriver": "RME Fireface",
  "vst3Path": "E:\\Software\\AudioLibraries\\VST3",
  "pluginScan": "automatic",
  "bufferSize": 256
}

// E:\Software\Settings\user-overrides.json
{
  "maxGpuMemory": 24000,  // MB
  "cpuThreads": 16,
  "audioLatency": 10,     // ms
  "autoUpdateDrivers": false,
  "backupSchedule": "weekly"
}
```

### Disk Layout Visualization

```
┌─ DISK 0 (900GB SSD) ────────────────────────────┐
│                                                  │
│  C:\ [OS]                200GB   [█████░░░░░░░] │
│  E:\ [Shared Software]   150GB   [███░░░░░░░░░] │
│  G:\ [Dev Drive VHDX]    250GB   [██████░░░░░░] │
│  K:\ [Quarantine]        150GB   [███░░░░░░░░░] │
│  Z:\ [Sandbox]           150GB   [███░░░░░░░░░] │
│                                                  │
│  Total: 900GB (100%)                            │
└──────────────────────────────────────────────────┘

┌─ DISK 1 (1.87TB HDD) ───────────────────────────┐
│                                                  │
│  D:\ [Data]              700GB   [███░░░░░░░░░] │
│  S:\ [Steam/Games]       400GB   [██░░░░░░░░░░] │
│  R:\ [Reaper/Audio]      400GB   [██░░░░░░░░░░] │
│  P:\ [Personal]           50GB   [░░░░░░░░░░░░] │
│  F:\ [OneDrive]          200GB   [█░░░░░░░░░░░] │
│  Rec [Recovery]           20GB   [░░░░░░░░░░░░] │
│                                                  │
│  Total: 1.77TB (95%)                            │
│  Reserved: 100GB (5%)                           │
└──────────────────────────────────────────────────┘
```

---

## Installation Order

### Phase 1: OS & Core (C: partition)
1. Windows 11 Pro (clean install)
2. Essential drivers (chipset, network)
3. System updates

### Phase 2: Shared Software (E: partition)
1. CUDA 12.4 + cuDNN (GPU drivers)
2. AMD/Intel driver fallbacks
3. Audio libraries (Spatial Audio, ASIO)
4. Developer tools (.NET, Git, CMake)

### Phase 3: Dev Environment (G: partition)
1. Create VHDX container
2. Visual Studio 2022
3. Git repositories (MonadoBlade, HeliosPlatform)
4. Build artifacts

### Phase 4: Production Storage (Disk 1)
1. Format D: (Data)
2. Steam installation (S:)
3. Reaper installation (R:)
4. OneDrive setup (F:)
5. Personal archive (P:)

### Phase 5: Security & Recovery
1. Create Recovery partition
2. BitLocker encryption (optional)
3. Shadow Copies setup (D:, S:, R:)

---

## Maintenance Schedule

### Weekly
- TRIM all SSD partitions (C:, E:, G:, K:, Z:)
- Verify Recovery partition
- Backup critical data

### Monthly
- Full defragmentation (HDD partitions D:, S:, R:, F:)
- Archive old files (Z: sandbox cleanup)
- Driver update check

### Quarterly
- OS updates and patches
- Driver version maintenance (keep 2 previous versions)
- Storage audit (fragmentation check)

### Yearly
- Full backup to external storage
- Disaster recovery test
- Partition resizing assessment

---

## Performance Targets

### After Optimization

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| OS Boot Time | 30s | 12s | -60% |
| App Launch | 5s avg | 1.5s avg | -70% |
| File Access | 150ms | 50ms | -67% |
| Defrag Status | 40-60% | <5% | -93% |
| Fragmentation | Baseline | Minimal | -95% |
| GPU Transfer | 500MB/s | 1.2GB/s | +140% |
| Build Time | 45s | 30s | -33% |

---

## Integration with Phase 10 + GPU

**Development Workflow:**
```
Project Code ← G:\ (Dev Drive, zero fragmentation)
    ↓
Build Artifacts → G:\ (2-5 minute builds max)
    ↓
GPU Compute ← E:\ (CUDA libraries pinned in memory)
    ↓
Optimization Tasks
    ↓
Results → D:\ (Data partition, persistent)
```

**Audio Production:**
```
Projects ← R:\ (Reaper projects, 100GB capacity)
    ↓
Plugins → E:\ (Shared VST3 libraries)
    ↓
Samples ← R:\ (140GB sample libraries)
    ↓
Exports → R:\ (Audio files)
    ↓
Archive → P:\ (Personal, backed up)
```

**Gaming:**
```
Steam Library ← S:\ (400GB games)
    ↓
Game Saves → S:\ (Steam Cloud sync)
    ↓
Mods → S:\ (Mod storage)
```

---

## Security Considerations

### Data Protection
- ✅ K: (Quarantine) - Isolated, immutable
- ✅ P: (Personal) - BitLocker optional
- ✅ F: (OneDrive) - Cloud encryption
- ✅ Recovery - Encrypted bitlocker recovery

### Access Control
- ✅ C: - System only
- ✅ E: - Read-most (updates via admin)
- ✅ G: - Developer only
- ✅ K: - Admin only
- ✅ Z: - Temporary files (auto-cleanup)

### Backup Strategy
- ✅ D: (Data) - Weekly backup
- ✅ R: (Reaper) - Daily backup (important)
- ✅ P: (Personal) - Monthly backup
- ✅ Recovery - Yearly test

---

## Conclusion

This partition strategy optimizes Monado Blade for:
- **Zero fragmentation** (VHDX Dev Drive, TRIM optimization)
- **Easy management** (centralized settings, user overrides)
- **Performance** (SSD for OS/dev, HDD for storage)
- **Security** (quarantine, sandbox, encryption)
- **Production workflows** (Reaper, gaming, development, GPU)
- **Scalability** (easy to add new partitions or expand existing)

**Status:** ✅ Ready for implementation with Phase 10 + GPU optimization
