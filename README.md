# Hermes Fleet Platforms + HELIOS Consolidation

This repository consolidates Hermes Fleet, HELIOS Platform, and Monado Blade-aligned assets into one working platform for orchestration, security, and AI-driven operations.

## What is included

- Hermes multi-agent runtime and orchestration modules (`core/`, `ui/`)
- SQL learning and model-tracking workflow (`SQL_LEARNING_GROUND.md`, `sql-learning/`)
- HELIOS platform history imported from `M0nado/helios-platform`
- Cross-platform integration docs for cloud/dev tooling and operations

## Current focus

1. Unified branch history across Hermes + HELIOS sources.
2. Organized docs for operational delivery and onboarding.
3. GitHub CLI-driven workflow for branch/PR/release automation.

## Key documentation

- [`docs/README.md`](./docs/README.md)
- [`docs/GETTING_STARTED.md`](./docs/GETTING_STARTED.md)
- [`docs/HERMES_AGENTS_SQL_GHCLI.md`](./docs/HERMES_AGENTS_SQL_GHCLI.md)
- [`SQL_LEARNING_GROUND.md`](./SQL_LEARNING_GROUND.md)

## Quick workflow

```bash
git checkout integration/helios-website-consolidation
git pull
```

To open a PR with GitHub CLI:

```bash
gh pr create --title "feat: consolidate helios and hermes platform history" --body "Merges latest HELIOS website/platform history and organizes agent/sql/github-cli docs." --base main --head integration/helios-website-consolidation
```
