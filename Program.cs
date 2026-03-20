using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace XtremeLoadTester
{
    class Program
    {
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
        private static readonly ConcurrentDictionary<string, long> errorBreakdown = new ConcurrentDictionary<string, long>();

        private static readonly string[] userAgents = new string[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/605.1.15"
        };

        private static void TrackError(string key) =>
            errorBreakdown.AddOrUpdate(key, 1, (_, old) => old + 1);

        static async Task Main(string[] args)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🚀 XTREME LOAD TESTER v4.2.1 | Stable Release");
            Console.WriteLine("--------------------------------------------------");
            Console.ResetColor();

            Console.Write("🌐 Target URL: "); string url = Console.ReadLine() ?? "https://example.com";
            Console.Write("🛠 Method (GET/POST): "); string method = Console.ReadLine()?.ToUpper() ?? "GET";
            Console.Write("🧵 Concurrent Workers: "); int.TryParse(Console.ReadLine(), out int workersCount);
            if (workersCount <= 0) workersCount = 50;

            Console.Write("⏱ Duration (seconds): "); int.TryParse(Console.ReadLine(), out int duration);
            if (duration <= 0) duration = 30;

            Console.Write("⏳ Request Timeout (seconds): "); int.TryParse(Console.ReadLine(), out int timeoutSeconds);
            if (timeoutSeconds <= 0) timeoutSeconds = 15;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            using var cts = new CancellationTokenSource();
            var sw = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, workersCount)
                .Select(_ => Task.Run(() => DoWork(url, method, cts.Token)))
                .ToList();

            // Monitoring Live
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    double elapsed = sw.Elapsed.TotalSeconds;
                    long reqs = Interlocked.Read(ref totalRequests);
                    Console.Write($"\r[LIVE] RPS: {reqs / (elapsed > 0 ? elapsed : 1):F0} | Success: {Interlocked.Read(ref successCount)} | Errors: {Interlocked.Read(ref errorCount)}   ");
                }
            });

            Console.WriteLine("\n[!] Test w toku... NACIŚNIJ DOWOLNY KLAWISZ, aby przerwać.");

            // ✅ POPRAWKA: Pętla oczekiwania na klawisz bez natychmiastowego Cancel()
            var keyTask = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        cts.Cancel();
                        break;
                    }
                    Thread.Sleep(100); // Małe opóźnienie, żeby nie obciążać procesora
                }
            });

            try 
            { 
                await Task.Delay(TimeSpan.FromSeconds(duration), cts.Token); 
            }
            catch (TaskCanceledException) { }

            cts.Cancel(); 
            sw.Stop();
            await Task.WhenAll(tasks);

            PrintSummary(sw.Elapsed, url);
        }

        private static async Task DoWork(string url, string method, CancellationToken ct)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            string ua = userAgents[random.Next(userAgents.Length)];

            while (!ct.IsCancellationRequested)
            {
                var requestSw = Stopwatch.StartNew();
                try
                {
                    using var request = new HttpRequestMessage(new HttpMethod(method), url);
                    request.Headers.UserAgent.ParseAdd(ua);
                    
                    if (method == "POST")
                        request.Content = JsonContent.Create(new { ts = DateTime.UtcNow, rand = random.Next() });

                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                    requestSw.Stop();

                    Interlocked.Increment(ref totalRequests);
                    if (response.IsSuccessStatusCode)
                    {
                        Interlocked.Increment(ref successCount);
                        if (totalRequests % 20 == 0) latencyQueue.Enqueue(requestSw.ElapsedMilliseconds);
                    }
                    else
                    {
                        TrackError($"HTTP {(int)response.StatusCode}");
                        Interlocked.Increment(ref errorCount);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    TrackError(ex is TaskCanceledException ? "Timeout" : "Błąd sieci");
                    Interlocked.Increment(ref errorCount);
                    Interlocked.Increment(ref totalRequests);
                }
            }
        }

        private static void PrintSummary(TimeSpan elapsed, string url)
        {
            var l = latencyQueue.OrderBy(x => x).ToList();
            Console.WriteLine("\n\n" + new string('=', 55));
            Console.WriteLine($"📊 RAPORT KOŃCOWY: {url}");
            Console.WriteLine($"Zapytań: {totalRequests} | RPS: {totalRequests / elapsed.TotalSeconds:F0}");
            Console.WriteLine($"Sukcesy: {successCount} | Błędy: {errorCount}");
            
            if (l.Any()) 
                Console.WriteLine($"Latencja p95: {l[(int)(l.Count * 0.95)]}ms | p99: {l[(int)(l.Count * 0.99)]}ms");

            if (errorBreakdown.Any())
            {
                Console.WriteLine("\n📋 ROZBICIE BŁĘDÓW:");
                foreach (var kv in errorBreakdown) Console.WriteLine($"  {kv.Value}x {kv.Key}");
            }
            Console.WriteLine(new string('=', 55));
            Console.ReadKey();
        }

        private static long Percentile(List<long> sortedList, double percentile)
        {
            if (!sortedList.Any()) return 0;
            int index = (int)Math.Ceiling(percentile * sortedList.Count) - 1;
            return sortedList[Math.Max(0, Math.Min(index, sortedList.Count - 1))];
        }
    }
}