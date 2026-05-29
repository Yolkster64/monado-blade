# HELIOS / Monado Blade Consolidation Blueprint

This document is the current source-of-truth plan for consolidating the HELIOS Platform, Monado Blade GUI, installer, optimizer, security stack, AIHub, Azure integrations, and Codespaces workflow into one C#-first product experience.

## North Star

HELIOS should feel like one solid C#/.NET Windows platform, not a pile of scripts and phase reports.

The final experience should be:

1. Open Codespace or Visual Studio.
2. Build the main solution.
3. Run the Monado Boot / HELIOS Control Center GUI.
4. Choose install, optimize, secure, develop, AIHub, vault, or profile workflows from one interface.
5. Deploy locally, to USB, or into Azure with validated automation.

## Architectural Decision: C# First

The installer and boot flow should be C# first because the rest of the platform is C#/.NET.

PowerShell remains useful, but only as external templates or execution adapters. Embedded PowerShell inside C# source should be removed because it currently breaks compilation and makes the boot installer hard to test.

### Recommended layering

- C# GUI and orchestration: main experience, state machine, validation, progress UI, logs.
- C# services: optimizer, security, profiles, vault, sandbox, drivers, Azure, AIHub.
- External `.ps1` scripts: Windows-only actions such as partitioning, BitLocker, Defender, AppLocker, WDAC, driver staging, DISM, WinPE work.
- JSON/YAML manifests: partition layout, software catalog, driver catalog, Azure resources, profile presets.
- Python: optional legacy/rescue reference only, not the main product path.

## Major Recovered / Existing Feature Areas

### 1. Monado Boot GUI / Installer GUI

Key assets:

- `src/core/HELIOS.Platform/Phase10/BootEnvironment/MonadoUSBManagementGUI.cs`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/USBFlasher.cs`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/BootDiagnostics.cs`
- `src/core/HELIOS.Platform/Phase10/BootEnvironment/Channel3BootTimeAutomationOrchestrator.cs`
- `src/core/HELIOS.Platform/Phase10/BuilderUI/BuilderUIHost.cs`
- `src/core/HELIOS.Platform/Phase10/BuilderUI/DriveSelector.cs`
- `src/core/HELIOS.Platform/Phase10/BuilderUI/ProfileSelector.cs`
- `src/core/HELIOS.Platform/Phase10/BuilderUI/OptionsPanel.cs`
- `src/core/HELIOS.Platform/Phase10/BuilderUI/SummaryReview.cs`

Target experience:

- Full-screen Monado Boot wizard.
- Hardware detection.
- Drive/USB selection.
- Partition preview.
- Profile selection.
- Security mode selection.
- Azure/Entra/Vault setup check.
- Software bundle selection.
- Confirmation and staged execution.
- Recovery-safe logging.

### 2. Monado Blade graphics and Xenoblade-style UI

Key assets:

- `src/gui/MonadoBlade.GUI/Components/MonadoBlade.cs`
- `src/gui/MonadoBlade.GUI/Components/MonadoBladeAdvanced.cs`
- `src/gui/MonadoBlade.GUI/Components/XenobladeMondo.cs`
- `src/gui/MonadoBlade.GUI/Systems/BladeAnimationController.cs`
- `src/gui/MonadoBlade.GUI/Systems/KanjiAnimationController.cs`
- `src/gui/MonadoBlade.GUI/Windows/MonadoLoadingScreen.cs`
- `src/gui/MonadoBlade.GUI/Windows/MonadoMainWindow.cs`
- `src/gui/MonadoBlade.GUI/Windows/AIHubWindow.cs`
- `src/gui/MonadoBlade.GUI/Themes/MonadoColorPalette.xaml`
- `src/gui/MonadoBlade.GUI/Themes/XenobladeThemeSystem.xaml`
- `docs/ui-xenoblade/`

Target experience:

- Monado loading screen.
- Animated blade/kanji/rune system.
- System status as visual energy/health indicators.
- AIHub, Dev tools, Security, Vault, Optimizer, Installer, Profiles, Azure panes.
- Consistent Monado visual language across installer, tray, GUI, and dashboard.

