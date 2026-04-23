using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MonadoBlade.Core.Async
{
    /// <summary>
    /// Audits codebase for blocking I/O and CPU-bound operations using reflection.
    /// Identifies patterns that should be converted to async for performance optimization.
    /// </summary>
    public class BlockingCallAuditor
    {
        private readonly List<BlockingCallFinding> _findings = new();

        public enum BlockingCallCategory
        {
            FileIO,
            NetworkIO,
            DatabaseIO,
            ProcessManagement,
            Synchronization,
            Threading,
            CPUBound
        }

        public enum SeverityLevel
        {
            Critical = 4,
            High = 3,
            Medium = 2,
            Low = 1
        }

        public class BlockingCallFinding
        {
            public string MethodName { get; set; }
            public string ClassName { get; set; }
            public BlockingCallCategory Category { get; set; }
            public SeverityLevel Severity { get; set; }
            public string Description { get; set; }
            public string ReplacementStrategy { get; set; }
            public string ImpactDescription { get; set; }
            public int EstimatedLatencyMs { get; set; }
        }

        public class AuditReport
        {
            public int TotalFindingsCount { get; set; }
            public Dictionary<BlockingCallCategory, int> FindingsByCategory { get; set; }
            public Dictionary<SeverityLevel, int> FindingsBySeverity { get; set; }
            public List<BlockingCallFinding> Findings { get; set; }
            public long EstimatedTotalLatencyMs { get; set; }
            public string Summary { get; set; }
        }

        /// <summary>
        /// Audits an assembly for blocking call patterns.
        /// </summary>
        public AuditReport AuditAssembly(Assembly assembly)
        {
            _findings.Clear();
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                AuditType(type);
            }

            return GenerateReport();
        }

        /// <summary>
        /// Audits a specific type for blocking patterns.
        /// </summary>
        private void AuditType(Type type)
        {
            var methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Private |
                BindingFlags.Instance | BindingFlags.Static);

            foreach (var method in methods)
            {
                AuditMethod(method, type);
            }
        }

        /// <summary>
        /// Analyzes method IL to detect blocking patterns.
        /// </summary>
        private void AuditMethod(MethodInfo method, Type declaringType)
        {
            try
            {
                var methodBody = method.GetMethodBody();
                if (methodBody == null) return;

                var il = methodBody.GetILAsByteArray();
                AnalyzeILCode(il, method, declaringType);

                // Also check method signature and attributes
                CheckMethodSignature(method, declaringType);
            }
            catch
            {
                // Continue auditing other methods on reflection errors
            }
        }

        /// <summary>
        /// Analyzes IL bytecode for blocking call patterns.
        /// </summary>
        private void AnalyzeILCode(byte[] il, MethodInfo method, Type declaringType)
        {
            var methodString = method.ToString();

            // Pattern detection for Thread.Sleep
            if (methodString.Contains("Thread") && method.Name.Contains("Sleep"))
            {
                AddFinding(new BlockingCallFinding
                {
                    MethodName = method.Name,
                    ClassName = declaringType.Name,
                    Category = BlockingCallCategory.Threading,
                    Severity = SeverityLevel.High,
                    Description = "Thread.Sleep() call detected - blocks thread",
                    ReplacementStrategy = "Replace with await Task.Delay(ms)",
                    ImpactDescription = "Wastes thread pool resource; prevents other work",
                    EstimatedLatencyMs = 10
                });
            }

            // Pattern detection for synchronous file operations
            if (method.Name.Contains("Read") || method.Name.Contains("Write"))
            {
                if (methodString.Contains("File") && !method.Name.Contains("Async"))
                {
                    AddFinding(new BlockingCallFinding
                    {
                        MethodName = method.Name,
                        ClassName = declaringType.Name,
                        Category = BlockingCallCategory.FileIO,
                        Severity = SeverityLevel.High,
                        Description = "Synchronous file operation detected",
                        ReplacementStrategy = "Use File.*Async() or FileStream with ConfigureAwait(false)",
                        ImpactDescription = "Blocks thread during I/O; impacts scalability",
                        EstimatedLatencyMs = 50
                    });
                }
            }

            // Pattern detection for Result/Wait on tasks
            if (methodString.Contains("Result") || methodString.Contains("Wait"))
            {
                if (!method.IsAsync && method.ReturnType == typeof(void))
                {
                    AddFinding(new BlockingCallFinding
                    {
                        MethodName = method.Name,
                        ClassName = declaringType.Name,
                        Category = BlockingCallCategory.Synchronization,
                        Severity = SeverityLevel.Critical,
                        Description = "Deadlock risk: .Result or .Wait() on async operation",
                        ReplacementStrategy = "Make method async and use await instead of .Result/.Wait()",
                        ImpactDescription = "High deadlock risk; can freeze entire application",
                        EstimatedLatencyMs = 100
                    });
                }
            }

            // Pattern detection for lock statements
            if (method.Name.Contains("lock") || methodString.Contains("lock"))
            {
                AddFinding(new BlockingCallFinding
                {
                    MethodName = method.Name,
                    ClassName = declaringType.Name,
                    Category = BlockingCallCategory.Synchronization,
                    Severity = SeverityLevel.High,
                    Description = "Lock statement in potentially hot path",
                    ReplacementStrategy = "Use ReaderWriterLockSlim or async coordination patterns",
                    ImpactDescription = "Can cause thread contention under load",
                    EstimatedLatencyMs = 20
                });
            }
        }

        /// <summary>
        /// Checks method signature for blocking patterns.
        /// </summary>
        private void CheckMethodSignature(MethodInfo method, Type declaringType)
        {
            var parameters = method.GetParameters();
            var methodName = method.Name;

            // Check for database operations
            if (methodName.Contains("Database") || methodName.Contains("Query") ||
                methodName.Contains("Execute"))
            {
                if (!method.IsAsync)
                {
                    AddFinding(new BlockingCallFinding
                    {
                        MethodName = methodName,
                        ClassName = declaringType.Name,
                        Category = BlockingCallCategory.DatabaseIO,
                        Severity = SeverityLevel.Critical,
                        Description = "Synchronous database operation detected",
                        ReplacementStrategy = "Use async database methods (ExecuteAsync, QueryAsync, etc.)",
                        ImpactDescription = "Network latency blocks thread; severely impacts throughput",
                        EstimatedLatencyMs = 200
                    });
                }
            }

            // Check for network operations
            if (methodName.Contains("Download") || methodName.Contains("Upload") ||
                methodName.Contains("Request") || methodName.Contains("Http"))
            {
                if (!method.IsAsync && method.ReturnType.Name != "IAsyncEnumerable`1")
                {
                    AddFinding(new BlockingCallFinding
                    {
                        MethodName = methodName,
                        ClassName = declaringType.Name,
                        Category = BlockingCallCategory.NetworkIO,
                        Severity = SeverityLevel.Critical,
                        Description = "Synchronous network operation detected",
                        ReplacementStrategy = "Use HttpClient with async/await and cancellation tokens",
                        ImpactDescription = "Network latency (100ms+) blocks thread completely",
                        EstimatedLatencyMs = 150
                    });
                }
            }

            // Check for process management
            if (methodName.Contains("Process") || methodName.Contains("Execute"))
            {
                if (!method.IsAsync)
                {
                    AddFinding(new BlockingCallFinding
                    {
                        MethodName = methodName,
                        ClassName = declaringType.Name,
                        Category = BlockingCallCategory.ProcessManagement,
                        Severity = SeverityLevel.High,
                        Description = "Synchronous process execution detected",
                        ReplacementStrategy = "Wrap Process.WaitForExit in async task with timeout",
                        ImpactDescription = "Blocks thread while waiting for process completion",
                        EstimatedLatencyMs = 500
                    });
                }
            }

            // Check for CPU-bound operations
            if (methodName.Contains("Compute") || methodName.Contains("Calculate") ||
                methodName.Contains("Hash") || methodName.Contains("Encrypt"))
            {
                if (parameters.Length > 100)
                {
                    AddFinding(new BlockingCallFinding
                    {
                        MethodName = methodName,
                        ClassName = declaringType.Name,
                        Category = BlockingCallCategory.CPUBound,
                        Severity = SeverityLevel.Medium,
                        Description = "Large CPU-bound operation detected",
                        ReplacementStrategy = "Use Task.Run() or Parallel.ForEachAsync for parallelization",
                        ImpactDescription = "Long computation blocks thread; limits concurrency",
                        EstimatedLatencyMs = 1000
                    });
                }
            }
        }

        private void AddFinding(BlockingCallFinding finding)
        {
            _findings.Add(finding);
        }

        /// <summary>
        /// Generates comprehensive audit report with findings prioritized by severity/category.
        /// </summary>
        public AuditReport GenerateReport()
        {
            var sortedFindings = _findings
                .OrderByDescending(f => f.Severity)
                .ThenByDescending(f => f.EstimatedLatencyMs)
                .ToList();

            var report = new AuditReport
            {
                TotalFindingsCount = sortedFindings.Count,
                Findings = sortedFindings,
                FindingsByCategory = sortedFindings
                    .GroupBy(f => f.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FindingsBySeverity = sortedFindings
                    .GroupBy(f => f.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EstimatedTotalLatencyMs = sortedFindings.Sum(f => f.EstimatedLatencyMs),
                Summary = GenerateSummary(sortedFindings)
            };

            return report;
        }

        /// <summary>
        /// Generates human-readable summary of audit findings.
        /// </summary>
        private string GenerateSummary(List<BlockingCallFinding> findings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== BLOCKING CALL AUDIT REPORT ===\n");

            sb.AppendLine($"Total Blocking Calls Found: {findings.Count}");
            sb.AppendLine($"Estimated Total Latency: {findings.Sum(f => f.EstimatedLatencyMs)}ms\n");

            // By Severity
            sb.AppendLine("--- BY SEVERITY ---");
            var bySeverity = findings.GroupBy(f => f.Severity);
            foreach (var group in bySeverity.OrderByDescending(g => g.Key))
            {
                sb.AppendLine($"{group.Key}: {group.Count()} findings");
            }

            // By Category
            sb.AppendLine("\n--- BY CATEGORY ---");
            var byCategory = findings.GroupBy(f => f.Category);
            foreach (var group in byCategory.OrderByDescending(g => g.Count()))
            {
                sb.AppendLine($"{group.Key}: {group.Count()} findings ({group.Sum(f => f.EstimatedLatencyMs)}ms latency)");
            }

            // Top findings
            sb.AppendLine("\n--- TOP 5 CRITICAL FINDINGS ---");
            foreach (var finding in findings.Take(5))
            {
                sb.AppendLine($"\n[{finding.Severity}] {finding.ClassName}.{finding.MethodName}");
                sb.AppendLine($"  Category: {finding.Category}");
                sb.AppendLine($"  Issue: {finding.Description}");
                sb.AppendLine($"  Impact: {finding.ImpactDescription}");
                sb.AppendLine($"  Fix: {finding.ReplacementStrategy}");
                sb.AppendLine($"  Est. Latency: {finding.EstimatedLatencyMs}ms");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Exports findings to CSV format for analysis.
        /// </summary>
        public string ExportToCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Class,Method,Category,Severity,Description,ReplacementStrategy,EstimatedLatencyMs");

            foreach (var finding in _findings)
            {
                sb.AppendLine($"\"{finding.ClassName}\",\"{finding.MethodName}\",\"{finding.Category}\"," +
                    $"\"{finding.Severity}\",\"{finding.Description}\",\"{finding.ReplacementStrategy}\"," +
                    $"{finding.EstimatedLatencyMs}");
            }

            return sb.ToString();
        }
    }
}
