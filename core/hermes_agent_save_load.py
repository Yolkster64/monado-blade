# hermes_agent_save_load.py
# Save/load Hermes agent state to/from file

import pickle
import os

def save_agent(agent, path):
    with open(path, 'wb') as f:
        pickle.dump(agent, f)

def load_agent(path):
    with open(path, 'rb') as f:
        return pickle.load(f)

# Example usage:
# save_agent(agent_obj, 'agent1.pkl')
# agent2 = load_agent('agent1.pkl')
