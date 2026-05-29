# hermes_adversarial_training.py
# Agents train against each other (adversarial/competitive)

import random

class AdversarialTrainer:
    def __init__(self, agent_a, agent_b, env):
        self.agent_a = agent_a
        self.agent_b = agent_b
        self.env = env

    def train(self, episodes=10):
        results = []
        for _ in range(episodes):
            outcome = self.env.compete(self.agent_a, self.agent_b)
            results.append(outcome)
            self.agent_a.learn(outcome)
            self.agent_b.learn(-outcome)
        return results

# Example: AdversarialTrainer(agent1, agent2, env).train()
