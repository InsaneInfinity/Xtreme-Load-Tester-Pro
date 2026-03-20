# 🚀 Xtreme Load Tester v4.6.0 | Pure Power & Stealth Edition

High-performance HTTP stress testing and infrastructure diagnostic tool built with **C# and .NET 10**. Engineered to identify bottlenecks, test WAF resilience, and simulate real-world browser traffic.

## ✨ New in v4.6.0 "Pure Power"
* **🕵️ Ghost Mode 2.0:** Enhanced header spoofing (`Sec-Fetch`, `Accept-Language`, `Referer`) to bypass advanced WAF fingerprinting.
* **⚡ Atomic Telemetry:** Transitioned to `Interlocked` counters for O(1) performance, allowing millions of requests without local overhead.
* **📊 Precision Latency Sampling:** Accurate **p50, p95, and p99** metrics using thread-safe telemetry collection and optimized percentile algorithms.
* **🛡️ Connection Resilience:** Fine-tuned `SocketsHttpHandler` with connection pooling to prevent socket exhaustion at high concurrency.
* **🧩 Smart URL Handling:** Automatic protocol completion (http/https) and robust exception handling for network resets.

## 🛠 Diagnostics & Status Codes
This tool provides deep insights into how a server behaves under heavy load:
- **HTTP 429/403**: Active Rate Limiting or WAF intervention (IP-based blocking).
- **HTTP 503/504**: Resource Exhaustion (CPU/RAM or Database bottleneck).
- **Network Reset / Timeout**: Complete service hang or firewall-level packet dropping.

## 🚀 Getting Started
1. **Clone:** `git clone https://github.com/YourNick/XtremeLoadTester.git`
2. **Setup:** Ensure you have .NET 10 SDK installed.
3. **Build:** `dotnet build -c Release`
4. **Execute:** `dotnet run -c Release`

## 📈 Technical Benchmarks
| Metric | Capacity |
| :--- | :--- |
| **Max Throughput** | 20,000+ RPS (Link speed dependent) |
| **Concurrent Workers** | Scalable up to 15,000+ threads |
| **Telemetry** | Real-time p50, p95, p99 & Error Breakdown |
| **Compatibility** | Windows, Linux, macOS (Cross-platform .NET) |

---
> **Disclaimer:** This tool is for authorized performance testing and educational purposes only. The author is not responsible for any misuse or damage caused by this software. Use it responsibly on infrastructure you own or have explicit permission to test.