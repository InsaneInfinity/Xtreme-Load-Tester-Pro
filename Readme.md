# 🚀 Xtreme Load Tester Pro v4.1

A high-performance, asynchronous HTTP stress testing tool built with **C# and .NET**. Designed for massive throughput and minimal resource overhead using modern asynchronous patterns.

## ✨ Key Features
* **Asynchronous Engine:** Leverages `Task.Run` and `HttpClient` with `SocketsHttpHandler` for maximum concurrency.
* **Low Memory Footprint:** Utilizes `HttpCompletionOption.ResponseHeadersRead` to avoid unnecessary buffer allocations and minimize GC pressure.
* **Thread-Safe Metrics:** High-speed counting using `Interlocked` operations to ensure data integrity under heavy load.
* **HTTP/2 Ready:** Supports multiplexing to saturate network bandwidth effectively.
* **Advanced Analytics:** Tracks **p50, p95, and p99 latencies** to provide a realistic view of server performance.
* **Dynamic Traffic:** Supports both GET and POST methods with dynamic JSON payload simulation.

## 🛠 Technologies & Concepts
* **Language:** C# 12 / .NET 8.0
* **Networking:** `HttpClient`, `SocketsHttpHandler` (Connection Pooling)
* **Concurrency:** `Task Parallel Library (TPL)`, `CancellationTokenSource`
* **Data structures:** `ConcurrentQueue` for thread-safe telemetry collection

## 🚀 How to Run
1. **Clone the repo:** `git clone https://github.com/TwojNick/XtremeLoadTester.git`
2. **Build for performance:** `dotnet build -c Release`
3. **Execute:** `dotnet run -c Release`

## 📈 Example Benchmarks
*Tested on local environment (results may vary based on hardware/network):*

| Metric | Value |
| :--- | :--- |
| **Max RPS** | 15,000+ |
| **Average p50** | 45ms |
| **Average p95** | 120ms |
| **Average p99** | 350ms |

---
> **Disclaimer:** This tool is for educational and authorized testing purposes only. Using it against targets without permission is illegal.