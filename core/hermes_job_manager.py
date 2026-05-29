# hermes_job_manager.py
# Job training and management for Hermes agents

import threading
import queue

class JobManager:
    def __init__(self):
        self.job_queue = queue.Queue()
        self.threads = []

    def add_job(self, job_func, *args, **kwargs):
        self.job_queue.put((job_func, args, kwargs))

    def worker(self):
        while not self.job_queue.empty():
            job_func, args, kwargs = self.job_queue.get()
            job_func(*args, **kwargs)
            self.job_queue.task_done()

    def run_jobs(self, num_workers=4):
        for _ in range(num_workers):
            t = threading.Thread(target=self.worker)
            t.start()
            self.threads.append(t)
        for t in self.threads:
            t.join()
