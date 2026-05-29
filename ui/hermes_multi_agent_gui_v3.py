# hermes_multi_agent_gui_v3.py
# GUI: select agent, give instructions, execute tasks

import tkinter as tk
from tkinter import simpledialog, messagebox
from hermes_animated_bots_gui import AnimatedBot
from hermes_agent_registry import save_agent_to_registry, load_agent_from_registry
from hermes_agent_task_system import AgentTaskSystem

class HermesMultiAgentGUIv3(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('Hermes Multi-Agent GUI v3')
        self.agents = []
        self.agent_frames = []
        self.text = tk.Label(self, text='Select an agent and give instructions:')
        self.text.pack()
        self.add_button = tk.Button(self, text='Add Agent', command=self.add_agent)
        self.add_button.pack()
        self.save_button = tk.Button(self, text='Save Agents', command=self.save_agents)
        self.save_button.pack()
        self.load_button = tk.Button(self, text='Load Agent', command=self.load_agent)
        self.load_button.pack()
        self.instruction_button = tk.Button(self, text='Give Instruction', command=self.give_instruction)
        self.instruction_button.pack()
        self.agent_area = tk.Frame(self)
        self.agent_area.pack()
        self.selected_agent = None

    def add_agent(self):
        name = simpledialog.askstring('Agent Name', 'Enter agent name:')
        if name:
            bot = AnimatedBot(self.agent_area, name, 1, 'Skill')
            bot.pack()
            agent = {'name': name, 'level': 1, 'skill': 'Skill', 'object': AgentTaskSystem(self)}
            self.agents.append(agent)
            self.agent_frames.append(bot)

    def save_agents(self):
        for agent in self.agents:
            save_agent_to_registry(agent, agent['name'])
        messagebox.showinfo('Saved', 'Agents saved!')

    def load_agent(self):
        name = simpledialog.askstring('Load Agent', 'Enter agent name to load:')
        if name:
            agent = load_agent_from_registry(name)
            if agent:
                bot = AnimatedBot(self.agent_area, agent['name'], agent['level'], agent['skill'])
                bot.pack()
                agent['object'] = AgentTaskSystem(self)
                self.agents.append(agent)
                self.agent_frames.append(bot)
            else:
                messagebox.showerror('Not found', f'Agent "{name}" not found.')

    def give_instruction(self):
        if not self.agents:
            messagebox.showerror('No agents', 'No agents available.')
            return
        agent_names = [a['name'] for a in self.agents]
        name = simpledialog.askstring('Select Agent', f'Agent names: {agent_names}\nEnter agent name:')
        agent = next((a for a in self.agents if a['name'] == name), None)
        if agent:
            instruction = simpledialog.askstring('Instruction', 'Enter instruction for agent:')
            if instruction:
                result = agent['object'].assign_task(instruction)
                messagebox.showinfo('Task Result', f'Agent {name} result: {result}')
        else:
            messagebox.showerror('Not found', f'Agent "{name}" not found.')

# Example usage:
# if __name__ == "__main__":
#     app = HermesMultiAgentGUIv3()
#     app.mainloop()
