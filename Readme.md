# 🚀 Xtreme Load Tester v4.2.1 | Professional Diagnostic Edition

High-performance HTTP stress testing and infrastructure diagnostic tool built with **C# and .NET 8**. Designed to identify bottlenecks and test WAF resilience.

## ✨ New in v4.2.1
* **🕵️ Advanced Error Breakdown:** Real-time tracking of HTTP status codes (e.g., 429 Too Many Requests, 503 Service Unavailable).
* **🎭 Dynamic User-Agent Masking:** Randomly rotates between 9+ modern browser signatures (Chrome, Firefox, Safari, iOS/Android) to bypass basic filters.
* **📊 Precision Latency Sampling:** Accurate p50, p95, and p99 metrics using thread-safe telemetry collection.
* **🛡️ Connection Resilience:** Optimized `SocketsHttpHandler` with connection pooling to prevent socket exhaustion.

## 🛠 Diagnostics & Status Codes
This tool helps identify how a server fails under pressure:
- **HTTP 429/403**: Active Rate Limiting / WAF Block (IP-based).
- **HTTP 503/504**: Resource Exhaustion (CPU/RAM/Database bottleneck).
- **Timeout**: Network saturation or complete service hang.

## 🚀 How to Run
1. **Clone:** `git clone https://github.com/YourNick/XtremeLoadTester.git`
2. **Build:** `dotnet build -c Release`
3. **Execute:** `dotnet run -c Release`

## 📈 Proven Benchmarks
| Metric | Capacity |
| :--- | :--- |
| **Max Throughput** | 15,000+ RPS |
| **Concurrent Workers** | Tested up to 10,000 |
| **Telemetry** | p50, p95, p99, Min/Max |

---
> **Disclaimer:** This tool is for authorized performance testing and educational purposes only.