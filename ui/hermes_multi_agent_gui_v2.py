# hermes_multi_agent_gui_v2.py
# Enhanced GUI: text, add button, naming, save/load

import tkinter as tk
from tkinter import simpledialog, messagebox
from hermes_animated_bots_gui import AnimatedBot
from hermes_agent_registry import save_agent_to_registry, load_agent_from_registry

class HermesMultiAgentGUI(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('Hermes Multi-Agent GUI')
        self.agents = []
        self.agent_frames = []
        self.text = tk.Label(self, text='Manage your Hermes agents below:')
        self.text.pack()
        self.add_button = tk.Button(self, text='Add Agent', command=self.add_agent)
        self.add_button.pack()
        self.save_button = tk.Button(self, text='Save Agents', command=self.save_agents)
        self.save_button.pack()
        self.load_button = tk.Button(self, text='Load Agent', command=self.load_agent)
        self.load_button.pack()
        self.agent_area = tk.Frame(self)
        self.agent_area.pack()

    def add_agent(self):
        name = simpledialog.askstring('Agent Name', 'Enter agent name:')
        if name:
            bot = AnimatedBot(self.agent_area, name, 1, 'Skill')
            bot.pack()
            self.agents.append({'name': name, 'level': 1, 'skill': 'Skill'})
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
                self.agents.append(agent)
                self.agent_frames.append(bot)
            else:
                messagebox.showerror('Not found', f'Agent "{name}" not found.')

# Example usage:
# if __name__ == "__main__":
#     app = HermesMultiAgentGUI()
#     app.mainloop()
