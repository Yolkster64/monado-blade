# Hermes Orchestration Layer (Python)
import threading, queue, time

class HermesAgent:
    def __init__(self, name):
        self.name = name
        self.status = 'idle'
    def run_job(self, job):
        self.status = 'busy'
        print(f"{self.name} running job: {job}")
        time.sleep(1)
        self.status = 'idle'

class Orchestrator:
    def __init__(self, agents):
        self.agents = agents
        self.job_queue = queue.Queue()
    def schedule_jobs(self, jobs):
        for job in jobs:
            self.job_queue.put(job)
        threads = []
        for agent in self.agents:
            t = threading.Thread(target=self.agent_worker, args=(agent,))
            t.start()
            threads.append(t)
        for t in threads:
            t.join()
    def agent_worker(self, agent):
        while not self.job_queue.empty():
            job = self.job_queue.get()
            agent.run_job(job)

if __name__ == "__main__":
    agents = [HermesAgent(f"Hermes-{i}") for i in range(1, 16)]
    orchestrator = Orchestrator(agents)
    orchestrator.schedule_jobs([f"Job-{j}" for j in range(1, 31)])
