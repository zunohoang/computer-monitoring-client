using System;
using System.Diagnostics;
using ComputerMonitoringClient.Services;

namespace ComputerMonitoringClient.Utils
{
    /// <summary>
    /// Utility class for testing process monitoring performance
    /// </summary>
    public static class PerformanceTestHelper
    {
        /// <summary>
        /// Test the performance of process enumeration
        /// </summary>
        public static string TestProcessPerformance()
        {
            var processService = ProcessService.Instance;
            var stopwatch = new Stopwatch();
            var results = new System.Text.StringBuilder();
            
            results.AppendLine("=== PERFORMANCE TEST RESULTS ===");
            results.AppendLine($"Test Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            results.AppendLine();

            // Test 1: Cold cache (first call)
            stopwatch.Restart();
            var processes1 = processService.GetRunningProcesses();
            stopwatch.Stop();
            var coldCacheTime = stopwatch.ElapsedMilliseconds;
            
            results.AppendLine($"?? Cold Cache Performance:");
            results.AppendLine($"   Time: {coldCacheTime} ms");
            results.AppendLine($"   Processes Found: {processes1.Count}");
            results.AppendLine();

            // Test 2: Warm cache (second call immediately)
            stopwatch.Restart();
            var processes2 = processService.GetRunningProcesses();
            stopwatch.Stop();
            var warmCacheTime = stopwatch.ElapsedMilliseconds;
            
            results.AppendLine($"? Warm Cache Performance:");
            results.AppendLine($"   Time: {warmCacheTime} ms");
            results.AppendLine($"   Processes Found: {processes2.Count}");
            results.AppendLine($"   Performance Improvement: {((double)(coldCacheTime - warmCacheTime) / coldCacheTime * 100):F1}%");
            results.AppendLine();

            // Test 3: Suspicious process detection
            stopwatch.Restart();
            var suspicious = processService.GetSuspiciousProcesses();
            stopwatch.Stop();
            var suspiciousTime = stopwatch.ElapsedMilliseconds;
            
            results.AppendLine($"?? Suspicious Process Detection:");
            results.AppendLine($"   Time: {suspiciousTime} ms");
            results.AppendLine($"   Suspicious Processes: {suspicious.Count}");
            results.AppendLine();

            // Test 4: System resources
            stopwatch.Restart();
            var resources = processService.GetSystemResourceInfo();
            stopwatch.Stop();
            var resourceTime = stopwatch.ElapsedMilliseconds;
            
            results.AppendLine($"?? System Resources:");
            results.AppendLine($"   Time: {resourceTime} ms");
            results.AppendLine();

            // Test 5: Process monitoring simulation
            stopwatch.Restart();
            var monitoringService = MonitoringService.Instance;
            var overview = monitoringService.GetProcessOverview();
            stopwatch.Stop();
            var overviewTime = stopwatch.ElapsedMilliseconds;
            
            results.AppendLine($"?? Process Overview:");
            results.AppendLine($"   Time: {overviewTime} ms");
            results.AppendLine();

            // Summary
            var totalTime = coldCacheTime + warmCacheTime + suspiciousTime + resourceTime + overviewTime;
            results.AppendLine($"?? SUMMARY:");
            results.AppendLine($"   Total Test Time: {totalTime} ms");
            results.AppendLine($"   Average Operation: {totalTime / 5:F1} ms");
            
            if (coldCacheTime > 1000)
            {
                results.AppendLine($"   ?? WARNING: Cold cache time > 1000ms, consider system optimization");
            }
            else if (coldCacheTime > 500)
            {
                results.AppendLine($"   ? GOOD: Moderate performance, caching helps significantly");
            }
            else
            {
                results.AppendLine($"   ?? EXCELLENT: Fast performance achieved!");
            }

            return results.ToString();
        }

        /// <summary>
        /// Test memory usage of the process service
        /// </summary>
        public static string TestMemoryUsage()
        {
            var results = new System.Text.StringBuilder();
            var processService = ProcessService.Instance;
            
            results.AppendLine("=== MEMORY USAGE TEST ===");
            results.AppendLine($"Test Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            results.AppendLine();

            // Get initial memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var initialMemory = GC.GetTotalMemory(false);
            results.AppendLine($"Initial Memory: {initialMemory / 1024:N0} KB");

            // Run process enumeration multiple times
            for (int i = 0; i < 5; i++)
            {
                var processes = processService.GetRunningProcesses();
                var suspicious = processService.GetSuspiciousProcesses();
                var resources = processService.GetSystemResourceInfo();
            }

            var afterTestMemory = GC.GetTotalMemory(false);
            results.AppendLine($"After Tests: {afterTestMemory / 1024:N0} KB");
            results.AppendLine($"Memory Delta: {(afterTestMemory - initialMemory) / 1024:N0} KB");

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);
            results.AppendLine($"After GC: {finalMemory / 1024:N0} KB");
            results.AppendLine($"Memory Leaked: {(finalMemory - initialMemory) / 1024:N0} KB");

            if ((finalMemory - initialMemory) < 1024 * 100) // Less than 100KB
            {
                results.AppendLine("?? EXCELLENT: Low memory footprint");
            }
            else if ((finalMemory - initialMemory) < 1024 * 500) // Less than 500KB
            {
                results.AppendLine("?? GOOD: Acceptable memory usage");
            }
            else
            {
                results.AppendLine("?? WARNING: High memory usage detected");
            }

            return results.ToString();
        }

        /// <summary>
        /// Quick performance check for UI responsiveness
        /// </summary>
        public static bool IsPerformanceAcceptable()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var processService = ProcessService.Instance;
                
                // Test critical operations
                var processes = processService.GetRunningProcesses();
                var suspicious = processService.GetSuspiciousProcesses();
                
                stopwatch.Stop();
                
                // Consider acceptable if total time < 500ms
                return stopwatch.ElapsedMilliseconds < 500;
            }
            catch
            {
                return false;
            }
        }
    }
}