# Hermes Super Orchestration (GPU-Targeted, SQL-First)

This profile provides a practical baseline for a high-complexity Hermes AI hub:

- Multi-agent orchestration with specialization routing
- Adaptive reward tuning (gaussian perturbation)
- Natural-selection style retirement and spawning
- SQL telemetry with compressed event payloads
- Local API for GUI and automation integration
- Resource governor targeting ~75% GPU utilization

## Runtime Module

- `core/hermes_super_orchestrator.py`

Main classes:

1. `HermesSuperOrchestrator`
2. `ResourceGovernor`
3. `SqlTelemetryStore`
4. `OrchestratorApi`

## Quick Start

```bash
python core/hermes_super_orchestrator.py
```

API endpoints:

1. `GET /health`
2. `GET /snapshot`
3. `POST /train-step`
4. `POST /simulate`
5. `POST /horizon-tests`
6. `POST /rank-output`

Example training call:

```bash
curl -X POST http://127.0.0.1:8787/train-step -H "Content-Type: application/json" -d "{\"specialty\":\"sql_learning\",\"complexity\":0.7}"
```

Example simulation sweep:

```bash
curl -X POST http://127.0.0.1:8787/simulate -H "Content-Type: application/json" -d "{\"specialty\":\"llm_orchestration\",\"steps\":2000}"
```

Example short/mid/long horizon test suite:

```bash
curl -X POST http://127.0.0.1:8787/horizon-tests -H "Content-Type: application/json" -d "{\"specialty\":\"sql_learning\",\"short_steps\":100,\"mid_steps\":400,\"long_steps\":1500}"
```

Rank an LLM output with 3D gaussian objective shaping:

```bash
curl -X POST http://127.0.0.1:8787/rank-output -H "Content-Type: application/json" -d "{\"output\":\"candidate text\",\"goal\":\"quality\",\"quality\":0.84,\"speed\":0.71,\"cost_efficiency\":0.69,\"truth_score\":0.92}"
```

Build native kernel DLL on Windows (MSVC Developer Prompt):

```bash
cl /LD /O2 core\\native\\hermes_learning_kernel.cpp /Fe:core\\native\\hermes_learning_kernel.dll
```

## Optimization Model

Each train step computes an expanded optimization envelope:

- `quality` (result quality proxy)
- `speed` (throughput proxy under current load)
- `cost_efficiency` (resource/cost efficiency)
- `truth_score` (anti-false-promotion gate)
- `compression_gain` (signal density after compression/quantization)
- `data_freshness` (recency/online relevance factor)
- `pattern_diversity` (non-collapsing strategy diversity)
- `risk_adjusted` (risk-normalized confidence)
- plus `novelty` (exploration factor)

Reward update:

1. weighted multi-objective score (quality/speed/cost/truth/novelty)
2. gaussian adjustment of reward weights for exploration
3. hard truth-gate penalty if trust threshold is violated
4. bounded reward score and success-rate updates

Natural selection:

1. retire persistently weak agents when fleet size is high
2. periodically spawn evolved specialists from top performers

Learning strategies in parallel:

1. contextual bandit-style value updates
2. q-learning table updates by workload complexity bins
3. bayesian success priors per specialty
4. simulation sweeps (`/simulate`) for batch tuning
5. horizon-specific test suites (`/horizon-tests`) for short/mid/long objective balancing

Horizon optimization:

1. **Short** tests emphasize immediate speed + quality + truth.
2. **Mid** tests emphasize balanced speed/cost/truth stability.
3. **Long** tests emphasize truth durability + cost efficiency + compression wins.

## SQL + Compression

Database path:

- `runtime/auto/hermes_super_orchestrator.db`

Tables:

1. `agent_metrics`
2. `orchestrator_events`
3. `horizon_test_scores`
4. `llm_output_rankings`

`orchestrator_events.payload_compressed` stores zlib-compressed JSON for compact retention.
`llm_output_rankings` stores per-output rankings with 3D shape vectors and goal-aware gaussian scores.

## C++ and ML Integration Guidance

The baseline includes a `cpp_ml_kernels` specialty and can route C++ kernel work to a dedicated agent.
Use this with native kernels for critical hot paths and keep orchestration in Python/C#.

Implemented paths:

1. C++ kernel: `core/native/hermes_learning_kernel.cpp`
2. Python bridge: `core/hermes_cpp_native_bridge.py`
3. C# bridge: `src/MonadoBlade.Core/Services/NativeHermesLearningBridge.cs`

Native C++ also computes:

1. reward update scoring
2. float quantization
3. 3D gaussian shape score (`hermes_gaussian_3d_score`)

Recommended split:

1. C++: tight kernels, quantization, high-throughput transforms
2. Python: orchestration, experimentation, reward tuning
3. SQL: telemetry, retention, analytics, policy triggers
4. C#: application integration boundary for native kernels and service orchestration

## Security Notes

1. Bind API to localhost by default.
2. Keep credentials out of source; use environment variables.
3. Add auth/token middleware before exposing beyond local machine.
