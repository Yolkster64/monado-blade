# Hermes Chaos Engine (Python)
import random, time

def inject_fault(agent):
    print(f"Injecting fault into {agent}")
    # Simulate random failure
    if random.random() < 0.2:
        print(f"{agent} failed! Recovering...")
        time.sleep(0.5)
        print(f"{agent} recovered.")
    else:
        print(f"{agent} passed chaos test.")

if __name__ == "__main__":
    agents = [f"Hermes-{i}" for i in range(1, 16)]
    for agent in agents:
        inject_fault(agent)
