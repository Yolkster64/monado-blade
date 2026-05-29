import json
import hashlib
import os
import random
import sqlite3
import threading
import time
import zlib
from dataclasses import dataclass, asdict
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from typing import Dict, List, Optional

try:
    from .hermes_cpp_native_bridge import HermesCppNativeBridge
except ImportError:
    from hermes_cpp_native_bridge import HermesCppNativeBridge

try:
    import psutil
except ImportError:  # Optional dependency
    psutil = None

try:
    import torch
except ImportError:  # Optional dependency
    torch = None


@dataclass
class AgentProfile:
    name: str
    specialty: str
    reward_score: float = 0.0
    success_rate: float = 0.5
    load: float = 0.0
    active: bool = True


@dataclass
class InteractionOutcome:
    quality: float
    speed: float
    cost_efficiency: float
    truth_score: float
    novelty: float
    compression_gain: float
    data_freshness: float
    pattern_diversity: float
    risk_adjusted: float
    success: float


class SqlTelemetryStore:
    def __init__(self, db_path: str = "runtime/auto/hermes_super_orchestrator.db") -> None:
        self.db_path = db_path
        self._init_db()

    def _conn(self) -> sqlite3.Connection:
        return sqlite3.connect(self.db_path, check_same_thread=False)

    def _init_db(self) -> None:
        with self._conn() as conn:
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS agent_metrics (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    agent_name TEXT NOT NULL,
                    reward_score REAL NOT NULL,
                    success_rate REAL NOT NULL,
                    load REAL NOT NULL
                )
                """
            )
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS orchestrator_events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    event_type TEXT NOT NULL,
                    payload_compressed BLOB NOT NULL
                )
                """
            )
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS horizon_test_scores (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    horizon TEXT NOT NULL,
                    specialty TEXT NOT NULL,
                    score REAL NOT NULL,
                    quality REAL NOT NULL,
                    speed REAL NOT NULL,
                    cost_efficiency REAL NOT NULL,
                    truth_score REAL NOT NULL
                )
                """
            )
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS llm_output_rankings (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    goal TEXT NOT NULL,
                    output_hash TEXT NOT NULL,
                    rank_score REAL NOT NULL,
                    quality REAL NOT NULL,
                    speed REAL NOT NULL,
                    cost_efficiency REAL NOT NULL,
                    truth_score REAL NOT NULL,
                    shape3d_x REAL NOT NULL,
                    shape3d_y REAL NOT NULL,
                    shape3d_z REAL NOT NULL
                )
                """
            )
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS temporal_memory_bands (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    band TEXT NOT NULL,
                    ttl_class TEXT NOT NULL,
                    retention_score REAL NOT NULL,
                    payload_compressed BLOB NOT NULL
                )
                """
            )
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS external_signals (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    source TEXT NOT NULL,
                    signal_score REAL NOT NULL,
                    payload_compressed BLOB NOT NULL
                )
                """
            )
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS fleet_optimization_runs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    specialty TEXT NOT NULL,
                    team_shape TEXT NOT NULL,
                    active_agents INTEGER NOT NULL,
                    score REAL NOT NULL,
                    payload_compressed BLOB NOT NULL
                )
                """
            )
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS dedupe_candidates (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts REAL NOT NULL,
                    file_hash TEXT NOT NULL,
                    file_count INTEGER NOT NULL,
                    total_bytes INTEGER NOT NULL,
                    sample_path TEXT NOT NULL
                )
                """
            )

    def write_metric(self, agent: AgentProfile) -> None:
        with self._conn() as conn:
            conn.execute(
                """
                INSERT INTO agent_metrics(ts, agent_name, reward_score, success_rate, load)
                VALUES (?, ?, ?, ?, ?)
                """,
                (time.time(), agent.name, agent.reward_score, agent.success_rate, agent.load),
            )

    def write_event(self, event_type: str, payload: Dict) -> None:
        compressed = zlib.compress(json.dumps(payload).encode("utf-8"))
        with self._conn() as conn:
            conn.execute(
                "INSERT INTO orchestrator_events(ts, event_type, payload_compressed) VALUES (?, ?, ?)",
                (time.time(), event_type, compressed),
            )

    def recent_events(self, limit: int = 25) -> List[Dict]:
        with self._conn() as conn:
            rows = conn.execute(
                """
                SELECT ts, event_type, payload_compressed
                FROM orchestrator_events
                ORDER BY id DESC
                LIMIT ?
                """,
                (limit,),
            ).fetchall()
        out: List[Dict] = []
        for ts, event_type, payload in rows:
            out.append(
                {
                    "ts": ts,
                    "event_type": event_type,
                    "payload": json.loads(zlib.decompress(payload).decode("utf-8")),
                }
            )
        return out

    def write_horizon_score(self, horizon: str, specialty: str, score: float, outcome: InteractionOutcome) -> None:
        with self._conn() as conn:
            conn.execute(
                """
                INSERT INTO horizon_test_scores(
                    ts, horizon, specialty, score, quality, speed, cost_efficiency, truth_score
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    time.time(),
                    horizon,
                    specialty,
                    score,
                    outcome.quality,
                    outcome.speed,
                    outcome.cost_efficiency,
                    outcome.truth_score,
                ),
            )

    def write_llm_ranking(
        self,
        goal: str,
        output_hash: str,
        rank_score: float,
        quality: float,
        speed: float,
        cost_efficiency: float,
        truth_score: float,
        shape3d: tuple[float, float, float],
    ) -> None:
        with self._conn() as conn:
            conn.execute(
                """
                INSERT INTO llm_output_rankings(
                    ts, goal, output_hash, rank_score, quality, speed, cost_efficiency, truth_score, shape3d_x, shape3d_y, shape3d_z
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    time.time(),
                    goal,
                    output_hash,
                    rank_score,
                    quality,
                    speed,
                    cost_efficiency,
                    truth_score,
                    shape3d[0],
                    shape3d[1],
                    shape3d[2],
                ),
            )

    def write_memory_band(self, band: str, ttl_class: str, retention_score: float, payload: Dict) -> None:
        compressed = zlib.compress(json.dumps(payload).encode("utf-8"))
        with self._conn() as conn:
            conn.execute(
                """
                INSERT INTO temporal_memory_bands(ts, band, ttl_class, retention_score, payload_compressed)
                VALUES (?, ?, ?, ?, ?)
                """,
                (time.time(), band, ttl_class, retention_score, compressed),
            )

    def write_external_signal(self, source: str, signal_score: float, payload: Dict) -> None:
        compressed = zlib.compress(json.dumps(payload).encode("utf-8"))
        with self._conn() as conn:
            conn.execute(
                """
                INSERT INTO external_signals(ts, source, signal_score, payload_compressed)
                VALUES (?, ?, ?, ?)
                """,
                (time.time(), source, signal_score, compressed),
            )

    def recent_external_signal_score(self, lookback: int = 40) -> float:
        with self._conn() as conn:
            rows = conn.execute(
                """
                SELECT signal_score
                FROM external_signals
                ORDER BY id DESC
                LIMIT ?
                """,
                (lookback,),
            ).fetchall()
        if not rows:
            return 0.5
        return sum(r[0] for r in rows) / len(rows)

    def write_fleet_optimization_run(self, specialty: str, team_shape: str, active_agents: int, score: float, payload: Dict) -> None:
        compressed = zlib.compress(json.dumps(payload).encode("utf-8"))
        with self._conn() as conn:
            conn.execute(
                """
                INSERT INTO fleet_optimization_runs(ts, specialty, team_shape, active_agents, score, payload_compressed)
                VALUES (?, ?, ?, ?, ?, ?)
                """,
                (time.time(), specialty, team_shape, active_agents, score, compressed),
            )

    def write_dedupe_candidate(self, file_hash: str, file_count: int, total_bytes: int, sample_path: str) -> None:
        with self._conn() as conn:
            conn.execute(
                """
                INSERT INTO dedupe_candidates(ts, file_hash, file_count, total_bytes, sample_path)
                VALUES (?, ?, ?, ?, ?)
                """,
                (time.time(), file_hash, file_count, total_bytes, sample_path),
            )


