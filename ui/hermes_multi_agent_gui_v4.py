# hermes_multi_agent_gui_v4.py
# GUI: drag-and-drop, focus text, smart/manual data

import tkinter as tk
from tkinter import simpledialog, messagebox, filedialog
from hermes_animated_bots_gui import AnimatedBot
from hermes_agent_registry import save_agent_to_registry, load_agent_from_registry
from hermes_agent_task_system import AgentTaskSystem

class HermesMultiAgentGUIv4(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('Hermes Multi-Agent GUI v4')
        self.agents = []
        self.agent_frames = []
        self.text = tk.Label(self, text='Drag a bot to a task, set focus, and provide training data:')
        self.text.pack()
        self.add_button = tk.Button(self, text='Add Agent', command=self.add_agent)
        self.add_button.pack()
        self.save_button = tk.Button(self, text='Save Agents', command=self.save_agents)
        self.save_button.pack()
        self.load_button = tk.Button(self, text='Load Agent', command=self.load_agent)
        self.load_button.pack()
        self.agent_area = tk.Frame(self)
        self.agent_area.pack(side=tk.LEFT)
        self.task_area = tk.Frame(self, width=300, height=400, bg='lightgray')
        self.task_area.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True)
        self.task_area.pack_propagate(False)
        self.focus_label = tk.Label(self.task_area, text='Focus Areas:')
        self.focus_label.pack()
        self.focus_entry = tk.Entry(self.task_area, width=40)
        self.focus_entry.pack()
        self.data_label = tk.Label(self.task_area, text='Training Data (manual or smart):')
        self.data_label.pack()
        self.data_entry = tk.Entry(self.task_area, width=40)
        self.data_entry.pack()
        self.data_button = tk.Button(self.task_area, text='Load Data File', command=self.load_data_file)
        self.data_button.pack()
        self.train_button = tk.Button(self.task_area, text='Train Selected Agent', command=self.train_agent)
        self.train_button.pack()
        self.selected_agent = None
        self.drag_data = {'widget': None}

    def add_agent(self):
        name = simpledialog.askstring('Agent Name', 'Enter agent name:')
        if name:
            bot = AnimatedBot(self.agent_area, name, 1, 'Skill')
            bot.pack()
            bot.bind('<ButtonPress-1>', self.on_drag_start)
            bot.bind('<B1-Motion>', self.on_drag_motion)
            bot.bind('<ButtonRelease-1>', self.on_drag_release)
            agent = {'name': name, 'level': 1, 'skill': 'Skill', 'object': AgentTaskSystem(self)}
            self.agents.append(agent)
            self.agent_frames.append(bot)

    def on_drag_start(self, event):
        self.drag_data['widget'] = event.widget

    def on_drag_motion(self, event):
        x, y = event.widget.winfo_pointerxy()
        event.widget.place(x=x, y=y, anchor='center')

    def on_drag_release(self, event):
        x, y = event.widget.winfo_pointerxy()
        if self.task_area.winfo_containing(x, y):
            idx = self.agent_frames.index(event.widget)
            self.selected_agent = self.agents[idx]
            messagebox.showinfo('Agent Selected', f'Agent {self.selected_agent["name"]} ready for task.')
        event.widget.place_forget()
        event.widget.pack()
        self.drag_data['widget'] = None

    def load_data_file(self):
        file_path = filedialog.askopenfilename()
        if file_path:
            with open(file_path, 'r') as f:
                data = f.read()
            self.data_entry.delete(0, tk.END)
            self.data_entry.insert(0, data[:1000])  # Preview only

    def train_agent(self):
        if not self.selected_agent:
            messagebox.showerror('No agent', 'No agent selected.')
            return
        focus = self.focus_entry.get()
        data = self.data_entry.get()
        instruction = f'Train on: {focus} | Data: {data[:100]}...'
        result = self.selected_agent['object'].assign_task(instruction)
        messagebox.showinfo('Training Result', f'Agent {self.selected_agent["name"]} result: {result}')

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
                bot.bind('<ButtonPress-1>', self.on_drag_start)
                bot.bind('<B1-Motion>', self.on_drag_motion)
                bot.bind('<ButtonRelease-1>', self.on_drag_release)
                agent['object'] = AgentTaskSystem(self)
                self.agents.append(agent)
                self.agent_frames.append(bot)
            else:
                messagebox.showerror('Not found', f'Agent "{name}" not found.')

# Example usage:
# if __name__ == "__main__":
#     app = HermesMultiAgentGUIv4()
#     app.mainloop()
