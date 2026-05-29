// Hermes Orchestration Layer (C#)
using System;
using System.Collections.Generic;
using System.Threading;

class HermesAgent {
    public string Name { get; set; }
    public string Status { get; set; } = "idle";
    public void RunJob(string job) {
        Status = "busy";
        Console.WriteLine($"{Name} running job: {job}");
        Thread.Sleep(1000);
        Status = "idle";
    }
}

class Orchestrator {
    List<HermesAgent> agents;
    Queue<string> jobQueue = new Queue<string>();
    public Orchestrator(List<HermesAgent> agents) { this.agents = agents; }
    public void ScheduleJobs(List<string> jobs) {
        foreach (var job in jobs) jobQueue.Enqueue(job);
        var threads = new List<Thread>();
        foreach (var agent in agents) {
            var t = new Thread(() => AgentWorker(agent));
            t.Start();
            threads.Add(t);
        }
        foreach (var t in threads) t.Join();
    }
    void AgentWorker(HermesAgent agent) {
        while (jobQueue.Count > 0) {
            string job;
            lock (jobQueue) {
                if (jobQueue.Count == 0) break;
                job = jobQueue.Dequeue();
            }
            agent.RunJob(job);
        }
    }
}

class Program {
    static void Main() {
        var agents = new List<HermesAgent>();
        for (int i = 1; i <= 15; i++) agents.Add(new HermesAgent { Name = $"Hermes-{i}" });
        var orchestrator = new Orchestrator(agents);
        orchestrator.ScheduleJobs(new List<string> { "Job-1", "Job-2", "Job-3", "Job-4", "Job-5" });
    }
}
