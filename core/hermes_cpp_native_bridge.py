import ctypes
from ctypes import c_double, c_size_t
from pathlib import Path
from typing import Optional


class HermesCppNativeBridge:
    def __init__(self, dll_path: Optional[str] = None) -> None:
        self.dll_path = dll_path or str(Path("core/native/hermes_learning_kernel.dll"))
        self._dll = None
        self._load()

    def _load(self) -> None:
        dll = Path(self.dll_path)
        if not dll.exists():
            return
        self._dll = ctypes.CDLL(str(dll))
        self._dll.hermes_reward_update.argtypes = [
            c_double,
            c_double,
            c_double,
            c_double,
            c_double,
            ctypes.POINTER(c_double),
            c_size_t,
        ]
        self._dll.hermes_reward_update.restype = c_double
        self._dll.hermes_gaussian_3d_score.argtypes = [
            c_double,
            c_double,
            c_double,
            c_double,
            c_double,
            c_double,
            c_double,
        ]
        self._dll.hermes_gaussian_3d_score.restype = c_double
        try:
            self._dll.hermes_knaa_qnaa_score.argtypes = [
                ctypes.POINTER(c_double),
                c_size_t,
                ctypes.POINTER(c_double),
                c_size_t,
                ctypes.POINTER(c_double),
                c_size_t,
                c_double,
                c_double,
                c_double,
            ]
            self._dll.hermes_knaa_qnaa_score.restype = c_double
        except AttributeError:
            pass
        try:
            self._dll.hermes_fleet_shape_score.argtypes = [
                c_double,
                c_double,
                c_double,
                c_double,
                c_double,
                c_double,
            ]
            self._dll.hermes_fleet_shape_score.restype = c_double
        except AttributeError:
            pass
        try:
            self._dll.hermes_quantized_compression_score.argtypes = [
                ctypes.POINTER(c_double),
                c_size_t,
            ]
            self._dll.hermes_quantized_compression_score.restype = c_double
        except AttributeError:
            pass
        try:
            self._dll.hermes_long_haul_meta_score.argtypes = [
                ctypes.POINTER(c_double),
                c_size_t,
                ctypes.POINTER(c_double),
                c_size_t,
                ctypes.POINTER(c_double),
                c_size_t,
                c_double,
                c_double,
                c_double,
                c_double,
            ]
            self._dll.hermes_long_haul_meta_score.restype = c_double
        except AttributeError:
            pass

    @property
    def available(self) -> bool:
        return self._dll is not None

    def reward_update(
        self,
        quality: float,
        speed: float,
        cost_efficiency: float,
        truth_score: float,
        novelty: float,
        weights: list[float],
    ) -> float:
        if not self._dll or not hasattr(self._dll, "hermes_reward_update"):
            score = (
                quality * weights[0]
                + speed * weights[1]
                + cost_efficiency * weights[2]
                + truth_score * weights[3]
                + novelty * weights[4]
            )
            if truth_score < 0.68:
                score -= (0.68 - truth_score) * 1.5
            return score

        arr = (c_double * len(weights))(*weights)
        return float(
            self._dll.hermes_reward_update(
                c_double(quality),
                c_double(speed),
                c_double(cost_efficiency),
                c_double(truth_score),
                c_double(novelty),
                arr,
                c_size_t(len(weights)),
            )
        )

    def gaussian_3d_score(
        self,
        x: float,
        y: float,
        z: float,
        target_x: float,
        target_y: float,
        target_z: float,
        sigma: float = 0.2,
    ) -> float:
        if not self._dll:
            dx = x - target_x
            dy = y - target_y
            dz = z - target_z
            dist2 = dx * dx + dy * dy + dz * dz
            safe_sigma = max(0.000001, sigma)
            return pow(2.718281828, -(dist2 / (2 * safe_sigma * safe_sigma)))

        return float(
            self._dll.hermes_gaussian_3d_score(
                c_double(x),
                c_double(y),
                c_double(z),
                c_double(target_x),
                c_double(target_y),
                c_double(target_z),
                c_double(sigma),
            )
        )

    def knaa_qnaa_score(
        self,
        short_values: list[float],
        mid_values: list[float],
        long_values: list[float],
        truth_score: float,
        reward_score: float,
        exploration_rate: float = 0.1,
    ) -> float:
        if not self._dll:
            short_avg = sum(short_values) / max(1, len(short_values))
            mid_avg = sum(mid_values) / max(1, len(mid_values))
            long_avg = sum(long_values) / max(1, len(long_values))
            knaa = (short_avg * 0.42) + (mid_avg * 0.34) + (long_avg * 0.24)
            qnaa = (
                (1.0 / (1.0 + pow(2.718281828, -((truth_score - 0.5) * 7.0)))) * 0.62
                + (1.0 / (1.0 + pow(2.718281828, -((reward_score - 0.5) * 5.0)))) * 0.38
            )
            explore = max(0.0, min(1.0, exploration_rate)) * 0.14
            return max(0.0, min(1.0, (knaa * 0.64) + (qnaa * 0.36) + explore))

        short_arr = (c_double * len(short_values))(*short_values)
        mid_arr = (c_double * len(mid_values))(*mid_values)
        long_arr = (c_double * len(long_values))(*long_values)
        return float(
            self._dll.hermes_knaa_qnaa_score(
                short_arr,
                c_size_t(len(short_values)),
                mid_arr,
                c_size_t(len(mid_values)),
                long_arr,
                c_size_t(len(long_values)),
                c_double(truth_score),
                c_double(reward_score),
                c_double(exploration_rate),
            )
        )

    def fleet_shape_score(
        self,
        active_agents: float,
        latency_ms: float,
        throughput_rps: float,
        error_rate: float,
        diversity: float,
        memory_retention: float,
    ) -> float:
        if not self._dll or not hasattr(self._dll, "hermes_fleet_shape_score"):
            agent_factor = 1.0 / (1.0 + pow(2.718281828, -((active_agents - 8.0) * 0.28)))
            latency_factor = 1.0 / (1.0 + pow(2.718281828, -((220.0 - latency_ms) / 45.0)))
            throughput_factor = 1.0 / (1.0 + pow(2.718281828, -((throughput_rps - 140.0) / 30.0)))
            reliability = 1.0 - max(0.0, min(1.0, error_rate))
            diversity_factor = max(0.0, min(1.0, diversity))
            retention_factor = max(0.0, min(1.0, memory_retention))
            return max(
                0.0,
                min(
                    1.0,
                    (agent_factor * 0.20)
                    + (latency_factor * 0.22)
                    + (throughput_factor * 0.22)
                    + (reliability * 0.18)
                    + (diversity_factor * 0.08)
                    + (retention_factor * 0.10),
                ),
            )

        return float(
            self._dll.hermes_fleet_shape_score(
                c_double(active_agents),
                c_double(latency_ms),
                c_double(throughput_rps),
                c_double(error_rate),
                c_double(diversity),
                c_double(memory_retention),
            )
        )

    def quantized_compression_score(self, values: list[float]) -> float:
        if not values:
            return 0.0
        if not self._dll or not hasattr(self._dll, "hermes_quantized_compression_score"):
            min_v = min(values)
            max_v = max(values)
            r = max(0.000001, max_v - min_v)
            deq = []
            for v in values:
                norm = max(0.0, min(1.0, (v - min_v) / r))
                q = round(norm * 255.0)
                deq.append(min_v + (q / 255.0) * r)
            mse = sum((a - b) ** 2 for a, b in zip(values, deq)) / len(values)
            fidelity = 1.0 / (1.0 + mse * 200.0)
            compression_ratio = 1.0 - (8.0 / 64.0)
            return max(0.0, min(1.0, (fidelity * 0.72) + (compression_ratio * 0.28)))

        arr = (c_double * len(values))(*values)
        return float(self._dll.hermes_quantized_compression_score(arr, c_size_t(len(values))))

    def long_haul_meta_score(
        self,
        short_values: list[float],
        mid_values: list[float],
        long_values: list[float],
        external_signal_score: float,
        correction_signal: float,
        truth_score: float,
        gaussian_alignment: float,
    ) -> float:
        if not self._dll or not hasattr(self._dll, "hermes_long_haul_meta_score"):
            short_avg = sum(short_values) / max(1, len(short_values))
            mid_avg = sum(mid_values) / max(1, len(mid_values))
            long_avg = sum(long_values) / max(1, len(long_values))
            horizon_stability = (short_avg * 0.20) + (mid_avg * 0.33) + (long_avg * 0.47)
            signal_quality = (
                max(0.0, min(1.0, external_signal_score)) * 0.34
                + max(0.0, min(1.0, truth_score)) * 0.41
                + max(0.0, min(1.0, gaussian_alignment)) * 0.25
            )
            correction = 0.5 + max(-1.0, min(1.0, correction_signal)) * 0.5
            return max(0.0, min(1.0, (horizon_stability * 0.58) + (signal_quality * 0.30) + (correction * 0.12)))

        short_arr = (c_double * len(short_values))(*short_values)
        mid_arr = (c_double * len(mid_values))(*mid_values)
        long_arr = (c_double * len(long_values))(*long_values)
        return float(
            self._dll.hermes_long_haul_meta_score(
                short_arr,
                c_size_t(len(short_values)),
                mid_arr,
                c_size_t(len(mid_values)),
                long_arr,
                c_size_t(len(long_values)),
                c_double(external_signal_score),
                c_double(correction_signal),
                c_double(truth_score),
                c_double(gaussian_alignment),
            )
        )