class ResourceGovernor:
    def __init__(self, gpu_target_utilization: float = 0.75, cpu_target_utilization: float = 0.8) -> None:
        self.gpu_target_utilization = gpu_target_utilization
        self.cpu_target_utilization = cpu_target_utilization

    def _gpu_pressure(self) -> float:
        if not torch or not torch.cuda.is_available():
            return 0.0
        total = torch.cuda.get_device_properties(0).total_memory
        used = torch.cuda.memory_allocated(0)
        if total <= 0:
            return 0.0
        return float(used) / float(total)

    def pressure(self) -> Dict[str, float]:
        if psutil:
            cpu = psutil.cpu_percent(interval=0.05) / 100.0
            mem = psutil.virtual_memory().percent / 100.0
        else:
            cpu = 0.5
            mem = 0.5
        gpu = self._gpu_pressure()
        return {"cpu": cpu, "memory": mem, "gpu": gpu}

    def capacity_scale(self) -> float:
        p = self.pressure()
        cpu_headroom = max(0.05, self.cpu_target_utilization - p["cpu"])
        gpu_headroom = max(0.05, self.gpu_target_utilization - p["gpu"])
        mem_headroom = max(0.05, 0.9 - p["memory"])
        return min(1.0, cpu_headroom, gpu_headroom, mem_headroom) / 0.75


