using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace XtremeLoadTester
{
    class Program
    {
        // Statistics
        private static int _successCount = 0;
        private static int _failCount = 0;
        private static long _totalResponseTime = 0;

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("    XTREME LOAD TESTER PRO v2.0         ");
            Console.WriteLine("========================================");
            Console.ResetColor();

            Console.Write("Enter Target URL (e.g., https://example.com): ");
            string targetUrl = Console.ReadLine();

            Console.Write("Enter Thread Count (Concurrent Tasks): ");
            if (!int.TryParse(Console.ReadLine(), out int threadCount)) threadCount = 10;

            Console.Write("Enter Duration in seconds: ");
            if (!int.TryParse(Console.ReadLine(), out int duration)) duration = 30;

            Console.WriteLine($"\n[!] Starting Load Test on {targetUrl}...");
            Console.WriteLine($"[!] Threads: {threadCount} | Duration: {duration}s\n");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duration));
            var tasks = new List<Task>();

            // Warm up HttpClient
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "XtremeLoadTester/2.0");

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(() => Worker(client, targetUrl, cts.Token)));
            }

            // Real-time monitor loop
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(1000);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"\rRequests: Success={_successCount} | Failed={_failCount} | Elapsed={sw.Elapsed.Seconds}s");
            }

            await Task.WhenAll(tasks);
            sw.Stop();

            // Final Report
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n========================================");
            Console.WriteLine("           TEST COMPLETED               ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Total Successful Requests: {_successCount}");
            Console.WriteLine($"Total Failed Requests:     {_failCount}");
            
            if (_successCount > 0)
            {
                double avgTime = (double)_totalResponseTime / _successCount;
                Console.WriteLine($"Average Response Time:     {avgTime:F2} ms");
            }
            Console.WriteLine("========================================\n");
            Console.ResetColor();
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task Worker(HttpClient client, string url, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var response = await client.GetAsync(url, token);
                    sw.Stop();

                    if (response.IsSuccessStatusCode)
                    {
                        Interlocked.Increment(ref _successCount);
                        Interlocked.Add(ref _totalResponseTime, sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        Interlocked.Increment(ref _failCount);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref _failCount);
                }
            }
        }
    }
}
