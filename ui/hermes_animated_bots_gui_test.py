# hermes_animated_bots_gui_test.py
# Test for animated bot GUI

from hermes_animated_bots_gui import AnimatedBot
import tkinter as tk

def test_animated_bot():
    root = tk.Tk()
    bot = AnimatedBot(root, 'Hermes', 5, 'Vision')
    bot.pack()
    root.mainloop()

if __name__ == "__main__":
    test_animated_bot()
