# hermes_sql_gui_v2.py
# Enhanced SQL GUI: table view, query builder, data-to-agent assignment

import tkinter as tk
from tkinter import simpledialog, messagebox, ttk
import sys
import os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'core')))
from hermes_sql_server import HermesSQLServer
from hermes_data_workflow import DataWorkflowManager

class HermesSQLGUIv2(tk.Toplevel):
    def __init__(self, master, sql_uri, agents):
        super().__init__(master)
        self.title('Hermes SQL Data GUI v2')
        self.sql = HermesSQLServer(sql_uri)
        self.agents = agents
        self.workflow = DataWorkflowManager(self.sql, self.agents)
        self.query_entry = tk.Entry(self, width=60)
        self.query_entry.pack()
        self.run_button = tk.Button(self, text='Run Query', command=self.run_query)
        self.run_button.pack()
        self.table = ttk.Treeview(self)
        self.table.pack(fill=tk.BOTH, expand=True)
        self.assign_button = tk.Button(self, text='Assign Data to Agent', command=self.assign_data)
        self.assign_button.pack()
        self.agent_select = ttk.Combobox(self, values=[a['name'] for a in self.agents])
        self.agent_select.pack()
        self.task_entry = tk.Entry(self, width=40)
        self.task_entry.pack()
        self.run_agent_button = tk.Button(self, text='Run Agent on Data', command=self.run_agent_on_data)
        self.run_agent_button.pack()
        self.last_data = []
        self.auto_refresh_interval = 10000  # 10 seconds
        self.after(self.auto_refresh_interval, self.auto_refresh_data)

    def auto_refresh_data(self):
        query = self.query_entry.get()
        if query:
            try:
                rows = self.sql.fetch_data(query)
                self.last_data = rows
                self.table.delete(*self.table.get_children())
                if rows:
                    self.table['columns'] = list(rows[0].keys())
                    for col in self.table['columns']:
                        self.table.heading(col, text=col)
                    for row in rows:
                        self.table.insert('', 'end', values=list(row.values()))
            except Exception as e:
                pass  # Silently ignore errors on auto-refresh
        self.after(self.auto_refresh_interval, self.auto_refresh_data)

    def run_query(self):
        query = self.query_entry.get()
        try:
            rows = self.sql.fetch_data(query)
            self.last_data = rows
            self.table.delete(*self.table.get_children())
            if rows:
                self.table['columns'] = list(rows[0].keys())
                for col in self.table['columns']:
                    self.table.heading(col, text=col)
                for row in rows:
                    self.table.insert('', 'end', values=list(row.values()))
        except Exception as e:
            messagebox.showerror('Query Error', str(e))
    def assign_data(self):
        agent_name = self.agent_select.get()
        if not agent_name or not self.last_data:
            messagebox.showerror('Error', 'Select agent and run a query first.')
            return
        agent = next(a for a in self.agents if a['name'] == agent_name)
        agent['data'] = self.last_data
        messagebox.showinfo('Assigned', f'Data assigned to {agent_name}.')
    def run_agent_on_data(self):
        agent_name = self.agent_select.get()
        task = self.task_entry.get()
        if not agent_name or not task:
            messagebox.showerror('Error', 'Select agent and enter a task.')
            return
        agent = next(a for a in self.agents if a['name'] == agent_name)
        if 'data' not in agent:
            # Try agent-triggered data update if not present
            query = self.query_entry.get()
            if query:
                self.workflow.agent_triggered_data_update(agent_name, query)
            else:
                messagebox.showerror('Error', 'No data assigned to agent and no query provided.')
                return
        result = agent['object'].assign_task(f'{task} on {agent["data"][:5]}...')
        messagebox.showinfo('Result', f'Agent {agent_name} result: {result}')

# Example usage:
# HermesSQLGUIv2(root, 'postgresql://user:pass@host/db', agents)
