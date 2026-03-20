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
        private static readonly HttpClient client = new HttpClient(
            new SocketsHttpHandler
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
            Console.WriteLine("🚀 XTREME LOAD TESTER v4.1 | Professional Edition");
            Console.WriteLine("--------------------------------------------------");
            Console.ResetColor();

            // POPRAWKA #2: TryParse zamiast Parse – nie rzuca wyjątku przy złym wejściu
            Console.Write("🌐 Target URL: ");
            string url = Console.ReadLine() ?? "https://example.com";

            Console.Write("🛠 Method (GET/POST): ");
            string method = Console.ReadLine()?.ToUpper() ?? "GET";

            Console.Write("🧵 Concurrent Workers: ");
            if (!int.TryParse(Console.ReadLine(), out int workersCount) || workersCount <= 0)
            {
                Console.WriteLine("[!] Nieprawidłowa wartość, użyto domyślnej: 50");
                workersCount = 50;
            }

            Console.Write("⏱ Duration (seconds): ");
            if (!int.TryParse(Console.ReadLine(), out int duration) || duration <= 0)
            {
                Console.WriteLine("[!] Nieprawidłowa wartość, użyto domyślnej: 30");
                duration = 30;
            }

            Console.Write("⏳ Request Timeout (seconds, np. 10-20s dla wolnych serwerów): ");
            if (!int.TryParse(Console.ReadLine(), out int timeoutSeconds) || timeoutSeconds <= 0)
            {
                Console.WriteLine("[!] Nieprawidłowa wartość, użyto domyślnej: 10s");
                timeoutSeconds = 10;
            }
            // Ustawiamy timeout po inicjalizacji klienta – HttpClient pozwala na to przed pierwszym requestem
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            // POPRAWKA #3: Dodanie User-Agent – niektóre serwery odrzucają requesty bez UA
            client.DefaultRequestHeaders.UserAgent.ParseAdd("XtremeLoadTester/4.1");

            using var cts = new CancellationTokenSource();
            var sw = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, workersCount)
                .Select(_ => Task.Run(() => DoWork(url, method, cts.Token)))
                .ToList();

            // Live stats w tle
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    double elapsed = sw.Elapsed.TotalSeconds;
                    long reqs = Interlocked.Read(ref totalRequests);
                    long succ = Interlocked.Read(ref successCount);
                    long errs = Interlocked.Read(ref errorCount);
                    double errPct = reqs > 0 ? (double)errs / reqs * 100.0 : 0.0;

                    // POPRAWKA #4: Wyświetlanie % błędów obok live stats
                    Console.Write($"\r[LIVE] RPS: {reqs / (elapsed > 0 ? elapsed : 1):F0} | " +
                                  $"Success: {succ} | Errors: {errs} ({errPct:F1}%)   ");
                }
            });

            Console.WriteLine("\n[!] Stress test running... Press any key to stop early.");

            // POPRAWKA #5: Poprawne zatrzymywanie przez klawisz – działa w pętli, nie jednorazowo
            var keyTask = Task.Run(() =>
            {
                Console.ReadKey(true);
                cts.Cancel();
            });

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(duration), cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Zatrzymano przez użytkownika lub upłynął czas – oba przypadki są OK
            }

            cts.Cancel(); // Upewnij się, że cancel jest wywołany niezależnie od powodu wyjścia
            sw.Stop();
            await Task.WhenAll(tasks);

            PrintSummary(sw.Elapsed, url);
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
                            // Sampling: co 20-ste zapytanie do statystyk latencji
                            if (Interlocked.Read(ref totalRequests) % 20 == 0)
                                latencyQueue.Enqueue(requestSw.ElapsedMilliseconds);
                        }
                        else
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                }
                // POPRAWKA #6: Nie liczymy OperationCanceledException jako błędu – to normalne zatrzymanie
                catch (OperationCanceledException)
                {
                    // Wyjście z pętli bez inkrementacji errorCount
                    break;
                }
                catch (Exception ex) when (
                    ex is HttpRequestException ||
                    ex is TaskCanceledException    // TaskCanceledException z powodu Timeout HttpClient
                )
                {
                    Interlocked.Increment(ref errorCount);
                    Interlocked.Increment(ref totalRequests);
                }
                catch
                {
                    // Nieoczekiwany wyjątek – nadal liczymy jako błąd, ale nie crashujemy workera
                    Interlocked.Increment(ref errorCount);
                    Interlocked.Increment(ref totalRequests);
                }
            }
        }

        private static void PrintSummary(TimeSpan elapsed, string url)
        {
            var l = latencyQueue.OrderBy(x => x).ToList();
            string report = "\n\n" + new string('=', 55) + "\n";
            report += "📊 FINAL REPORT\n";
            report += $"Target:          {url}\n";
            report += $"Total Requests:  {totalRequests}\n";
            report += $"Success:         {successCount}\n";
            report += $"Errors:          {errorCount}";

            if (totalRequests > 0)
                report += $" ({(double)errorCount / totalRequests * 100.0:F1}%)";

            report += "\n";
            report += $"Elapsed:         {elapsed.TotalSeconds:F1}s\n";
            report += $"Average RPS:     {totalRequests / (elapsed.TotalSeconds > 0 ? elapsed.TotalSeconds : 1):F0}\n";

            if (l.Any())
            {
                report += $"p50 Latency:     {Percentile(l, 0.50)}ms\n";
                report += $"p95 Latency:     {Percentile(l, 0.95)}ms\n";
                report += $"p99 Latency:     {Percentile(l, 0.99)}ms\n";
                report += $"Min Latency:     {l.First()}ms\n";
                report += $"Max Latency:     {l.Last()}ms\n";
            }
            else
            {
                report += "Latency:         (brak próbek – za mało requestów)\n";
            }

            report += new string('=', 55);

            Console.WriteLine(report);

            // Zapis do pliku
            try
            {
                File.AppendAllText("stress_test_report.txt", $"\n[{DateTime.Now}]{report}");
                Console.WriteLine("\n[✔] Raport zapisany do stress_test_report.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[✘] Nie udało się zapisać raportu: {ex.Message}");
            }

            Console.WriteLine("Naciśnij dowolny klawisz, aby wyjść...");
            Console.ReadKey();
        }

        // POPRAWKA #7: Pomocnicza metoda percentyla – czystsza niż inline obliczenia
        private static long Percentile(List<long> sortedList, double percentile)
        {
            if (!sortedList.Any()) return 0;
            int index = (int)Math.Ceiling(percentile * sortedList.Count) - 1;
            return sortedList[Math.Max(0, Math.Min(index, sortedList.Count - 1))];
        }
    }
}
