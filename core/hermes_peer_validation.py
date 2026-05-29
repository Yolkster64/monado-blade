# hermes_peer_validation.py
# Agents cross-check results with each other

class PeerValidator:
    def __init__(self, agents):
        self.agents = agents

    def validate(self, task, data):
        results = [agent.execute(task, data) for agent in self.agents]
        consensus = max(set(results), key=results.count)
        return consensus, results

# Example: PeerValidator([agent1, agent2, agent3]).validate('classify', data)
