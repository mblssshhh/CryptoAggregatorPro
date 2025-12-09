using CryptoAggregatorPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CryptoAggregatorPro.Controllers
{
    /// <summary>
    /// Provides WebSocket streaming for cryptocurrency market data.
    /// </summary>
    /// <remarks>
    /// This controller establishes a persistent WebSocket connection and streams crypto data 
    /// (ticker, orderbook, aggregated data) in real time from Redis cache.
    /// </remarks>
    [ApiController]
    [Route("api/crypto/ws")]
    public class CryptoWebSocketController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly AppSettings _settings;
        private readonly ILogger<CryptoWebSocketController> _logger;

        public CryptoWebSocketController(IConnectionMultiplexer redis, IOptions<AppSettings> options, ILogger<CryptoWebSocketController> logger)
        {
            _redis = redis;
            _settings = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Opens a WebSocket connection and subscribes the client to market updates based on requested type and symbol.
        /// </summary>
        /// <param name="type">Type of WebSocket data stream (ticker, orderbook, aggregated-ticker, best-orderbook).</param>
        /// <param name="symbol">Cryptocurrency symbol (e.g., BTCUSDT).</param>
        /// <returns>WebSocket stream with real-time crypto updates.</returns>
        /// <response code="400">The request was not a valid WebSocket request.</response>
        [HttpGet("{type}/{symbol}")]
        public async Task Get([FromRoute, BindRequired] WebSocketType type, string symbol)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocketAsync(webSocket, type.ToString(), symbol);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        /// <summary>
        /// Handles WebSocket lifecycle, Redis subscription, and message broadcasting.
        /// </summary>
        private async Task HandleWebSocketAsync(WebSocket webSocket, string type, string symbol)
        {
            var subscriber = _redis.GetSubscriber();
            var db = _redis.GetDatabase();
            var cancellationToken = HttpContext.RequestAborted;

            var channels = GetChannels(type, symbol);

            Action<RedisChannel, RedisValue> handler = (channel, message) =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var data = await GetDataAsync(type, symbol, db, message);
                        if (data != null)
                        {
                            var json = JsonSerializer.Serialize(data);
                            var bytes = Encoding.UTF8.GetBytes(json);
                            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending WebSocket message");
                    }
                });
            };

            foreach (var channel in channels)
                await subscriber.SubscribeAsync(channel, handler);

            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }

            foreach (var channel in GetChannels(type, symbol))
                await subscriber.UnsubscribeAsync(channel);

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", cancellationToken);
        }

        /// <summary>
        /// Builds a list of Redis channels to subscribe to based on the WebSocket request type.
        /// </summary>
        private RedisChannel[] GetChannels(string type, string symbol)
        {
            var baseChannels = _settings.Exchanges.Select(ex => (RedisChannel)$"updates:{type}:{symbol}:{ex}").ToArray();
            return type switch
            {
                "ticker" or "orderbook" => baseChannels,
                "aggregated-ticker" or "best-orderbook" => _settings.Exchanges.Select(ex => (RedisChannel)$"updates:ticker:{symbol}:{ex}").ToArray(),
                _ => Array.Empty<RedisChannel>()
            };
        }

        /// <summary>
        /// Retrieves data from Redis and prepares the message results depending on requested WebSocket type.
        /// </summary>
        private async Task<object?> GetDataAsync(string type, string symbol, IDatabase db, RedisValue message)
        {
            switch (type)
            {
                case "ticker":
                    return JsonSerializer.Deserialize<TickerData>(message!);

                case "orderbook":
                    return JsonSerializer.Deserialize<OrderBookData>(message!);

                case "aggregated-ticker":
                    var tickers = new List<TickerData>();
                    foreach (var exchange in _settings.Exchanges)
                    {
                        var key = $"ticker:{symbol}:{exchange}";
                        var value = await db.StringGetAsync(key);
                        if (!value.IsNull)
                        {
                            var ticker = JsonSerializer.Deserialize<TickerData>(value!);
                            if (ticker != null) tickers.Add(ticker);
                        }
                    }
                    if (!tickers.Any()) return null;
                    return new AggregatedTicker
                    {
                        Symbol = symbol,
                        AveragePrice = tickers.Average(t => t.Price),
                        TotalVolume = tickers.Sum(t => t.Volume),
                        MinPrice = tickers.Min(t => t.Price),
                        MaxPrice = tickers.Max(t => t.Price),
                        Timestamp = tickers.Max(t => t.Timestamp),
                        ExchangesCount = tickers.Count
                    };

                case "best-orderbook":
                    var orderBooks = new List<OrderBookData>();
                    foreach (var exchange in _settings.Exchanges)
                    {
                        var key = $"orderbook:{symbol}:{exchange}";
                        var value = await db.StringGetAsync(key);
                        if (!value.IsNull)
                        {
                            var orderBook = JsonSerializer.Deserialize<OrderBookData>(value!);
                            if (orderBook != null) orderBooks.Add(orderBook);
                        }
                    }
                    if (!orderBooks.Any()) return null;
                    var bestBid = orderBooks.SelectMany(ob => ob.Bids).MaxBy(b => b.Price);
                    var bestAsk = orderBooks.SelectMany(ob => ob.Asks).MinBy(a => a.Price);
                    return new BestOrderBook
                    {
                        Symbol = symbol,
                        BestBid = bestBid,
                        BestAsk = bestAsk,
                        Timestamp = DateTime.UtcNow
                    };

                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Types of supported WebSocket streams for crypto market data.
    /// </summary>
    public enum WebSocketType
    {
        ticker,
        orderbook,
        aggregated_ticker,
        best_orderbook
    }
}
