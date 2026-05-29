# hermes_job_tracking.py
# Track and persist agent training jobs

import sqlite3
import os

DB_PATH = os.path.join(os.path.dirname(__file__), 'hermes_jobs.db')

def init_db():
    with sqlite3.connect(DB_PATH) as conn:
        c = conn.cursor()
        c.execute('''CREATE TABLE IF NOT EXISTS jobs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            agent_name TEXT,
            job_type TEXT,
            status TEXT,
            start_time TEXT,
            end_time TEXT,
            result TEXT
        )''')
        conn.commit()

def log_job(agent_name, job_type, status, start_time, end_time=None, result=None):
    with sqlite3.connect(DB_PATH) as conn:
        c = conn.cursor()
        c.execute('''INSERT INTO jobs (agent_name, job_type, status, start_time, end_time, result)
                     VALUES (?, ?, ?, ?, ?, ?)''',
                  (agent_name, job_type, status, start_time, end_time, result))
        conn.commit()

# Example usage:
# init_db()
# log_job('Hermes1', 'train', 'started', '2026-05-15T16:42:00')
