# 🌐 CryptoAggregatorPro

A powerful, real-time cryptocurrency data aggregator built with **.NET 8**, **Redis**, **RabbitMQ**, and **WebSockets**.

---

## 📚 Overview

CryptoAggregatorPro is a pet project designed to aggregate real-time market data from leading cryptocurrency exchanges (**Binance**, **KuCoin**).  
It collects tickers, order books, and aggregated metrics, caches them in **Redis**, and broadcasts live updates via **WebSocket**.

### Core Principles
- ⚡ High performance  
- 🛡 Reliability & fault tolerance  
- 📈 Scalability  
- 🧩 Modern .NET architecture  

---

## ✨ Key Highlights
- ⚡ Real-time streaming  
- 📊 Data aggregation (avg / min / max / volume)  
- ❤️ Health monitoring  
- 🗄 Redis + RabbitMQ  
- 🐳 Docker-ready  

---

## 🚀 Features

### 📌 REST API
- Tickers  
- Order books  
- Aggregated data  
- Best bid/ask  
- Health endpoints  
- Rate limiting  
- Swagger UI  

### 🔌 WebSocket Streaming
- ticker  
- orderbook  
- aggregated-ticker  
- best-orderbook  

### 💱 Supported Exchanges
- Binance  
- KuCoin  

### 🪙 Supported Symbols
- BTCUSDT  
- ETHUSDT  

---

## 🏗 Architecture

Data Flow:
Exchange WS → Background Service → RabbitMQ → Aggregator → Redis → API / WS Clients

---

## ⚙️ Installation

### 🔧 Local Development
```
git clone https://github.com/mblssshhh/CryptoAggregatorPro.git
```
```
cd CryptoAggregatorPro
```

### 🧩 Environment Variables

```
RABBITMQ_HOST=rabbitmq  
RABBITMQ_PORT=5672  
REDIS_HOST=redis  
REDIS_PORT=6379  
SYMBOLS=BTCUSDT,ETHUSDT  
EXCHANGES=Binance,KuCoin
```

### ▶️ Run
```
dotnet restore
```
```
dotnet run
```


Swagger: http://localhost:5000/swagger

---

## 🐳 Docker Setup

```
docker-compose up --build
```


API: http://localhost:5000  
RabbitMQ: http://localhost:15672  
Redis: 6379  

---

## 📡 Usage
REST endpoints listed above.

WebSocket:  
ws://localhost:5000/api/crypto/ws/{type}/{symbol}

---

## 🤝 Contributing
Pull requests are welcome!

## 📄 License
MIT License