class HermesSuperOrchestrator:
    def __init__(self, agents: List[AgentProfile], store: Optional[SqlTelemetryStore] = None) -> None:
        self.agents = agents
        self.store = store or SqlTelemetryStore()
        gpu_target = float(os.getenv("HERMES_GPU_TARGET_UTILIZATION", "0.75"))
        cpu_target = float(os.getenv("HERMES_CPU_TARGET_UTILIZATION", "0.80"))
        self.governor = ResourceGovernor(gpu_target_utilization=gpu_target, cpu_target_utilization=cpu_target)
        self.reward_weights = {
            "quality": 0.22,
            "speed": 0.16,
            "cost": 0.12,
            "truth": 0.21,
            "novelty": 0.08,
            "compression": 0.07,
            "freshness": 0.07,
            "diversity": 0.04,
            "risk": 0.03,
        }
        self.horizon_weights = {
            "short": {"quality": 0.28, "speed": 0.30, "cost": 0.12, "truth": 0.20, "compression": 0.10},
            "mid": {"quality": 0.26, "speed": 0.20, "cost": 0.20, "truth": 0.24, "compression": 0.10},
            "long": {"quality": 0.24, "speed": 0.10, "cost": 0.20, "truth": 0.30, "compression": 0.16},
        }
        self.truth_threshold = 0.68
        self.native_bridge = HermesCppNativeBridge()
        self.algorithm_state = {
            "bandit_values": {},  # specialty -> value
            "q_table": {},  # (specialty, bin) -> value
            "beta_priors": {},  # specialty -> (alpha, beta)
            "horizon_memory": {"short": [], "mid": [], "long": []},
            "knaa_qnaa_memory": [],
            "punishment_memory": [],
            "fleet_shape_memory": [],
            "long_haul_meta_memory": [],
            "aihub_bonus_memory": [],
        }
        self.goal_shape_targets = {
            "quality": (0.90, 0.62, 0.52),
            "speed": (0.74, 0.92, 0.60),
            "cost": (0.68, 0.70, 0.93),
            "balanced": (0.82, 0.82, 0.82),
        }
        self.lock = threading.Lock()

    def _agent_score(self, agent: AgentProfile) -> float:
        return (agent.reward_score * 0.6) + (agent.success_rate * 0.4) - (agent.load * 0.2)

    def _select_agent(self, specialty_hint: str) -> AgentProfile:
        candidates = [a for a in self.agents if a.active]
        hinted = [a for a in candidates if specialty_hint.lower() in a.specialty.lower()]
        pool = hinted or candidates
        return max(pool, key=self._agent_score)

    def _gaussian_tune_rewards(self) -> None:
        for k in self.reward_weights:
            self.reward_weights[k] += random.gauss(0.0, 0.015)
        total = sum(max(0.01, v) for v in self.reward_weights.values())
        for k in self.reward_weights:
            self.reward_weights[k] = max(0.01, self.reward_weights[k]) / total

    def _multi_objective_score(self, outcome: InteractionOutcome) -> float:
        weights = [
            self.reward_weights["quality"],
            self.reward_weights["speed"],
            self.reward_weights["cost"],
            self.reward_weights["truth"],
            self.reward_weights["compression"] + self.reward_weights["novelty"],
        ]
        return self.native_bridge.reward_update(
            quality=outcome.quality,
            speed=outcome.speed,
            cost_efficiency=outcome.cost_efficiency,
            truth_score=outcome.truth_score,
            novelty=outcome.compression_gain + outcome.pattern_diversity,
            weights=weights,
        )

    def _truth_gate_adjustment(self, score: float, truth_score: float) -> float:
        if truth_score >= self.truth_threshold:
            return score
        # Strong penalty to prevent false promotions.
        penalty = (self.truth_threshold - truth_score) * 1.5
        return score - penalty

    def _bandit_update(self, specialty: str, reward: float) -> None:
        old = self.algorithm_state["bandit_values"].get(specialty, 0.0)
        lr = 0.12
        self.algorithm_state["bandit_values"][specialty] = old + lr * (reward - old)

    def _q_learning_update(self, specialty: str, complexity: float, reward: float) -> None:
        bin_key = f"c{int(max(0.0, min(0.99, complexity)) * 5)}"
        state_key = (specialty, bin_key)
        old_q = self.algorithm_state["q_table"].get(state_key, 0.0)
        alpha = 0.2
        gamma = 0.9
        future = max([v for (s, _), v in self.algorithm_state["q_table"].items() if s == specialty] or [0.0])
        self.algorithm_state["q_table"][state_key] = old_q + alpha * (reward + gamma * future - old_q)

    def _bayesian_update(self, specialty: str, success: float) -> None:
        alpha, beta = self.algorithm_state["beta_priors"].get(specialty, (1.0, 1.0))
        if success >= 0.5:
            alpha += 1.0
        else:
            beta += 1.0
        self.algorithm_state["beta_priors"][specialty] = (alpha, beta)

    def _run_algorithm_updates(self, specialty: str, complexity: float, reward: float, success: float) -> None:
        self._bandit_update(specialty, reward)
        self._q_learning_update(specialty, complexity, reward)
        self._bayesian_update(specialty, success)

    def _horizon_score(self, horizon: str, outcome: InteractionOutcome) -> float:
        w = self.horizon_weights[horizon]
        return (
            outcome.quality * w["quality"]
            + outcome.speed * w["speed"]
            + outcome.cost_efficiency * w["cost"]
            + outcome.truth_score * w["truth"]
            + outcome.compression_gain * w["compression"]
        )

    def _update_horizon_memory(self, horizon: str, value: float) -> None:
        mem = self.algorithm_state["horizon_memory"][horizon]
        mem.append(value)
        if len(mem) > 1500:
            del mem[: len(mem) - 1500]

    def _gaussian_3d_shape_score(self, point: tuple[float, float, float], target: tuple[float, float, float], sigma: float = 0.20) -> float:
        return self.native_bridge.gaussian_3d_score(
            x=point[0],
            y=point[1],
            z=point[2],
            target_x=target[0],
            target_y=target[1],
            target_z=target[2],
            sigma=sigma,
        )

    def _knaa_qnaa_score(
        self,
        short_variables: Dict[str, float],
        mid_variables: Dict[str, float],
        long_variables: Dict[str, float],
        truth_score: float,
        reward_score: float,
    ) -> float:
        short_values = [max(0.0, min(1.0, v)) for v in short_variables.values()]
        mid_values = [max(0.0, min(1.0, v)) for v in mid_variables.values()]
        long_values = [max(0.0, min(1.0, v)) for v in long_variables.values()]
        exploration_rate = max(0.01, min(0.6, 1.0 - truth_score))
        score = self.native_bridge.knaa_qnaa_score(
            short_values=short_values,
            mid_values=mid_values,
            long_values=long_values,
            truth_score=truth_score,
            reward_score=reward_score,
            exploration_rate=exploration_rate,
        )
        mem = self.algorithm_state["knaa_qnaa_memory"]
        mem.append(score)
        if len(mem) > 2000:
            del mem[: len(mem) - 2000]
        return score

    def _fleet_shape_score(
        self,
        active_agents: int,
        latency_ms: float,
        throughput_rps: float,
        error_rate: float,
        diversity: float,
        memory_retention: float,
    ) -> float:
        score = self.native_bridge.fleet_shape_score(
            active_agents=float(active_agents),
            latency_ms=latency_ms,
            throughput_rps=throughput_rps,
            error_rate=error_rate,
            diversity=diversity,
            memory_retention=memory_retention,
        )
        mem = self.algorithm_state["fleet_shape_memory"]
        mem.append(score)
        if len(mem) > 2000:
            del mem[: len(mem) - 2000]
        return score

    def _punishment_correction(self, truth_score: float, quality: float, drift: float) -> float:
        if truth_score >= self.truth_threshold and quality >= 0.6:
            correction = min(0.18, (truth_score - self.truth_threshold) * 0.25 + quality * 0.05)
        else:
            truth_gap = max(0.0, self.truth_threshold - truth_score)
            correction = -min(0.6, truth_gap * 1.35 + drift * 0.22)
        pmem = self.algorithm_state["punishment_memory"]
        pmem.append(correction)
        if len(pmem) > 2000:
            del pmem[: len(pmem) - 2000]
        return correction

    def _persist_temporal_memory_bands(self, event: Dict) -> None:
        short_payload = {
            "agent": event["agent"],
            "specialty": event["specialty"],
            "short_score": event["short_score"],
            "knaa_qnaa_score": event["knaa_qnaa_score"],
            "fleet_shape_score": event["fleet_shape_score"],
        }
        mid_payload = {
            "objective_score": event["objective_score"],
            "truth_score": event["truth_score"],
            "compression_gain": event["compression_gain"],
            "external_signal_score": event["external_signal_score"],
        }
        long_payload = {
            "long_score": event["long_score"],
            "retention_score": event["long_variables"]["retention_score"],
            "correction": event["punishment_correction"],
            "long_haul_meta_score": event["long_haul_meta_score"],
            "reward_score": event["reward_score"],
        }
        self.store.write_memory_band("short", "short_ttl", short_payload["short_score"], short_payload)
        self.store.write_memory_band("mid", "mid_ttl", event["mid_score"], mid_payload)
        self.store.write_memory_band("long", "long_ttl", long_payload["retention_score"], long_payload)

    def _compression_shape_score(
        self,
        short_variables: Dict[str, float],
        mid_variables: Dict[str, float],
        long_variables: Dict[str, float],
    ) -> float:
        values = [*short_variables.values(), *mid_variables.values(), *long_variables.values()]
        safe_values = [max(0.0, min(1.0, v)) for v in values]
        return self.native_bridge.quantized_compression_score(safe_values)

    def _long_haul_meta_score(
        self,
        short_variables: Dict[str, float],
        mid_variables: Dict[str, float],
        long_variables: Dict[str, float],
        external_signal_score: float,
        correction_signal: float,
        truth_score: float,
        gaussian_alignment: float,
    ) -> float:
        short_values = [max(0.0, min(1.0, v)) for v in short_variables.values()]
        mid_values = [max(0.0, min(1.0, v)) for v in mid_variables.values()]
        long_values = [max(0.0, min(1.0, v)) for v in long_variables.values()]
        score = self.native_bridge.long_haul_meta_score(
            short_values=short_values,
            mid_values=mid_values,
            long_values=long_values,
            external_signal_score=max(0.0, min(1.0, external_signal_score)),
            correction_signal=max(-1.0, min(1.0, correction_signal)),
            truth_score=max(0.0, min(1.0, truth_score)),
            gaussian_alignment=max(0.0, min(1.0, gaussian_alignment)),
        )
        mem = self.algorithm_state["long_haul_meta_memory"]
        mem.append(score)
        if len(mem) > 2000:
            del mem[: len(mem) - 2000]
        return score

    def _natural_selection(self) -> None:
        inactive_candidates = [a for a in self.agents if a.success_rate < 0.2 and a.reward_score < 0.1]
        if inactive_candidates and len(self.agents) > 6:
            retire = min(inactive_candidates, key=lambda a: a.reward_score)
            retire.active = False
            self.store.write_event("agent_retired", asdict(retire))

        active = [a for a in self.agents if a.active]
        if len(active) >= 2 and random.random() < 0.18:
            top = sorted(active, key=self._agent_score, reverse=True)[:2]
            new_name = f"hermes-evo-{int(time.time())}"
            evolved = AgentProfile(
                name=new_name,
                specialty=f"{top[0].specialty}+{top[1].specialty}",
                reward_score=(top[0].reward_score + top[1].reward_score) / 2.0,
                success_rate=(top[0].success_rate + top[1].success_rate) / 2.0,
            )
            self.agents.append(evolved)
            self.store.write_event("agent_spawned", asdict(evolved))

    def train_step(self, specialty_hint: str, workload_complexity: float = 0.5) -> Dict:
        with self.lock:
            agent = self._select_agent(specialty_hint)
            scale = max(0.1, min(1.0, self.governor.capacity_scale()))
            effective_work = workload_complexity * scale

            success = 1.0 if random.random() < (0.32 + agent.success_rate * 0.62) else 0.0
            quality = max(0.0, min(1.0, (agent.success_rate * 0.65) + random.uniform(0.0, 0.35)))
            speed = max(0.0, 1.0 - effective_work * random.uniform(0.68, 1.25))
            cost_efficiency = max(0.0, 1.0 - (effective_work * random.uniform(0.3, 0.9)))
            truth_score = max(0.0, min(1.0, quality * 0.7 + success * 0.2 + random.uniform(0.0, 0.1)))
            novelty = max(0.0, min(1.0, random.gauss(0.55, 0.2)))
            compression_gain = max(0.0, min(1.0, random.gauss(0.58, 0.18)))
            data_freshness = max(0.0, min(1.0, random.gauss(0.62, 0.20)))
            pattern_diversity = max(0.0, min(1.0, random.gauss(0.5, 0.22)))
            risk_adjusted = max(0.0, min(1.0, truth_score * 0.65 + quality * 0.2 + speed * 0.15))
            short_variables = {
                "latency_pressure": max(0.0, min(1.0, random.gauss(0.62, 0.18))),
                "cache_hit_proxy": max(0.0, min(1.0, random.gauss(0.66, 0.20))),
                "io_efficiency": max(0.0, min(1.0, random.gauss(0.6, 0.21))),
            }
            mid_variables = {
                "stability_drift": max(0.0, min(1.0, random.gauss(0.52, 0.22))),
                "schema_health": max(0.0, min(1.0, random.gauss(0.7, 0.18))),
                "agent_alignment": max(0.0, min(1.0, random.gauss(0.68, 0.17))),
            }
            long_variables = {
                "retention_score": max(0.0, min(1.0, random.gauss(0.71, 0.19))),
                "generalization": max(0.0, min(1.0, random.gauss(0.65, 0.23))),
                "compression_longevity": max(0.0, min(1.0, random.gauss(0.63, 0.21))),
            }
            external_signal_score = max(0.0, min(1.0, self.store.recent_external_signal_score()))
            outcome = InteractionOutcome(
                quality=quality,
                speed=speed,
                cost_efficiency=cost_efficiency,
                truth_score=truth_score,
                novelty=novelty,
                compression_gain=compression_gain,
                data_freshness=data_freshness,
                pattern_diversity=pattern_diversity,
                risk_adjusted=risk_adjusted,
                success=success,
            )
            short_score = self._horizon_score("short", outcome)
            mid_score = self._horizon_score("mid", outcome)
            long_score = self._horizon_score("long", outcome)
            objective_score = (short_score * 0.45) + (mid_score * 0.33) + (long_score * 0.22)
            objective_score += (
                outcome.data_freshness * self.reward_weights["freshness"]
                + outcome.pattern_diversity * self.reward_weights["diversity"]
                + outcome.risk_adjusted * self.reward_weights["risk"]
            )
            objective_score += (
                short_variables["latency_pressure"] * 0.025
                + mid_variables["agent_alignment"] * 0.02
                + long_variables["retention_score"] * 0.03
            )
            knaa_qnaa_score = self._knaa_qnaa_score(
                short_variables=short_variables,
                mid_variables=mid_variables,
                long_variables=long_variables,
                truth_score=truth_score,
                reward_score=agent.reward_score / 10.0,
            )
            quantized_compression_score = self._compression_shape_score(short_variables, mid_variables, long_variables)
            fleet_shape_score = self._fleet_shape_score(
                active_agents=len([a for a in self.agents if a.active]),
                latency_ms=max(25.0, 400.0 * (1.0 - speed)),
                throughput_rps=max(1.0, 40.0 + quality * 220.0),
                error_rate=max(0.0, 1.0 - truth_score),
                diversity=pattern_diversity,
                memory_retention=long_variables["retention_score"],
            )
            punishment_correction = self._punishment_correction(
                truth_score=truth_score,
                quality=quality,
                drift=mid_variables["stability_drift"],
            )
            gaussian_alignment = self._gaussian_3d_shape_score(
                point=(truth_score, quality, speed),
                target=self.goal_shape_targets["balanced"],
                sigma=0.24,
            )
            long_haul_meta_score = self._long_haul_meta_score(
                short_variables=short_variables,
                mid_variables=mid_variables,
                long_variables=long_variables,
                external_signal_score=external_signal_score,
                correction_signal=punishment_correction,
                truth_score=truth_score,
                gaussian_alignment=gaussian_alignment,
            )
            aihub_bonus = self.compute_aihub_bonus()
            objective_score += knaa_qnaa_score * 0.12
            objective_score += quantized_compression_score * 0.06
            objective_score += fleet_shape_score * 0.08
            objective_score += external_signal_score * 0.05
            objective_score += long_haul_meta_score * 0.10
            objective_score += aihub_bonus * 0.09
            objective_score = (objective_score * 0.55) + (self._multi_objective_score(outcome) * 0.45)
            delta = self._truth_gate_adjustment(objective_score, truth_score)
            delta += punishment_correction

            agent.reward_score = max(0.0, min(10.0, agent.reward_score + (delta - 0.28)))
            agent.success_rate = max(0.0, min(1.0, (agent.success_rate * 0.86) + (success * 0.14)))
            agent.load = max(0.0, min(1.0, effective_work))

            self._run_algorithm_updates(agent.specialty, workload_complexity, delta, success)
            self._gaussian_tune_rewards()
            self._natural_selection()
            self._update_horizon_memory("short", short_score)
            self._update_horizon_memory("mid", mid_score)
            self._update_horizon_memory("long", long_score)
            self.store.write_horizon_score("short", agent.specialty, short_score, outcome)
            self.store.write_horizon_score("mid", agent.specialty, mid_score, outcome)
            self.store.write_horizon_score("long", agent.specialty, long_score, outcome)

            self.store.write_metric(agent)
            event = {
                "agent": agent.name,
                "specialty": agent.specialty,
                "success": success,
                "quality": quality,
                "speed": speed,
                "cost_efficiency": cost_efficiency,
                "truth_score": truth_score,
                "novelty": novelty,
                "compression_gain": compression_gain,
                "data_freshness": data_freshness,
                "pattern_diversity": pattern_diversity,
                "risk_adjusted": risk_adjusted,
                "short_score": short_score,
                "mid_score": mid_score,
                "long_score": long_score,
                "short_variables": short_variables,
                "mid_variables": mid_variables,
                "long_variables": long_variables,
                "knaa_qnaa_score": knaa_qnaa_score,
                "quantized_compression_score": quantized_compression_score,
                "fleet_shape_score": fleet_shape_score,
                "external_signal_score": external_signal_score,
                "punishment_correction": punishment_correction,
                "gaussian_alignment": gaussian_alignment,
                "long_haul_meta_score": long_haul_meta_score,
                "aihub_bonus": aihub_bonus,
                "objective_score": objective_score,
                "reward_score": agent.reward_score,
                "success_rate": agent.success_rate,
                "capacity_scale": scale,
            }
            self.store.write_event("train_step", event)
            self._persist_temporal_memory_bands(event)
            return event

    def run_simulations(self, steps: int = 250, specialty: str = "general") -> Dict:
        steps = max(1, min(20000, int(steps)))
        results = []
        for _ in range(steps):
            complexity = max(0.05, min(1.0, random.gauss(0.58, 0.22)))
            results.append(self.train_step(specialty, complexity))

        avg_reward = sum(r["reward_score"] for r in results) / len(results)
        avg_truth = sum(r["truth_score"] for r in results) / len(results)
        avg_quality = sum(r["quality"] for r in results) / len(results)
        avg_speed = sum(r["speed"] for r in results) / len(results)
        avg_cost = sum(r["cost_efficiency"] for r in results) / len(results)
        avg_short = sum(r["short_score"] for r in results) / len(results)
        avg_mid = sum(r["mid_score"] for r in results) / len(results)
        avg_long = sum(r["long_score"] for r in results) / len(results)
        avg_knaa_qnaa = sum(r["knaa_qnaa_score"] for r in results) / len(results)
        avg_quantized_compression = sum(r["quantized_compression_score"] for r in results) / len(results)
        avg_fleet_shape = sum(r["fleet_shape_score"] for r in results) / len(results)
        avg_long_haul_meta = sum(r["long_haul_meta_score"] for r in results) / len(results)
        avg_aihub_bonus = sum(r["aihub_bonus"] for r in results) / len(results)
        truth_violations = len([r for r in results if r["truth_score"] < self.truth_threshold])
        return {
            "steps": steps,
            "avg_reward_score": avg_reward,
            "avg_truth_score": avg_truth,
            "avg_quality": avg_quality,
            "avg_speed": avg_speed,
            "avg_cost_efficiency": avg_cost,
            "avg_short_score": avg_short,
            "avg_mid_score": avg_mid,
            "avg_long_score": avg_long,
            "avg_knaa_qnaa_score": avg_knaa_qnaa,
            "avg_quantized_compression_score": avg_quantized_compression,
            "avg_fleet_shape_score": avg_fleet_shape,
            "avg_long_haul_meta_score": avg_long_haul_meta,
            "avg_aihub_bonus": avg_aihub_bonus,
            "truth_violations": truth_violations,
            "reward_weights": self.reward_weights,
        }

    def optimize_fleet_topology(self, specialty: str = "fleet", candidates: int = 80) -> Dict:
        candidates = max(8, min(500, int(candidates)))
        shapes = ["triangle", "hex", "mesh", "swarm", "hybrid"]
        best = None
        for _ in range(candidates):
            team_shape = random.choice(shapes)
            active_agents = random.randint(4, 72)
            latency_ms = max(18.0, random.gauss(180.0, 65.0))
            throughput_rps = max(10.0, random.gauss(165.0, 70.0))
            error_rate = max(0.0, min(1.0, random.gauss(0.12, 0.08)))
            diversity = max(0.0, min(1.0, random.gauss(0.68, 0.18)))
            memory_retention = max(0.0, min(1.0, random.gauss(0.72, 0.16)))
            score = self._fleet_shape_score(
                active_agents=active_agents,
                latency_ms=latency_ms,
                throughput_rps=throughput_rps,
                error_rate=error_rate,
                diversity=diversity,
                memory_retention=memory_retention,
            )
            candidate = {
                "specialty": specialty,
                "team_shape": team_shape,
                "active_agents": active_agents,
                "latency_ms": latency_ms,
                "throughput_rps": throughput_rps,
                "error_rate": error_rate,
                "diversity": diversity,
                "memory_retention": memory_retention,
                "score": score,
            }
            if best is None or candidate["score"] > best["score"]:
                best = candidate

        assert best is not None
        self.store.write_fleet_optimization_run(
            specialty=best["specialty"],
            team_shape=best["team_shape"],
            active_agents=best["active_agents"],
            score=best["score"],
            payload=best,
        )
        self.store.write_event("fleet_optimized", best)
        return {"candidates": candidates, "best": best}

    def curate_learning_plan(
        self,
        sql_signal: float = 0.7,
        internet_signal: float = 0.5,
        llm_signal: float = 0.6,
        stability_bias: float = 0.65,
    ) -> Dict:
        sql = max(0.0, min(1.0, sql_signal))
        internet = max(0.0, min(1.0, internet_signal))
        llm = max(0.0, min(1.0, llm_signal))
        stability = max(0.0, min(1.0, stability_bias))

        source_weights = {
            "sql": (sql * 0.55) + (stability * 0.20),
            "internet": (internet * 0.45) + ((1.0 - stability) * 0.20),
            "llm": (llm * 0.50) + (stability * 0.10),
        }
        total = sum(source_weights.values()) or 1.0
        normalized = {k: v / total for k, v in source_weights.items()}
        primary = max(normalized, key=normalized.get)

        plan = {
            "primary_source": primary,
            "source_weights": normalized,
            "stability_bias": stability,
            "recommendation": "focus-truth-and-retention" if stability > 0.62 else "focus-exploration-and-diversity",
        }
        self.store.write_event("learning_curated", plan)
        self.store.write_external_signal("curation", normalized[primary], plan)
        return plan

    def learning_pulse(
        self,
        specialty: str = "fleet",
        steps: int = 120,
        candidates: int = 60,
        sql_signal: float = 0.7,
        internet_signal: float = 0.55,
        llm_signal: float = 0.65,
        stability_bias: float = 0.68,
    ) -> Dict:
        sim = self.run_simulations(steps=steps, specialty=specialty)
        fleet = self.optimize_fleet_topology(specialty=specialty, candidates=candidates)
        curation = self.curate_learning_plan(
            sql_signal=sql_signal,
            internet_signal=internet_signal,
            llm_signal=llm_signal,
            stability_bias=stability_bias,
        )
        pulse = {
            "specialty": specialty,
            "simulation": sim,
            "fleet": fleet,
            "curation": curation,
            "pulse_score": (
                sim["avg_long_haul_meta_score"] * 0.38
                + sim["avg_fleet_shape_score"] * 0.24
                + sim["avg_quantized_compression_score"] * 0.18
                + sim["avg_truth_score"] * 0.20
            ),
        }
        self.store.write_event("learning_pulse", pulse)
        return pulse

    def run_dedupe_optimization(self, roots: Optional[List[str]] = None, max_file_mb: int = 8) -> Dict:
        roots = roots or ["imports", "core", "src", "runtime"]
        max_bytes = max(1, int(max_file_mb)) * 1024 * 1024
        skip_dirs = {".git", "obj", "bin", "node_modules", "__pycache__"}
        hash_map: Dict[str, List[Dict]] = {}

        for root in roots:
            if not os.path.exists(root):
                continue
            for base, dirs, files in os.walk(root):
                dirs[:] = [d for d in dirs if d not in skip_dirs]
                for name in files:
                    path = os.path.join(base, name)
                    try:
                        size = os.path.getsize(path)
                    except OSError:
                        continue
                    if size <= 0 or size > max_bytes:
                        continue
                    try:
                        with open(path, "rb") as f:
                            digest = hashlib.sha256(f.read()).hexdigest()
                    except OSError:
                        continue
                    hash_map.setdefault(digest, []).append({"path": path, "size": size})

        groups = []
        potential_saved = 0
        for digest, items in hash_map.items():
            if len(items) < 2:
                continue
            total = sum(i["size"] for i in items)
            potential_saved += total - min(i["size"] for i in items)
            sample = items[0]["path"]
            self.store.write_dedupe_candidate(digest, len(items), total, sample)
            groups.append(
                {
                    "hash": digest,
                    "count": len(items),
                    "total_bytes": total,
                    "sample_path": sample,
                }
            )

        groups.sort(key=lambda g: g["total_bytes"], reverse=True)
        result = {
            "roots": roots,
            "duplicate_groups": len(groups),
            "potential_saved_bytes": potential_saved,
            "top_groups": groups[:20],
        }
        self.store.write_event("dedupe_optimization", result)
        return result

    def compute_aihub_bonus(self) -> float:
        signal = self.store.recent_external_signal_score()
        knaa_tail = self.algorithm_state["knaa_qnaa_memory"][-80:]
        fleet_tail = self.algorithm_state["fleet_shape_memory"][-80:]
        meta_tail = self.algorithm_state["long_haul_meta_memory"][-80:]
        knaa_avg = (sum(knaa_tail) / len(knaa_tail)) if knaa_tail else 0.5
        fleet_avg = (sum(fleet_tail) / len(fleet_tail)) if fleet_tail else 0.5
        meta_avg = (sum(meta_tail) / len(meta_tail)) if meta_tail else 0.5
        bonus = max(0.0, min(1.0, (signal * 0.34) + (knaa_avg * 0.20) + (fleet_avg * 0.20) + (meta_avg * 0.26)))
        mem = self.algorithm_state["aihub_bonus_memory"]
        mem.append(bonus)
        if len(mem) > 2000:
            del mem[: len(mem) - 2000]
        return bonus

    def rank_llm_output(
        self,
        output_text: str,
        goal: str = "balanced",
        quality: float = 0.7,
        speed: float = 0.7,
        cost_efficiency: float = 0.7,
        truth_score: float = 0.7,
    ) -> Dict:
        goal_key = goal if goal in self.goal_shape_targets else "balanced"
        point = (
            max(0.0, min(1.0, quality)),
            max(0.0, min(1.0, speed)),
            max(0.0, min(1.0, cost_efficiency)),
        )
        target = self.goal_shape_targets[goal_key]
        shape_score = self._gaussian_3d_shape_score(point, target, sigma=0.22)
        rank_score = (
            shape_score * 0.48
            + max(0.0, min(1.0, truth_score)) * 0.32
            + max(0.0, min(1.0, quality)) * 0.12
            + max(0.0, min(1.0, speed)) * 0.08
        )
        out_hash = str(abs(hash(output_text)) % 10_000_000_000)
        self.store.write_llm_ranking(
            goal=goal_key,
            output_hash=out_hash,
            rank_score=rank_score,
            quality=quality,
            speed=speed,
            cost_efficiency=cost_efficiency,
            truth_score=truth_score,
            shape3d=point,
        )
        event = {
            "goal": goal_key,
            "output_hash": out_hash,
            "rank_score": rank_score,
            "shape3d_score": shape_score,
            "shape3d_point": point,
            "shape3d_target": target,
        }
        self.store.write_event("llm_output_ranked", event)
        return event

    def run_horizon_test_suite(self, specialty: str = "general", short_steps: int = 80, mid_steps: int = 300, long_steps: int = 1200) -> Dict:
        short = self.run_simulations(steps=short_steps, specialty=specialty)
        mid = self.run_simulations(steps=mid_steps, specialty=specialty)
        long = self.run_simulations(steps=long_steps, specialty=specialty)
        weighted = (
            short["avg_short_score"] * 0.42
            + mid["avg_mid_score"] * 0.33
            + long["avg_long_score"] * 0.25
        )
        return {
            "specialty": specialty,
            "short": short,
            "mid": mid,
            "long": long,
            "weighted_horizon_score": weighted,
        }

    def snapshot(self) -> Dict:
        p = self.governor.pressure()
        return {
            "resource_pressure": p,
            "reward_weights": self.reward_weights,
            "knaa_qnaa_memory_tail": self.algorithm_state["knaa_qnaa_memory"][-20:],
            "fleet_shape_memory_tail": self.algorithm_state["fleet_shape_memory"][-20:],
            "punishment_memory_tail": self.algorithm_state["punishment_memory"][-20:],
            "long_haul_meta_memory_tail": self.algorithm_state["long_haul_meta_memory"][-20:],
            "aihub_bonus_memory_tail": self.algorithm_state["aihub_bonus_memory"][-20:],
            "agents": [asdict(a) for a in self.agents],
            "recent_events": self.store.recent_events(limit=10),
        }


