# Hermes Agents + SQL Training + GitHub CLI Integration

This guide consolidates the latest Codex website/platform work into one operational workflow for HELIOS/Hermes.

## 1. Objectives

- Run multi-agent Hermes workflows (task routing, specialization, registry, upgrades).
- Persist training and telemetry state in SQL for repeatable learning cycles.
- Use GitHub CLI to standardize branch/PR/release operations from terminal-first automation.

## 2. Agent Runtime Components

Key modules in `core/`:

- `hermes_agent_registry.py` - agent catalog and capability routing.
- `hermes_agent_task_system.py` - task dispatch and assignment logic.
- `hermes_agent_specialization.py` - role specialization hooks.
- `hermes_agent_smart_upgrades.py` - adaptive upgrade policy handling.
- `hermes_job_manager.py` and `hermes_job_tracking.py` - job lifecycle and status accounting.

Key UI modules in `ui/`:

- `hermes_multi_agent_gui_v2.py`, `v3.py`, `v4.py` - runtime dashboards.
- `hermes_sql_gui.py`, `hermes_sql_gui_v2.py` - SQL visibility and quick admin workflows.

## 3. SQL Training Flow

1. Capture agent events and outcomes into SQL tables (`experiments`, `runs`, `metrics`, `alerts`).
2. Use `hermes_sql_server.py` and `hermes_data_workflow.py` to normalize and store telemetry.
3. Run training loops via:
   - `hermes_hybrid_training.py`
   - `hermes_multi_parallel_training.py`
   - `hermes_counter_parallel_training.py`
4. Benchmark/selection orchestration:
   - `hermes_unified_ml.py`
   - `hermes_ml_algorithms.py`

Recommended schema strategy:

- `agent_runs` for runtime metadata and success/failure outcomes.
- `model_metrics` for training statistics (accuracy, loss, latency, drift).
- `ops_audit` for privileged actions and security events.

## 4. GitHub CLI Delivery Workflow

Use `gh` for consistent branch-to-PR flow:

```bash
git checkout -b integration/helios-hermes-sync
git add .
git commit -m "feat: consolidate helios website and hermes agent/sql flow"
git push -u origin integration/helios-hermes-sync
gh pr create --title "feat: consolidate helios + hermes platform updates" --body "Merges latest website/platform work and documents agent/sql/gh integration." --base main --head integration/helios-hermes-sync
```

Operational checks:

```bash
gh pr status
gh run list
gh run watch
```

## 5. Branch Consolidation Strategy

- Keep `main` as release-ready integration branch.
- Merge long-running streams (`develop`, rescue branches, codex/copilot branches) into integration branches first.
- Resolve docs/index drift by updating `docs/README.md` in every consolidation merge.
- Avoid committing generated artifacts (`__pycache__`, local env files, binaries) to keep diffs reviewable.

## 6. Website and Platform Synchronization Notes

- Treat website-facing docs and dashboards as first-class release artifacts.
- Every platform merge should include:
  - docs index update
  - security baseline verification
  - SQL training flow compatibility check
  - GitHub CLI PR metadata (title/body/checklist)

## 7. Optimization Priorities

1. Reduce duplicate docs and use canonical links (`SECURITY.md` + checklist model).
2. Keep agent orchestration logic in `core/`; keep UI-only logic in `ui/`.
3. Persist model and runtime state centrally so agent tuning is measurable and reversible.
4. Standardize CLI automation around `gh` + branch naming for predictable delivery.
