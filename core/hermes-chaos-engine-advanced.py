# Advanced Hermes Chaos Engine (Python)
import random, time, logging

def inject_fault(agent):
    logging.info(f"Injecting fault into {agent}")
    if random.random() < 0.2:
        logging.warning(f"{agent} failed! Recovering...")
        time.sleep(0.5)
        logging.info(f"{agent} recovered.")
    else:
        logging.info(f"{agent} passed chaos test.")

def run_chaos_tests(agents):
    for agent in agents:
        inject_fault(agent)

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    agents = [f"Hermes-{i}" for i in range(1, 16)]
    run_chaos_tests(agents)
