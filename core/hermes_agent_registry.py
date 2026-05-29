# hermes_agent_registry.py
# Persistent registry for named Hermes agents

import pickle
import os

REGISTRY_PATH = os.path.join(os.path.dirname(__file__), 'hermes_agent_registry.pkl')

def save_agent_to_registry(agent, name):
    registry = load_registry()
    registry[name] = agent
    with open(REGISTRY_PATH, 'wb') as f:
        pickle.dump(registry, f)

def load_agent_from_registry(name):
    registry = load_registry()
    return registry.get(name)

def load_registry():
    if os.path.exists(REGISTRY_PATH):
        with open(REGISTRY_PATH, 'rb') as f:
            return pickle.load(f)
    return {}

# Example usage:
# save_agent_to_registry(agent, 'Hermes1')
# agent2 = load_agent_from_registry('Hermes1')
