# Hermes Agent Scaler
import os, multiprocessing
import psutil

def get_max_agents():
    cpu_count = multiprocessing.cpu_count()
    mem = psutil.virtual_memory().total // (1024*1024*1024)  # GB
    # Example: 1 agent per 2 CPUs, 1 per 2GB RAM
    return min(cpu_count // 2, mem // 2)

if __name__ == "__main__":
    print(f"Max Hermes agents supported: {get_max_agents()}")
