# hermes_animated_bots_gui.py
# Animated bot avatars and skill/level GUI

import tkinter as tk
from tkinter import Canvas

class AnimatedBot(tk.Frame):
    def __init__(self, master, name, level, skill, **kwargs):
        super().__init__(master, **kwargs)
        self.name = name
        self.level = level
        self.skill = skill
        self.canvas = Canvas(self, width=100, height=100)
        self.canvas.pack()
        self.draw_bot()

    def draw_bot(self):
        # Simple animated circle bot
        self.canvas.create_oval(20, 20, 80, 80, fill='skyblue')
        self.canvas.create_text(50, 50, text=f"{self.name}\nLv{self.level}")
        self.canvas.create_text(50, 80, text=f"Skill: {self.skill}")

# Example usage:
# root = tk.Tk()
# bot = AnimatedBot(root, 'Hermes', 5, 'Vision')
# bot.pack()
# root.mainloop()
