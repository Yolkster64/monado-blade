#include <algorithm>
#include <cmath>
#include <cstddef>
#include <vector>

namespace {
double sigmoid(double x) {
    return 1.0 / (1.0 + std::exp(-x));
}

double mean_or_zero(const double* values, std::size_t len) {
    if (values == nullptr || len == 0) {
        return 0.0;
    }
    double sum = 0.0;
    for (std::size_t i = 0; i < len; ++i) {
        sum += values[i];
    }
    return sum / static_cast<double>(len);
}
}

extern "C" {

__declspec(dllexport) double hermes_reward_update(
    double quality,
    double speed,
    double cost_efficiency,
    double truth_score,
    double novelty,
    double* weights,
    std::size_t weight_len) {
    if (weights == nullptr || weight_len < 5) {
        return 0.0;
    }

    const double values[5] = {
        quality,
        speed,
        cost_efficiency,
        truth_score,
        novelty
    };

    double score = 0.0;
    for (std::size_t i = 0; i < 5; ++i) {
        score += values[i] * weights[i];
    }

    // Truth gate penalty: suppress false-positive reward promotion.
    if (truth_score < 0.68) {
        score -= (0.68 - truth_score) * 1.5;
    }

    return score;
}

__declspec(dllexport) void hermes_quantize_float32(
    const float* input,
    std::size_t len,
    float min_value,
    float max_value,
    unsigned char* output) {
    if (input == nullptr || output == nullptr || len == 0) {
        return;
    }

    const float range = std::max(0.000001f, max_value - min_value);
    for (std::size_t i = 0; i < len; ++i) {
        float norm = (input[i] - min_value) / range;
        norm = std::clamp(norm, 0.0f, 1.0f);
        output[i] = static_cast<unsigned char>(std::round(norm * 255.0f));
    }
}

__declspec(dllexport) double hermes_gaussian_3d_score(
    double x,
    double y,
    double z,
    double target_x,
    double target_y,
    double target_z,
    double sigma) {
    const double safe_sigma = std::max(0.000001, sigma);
    const double dx = x - target_x;
    const double dy = y - target_y;
    const double dz = z - target_z;
    const double dist2 = dx * dx + dy * dy + dz * dz;
    return std::exp(-(dist2 / (2.0 * safe_sigma * safe_sigma)));
}

__declspec(dllexport) double hermes_knaa_qnaa_score(
    const double* short_values,
    std::size_t short_len,
    const double* mid_values,
    std::size_t mid_len,
    const double* long_values,
    std::size_t long_len,
    double truth_score,
    double reward_score,
    double exploration_rate) {
    const double short_avg = mean_or_zero(short_values, short_len);
    const double mid_avg = mean_or_zero(mid_values, mid_len);
    const double long_avg = mean_or_zero(long_values, long_len);

    const double knaa = (short_avg * 0.42) + (mid_avg * 0.34) + (long_avg * 0.24);
    const double qnaa = (sigmoid((truth_score - 0.5) * 7.0) * 0.62)
        + (sigmoid((reward_score - 0.5) * 5.0) * 0.38);
    const double explore = std::clamp(exploration_rate, 0.0, 1.0) * 0.14;
    return std::clamp((knaa * 0.64) + (qnaa * 0.36) + explore, 0.0, 1.0);
}

__declspec(dllexport) double hermes_fleet_shape_score(
    double active_agents,
    double latency_ms,
    double throughput_rps,
    double error_rate,
    double diversity,
    double memory_retention) {
    const double agent_factor = sigmoid((active_agents - 8.0) * 0.28);
    const double latency_factor = sigmoid((220.0 - latency_ms) / 45.0);
    const double throughput_factor = sigmoid((throughput_rps - 140.0) / 30.0);
    const double reliability = 1.0 - std::clamp(error_rate, 0.0, 1.0);
    const double diversity_factor = std::clamp(diversity, 0.0, 1.0);
    const double retention_factor = std::clamp(memory_retention, 0.0, 1.0);

    return std::clamp(
        (agent_factor * 0.20)
        + (latency_factor * 0.22)
        + (throughput_factor * 0.22)
        + (reliability * 0.18)
        + (diversity_factor * 0.08)
        + (retention_factor * 0.10),
        0.0,
        1.0
    );
}

__declspec(dllexport) double hermes_quantized_compression_score(
    const double* values,
    std::size_t len) {
    if (values == nullptr || len == 0) {
        return 0.0;
    }

    double min_v = values[0];
    double max_v = values[0];
    for (std::size_t i = 1; i < len; ++i) {
        min_v = std::min(min_v, values[i]);
        max_v = std::max(max_v, values[i]);
    }

    const double range = std::max(0.000001, max_v - min_v);
    std::vector<unsigned char> q(len, 0);
    std::vector<double> deq(len, 0.0);

    for (std::size_t i = 0; i < len; ++i) {
        const double norm = std::clamp((values[i] - min_v) / range, 0.0, 1.0);
        q[i] = static_cast<unsigned char>(std::round(norm * 255.0));
        deq[i] = min_v + (static_cast<double>(q[i]) / 255.0) * range;
    }

    double mse = 0.0;
    for (std::size_t i = 0; i < len; ++i) {
        const double d = values[i] - deq[i];
        mse += d * d;
    }
    mse /= static_cast<double>(len);

    const double fidelity = 1.0 / (1.0 + mse * 200.0);
    const double compression_ratio = 1.0 - (8.0 / 64.0); // 64-bit input to 8-bit
    return std::clamp((fidelity * 0.72) + (compression_ratio * 0.28), 0.0, 1.0);
}

__declspec(dllexport) double hermes_long_haul_meta_score(
    const double* short_values,
    std::size_t short_len,
    const double* mid_values,
    std::size_t mid_len,
    const double* long_values,
    std::size_t long_len,
    double external_signal_score,
    double correction_signal,
    double truth_score,
    double gaussian_alignment) {
    const double short_avg = mean_or_zero(short_values, short_len);
    const double mid_avg = mean_or_zero(mid_values, mid_len);
    const double long_avg = mean_or_zero(long_values, long_len);
    const double horizon_stability = (short_avg * 0.20) + (mid_avg * 0.33) + (long_avg * 0.47);

    const double signal_quality =
        (std::clamp(external_signal_score, 0.0, 1.0) * 0.34) +
        (std::clamp(truth_score, 0.0, 1.0) * 0.41) +
        (std::clamp(gaussian_alignment, 0.0, 1.0) * 0.25);

    const double correction = 0.5 + std::clamp(correction_signal, -1.0, 1.0) * 0.5;
    return std::clamp((horizon_stability * 0.58) + (signal_quality * 0.30) + (correction * 0.12), 0.0, 1.0);
}

}
