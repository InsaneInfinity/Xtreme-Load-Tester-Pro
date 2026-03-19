# 🚀 Xtreme Load Tester

A high-performance, asynchronous HTTP stress testing tool built with C# and .NET. Designed for massive throughput and minimal resource overhead.

## ✨ Key Features
- **Asynchronous Engine**: Built on `Task.Run` and `HttpClient` for maximum concurrency.
- **Low Memory Footprint**: Uses `ResponseHeadersRead` to avoid unnecessary buffer allocations.
- **HTTP/2 Ready**: Supports multiplexing to saturate network bandwidth effectively.
- **Smart Metrics**: Tracks `p95` and `p99` latencies for realistic performance analysis.
- **POST/GET Support**: Simulates real traffic with dynamic JSON payloads.

## 🛠 How to run
1. Clone the repo.
2. Run `dotnet build -c Release`.
3. Execute `dotnet run -c Release`.

## 📈 Example Results
| Metric | Value |
|--------|-------|
| RPS    | 15,000+ |
| p95    | 120ms |
| p99    | 350ms |