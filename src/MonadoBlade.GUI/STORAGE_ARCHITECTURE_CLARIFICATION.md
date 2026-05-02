/**
 * STORAGE ARCHITECTURE CLARIFICATION
 * 
 * IMPORTANT: DevDrive (E:) is a VHDX with ReFS filesystem
 * 
 * Physical Partitions (Native Filesystems):
 *   C: → NTFS (Windows system)
 *   D: → NTFS (User data)
 * 
 * Virtual Containers (VHDX Files - All stored on D: partition):
 *   E: → DevDrive.vhdx (400GB dynamic, ReFS filesystem) ← HIGH-PERFORMANCE DEV STORAGE
 *   V: → Vault.vhdx (50-500GB dynamic, NTFS filesystem, encrypted with BitLocker)
 *   Q: → Quarantine.vhdx (20-200GB dynamic, NTFS filesystem, forensic analysis)
 *   S: → Sandbox.vhdx (50GB fixed, NTFS filesystem, disposable per-session)
 * 
 * Storage Layout on 1TB SSD:
 *   C: Windows System (100GB, NTFS, native partition)
 *   D: User Data (400GB, NTFS, native partition)
 *     └─ Contains: D:\Monado\Containers\*.vhdx
 *        ├─ DevDrive.vhdx (400GB, ReFS)
 *        ├─ Vault.vhdx (50-500GB, NTFS, encrypted)
 *        ├─ Sandbox.vhdx (50GB, NTFS)
 *        └─ Quarantine.vhdx (20-200GB, NTFS)
 *   Free: 100GB (system reserved)
 * 
 * Mount Points After Boot:
 *   C: → Windows (NTFS native)
 *   D: → User Data (NTFS native)
 *   E: → DevDrive.vhdx (ReFS accelerated, mounted as virtual drive)
 *   V: → Vault.vhdx (encrypted, requires authentication)
 *   Q: → Quarantine.vhdx (always mounted, read-only)
 *   S: → Sandbox.vhdx (on-demand per session, auto-reset)
 * 
 * VHDX Container Specifications:
 * 
 * DevDrive (E: ReFS/VHDX):
 *   • VHDX container with ReFS filesystem
 *   • 40% faster than NTFS (ReFS acceleration)
 *   • Dynamic sizing (starts small, grows to 400GB)
 *   • High-performance development storage
 *   • Located: D:\Monado\Containers\DevDrive.vhdx
 *   • Best for: Builds, node_modules, Docker images, compilation
 *   • Portable (can move/backup entire container)
 *   • 5-10% I/O overhead (acceptable for ReFS speed gain)
 *   • No encryption by default (encrypt if needed)
 * 
 * Vault (V: NTFS/VHDX, Encrypted):
 *   • VHDX container with NTFS filesystem
 *   • BitLocker AES-256 + TPM 2.0 encryption
 *   • 50-500GB dynamic sizing
 *   • Credentials, keys, sensitive documents
 *   • Located: D:\Monado\Containers\Vault.vhdx
 *   • Portable (can backup to external drive)
 *   • 5-10% I/O overhead + encryption overhead
 *   • On-demand mount (requires authentication)
 *   • Network blocked when mounted
 * 
 * Quarantine (Q: NTFS/VHDX, Read-Only):
 *   • VHDX container with NTFS filesystem
 *   • 20-200GB dynamic sizing
 *   • Threat isolation and forensic analysis
 *   • Always mounted as read-only (Q: drive)
 *   • Located: D:\Monado\Containers\Quarantine.vhdx
 *   • Evidence preservation (tamper-proof)
 *   • Admin/Malwarebytes access only
 *   • 5-10% I/O overhead
 * 
 * Sandbox (S: NTFS/VHDX, Disposable):
 *   • VHDX container with NTFS filesystem
 *   • 50GB fixed sizing
 *   • Isolated execution environment for untrusted apps
 *   • Located: D:\Monado\Containers\Sandbox.vhdx
 *   • Auto-reset after each session (no persistence)
 *   • Per-session mounting (on-demand)
 *   • Read-only access to host OS (C: D: E: protected)
 *   • Network disabled by default
 *   • 5-10% I/O overhead
 * 
 * Storage Architecture Benefits:
 *   ✓ All VHDX containers portable (backup, move, replicate)
 *   ✓ DevDrive provides high-speed ReFS performance
 *   ✓ Vault provides encrypted credential storage
 *   ✓ Sandbox provides complete application isolation
 *   ✓ Quarantine provides forensic evidence preservation
 *   ✓ All located on same partition (D:) for easy management
 *   ✓ Can snapshot/restore any container to known-good state
 *   ✓ Performance trade-off (5-10% overhead) < security/isolation value
 */
