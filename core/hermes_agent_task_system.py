# hermes_agent_task_system.py
# Assign and execute tasks/instructions for Hermes agents

class AgentTaskSystem:
    def __init__(self, agent):
        self.agent = agent
        self.task_history = []

    def assign_task(self, instruction):
        self.task_history.append(instruction)
        return self.agent.execute(instruction)

# Example: AgentTaskSystem(agent).assign_task('Analyze data set X')
