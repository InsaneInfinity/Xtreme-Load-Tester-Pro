using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace XtremeLoadTester
{
    class Program
    {
        // High-performance HttpClient configured for socket reuse
        private static readonly HttpClient client = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            MaxConnectionsPerServer = 10000,
            EnableMultipleHttp2Connections = true
        });

        private static long totalRequests = 0;
        private static long successCount = 0;
        private static long errorCount = 0;
        private static readonly ConcurrentQueue<long> latencyQueue = new ConcurrentQueue<long>();

        static async Task Main(string[] args)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🚀 XTREME LOAD TESTER v4.0 | Professional Edition");
            Console.WriteLine("--------------------------------------------------");
            Console.ResetColor();

            // Configuration
            Console.Write("🌐 Target URL: "); string url = Console.ReadLine() ?? "https://example.com";
            Console.Write("🛠 Method (GET/POST): "); string method = Console.ReadLine()?.ToUpper() ?? "GET";
            Console.Write("🧵 Concurrent Workers: "); int workersCount = int.Parse(Console.ReadLine() ?? "50");
            Console.Write("⏱ Duration (seconds): "); int duration = int.Parse(Console.ReadLine() ?? "30");

            using var cts = new CancellationTokenSource();
            var sw = Stopwatch.StartNew();

            // Fire up workers
            var tasks = Enumerable.Range(0, workersCount)
                .Select(_ => Task.Run(() => DoWork(url, method, cts.Token)))
                .ToList();

            // Live UI Monitoring
            _ = Task.Run(async () => {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    double elapsed = sw.Elapsed.TotalSeconds;
                    long reqs = Interlocked.Read(ref totalRequests);
                    Console.Write($"\r[LIVE] RPS: {reqs / elapsed:F0} | Success: {Interlocked.Read(ref successCount)} | Errors: {Interlocked.Read(ref errorCount)}   ");
                }
            });

            // Wait for time or keypress
            Console.WriteLine("\n[!] Stress test running... Press any key to stop early.");
            await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(duration)), Task.Run(() => Console.ReadKey(true)));
            
            cts.Cancel();
            sw.Stop();
            await Task.WhenAll(tasks);

            PrintSummary(sw.Elapsed);
        }

        private static async Task DoWork(string url, string method, CancellationToken ct)
        {
            var requestSw = new Stopwatch();
            var random = new Random(Guid.NewGuid().GetHashCode());

            while (!ct.IsCancellationRequested)
            {
                requestSw.Restart();
                try
                {
                    HttpResponseMessage response;
                    if (method == "POST")
                    {
                        var payload = new { id = random.Next(1, 10000), ts = DateTime.UtcNow, msg = "test" };
                        response = await client.PostAsJsonAsync(url, payload, ct);
                    }
                    else
                    {
                        response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    }

                    using (response)
                    {
                        requestSw.Stop();
                        Interlocked.Increment(ref totalRequests);
                        if (response.IsSuccessStatusCode)
                        {
                            Interlocked.Increment(ref successCount);
                            if (totalRequests % 20 == 0) latencyQueue.Enqueue(requestSw.ElapsedMilliseconds);
                        }
                        else Interlocked.Increment(ref errorCount);
                    }
                }
                catch { Interlocked.Increment(ref errorCount); Interlocked.Increment(ref totalRequests); }
            }
        }

        private static void PrintSummary(TimeSpan elapsed)
        {
            var l = latencyQueue.OrderBy(x => x).ToList();
            Console.WriteLine("\n\n" + new string('=', 50));
            Console.WriteLine("📊 FINAL REPORT");
            Console.WriteLine($"Total Requests: {totalRequests}");
            Console.WriteLine($"Average RPS: {totalRequests / elapsed.TotalSeconds:F0}");
            if (l.Any())
            {
                Console.WriteLine($"p50 Latency: {l[l.Count/2]}ms");
                Console.WriteLine($"p95 Latency: {l[(int)(l.Count*0.95)]}ms");
                Console.WriteLine($"p99 Latency: {l[(int)(l.Count*0.99)]}ms");
            }
            Console.WriteLine(new string('=', 50));
        }
    }
}
