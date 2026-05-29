# hermes_multi_agent_gui_cute.py
# Overhauled cute GUI with avatars, icons, XP bars

import tkinter as tk
from tkinter import simpledialog, messagebox
from ui.hermes_agent_icons import AGENT_ICONS

class CuteAgentFrame(tk.Frame):
    def __init__(self, master, name, level, skills, xp, **kwargs):
        super().__init__(master, **kwargs)
        self.name = name
        self.level = level
        self.skills = skills  # dict: skill -> xp or dict
        self.xp = xp
        self.icon = AGENT_ICONS.get(next(iter(skills)), '🤖')
        self.avatar = tk.Label(self, text=f'{self.icon}', font=('Arial', 32))
        self.avatar.pack(side=tk.LEFT)
        self.info = tk.Frame(self)
        self.info.pack(side=tk.LEFT, padx=10)
        self.name_label = tk.Label(self.info, text=f'{name} (Lv{level})', font=('Arial', 14, 'bold'))
        self.name_label.pack(anchor='w')
        self.skill_label = tk.Label(self.info, text=self._skills_text())
        self.skill_label.pack(anchor='w')
        self.xp_bar = tk.Canvas(self.info, width=100, height=10, bg='lightgray')
        self.xp_bar.pack(anchor='w', pady=2)
        self.draw_xp_bar()
    def _skills_text(self):
        def skill_str(skill, val):
            if isinstance(val, dict):
                return f'{skill}[' + ', '.join(f'{sub}:{xp}' for sub, xp in val.items()) + ']'
            return f'{skill}({val})'
        return 'Skills: ' + ', '.join(skill_str(k, v) for k, v in self.skills.items())
    def draw_xp_bar(self):
        percent = min(self.xp / 100, 1.0)
        self.xp_bar.delete('all')
        self.xp_bar.create_rectangle(0, 0, 100 * percent, 10, fill='green')
    def update_skills(self):
        self.skill_label.config(text=self._skills_text())
        # Show skill bars for each (sub)skill
        if hasattr(self, 'skill_frame'):
            self.skill_frame.destroy()
        self.skill_frame = tk.Frame(self.info)
        self.skill_frame.pack(anchor='w')
        row = 0
        for skill, xp in self.skills.items():
            icon = AGENT_ICONS.get(skill, '\U0001F916')
            if isinstance(xp, dict):
                tk.Label(self.skill_frame, text=f'{icon} {skill}:', font=('Arial', 10, 'bold')).grid(row=row, column=0, sticky='w')
                col = 1
                for sub, sub_xp in xp.items():
                    sub_icon = AGENT_ICONS.get(sub, '')
                    tk.Label(self.skill_frame, text=f'{sub_icon} {sub}', font=('Arial', 9)).grid(row=row, column=col, sticky='w')
                    bar = tk.Canvas(self.skill_frame, width=60, height=8, bg='lightgray')
                    bar.create_rectangle(0, 0, min(sub_xp, 100) * 0.6, 8, fill='blue')
                    bar.grid(row=row, column=col+1, padx=2)
                    col += 2
                row += 1
            else:
                tk.Label(self.skill_frame, text=f'{icon} {skill}', font=('Arial', 10)).grid(row=row, column=0, sticky='w')
                bar = tk.Canvas(self.skill_frame, width=100, height=8, bg='lightgray')
                bar.create_rectangle(0, 0, min(xp, 100), 8, fill='green')
                bar.grid(row=row, column=1, padx=2)
                row += 1

