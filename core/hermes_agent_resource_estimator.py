# hermes_agent_resource_estimator.py
# Estimates max agent count and resource usage

import psutil
import multiprocessing
import os

def estimate_max_agents(agent_ram_gb=1.0, agent_cpu_cores=1):
    total_ram = psutil.virtual_memory().total / (1024**3)
    total_cores = multiprocessing.cpu_count()
    max_by_ram = int(total_ram // agent_ram_gb)
    max_by_cpu = int(total_cores // agent_cpu_cores)
    return min(max_by_ram, max_by_cpu)

if __name__ == "__main__":
    print("Max agents:", estimate_max_agents())
