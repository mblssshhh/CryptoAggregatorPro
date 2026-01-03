# 🚀 CryptoAggregatorPro

> **Modern .NET 8 cryptocurrency data aggregation platform**
> Real‑time prices • Redis caching • RabbitMQ messaging • Docker‑ready

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Redis](https://img.shields.io/badge/Redis-In--Memory-red)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Messaging-orange)
![Docker](https://img.shields.io/badge/Docker-Ready-blue)

---

## 📌 Overview

**CryptoAggregatorPro** is a high‑performance cryptocurrency data aggregator built on **.NET 8**.
It collects, processes, and aggregates **real‑time market data** from leading exchanges such as **Binance** and **KuCoin**, delivering reliable, low‑latency price and volume metrics via a clean Web API.

Designed for **developers**, **traders**, and **distributed systems**, the project follows modern backend and microservice principles with emphasis on **scalability**, **fault tolerance**, and **observability**.

---

## 🧠 Core Concepts

* **Real‑time aggregation** via WebSockets
* **Asynchronous processing** with RabbitMQ
* **High‑speed caching** using Redis
* **Hosted background services** for exchange monitoring
* **Production‑ready API** with rate limiting and Swagger

---

## ⚙️ Tech Stack

| Technology     | Purpose                              |
| -------------- | ------------------------------------ |
| **.NET 8**     | Core platform & Web API              |
| **Redis**      | Cache & fast data storage            |
| **RabbitMQ**   | Event distribution & async messaging |
| **WebSockets** | Live exchange connections            |
| **Swagger**    | API documentation                    |
| **Docker**     | Containerized deployment             |

---

## ✨ Features

* 📊 **Multi‑Exchange Aggregation**
  Combine price and volume data from Binance & KuCoin

* ⚡ **Real‑Time Updates**
  Instant price streaming via WebSockets

* 🧠 **Redis Caching**
  Ultra‑fast reads with reconnect & retry support

* 📨 **RabbitMQ Messaging**
  Publish aggregated data to other services

* 🛡 **Rate Limiting**
  Fixed‑window limiter (100 req/min)

* 📖 **Swagger UI**
  Auto‑generated API documentation

* 🔧 **Highly Configurable**
  Symbols, exchanges, reconnect delays & ping intervals

* 🐳 **Docker Ready**
  Seamless local & production deployments

---

## 📋 Requirements

* **.NET SDK 8.0+**
* **Redis** (local or cloud)
* **RabbitMQ** (local or cloud)
* **Docker** *(optional)*
* **Exchange API access** *(public WebSockets supported)*

---

## 📥 Installation

### 1️⃣ Clone Repository

```bash
git clone https://github.com/mblssshhh/CryptoAggregatorPro.git
cd CryptoAggregatorPro
```

### 2️⃣ Restore Dependencies

```bash
dotnet restore
```

### 3️⃣ Environment Configuration

Create a `.env` file in the project root:

```env
RABBITMQ_HOST=rabbitmq
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=crypto_aggregator_user
RABBITMQ_PASSWORD=secretmq123

REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=secretredis123

SYMBOLS=BTCUSDT,ETHUSDT
EXCHANGES=Binance,KuCoin
RECONNECT_DELAY_SECONDS=10
PING_INTERVAL_MS=20000
```

### 4️⃣ App Settings

Edit `CryptoAggregatorPro/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AppSettings": {
    "Symbols": ["BTCUSDT", "ETHUSDT"],
    "Exchanges": ["Binance", "KuCoin"],
    "ReconnectDelaySeconds": 5,
    "PingIntervalMs": 18000
  }
}
```

---

## 🐳 Running Infrastructure (Local)

### Redis

```bash
redis-server
```

### RabbitMQ (Docker)

```bash
docker run -d \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:management
```

RabbitMQ UI: `http://localhost:15672`

---

## ▶️ Build & Run

```bash
dotnet build
dotnet run --project CryptoAggregatorPro/CryptoAggregatorPro.csproj
```

* API: `https://localhost:5001`
* Swagger UI: `https://localhost:5001/swagger`

---

## 🔌 Usage

### REST API

```http
GET /api/prices/{symbol}
```

Example:

```http
GET /api/prices/BTCUSDT
```

### WebSockets

```
/ws/ticker/BTCUSDT
```

Receive live aggregated price updates.

---

## 📊 Monitoring & Observability

* 📜 Console logging (Information level)
* 🐇 RabbitMQ Management UI for queues
* 🧠 Redis keys for cached prices

---

## 🏗 Architecture Overview

```
+-------------------+     WebSockets     +-------------------+
| Binance Exchange  | ----------------> |                   |
+-------------------+                   | Data Aggregator   |
                                        | (Hosted Services) |
+-------------------+     WebSockets    |                   |
| KuCoin Exchange   | ----------------> |                   |
+-------------------+                   +-------------------+
                                                |
                                                | Aggregate & Process
                                                v
+-------------------+                   +-------------------+
|   Redis Cache     | <--------------- | RabbitMQ Publisher|
| (Data Storage)    |                  | (Message Queue)   |
+-------------------+                   +-------------------+
        ^
        | Read Cached Data
        v
+-------------------+
|     Web API       |
|  Controllers &    |
|   Endpoints       |
+-------------------+
        ^
        | HTTP Requests
        v
+-------------------+
|   Clients/Users   |
+-------------------+
```

---

## 📜 License

This project is licensed under the **MIT License**.
See the `LICENSE` file for details.

---

## ⭐ Contribution & Support

If you find this project useful:

* ⭐ Star the repository
* 🛠 Submit pull requests
* 🐞 Report issues

**CryptoAggregatorPro — built for speed, reliability, and scale.**