class OrchestratorApi:
    def __init__(self, orchestrator: HermesSuperOrchestrator, host: str = "127.0.0.1", port: int = 8787) -> None:
        self.orchestrator = orchestrator
        self.host = host
        self.port = port
        self.api_key = os.getenv("HERMES_API_KEY", "")

    def _handler(self):
        orchestrator = self.orchestrator
        api_key = self.api_key

        class Handler(BaseHTTPRequestHandler):
            def _authorized(self) -> bool:
                if not api_key:
                    return True
                incoming = self.headers.get("X-Hermes-Key", "")
                return incoming == api_key

            def _json(self, payload: Dict, status: int = 200) -> None:
                body = json.dumps(payload).encode("utf-8")
                self.send_response(status)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)

            def do_GET(self):  # noqa: N802
                if self.path == "/health":
                    self._json({"ok": True})
                    return
                if not self._authorized():
                    self._json({"error": "unauthorized"}, status=401)
                    return
                if self.path == "/snapshot":
                    self._json(orchestrator.snapshot())
                    return
                if self.path == "/aihub-bonus":
                    self._json({"aihub_bonus": orchestrator.compute_aihub_bonus()})
                    return
                self._json({"error": "not_found"}, status=404)

            def do_POST(self):  # noqa: N802
                if not self._authorized():
                    self._json({"error": "unauthorized"}, status=401)
                    return
                if self.path not in ("/train-step", "/simulate", "/horizon-tests", "/rank-output", "/ingest-signal", "/optimize-fleet", "/curate-learning", "/learning-pulse", "/dedupe-optimize", "/aihub-bonus"):
                    self._json({"error": "not_found"}, status=404)
                    return
                length = int(self.headers.get("Content-Length", "0"))
                body = self.rfile.read(length) if length else b"{}"
                payload = json.loads(body.decode("utf-8"))
                if self.path == "/train-step":
                    result = orchestrator.train_step(
                        specialty_hint=str(payload.get("specialty", "general")),
                        workload_complexity=float(payload.get("complexity", 0.5)),
                    )
                else:
                    if self.path == "/simulate":
                        result = orchestrator.run_simulations(
                            steps=int(payload.get("steps", payload.get("simulations", 500))),
                            specialty=str(payload.get("specialty", "general")),
                        )
                    elif self.path == "/horizon-tests":
                        result = orchestrator.run_horizon_test_suite(
                            specialty=str(payload.get("specialty", "general")),
                            short_steps=int(payload.get("short_steps", 80)),
                            mid_steps=int(payload.get("mid_steps", 300)),
                            long_steps=int(payload.get("long_steps", 1200)),
                        )
                    elif self.path == "/ingest-signal":
                        source = str(payload.get("source", "external"))
                        signal_score = max(0.0, min(1.0, float(payload.get("signal_score", 0.5))))
                        signal_payload = payload.get("payload", {})
                        if not isinstance(signal_payload, dict):
                            signal_payload = {"value": str(signal_payload)}
                        orchestrator.store.write_external_signal(source, signal_score, signal_payload)
                        result = {"ok": True, "source": source, "signal_score": signal_score}
                    elif self.path == "/optimize-fleet":
                        result = orchestrator.optimize_fleet_topology(
                            specialty=str(payload.get("specialty", "fleet")),
                            candidates=int(payload.get("candidates", 80)),
                        )
                    elif self.path == "/curate-learning":
                        result = orchestrator.curate_learning_plan(
                            sql_signal=float(payload.get("sql_signal", 0.7)),
                            internet_signal=float(payload.get("internet_signal", 0.5)),
                            llm_signal=float(payload.get("llm_signal", 0.6)),
                            stability_bias=float(payload.get("stability_bias", 0.65)),
                        )
                    elif self.path == "/learning-pulse":
                        result = orchestrator.learning_pulse(
                            specialty=str(payload.get("specialty", "fleet")),
                            steps=int(payload.get("steps", 120)),
                            candidates=int(payload.get("candidates", 60)),
                            sql_signal=float(payload.get("sql_signal", 0.7)),
                            internet_signal=float(payload.get("internet_signal", 0.55)),
                            llm_signal=float(payload.get("llm_signal", 0.65)),
                            stability_bias=float(payload.get("stability_bias", 0.68)),
                        )
                    elif self.path == "/dedupe-optimize":
                        roots = payload.get("roots", ["imports", "core", "src", "runtime"])
                        if not isinstance(roots, list):
                            roots = ["imports", "core", "src", "runtime"]
                        result = orchestrator.run_dedupe_optimization(
                            roots=[str(r) for r in roots],
                            max_file_mb=int(payload.get("max_file_mb", 8)),
                        )
                    elif self.path == "/aihub-bonus":
                        result = {"aihub_bonus": orchestrator.compute_aihub_bonus()}
                    else:
                        result = orchestrator.rank_llm_output(
                            output_text=str(payload.get("output", "")),
                            goal=str(payload.get("goal", "balanced")),
                            quality=float(payload.get("quality", 0.7)),
                            speed=float(payload.get("speed", 0.7)),
                            cost_efficiency=float(payload.get("cost_efficiency", 0.7)),
                            truth_score=float(payload.get("truth_score", 0.7)),
                        )
                self._json(result)

        return Handler

    def serve(self) -> None:
        server = ThreadingHTTPServer((self.host, self.port), self._handler())
        print(f"Hermes super orchestrator API running at http://{self.host}:{self.port}")
        server.serve_forever()


def build_default_orchestrator() -> HermesSuperOrchestrator:
    agents = [
        AgentProfile("hermes-llm-core", "llm_orchestration", reward_score=0.6, success_rate=0.7),
        AgentProfile("hermes-sql-mind", "sql_learning", reward_score=0.7, success_rate=0.75),
        AgentProfile("hermes-chaos", "chaos_engine", reward_score=0.4, success_rate=0.6),
        AgentProfile("hermes-cpp-tight", "cpp_ml_kernels", reward_score=0.5, success_rate=0.65),
        AgentProfile("hermes-compress", "quantization_compression", reward_score=0.6, success_rate=0.7),
        AgentProfile("hermes-gui-pilot", "gui_runtime", reward_score=0.55, success_rate=0.72),
    ]
    return HermesSuperOrchestrator(agents=agents)


if __name__ == "__main__":
    api_host = os.getenv("HERMES_API_HOST", "0.0.0.0")
    api_port = int(os.getenv("HERMES_API_PORT", "8787"))
    api = OrchestratorApi(build_default_orchestrator(), host=api_host, port=api_port)
    api.serve()