class CuteMultiAgentGUI(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('Hermes Cute Multi-Agent GUI')
        self.agents = []
        self.agent_area = tk.Frame(self)
        self.agent_area.pack(side=tk.LEFT)
        self.canvas = tk.Canvas(self, width=500, height=400, bg='white')
        self.canvas.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True)
        self.add_button = tk.Button(self, text='Add Cute Agent', command=self.add_agent)
        self.add_button.pack()
        self.interact_button = tk.Button(self, text='Simulate Interaction', command=self.simulate_interaction)
        self.interact_button.pack()
        self.agent_canvas_objs = {}

    def add_agent(self):
        name = simpledialog.askstring('Agent Name', 'Enter agent name:')
        skills_str = simpledialog.askstring('Skills', 'Enter skills (comma separated, e.g., Vision,Code,Math):')
        skills = {}
        for s in skills_str.split(','):
            s = s.strip()
            if not s:
                continue
            # Ask for subskills if code or language
            if s.lower() in ['code', 'language']:
                subskills_str = simpledialog.askstring('Subskills', f'Enter subskills for {s} (comma separated, e.g., writer,analyzer,fixer):')
                subskills = {sub.strip(): 0 for sub in subskills_str.split(',') if sub.strip()}
                skills[s] = subskills
            else:
                skills[s] = 0
        level = 1
        xp = 0
        frame = CuteAgentFrame(self.agent_area, name, level, skills, xp)
        frame.pack(pady=5)
        agent = {'name': name, 'level': level, 'skills': skills, 'xp': xp, 'frame': frame}
        self.agents.append(agent)
        # Place agent on canvas
        x, y = 50 + 80 * (len(self.agents) - 1), 200
        icon = AGENT_ICONS.get(next(iter(skills)), '\U0001F916')
        obj = self.canvas.create_text(x, y, text=icon, font=('Arial', 32), fill='black')
        self.agent_canvas_objs[name] = obj
        # Bugfix: ensure unique agent names
        if len(set(a['name'] for a in self.agents)) != len(self.agents):
            messagebox.showerror('Error', 'Agent names must be unique.')
            frame.destroy()
            self.agents.pop()
            self.canvas.delete(obj)
            del self.agent_canvas_objs[name]
            return

    def simulate_interaction(self):
        # Simple: pick two agents, animate them moving together, "fight" or "learn"
        if len(self.agents) < 2:
            messagebox.showinfo('Info', 'Need at least 2 agents to interact.')
            return
        import random
        a1, a2 = random.sample(self.agents, 2)
        obj1 = self.agent_canvas_objs[a1['name']]
        obj2 = self.agent_canvas_objs[a2['name']]
        # Animate movement
        for _ in range(20):
            self.canvas.move(obj1, 2, 0)
            self.canvas.move(obj2, -2, 0)
            self.canvas.update()
            self.after(30)
        # Simulate "fight" or "learn"
        outcome = random.choice(['fight', 'disprove', 'learn', 'borg'])
        def update_skill_dict(skills, skill, delta):
            if isinstance(skills[skill], dict):
                for sub in skills[skill]:
                    skills[skill][sub] = max(0, skills[skill][sub] + delta)
            else:
                skills[skill] = max(0, skills[skill] + delta)
        if outcome == 'fight':
            self.canvas.itemconfig(obj1, fill='red')
            self.canvas.itemconfig(obj2, fill='red')
            for skill in set(a1['skills']) | set(a2['skills']):
                if skill in a1['skills']:
                    update_skill_dict(a1['skills'], skill, -10)
                if skill in a2['skills']:
                    update_skill_dict(a2['skills'], skill, -10)
        elif outcome == 'disprove':
            self.canvas.itemconfig(obj1, fill='orange')
            self.canvas.itemconfig(obj2, fill='orange')
            for skill in set(a1['skills']) | set(a2['skills']):
                if skill in a1['skills']:
                    update_skill_dict(a1['skills'], skill, -5)
                if skill in a2['skills']:
                    update_skill_dict(a2['skills'], skill, -5)
        elif outcome == 'learn':
            self.canvas.itemconfig(obj1, fill='green')
            self.canvas.itemconfig(obj2, fill='green')
            for skill in set(a1['skills']) | set(a2['skills']):
                if skill in a1['skills']:
                    update_skill_dict(a1['skills'], skill, 15)
                if skill in a2['skills']:
                    update_skill_dict(a2['skills'], skill, 15)
        else:  # 'borg' - share all skills/XP
            self.canvas.itemconfig(obj1, fill='blue')
            self.canvas.itemconfig(obj2, fill='blue')
            all_skills = set(a1['skills']) | set(a2['skills'])
            for skill in all_skills:
                if skill in a1['skills'] and skill in a2['skills']:
                    if isinstance(a1['skills'][skill], dict) and isinstance(a2['skills'][skill], dict):
                        for sub in set(a1['skills'][skill]) | set(a2['skills'][skill]):
                            max_xp = max(a1['skills'][skill].get(sub, 0), a2['skills'][skill].get(sub, 0))
                            a1['skills'][skill][sub] = max_xp
                            a2['skills'][skill][sub] = max_xp
                    else:
                        max_xp = max(a1['skills'].get(skill, 0), a2['skills'].get(skill, 0))
                        a1['skills'][skill] = max_xp
                        a2['skills'][skill] = max_xp
                elif skill in a1['skills']:
                    a2['skills'][skill] = a1['skills'][skill]
                elif skill in a2['skills']:
                    a1['skills'][skill] = a2['skills'][skill]
        # Update skill displays
        a1['frame'].skills = a1['skills']
        a2['frame'].skills = a2['skills']
        a1['frame'].update_skills()
        a2['frame'].update_skills()
        self.after(500)
        self.canvas.itemconfig(obj1, fill='black')
        self.canvas.itemconfig(obj2, fill='black')

    def add_agent(self):
        name = simpledialog.askstring('Agent Name', 'Enter agent name:')
        skill = simpledialog.askstring('Expertise', 'Enter agent expertise (e.g., Vision, Code):')
        level = 1
        xp = 0
        frame = CuteAgentFrame(self.agent_area, name, level, skill, xp)
        frame.pack(pady=5)
        self.agents.append({'name': name, 'level': level, 'skill': skill, 'xp': xp, 'frame': frame})

# Example usage:
# if __name__ == "__main__":
#     app = CuteMultiAgentGUI()
#     app.mainloop()
