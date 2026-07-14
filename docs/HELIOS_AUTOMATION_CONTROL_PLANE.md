# HELIOS Automation Control Plane

## Purpose

Connect the Monado Blade runtime setup, HELIOS/XTier workstation integration pack, AIHub services, GitHub Actions, Slack operations, Azure staging, and SharePoint runbooks through one governed automation model.

## Operating principle

Automation is split into four safety rings:

1. **Observe** — inventory, validate, lint, detect hardware, inspect configuration, and produce plans.
2. **Stage** — generate artifacts, Bicep what-if plans, package manifests, and reviewable pull requests.
3. **Apply guarded** — execute workstation or cloud changes only with explicit environment approvals and confirmation phrases.
4. **Operate** — health checks, drift detection, incident routing, rollback, and evidence retention.

No workflow may jump directly from Observe to Operate.

## Connected systems

| System | Role | Default mode |
|---|---|---|
| GitHub | Source, CI, issues, PR review, releases | Active |
| Slack `#helios-ops` | Operational notices, approvals, incident links | Active |
| SharePoint | Governed operator runbooks and evidence index | Planned mirror |
| OpenAI Platform | AIHub diagnostics and agent tooling | Secret-backed only |
| Azure / Foundry | Hosted control plane, model and agent services | What-if / stage-only |
| Local Windows | Hardware detection, DevDrive, software bundles | Dry-run first |
| WSL2 / Docker | AIHub trainer, gateway, GUI, APIs | Local isolated runtime |
| Hyper-V | Security-isolated workloads | Explicit enablement |

## Repository automation spine

```text
.github/workflows/
  helios-integration-validate.yml
  azure-oidc-whatif.yml          # future guarded workflow

docs/
  HELIOS_AUTOMATION_CONTROL_PLANE.md
  SETUP.md
config/
  integration-control-plane.json
scripts/
  windows/                       # guarded workstation adapters
  github/                        # repo backup/apply helpers
src/
  HELIOS.*                       # C# control-plane modules
python/
  x-tier/                        # AIHub sidecars and training loops
```

## Required gates

### Gate A — repository validation

- Validate JSON files.
- Compile Python modules without executing services.
- Restore/build .NET projects where project files exist.
- Scan committed text for obvious secret patterns.
- Confirm destructive PowerShell verbs are paired with guardrails.
- Upload validation evidence as workflow artifacts.

### Gate B — workstation planning

- Capture OS, CPU, RAM, GPU, disks, TPM, Secure Boot, Vulkan/OpenXR, WSL2, Docker, Hyper-V, and driver versions.
- Produce a machine-specific plan.
- Do not install, partition, encrypt, remove, or restart anything.

### Gate C — guarded workstation application

- Require a reviewed plan artifact.
- Require a named GitHub Environment approval.
- Require local elevation and an exact confirmation phrase.
- Create restore/recovery evidence first.
- Emit structured logs and rollback guidance.

### Gate D — Azure staging

- Use GitHub OIDC; never repository client secrets.
- Run Bicep lint and `what-if` only by default.
- Separate development, test, and production environments.
- Require private networking, Key Vault references, diagnostics, budgets, and policy checks.

### Gate E — operations

- Publish concise success/failure messages to `#helios-ops`.
- Link the GitHub run, issue, PR, artifact, and SharePoint evidence page.
- Route failures into issues with machine-readable diagnostic attachments.

## First automated path

```text
commit or pull request
  -> validate JSON / Python / .NET / PowerShell safety
  -> create evidence artifact
  -> update PR status
  -> post outcome to #helios-ops
  -> after approval, generate workstation plan
  -> after separate approval, apply guarded changes
```

## Secrets

Allowed locations:

- GitHub Environments / Actions secrets
- Azure Key Vault
- Local Windows Credential Manager or DPAPI-backed vault
- Managed identity / workload identity

Prohibited locations:

- Git history
- Slack messages or canvases
- SharePoint plaintext pages
- JSON configuration committed to the repository
- console logs or workflow artifacts

## Immediate backlog

1. Validate the uploaded integration pack in CI.
2. Import the pack through a dedicated branch and PR.
3. Add Pester tests for PowerShell guardrails.
4. Add pytest/unittest coverage for AIHub CLI/server modules.
5. Add .NET tests for hardware detection and software catalog services.
6. Add GitHub OIDC Azure `what-if` workflow.
7. Add Slack workflow notifications through a secret-backed webhook or app.
8. Mirror approved runbooks and evidence indexes into SharePoint.
9. Add scheduled drift checks for dependencies, drivers, security posture, and cloud infrastructure.
10. Build a GUI status view over the same manifests and evidence artifacts.