### 3. AIHub and local LLM integration

Current foundations:

- `src/gui/MonadoBlade.GUI/Windows/AIHubWindow.cs`
- AI orchestration helpers under `Phase10/AIOrchestration/`
- ML/AI dependencies in `src/core/HELIOS.Platform/HELIOS.Platform.csproj`
- Azure and cloud config assets.

Target features:

- Local LLM provider manager.
- Ollama / LM Studio / llama.cpp adapters.
- Azure OpenAI / Azure AI Foundry provider adapters.
- Tool router for HELIOS operations.
- Offline coding assistant mode.
- Secure model cache and model registry.
- GPU-aware inference routing.
- Policy controls for which local/cloud providers can touch which data.

### 4. Driver optimizer and workstation tuning

Key assets:

- `src/core/HELIOS.Platform/Phase10/Drivers/DriverInstaller.cs`
- `src/core/HELIOS.Platform/Phase10/Drivers/DriverUpdater.cs`
- `src/core/HELIOS.Platform/Phase10/Drivers/DriverRepository.cs`
- `src/core/HELIOS.Platform/Phase10/Optimizer/GPUOptimizer.cs`
- `src/core/HELIOS.Platform/Phase10/Optimizer/NetworkOptimizer.cs`
- `src/core/HELIOS.Platform/Phase10/Optimizer/PowerProfiler.cs`
- `src/core/HELIOS.Platform/Phase10/Optimizer/PerformanceTuner.cs`
- `src/core/HELIOS.Platform/Phase10/Optimizer/OptimizationProfiles.cs`

Target features:

- NVIDIA Studio/Game Ready profile switching.
- Razer Blade profile tuning.
- Low-latency music mode.
- Gaming performance mode.
- Secure work mode.
- Developer balanced mode.
- Network stack tuning.
- Driver catalog validation.
- Restore-point creation before driver changes.

### 5. Military-grade security optimizer / AEGIS layer

Key assets:

- `src/core/HELIOS.Platform/Phase10/Quarantine/`
- `src/core/HELIOS.Platform/Phase10/Malware/`
- `src/core/HELIOS.Platform/Phase10/Vault/`
- `src/core/HELIOS.Platform/Phase10/Sandbox/`
- `config/Phase10/quarantine-config.json`
- `src/Security/SecurityValidator.csproj`
- `tests/SecurityValidationTests.csproj`

Target features:

- Defender configuration.
- Malwarebytes integration hooks.
- WDAC policy generation.
- AppLocker policy generation.
- Firewall profile sets.
- Audit policy hardening.
- BitLocker and VHDX vault automation.
- Sandbox execution with reset snapshots.
- Quarantine and threat scoring.
- Security health dashboard.

### 6. Built-in tools and software installer

Existing paths and concepts:

- `tools/Install-Wizard-Complete.ps1`
- `tools/USB-Builder-Complete-Fresh-Build.ps1`
- `tools/USB-Builder-Bootable-Lean.ps1`
- `installer/`
- `scripts/deploy/`
- `scripts/build/`
- `scripts/optimization/`

Target C# modules:

- Software catalog service.
- Package source adapters: winget, Chocolatey, NuGet, GitHub Releases, direct vendor installers.
- Safe installer runner.
- Checksum/signature verification.
- Retry and rollback.
- Bundle presets: Core, Dev, Music, Gaming, Secure, AI, Full.

### 7. Partitions and storage layout

Target features:

- Guided partition layout preview.
- Default profile-aware layout.
- DevDrive support.
- VHDX vault creation.
- Work/Secure/Gaming/Music storage zones.
- Recovery and backup partition planning.
- Logs and telemetry storage separation.

Suggested logical layout:

- `C:` Windows and drivers.
- `D:` User data.
- `E:` DevDrive/workspace.
- `F:` Applications/tools.
- `G:` Games/media.
- `H:` Music/audio projects.
- `I:` AI models/cache.
- `J:` Backups/recovery.
- `K:` BitLocker/VHDX secure vault.
- `L:` Logs/sandbox/export.

