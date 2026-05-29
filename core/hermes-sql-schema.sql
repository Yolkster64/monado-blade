-- Hermes Fleet SQL Schema
CREATE TABLE IF NOT EXISTS agents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    status TEXT,
    last_heartbeat DATETIME
);
CREATE TABLE IF NOT EXISTS jobs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    agent_id INTEGER,
    job_type TEXT,
    status TEXT,
    created_at DATETIME,
    completed_at DATETIME,
    FOREIGN KEY(agent_id) REFERENCES agents(id)
);
