# Install Checklist

## Phase 0 — Before reset
- Export browser passwords if you want to preserve them.
- Export or note BitLocker recovery references.
- Run the Windows data capture script in `scripts/windows_data_capture.ps1`.
- Back up personal files.
- Back up the Helios folder if it already exists.

## Phase 1 — Reset-first build
Goal: keep firmware and trusted driver baselines, but rebuild Windows cleanly enough to shape the system around Helios.

Recommended order:
1. Reset / reinstall Windows.
2. Install Windows updates fully.
3. Install only trusted core drivers:
   - GPU
   - chipset / storage / network
   - audio interface or audio stack
   - Razer only if needed
4. Enable:
   - Windows Hello
   - BitLocker
   - Defender
5. Create accounts:
   - ADMIN
   - WORK
   - GAMING
   - DEV
   - MUSIC
6. Install:
   - Bitwarden desktop + Edge extension
   - Malwarebytes
   - OneDrive
   - Google Drive for desktop
   - VS Code / Git / Docker / Hyper-V or Sandbox
   - DAW + ASIO driver
7. Migrate browser passwords into Bitwarden.
8. Turn browser password saving off.
9. Put this Helios folder somewhere stable.

## Phase 2 — Control plane
- Use the scripts in `scripts/`.
- Use `templates/Add_New_Item_Template.csv` for missing accounts.
- Use the VHDX scaffold if you want a stronger local vault container.
