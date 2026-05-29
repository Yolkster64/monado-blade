// Hermes Chaos Engine (C++)
#include <iostream>
#include <random>
#include <thread>
#include <vector>
#include <chrono>

void inject_fault(const std::string& agent) {
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_real_distribution<> dis(0, 1);
    std::cout << "Injecting fault into " << agent << std::endl;
    if (dis(gen) < 0.2) {
        std::cout << agent << " failed! Recovering..." << std::endl;
        std::this_thread::sleep_for(std::chrono::milliseconds(500));
        std::cout << agent << " recovered." << std::endl;
    } else {
        std::cout << agent << " passed chaos test." << std::endl;
    }
}

int main() {
    std::vector<std::string> agents;
    for (int i = 1; i <= 15; ++i) agents.push_back("Hermes-" + std::to_string(i));
    for (const auto& agent : agents) inject_fault(agent);
    return 0;
}
