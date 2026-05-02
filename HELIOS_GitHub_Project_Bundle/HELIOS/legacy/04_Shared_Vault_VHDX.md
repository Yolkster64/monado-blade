# Shared Vault VHDX

## Why
A BitLocker-encrypted VHDX is a stronger local vault container than a plain folder if you want local encrypted storage outside the browser.

## Use for
- temporary import staging
- recovery references
- local Helios notes
- certificate inventories
- USB / BitLocker helper notes

## Rule
This is not a replacement for Bitwarden. It is a local encrypted helper vault.

## Suggested flow
1. Run the scaffold script in `scripts/create_vhdx_vault.ps1`.
2. Attach the VHDX manually.
3. Initialize and format it.
4. Enable BitLocker on the mounted volume after review.
5. Mount only when needed.
