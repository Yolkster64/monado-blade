# hermes_data_workflow.py
# Data-driven workflow manager for Hermes agents

class DataWorkflowManager:
    def __init__(self, sql_server, agents):
        self.sql_server = sql_server
        self.agents = agents
    def assign_data_to_agent(self, agent_name, query):
        data = self.sql_server.fetch_data(query)
        agent = next(a for a in self.agents if a['name'] == agent_name)
        agent['data'] = data
        return data
    def run_agent_on_data(self, agent_name, task):
        agent = next(a for a in self.agents if a['name'] == agent_name)
        if 'data' in agent:
            return agent['object'].assign_task(f'{task} on {agent["data"][:5]}...')
        return 'No data assigned.'
    def agent_triggered_data_update(self, agent_name, query):
        # Agent can call this to fetch latest data
        return self.assign_data_to_agent(agent_name, query)

