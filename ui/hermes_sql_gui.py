# hermes_sql_gui.py
# GUI for SQL data interaction

import tkinter as tk
from tkinter import simpledialog, messagebox
from hermes_sql_server import HermesSQLServer

class HermesSQLGUI(tk.Toplevel):
    def __init__(self, master, sql_uri):
        super().__init__(master)
        self.title('Hermes SQL Data GUI')
        self.sql = HermesSQLServer(sql_uri)
        self.query_entry = tk.Entry(self, width=60)
        self.query_entry.pack()
        self.run_button = tk.Button(self, text='Run Query', command=self.run_query)
        self.run_button.pack()
        self.result_text = tk.Text(self, width=80, height=20)
        self.result_text.pack()
    def run_query(self):
        query = self.query_entry.get()
        try:
            rows = self.sql.fetch_data(query)
            self.result_text.delete('1.0', tk.END)
            for row in rows:
                self.result_text.insert(tk.END, str(row) + '\n')
        except Exception as e:
            messagebox.showerror('Query Error', str(e))

# Example usage:
# HermesSQLGUI(root, 'postgresql://user:pass@host/db')
