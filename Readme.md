# 🚀 Xtreme Load Tester Pro v4.7.0 — Pure Power Edition

**High-concurrency HTTP stress testing engine in pure C# — GET & POST support, real-time latency analytics, stealth mode, p50/p95/max metrics.**

[![.NET](https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![C#](https://img.shields.io/badge/C%23-Pure%20Power-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-v4.7.0-blueviolet)](#)

---

## 🎯 What is this?

Xtreme Load Tester Pro is a **command-line HTTP stress testing tool** built for raw performance. It spins up thousands of concurrent workers, rotates User-Agents, supports both GET and POST requests, tracks latency per request and delivers a full p50/p95/max report when done.

Built to find your infrastructure's breaking point — rate limits, connection limits, queue saturation — before attackers do.

---

## ⚡ How It Works

```
User Input
  └─ URL, method (GET/POST), body, workers, timeout, stealth delay, duration
        │
        ▼
┌─────────────────────────────────────────────┐
│         SocketsHttpHandler                  │
│  MaxConnectionsPerServer = 10,000           │
│  EnableMultipleHttp2Connections = true      │
│  PooledConnectionLifetime = 10 min          │
└──────────────┬──────────────────────────────┘
               │  N concurrent workers (Tasks)
               ▼
┌─────────────────────────────────────────────┐
│  DoWork() loop (per worker)                 │
│                                             │
│  1. Random stealth delay (min–max ms)       │
│  2. Pick random User-Agent                  │
│  3. GET → Add Accept + Referer headers      │
│     POST → StringContent(body, UTF-8, CT)   │
│  4. SendAsync (ResponseHeadersRead)         │
│  5. Track: success / error / latency        │
└──────────────┬──────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│  Live stats (every 1 second)                │
│  RPS | ✅ success | ❌ errors | ⏱ countdown  │
└──────────────┬──────────────────────────────┘
               │  on stop
               ▼
┌─────────────────────────────────────────────┐
│  Final Report                               │
│  Method, total requests, success, errors    │
│  Latency: p50 | p95 | max                  │
│  Error breakdown by type                    │
└─────────────────────────────────────────────┘
```

---

## ✨ Key Features

- **GET & POST support** — test REST APIs, login endpoints, form submissions with custom body and Content-Type
- **Massive concurrency** — `SocketsHttpHandler` with up to 10,000 connections per server, HTTP/2 multiplexing enabled
- **ResponseHeadersRead** — doesn't wait for response body, maximizes throughput and minimizes memory
- **Stealth delay** — configurable random delay between requests per worker (`min max ms`) to simulate real traffic patterns
- **User-Agent rotation** — randomizes between Chrome, Firefox and Safari Mobile on every request
- **Live RPS counter** — updates every second: requests/s, success count, error count, time remaining
- **Latency sampling** — collects up to 100,000 samples, computes p50, p95 and max on completion
- **Error breakdown** — tracks each error type separately: `HTTP 403`, `HTTP 429`, `Timeout`, `Network error`
- **Graceful shutdown** — `CancellationTokenSource` ensures all workers stop cleanly

---

## 🛠️ Tech Stack

- **Language:** C# (.NET 8+)
- **HTTP:** `SocketsHttpHandler` + `HttpClient` (shared, single instance — intentional for connection pooling)
- **Concurrency:** `Task.Run`, `Interlocked`, `ConcurrentQueue`, `ConcurrentDictionary`
- **Metrics:** `Stopwatch` per request, ordered latency percentiles
- **Transport:** HTTP/1.1 + HTTP/2 (multi-connection)

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8+ SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/InsaneInfinity/Xtreme-Load-Tester-Pro.git
cd Xtreme-Load-Tester-Pro

dotnet run
```

---

## 📡 Interactive Setup

### GET mode
```
🌐 Target URL:             https://your-target.com
📡 Method (GET/POST):      GET
🧵 Workers:                500
⏳ Timeout (s):            5
🕵️ Stealth Delay (min max): 10 50
⏱ Duration (s):            30
```

### POST mode
```
🌐 Target URL:             https://your-target.com/api/login
📡 Method (GET/POST):      POST
📦 Content-Type:           application/json
📝 Body:                   {"username":"test","password":"test123"}
🧵 Workers:                200
⏳ Timeout (s):            5
🕵️ Stealth Delay (min max): 0 0
⏱ Duration (s):            30
```

---

## 📊 Live Output

```
[LIVE] RPS:  4823 | ✅  144690 | ❌    12 | ⏱  17s
```

---

## 📋 Final Report

```
============================================================
📊 RAPORT PURE POWER v4.7.0
Target : https://your-target.com/api/login
Method : POST
Czas   : 30.0s
Zapytań: 186432
Sukcesy: 185901
Błędy  : 531

Latencja p50: 18ms | p95: 142ms | max: 3891ms

📋 BŁĘDY:
 - HTTP 429: 498
 - Timeout:   33
============================================================
```

---

## 🕵️ Stealth Mode

Set `Stealth Delay` to a `min max` range (in milliseconds) to randomize the delay between requests per worker:

```
0 0      → no delay, maximum aggression
10 50    → random 10–50ms per worker
100 500  → slow, human-like traffic simulation
```

Each worker picks an independent random delay — with 500 workers and `10 50ms` delay you still generate thousands of requests per second while bypassing simple rate limiters.

---

## 🧪 Use Cases

- **Rate limit discovery** — find the exact threshold where your WAF or API gateway starts returning 429
- **REST API load testing** — hammer POST endpoints with JSON payloads to test throughput and error handling
- **Infrastructure bottleneck detection** — identify where your stack saturates under load
- **p95 latency profiling** — measure real-world latency distribution under load, not just averages
- **WAF bypass testing** — test how your [Shield-X](https://github.com/InsaneInfinity/ShieldX-L7-DeepDefense) handles high-concurrency traffic with rotating User-Agents
- **Connection limit testing** — push `MaxConnectionsPerServer` to find OS-level TCP limits

---

## ⚠️ Disclaimer

> This tool is intended for **authorized performance testing and infrastructure auditing only**.
> Always obtain explicit written permission before testing any system you do not own.
> Unauthorized use against third-party systems may violate local laws and regulations.

---

## 🇵🇱 Opis projektu

Xtreme Load Tester Pro v4.7.0 to narzędzie do testów obciążeniowych HTTP napisane w C#. Obsługuje metody GET i POST — w trybie POST użytkownik podaje Content-Type i body (np. JSON), które jest wysyłane jako `StringContent` z kodowaniem UTF-8. Narzędzie uruchamia N równoległych workerów opartych na `SocketsHttpHandler` z limitem 10 000 połączeń per serwer i wsparciem HTTP/2. Każdy worker rotuje losowo między trzema User-Agentami i opcjonalnie odczekuje losowy czas przed każdym requestem (tryb stealth). Latencja każdego żądania jest mierzona przez `Stopwatch` i zbierana do kolejki (max 100 000 sampli) — po zakończeniu generowany jest raport z metrykami p50, p95 i max oraz podziałem błędów per typ.

---

## ⚖️ License

MIT — free to use, modify and distribute. See [LICENSE](LICENSE) for details.

---

Built with ❤️ — because "standard" protection is never enough.
