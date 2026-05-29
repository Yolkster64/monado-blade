# hermes_sql_server.py
# SQL Server integration for Hermes agents

import sqlalchemy

class HermesSQLServer:
    def __init__(self, uri):
        self.engine = sqlalchemy.create_engine(uri)

    def fetch_data(self, query):
        with self.engine.connect() as conn:
            result = conn.execute(sqlalchemy.text(query))
            return [dict(row) for row in result]

    def save_data(self, table, data):
        with self.engine.connect() as conn:
            conn.execute(sqlalchemy.text(f"INSERT INTO {table} VALUES (:data)"), {'data': data})

# Example usage:
# sql = HermesSQLServer('postgresql://user:pass@host/db')
# rows = sql.fetch_data('SELECT * FROM mytable')
