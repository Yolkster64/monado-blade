# Hermes SQL Setup (Python)
import sqlite3

def setup_db():
    conn = sqlite3.connect('hermes_fleet.db')
    c = conn.cursor()
    c.executescript(open('core/hermes-sql-schema.sql').read())
    conn.commit()
    conn.close()

if __name__ == "__main__":
    setup_db()
    print("SQL schema created and ready.")