### 8. Azure, Entra, Purview, Vault, and Microsoft ecosystem integration

Found assets:

- `microsoft-ecosystem/azure-integration/SETUP_GUIDE.md`
- `microsoft-ecosystem/scripts/deploy-to-azure.ps1`
- `cloud-integration/configs/azure.config.json`
- `cloud-integration/configs/storage.config.json`
- `installer/core-infrastructure/shared-resources/config-templates/azure-config.template.json`
- `src/core/HELIOS.Platform/Core/Configuration/AzureConfiguration.cs`
- Azure dependencies in the main platform project.

Target features:

- Azure login and subscription validation.
- Azure Key Vault integration.
- Entra ID / Microsoft 365 Business Premium setup checks.
- Purview hooks for classification and compliance.
- Storage account and blob sync for configs/artifacts.
- Azure AI Foundry integration.
- Azure Container Apps or AKS deployment paths.
- Application Insights / monitoring dashboard.

## Primary Cleanup Plan

### Keep

- C# core platform.
- Phase10 systems.
- MonadoBlade GUI.
- Xenoblade theme docs and assets.
- Azure/cloud configs.
- Security/Vault/Sandbox/Optimizer systems.
- Installer tools as migration references.
- Build/test/deploy scripts that still map to active code.

### Convert

- Python USB installer branch becomes reference material or is ported into C#.
- Embedded PowerShell becomes external `.ps1` templates invoked by C#.
- Phase reports become archived documentation.
- Build logs become archive artifacts.

### Remove or archive

- Duplicate completion reports.
- Old generated reports that conflict with current status.
- Broken or stale docs that claim production readiness without passing CI.
- Stray build logs in source directories.
- Conflicting devcontainer variants.

## Proposed Final Product Structure

```text
HELIOS.Platform/
  src/
    core/HELIOS.Platform/              # core services
    gui/MonadoBlade.GUI/               # main WPF/WinUI GUI
    installer/HELIOS.Installer/        # C# installer/boot orchestration
    security/HELIOS.Security/          # vault, WDAC, Defender, quarantine
    optimization/HELIOS.Optimizer/     # GPU/network/power/driver optimizer
    ai/HELIOS.AIHub/                   # local/cloud AI provider router
    azure/HELIOS.Azure/                # Azure/Entra/Purview/KeyVault
    shared/HELIOS.Shared/              # shared abstractions
  scripts/
    windows/                           # external ps1 templates
    build/
    test/
    deploy/
  config/
    profiles/
    partitions/
    software-catalog/
    drivers/
    azure/
  docs/
    architecture/
    guides/
    optimization/
    testing/
    security/
    ui/
    archive/
```

## Build and Testing Priorities

1. Create a root `.sln` that includes only buildable projects first.
2. Add `HELIOS.Installer` as a C# project.
3. Move broken boot PowerShell out of `.cs` files.
4. Build `HELIOS.Platform` cleanly.
5. Build GUI separately on Windows runners if needed.
6. Add script validation for PowerShell.
7. Add Python validation only for legacy/reference tools.
8. Add security scan and dependency audit.

## Immediate Next Issues To Create / Complete

1. Refactor Channel3 boot files into C# orchestrator + external PowerShell templates.
2. Promote MonadoBlade GUI assets from scattered phase output into active GUI project.
3. Create C# AIHub provider abstraction for local LLM + Azure AI Foundry.
4. Create C# software catalog installer service.
5. Create C# partition layout planner and validation engine.
6. Create C# security baseline service for Defender, firewall, WDAC, AppLocker, BitLocker, VHDX.
7. Create C# driver optimizer service with rollback safety.
8. Create root solution and real GitHub Actions build/test matrix.
9. Archive old reports/logs into `docs/archive/`.
10. Create one `docs/FEATURE_MATRIX.md` with implemented / partial / planned states.

## Guiding Principle

HELIOS should be a C# Windows control plane with a mythic Monado GUI, serious security posture, local/cloud AI intelligence, and safe automation. Scripts are tools, not the product. Reports are history, not architecture. The GUI is the face. C# is the spine.
