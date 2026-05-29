# hermes_agent_power_control.py
# Adjust agent power (resources, model size, parallelism)

class HermesAgentPowerControl:
    def __init__(self, agent):
        self.agent = agent

    def set_cpu_cores(self, cores):
        self.agent.cpu_cores = cores

    def set_memory_gb(self, gb):
        self.agent.memory_gb = gb

    def set_model_size(self, size):
        self.agent.model_size = size  # e.g., 'small', 'medium', 'large'

    def enable_parallel_training(self, enabled=True):
        self.agent.parallel_training = enabled

# Example usage:
# power = HermesAgentPowerControl(agent)
# power.set_cpu_cores(4)
# power.set_memory_gb(8)
# power.set_model_size('large')
# power.enable_parallel_training(True)
