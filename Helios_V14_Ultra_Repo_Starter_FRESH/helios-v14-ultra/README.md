# Helios V14 Ultra

Local-first Windows USB/bootstrap repo scaffold for a segmented ADMIN / WORK / GAMING / DEV / MUSIC setup.

## Goals
- Preserve the local ADMIN account
- Create standard-user lanes for WORK / GAMING / DEV / MUSIC
- Install core apps from manifests
- Build a Helios control-plane folder layout
- Inventory startup/services/apps before enforcement
- Keep Entra/Purview as optional phase 2
- Stay review-first for risky changes

## Layout
- `docs/` design and workflow notes
- `scripts/` PowerShell automation
- `manifests/` software, driver, partition plans
- `templates/` AppLocker starter templates
- `agent/` baseline/drift/sentinel scaffolding
- `phase2/` optional cloud notes

## Usage
1. Review docs
2. Run scripts in order after a clean Windows install
3. Keep secrets out of GitHub
