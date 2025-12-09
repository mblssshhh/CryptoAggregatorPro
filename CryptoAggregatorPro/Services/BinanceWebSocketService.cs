using CryptoAggregatorPro.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CryptoAggregatorPro.Services
{
    public class BinanceWebSocketService : BackgroundService
    {
        private readonly RabbitMqService _rabbitMq;
        private readonly ILogger<BinanceWebSocketService> _logger;
        private readonly AppSettings _settings;
        private TimeSpan _reconnectDelay;
        private readonly IConnectionMultiplexer _redis;

        public BinanceWebSocketService(RabbitMqService rabbitMq, ILogger<BinanceWebSocketService> logger, IOptions<AppSettings> options, IConnectionMultiplexer redis)
        {
            _rabbitMq = rabbitMq;
            _logger = logger;
            _settings = options.Value;
            _reconnectDelay = TimeSpan.FromSeconds(_settings.ReconnectDelaySeconds);
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var ws = new ClientWebSocket();
                try
                {
                    await ws.ConnectAsync(new Uri("wss://stream.binance.com:9443/stream"), stoppingToken);
                    await UpdateStatusAsync("Binance", "Connected");
                    var paramsList = new List<string>();
                    foreach (var symbol in _settings.Symbols.Select(s => s.ToLowerInvariant()))
                    {
                        paramsList.Add($"{symbol}@ticker");
                        paramsList.Add($"{symbol}@depth5@100ms");
                    }
                    var subMessage = JsonSerializer.Serialize(new
                    {
                        method = "SUBSCRIBE",
                        @params = paramsList,
                        id = 1
                    });
                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(subMessage)), WebSocketMessageType.Text, true, stoppingToken);
                    var buffer = new byte[4096];
                    while (ws.State == WebSocketState.Open)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            using var jsonDoc = JsonDocument.Parse(message);
                            if (jsonDoc.RootElement.TryGetProperty("data", out var data) && jsonDoc.RootElement.TryGetProperty("stream", out var streamProp))
                            {
                                var stream = streamProp.GetString()!;
                                if (data.TryGetProperty("e", out var eProp))
                                {
                                    var eventType = eProp.GetString();
                                    if (eventType == "24hrTicker")
                                    {
                                        var symbol = data.GetProperty("s").GetString()!.Replace("-", "").ToUpper();
                                        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(data.GetProperty("E").GetInt64()).UtcDateTime;
                                        var ticker = new TickerData
                                        {
                                            Symbol = symbol,
                                            Price = decimal.Parse(data.GetProperty("c").GetString()!),
                                            Volume = decimal.Parse(data.GetProperty("v").GetString()!),
                                            Timestamp = timestamp,
                                            Exchange = "Binance"
                                        };
                                        await _rabbitMq.SendMessageAsync(ticker);
                                    }
                                }
                                else if (data.TryGetProperty("lastUpdateId", out _))
                                {
                                    var symbolRaw = stream.Split('@')[0];
                                    var symbol = symbolRaw.Replace("-", "").ToUpper();
                                    var timestamp = DateTime.UtcNow;
                                    var orderBook = new OrderBookData
                                    {
                                        Symbol = symbol,
                                        Bids = ParseOrderBookEntries(data.GetProperty("bids")),
                                        Asks = ParseOrderBookEntries(data.GetProperty("asks")),
                                        Timestamp = timestamp,
                                        Exchange = "Binance"
                                    };
                                    await _rabbitMq.SendMessageAsync(orderBook);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse Binance message");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Binance WebSocket connection. Reconnecting...");
                    await UpdateStatusAsync("Binance", "Disconnected");
                }
                finally
                {
                    if (ws.State != WebSocketState.Closed)
                    {
                        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", stoppingToken); } catch { }
                    }
                    await Task.Delay(_reconnectDelay, stoppingToken);
                    await UpdateStatusAsync("Binance", "Disconnected");
                }
            }
        }

        private List<OrderBookEntry> ParseOrderBookEntries(JsonElement array)
        {
            return array.EnumerateArray().Select(item =>
                new OrderBookEntry
                {
                    Price = decimal.Parse(item[0].GetString()!),
                    Quantity = decimal.Parse(item[1].GetString()!)
                }).ToList();
        }

        private async Task UpdateStatusAsync(string exchange, string status)
        {
            var db = _redis.GetDatabase();
            var exchangeStatus = new ExchangeStatus { Status = status, LastUpdate = DateTime.UtcNow };
            await db.StringSetAsync($"status:{exchange}", JsonSerializer.Serialize(exchangeStatus), TimeSpan.FromMinutes(5));
        }
    }
}