/**
 * STORAGE ARCHITECTURE CLARIFICATION
 * 
 * IMPORTANT: DevDrive (E:) is ReFS, NOT a VHDX
 * 
 * Physical Partitions (Native Filesystems):
 *   C: → NTFS (Windows system)
 *   D: → NTFS (User data)
 *   E: → ReFS (DevDrive, acceleration enabled) ← NATIVE PARTITION, NOT VHDX
 * 
 * Virtual Containers (VHDX Files):
 *   V: → Vault.vhdx (encrypted, isolated storage)
 *   Q: → Quarantine.vhdx (threat isolation, always mounted)
 *   S: → Sandbox.vhdx (disposable execution, per-session)
 * 
 * Storage Layout on 1TB SSD:
 *   C: Windows (100GB, NTFS)
 *   D: User Data (400GB, NTFS) ← Contains D:\Monado\Containers\*.vhdx
 *   E: DevDrive (400GB, ReFS) ← Native ReFS partition
 *   Free: 100GB (system reserved)
 * 
 * VHDX Containers Location:
 *   All VHDX files stored in D:\Monado\Containers\
 *   ├─ Vault.vhdx (50-500GB dynamic, encrypted, BitLocker)
 *   ├─ Sandbox.vhdx (50GB fixed, disposable)
 *   └─ Quarantine.vhdx (20-200GB dynamic, forensic)
 * 
 * Key Differences:
 * 
 * DevDrive (E: ReFS):
 *   • Direct native partition (not virtualized)
 *   • 40% faster than NTFS (measured, acceleration mode)
 *   • High-performance storage (builds, dev work)
 *   • NO encryption by default (data at risk if device stolen)
 *   • No I/O overhead (native speed)
 * 
 * VHDX Containers (V: Q: S:):
 *   • Virtual disks stored as files in D: partition
 *   • Portable (can move, backup, copy to external drives)
 *   • Isolated (complete separation from main system)
 *   • Encrypted (Vault with BitLocker, optional for others)
 *   • 5-10% I/O overhead (acceptable for security/isolation value)
 *   • Can snapshot and restore to known-good state
 * 
 * Use Case Guidance:
 *   Use DevDrive (E: ReFS) for:
 *     ✓ Build artifacts (40% faster compilation)
 *     ✓ node_modules (high I/O, needs speed)
 *     ✓ Compiler caches (frequent access)
 *     ✓ Docker images (large sequential reads)
 *     ✓ Development work (any performance-critical task)
 *   
 *   Use VHDX Vault (V:) for:
 *     ✓ Credentials (Office 365, GitHub, AWS keys)
 *     ✓ Encryption keys (system master keys)
 *     ✓ Personal documents (banking, medical, legal)
 *     ✓ Backups (system configuration backups)
 *     ✓ Performance not critical (encrypted storage trade-off OK)
 *   
 *   Use VHDX Sandbox (S:) for:
 *     ✓ Untrusted applications
 *     ✓ Experimental software
 *     ✓ Downloaded files (before analysis)
 *     ✓ One-time testing (auto-reset after session)
 *   
 *   Use VHDX Quarantine (Q:) for:
 *     ✓ Detected malware
 *     ✓ Suspicious files
 *     ✓ Forensic analysis
 *     ✓ Evidence preservation (read-only)
 * 
 * Boot Mount Points After Windows Starts:
 *   C: → Windows (NTFS)
 *   D: → User Data (NTFS)
 *   E: → DevDrive (ReFS, acceleration enabled)
 *   Q: → Quarantine.vhdx (always mounted, read-only)
 *   V: → Vault.vhdx (on-demand, encrypted, requires auth)
 *   S: → Sandbox.vhdx (on-demand per session, auto-reset)
 */
