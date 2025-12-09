using CryptoAggregatorPro.Models;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Globalization;
using CryptoAggregatorPro.Helpers;

namespace CryptoAggregatorPro.Services
{
    public class KuCoinWebSocketService : BackgroundService
    {
        private readonly RabbitMqService _rabbitMq;
        private readonly ILogger<KuCoinWebSocketService> _logger;
        private readonly AppSettings _settings;
        private TimeSpan _reconnectDelay;
        private int _pingIntervalMs;

        public KuCoinWebSocketService(RabbitMqService rabbitMq, ILogger<KuCoinWebSocketService> logger, IOptions<AppSettings> options)
        {
            _rabbitMq = rabbitMq;
            _logger = logger;
            _settings = options.Value;
            _reconnectDelay = TimeSpan.FromSeconds(_settings.ReconnectDelaySeconds);
            _pingIntervalMs = _settings.PingIntervalMs;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var httpClient = new HttpClient();
            while (!stoppingToken.IsCancellationRequested)
            {
                ClientWebSocket? ws = null;
                PeriodicTimer? pingTimer = null;
                Task? pingTask = null;
                try
                {
                    var response = await httpClient.PostAsync("https://api.kucoin.com/api/v1/bullet-public", null, stoppingToken);
                    response.EnsureSuccessStatusCode();
                    var responseJson = await response.Content.ReadAsStringAsync(stoppingToken);
                    using var doc = JsonDocument.Parse(responseJson);
                    var data = doc.RootElement.GetProperty("data");
                    var token = data.GetProperty("token").GetString();
                    var server = data.GetProperty("instanceServers")[0];
                    var endpoint = server.GetProperty("endpoint").GetString();
                    _pingIntervalMs = server.GetProperty("pingInterval").GetInt32(); // Override from API if needed
                    var connectId = Guid.NewGuid().ToString();
                    var uri = new Uri($"{endpoint}?token={token}&connectId={connectId}");
                    ws = new ClientWebSocket();
                    await ws.ConnectAsync(uri, stoppingToken);
                    foreach (var symbol in _settings.Symbols.Select(s => s.Replace("USDT", "-USDT").ToUpperInvariant()))
                    {
                        await SubscribeAsync(ws, $"/market/ticker:{symbol}", stoppingToken);
                        await SubscribeAsync(ws, $"/spotMarket/level2Depth5:{symbol}", stoppingToken);
                    }
                    pingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_pingIntervalMs));
                    pingTask = SendPingsAsync(ws, pingTimer, stoppingToken);
                    var buffer = new byte[8192];
                    while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                    {
                        var result = await ws.ReceiveAsync(buffer, stoppingToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                            break;
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            ProcessMessage(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "KuCoin: error parsing message");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KuCoin WebSocket error, reconnecting...");
                }
                finally
                {
                    try { ws?.Abort(); ws?.Dispose(); } catch { }
                    try { pingTimer?.Dispose(); } catch { }
                    if (pingTask != null) { try { await pingTask; } catch { } }
                }
                await Task.Delay(_reconnectDelay, stoppingToken);
            }
        }

        private void ProcessMessage(string message)
        {
            using var jsonDoc = JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;
            if (!(root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "message"))
                return;
            var topic = root.GetProperty("topic").GetString() ?? "";
            var dataElem = root.GetProperty("data");
            JsonElement payload = dataElem.TryGetProperty("data", out var inner) ? inner : dataElem;
            if (topic.StartsWith("/market/ticker:"))
            {
                HandleTicker(topic, payload);
            }
            else if (topic.StartsWith("/spotMarket/level2Depth5:"))
            {
                HandleOrderBook(topic, payload);
            }
        }

        private async void HandleTicker(string topic, JsonElement payload)
        {
            var symbolRaw = topic.Split(':')[1];
            var symbolNorm = symbolRaw.Replace("-", "").ToUpperInvariant();
            decimal price = 0m;
            decimal volume = 0m;
            if (payload.TryGetProperty("price", out var pProp))
                decimal.TryParse(pProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out price);
            if (payload.TryGetProperty("volValue", out var vvProp))
                decimal.TryParse(vvProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out volume);
            else if (payload.TryGetProperty("vol", out var vProp))
                decimal.TryParse(vProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out volume);
            DateTime timestamp = DateTime.UtcNow;
            if (payload.TryGetProperty("time", out var tProp) && tProp.ValueKind == JsonValueKind.Number)
            {
                var ms = tProp.GetInt64();
                timestamp = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
            }
            var ticker = new TickerData
            {
                Symbol = symbolNorm,
                Price = price,
                Volume = volume > 0 ? volume : await KuCoinVolumeProvider.GetVolumeAsync(symbolRaw),
                Timestamp = timestamp,
                Exchange = "KuCoin"
            };
            await _rabbitMq.SendMessageAsync(ticker);
        }

        private async void HandleOrderBook(string topic, JsonElement payload)
        {
            var symbolRaw = topic.Split(':')[1];
            var symbolNorm = symbolRaw.Replace("-", "").ToUpperInvariant();
            var bids = ParseOrderBook(payload, "bids");
            var asks = ParseOrderBook(payload, "asks");
            DateTime timestamp = DateTime.UtcNow;
            if (payload.TryGetProperty("timestamp", out var tsProp) && tsProp.ValueKind == JsonValueKind.Number)
            {
                timestamp = DateTimeOffset.FromUnixTimeMilliseconds(tsProp.GetInt64()).UtcDateTime;
            }
            var orderBook = new OrderBookData
            {
                Symbol = symbolNorm,
                Bids = bids,
                Asks = asks,
                Timestamp = timestamp,
                Exchange = "KuCoin"
            };
            await _rabbitMq.SendMessageAsync(orderBook);
        }

        private List<OrderBookEntry> ParseOrderBook(JsonElement payload, string side)
        {
            var list = new List<OrderBookEntry>();
            if (payload.TryGetProperty(side, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
                    {
                        var p = item[0].GetString();
                        var q = item[1].GetString();
                        if (decimal.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                            decimal.TryParse(q, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                        {
                            list.Add(new OrderBookEntry { Price = price, Quantity = qty });
                        }
                    }
                }
            }
            return list;
        }

        private async Task SubscribeAsync(ClientWebSocket ws, string topic, CancellationToken ct)
        {
            var subMessage = JsonSerializer.Serialize(new
            {
                id = Guid.NewGuid().ToString(),
                type = "subscribe",
                topic,
                privateChannel = false,
                response = true
            });
            var bytes = Encoding.UTF8.GetBytes(subMessage);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }

        private async Task SendPingsAsync(ClientWebSocket ws, PeriodicTimer timer, CancellationToken ct)
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (ws.State != WebSocketState.Open) break;
                var pingMessage = JsonSerializer.Serialize(new { id = Guid.NewGuid().ToString(), type = "ping" });
                var bytes = Encoding.UTF8.GetBytes(pingMessage);
                try { await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct); }
                catch { break; }
            }
        }
    }
}