using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace XtremeLoadTester
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            MaxConnectionsPerServer  = 10000,
            EnableMultipleHttp2Connections = true
        });

        private static long totalRequests    = 0;
        private static long successCount     = 0;
        private static long errorCount       = 0;
        private static long latencySampleCount = 0;
        private static readonly ConcurrentQueue<long>              latencyQueue    = new ConcurrentQueue<long>();
        private static readonly ConcurrentDictionary<string, long> errorBreakdown  = new ConcurrentDictionary<string, long>();

        private static readonly string[] userAgents = {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/605.1.15"
        };

        static async Task Main(string[] args)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("🚀 XTREME LOAD TESTER v4.7.0 | Pure Power Edition");
            Console.WriteLine("--------------------------------------------------");
            Console.ResetColor();

            Console.Write("🌐 Target URL: ");
            string url = Console.ReadLine()?.Trim() ?? "";
            if (!url.StartsWith("http")) url = "https://" + url;

            // ── Metoda HTTP ───────────────────────────────────────────
            Console.Write("📡 Method (GET/POST) [GET]: ");
            string methodInput = Console.ReadLine()?.Trim().ToUpper() ?? "GET";
            bool isPost = methodInput == "POST";

            string postBody        = "";
            string postContentType = "application/json";

            if (isPost)
            {
                Console.Write("📦 Content-Type [application/json]: ");
                string ct = Console.ReadLine()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(ct)) postContentType = ct;

                Console.Write("📝 Body (single line JSON lub tekst): ");
                postBody = Console.ReadLine()?.Trim() ?? "{}";
            }

            Console.Write("🧵 Workers: ");
            int.TryParse(Console.ReadLine(), out int workersCount);

            Console.Write("⏳ Timeout (s): ");
            int.TryParse(Console.ReadLine(), out int timeout);

            Console.Write("🕵️ Stealth Delay (min max ms): ");
            string[] delays = Console.ReadLine()?.Split(' ') ?? new[] { "0", "0" };
            int.TryParse(delays[0], out int minDelay);
            int.TryParse(delays.Length > 1 ? delays[1] : "0", out int maxDelay);

            Console.Write("⏱ Duration (s): ");
            int.TryParse(Console.ReadLine(), out int duration);

            Console.WriteLine($"\n[!] Test startuje ({(isPost ? "POST" : "GET")})... Naciśnij dowolny klawisz, aby przerwać.");

            using var cts = new CancellationTokenSource();
            var sw = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, workersCount)
                .Select(_ => Task.Run(() => DoWork(url, timeout, minDelay, maxDelay, isPost, postBody, postContentType, cts.Token)))
                .ToList();

            _ = Task.Run(async () => {
                long prev = 0;
                while (!cts.IsCancellationRequested) {
                    await Task.Delay(1000);
                    long cur = Interlocked.Read(ref totalRequests);
                    Console.Write($"\r[LIVE] RPS: {cur - prev,5} | ✅ {Interlocked.Read(ref successCount),7} | ❌ {Interlocked.Read(ref errorCount),5} | ⏱ {Math.Max(0, duration - (int)sw.Elapsed.TotalSeconds),3}s  ");
                    prev = cur;
                }
            });

            if (Console.KeyAvailable) Console.ReadKey(true);
            try { await Task.Delay(TimeSpan.FromSeconds(duration), cts.Token); }
            catch (TaskCanceledException) { }

            cts.Cancel();
            await Task.WhenAll(tasks);
            PrintSummary(sw.Elapsed, url, isPost ? "POST" : "GET");
        }

        private static async Task DoWork(
            string url, int timeout, int min, int max,
            bool isPost, string postBody, string postContentType,
            CancellationToken ct)
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());

            while (!ct.IsCancellationRequested)
            {
                var requestSw = Stopwatch.StartNew();
                try
                {
                    if (max > min) await Task.Delay(rnd.Next(min, max), ct);

                    using var reqCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    reqCts.CancelAfter(TimeSpan.FromSeconds(timeout));

                    var method = isPost ? HttpMethod.Post : HttpMethod.Get;
                    using var req = new HttpRequestMessage(method, url);

                    req.Headers.UserAgent.ParseAdd(userAgents[rnd.Next(userAgents.Length)]);
                    req.Headers.Add("Referer", "https://www.google.com/");

                    if (isPost)
                    {
                        req.Content = new StringContent(postBody, Encoding.UTF8, postContentType);
                    }
                    else
                    {
                        req.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                    }

                    var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, reqCts.Token);
                    Interlocked.Increment(ref totalRequests);

                    if (resp.IsSuccessStatusCode) Interlocked.Increment(ref successCount);
                    else
                    {
                        Interlocked.Increment(ref errorCount);
                        TrackError($"HTTP {(int)resp.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Interlocked.Increment(ref totalRequests);
                        Interlocked.Increment(ref errorCount);
                        TrackError(ex is TaskCanceledException ? "Timeout" : "Błąd sieci");
                    }
                }
                finally
                {
                    requestSw.Stop();
                    if (Interlocked.Increment(ref latencySampleCount) <= 100000)
                        latencyQueue.Enqueue(requestSw.ElapsedMilliseconds);
                }
            }
        }

        private static void TrackError(string key) =>
            errorBreakdown.AddOrUpdate(key, 1, (_, old) => old + 1);

        private static void PrintSummary(TimeSpan elapsed, string url, string method)
        {
            var l = latencyQueue.OrderBy(x => x).ToList();
            Console.WriteLine($"\n\n" + new string('=', 60));
            Console.WriteLine($"📊 RAPORT PURE POWER v4.7.0");
            Console.WriteLine($"Target : {url}");
            Console.WriteLine($"Method : {method}");
            Console.WriteLine($"Czas   : {elapsed.TotalSeconds:0.0}s");
            Console.WriteLine($"Zapytań: {totalRequests}");
            Console.WriteLine($"Sukcesy: {successCount}");
            Console.WriteLine($"Błędy  : {errorCount}");
            if (l.Any())
                Console.WriteLine($"Latencja p50: {l[(int)(l.Count * 0.5)]}ms | p95: {l[(int)(l.Count * 0.95)]}ms | max: {l.Last()}ms");
            if (errorBreakdown.Any())
            {
                Console.WriteLine("\n📋 BŁĘDY:");
                foreach (var e in errorBreakdown) Console.WriteLine($" - {e.Key}: {e.Value}");
            }
            Console.WriteLine(new string('=', 60));
            Console.ReadLine();
        }
    }
}
