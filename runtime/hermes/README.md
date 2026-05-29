# Hermes Local Runtime

1. Start stack:
   - `pwsh ./runtime/hermes/start-local.ps1`
2. API:
   - `http://localhost:8787`
2b. C# gateway front:
   - `http://localhost:8788`
3. GUI:
   - `http://localhost:8501`

This runtime enables:
- QNAA/KNAA training simulations through `/simulate` and `/horizon-tests`
- External/online signal ingest through `/ingest-signal` for cross-model learning influence
- Fleet topology optimization through `/optimize-fleet`
- Learning source curation through `/curate-learning` (SQL vs internet vs LLM weighting)
- Long-horizon meta-learning blended across gaussian alignment + correction + all-data signals
- Unified non-blocking flow pulse through `/learning-pulse` (simulate + optimize + curate in one call)
- Duplicate-data optimization scan through `/dedupe-optimize` to reduce redundant learning input
- AIHub intelligence bonus endpoint `/aihub-bonus` for cross-LLM/Hermes uplift signal
- SQL telemetry in `runtime/auto/hermes_super_orchestrator.db`
- Continuous fleet auto-training via `hermes-trainer` service
- C# performance front-end (`hermes-gateway`) for smoother API routing and integration
- API security between gateway and backend via `HERMES_API_KEY`
- Resource targeting via env vars:
  - `HERMES_GPU_TARGET_UTILIZATION` (default `0.75`)
  - `HERMES_CPU_TARGET_UTILIZATION` (default `0.80`)
