using CryptoAggregatorPro.Models;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System.Text.Json;

namespace CryptoAggregatorPro.Services
{
    public class DataAggregatorService : BackgroundService
    {
        private readonly RabbitMqService _rabbitMq;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<DataAggregatorService> _logger;

        public DataAggregatorService(RabbitMqService rabbitMq, IConnectionMultiplexer redis, ILogger<DataAggregatorService> logger)
        {
            _rabbitMq = rabbitMq;
            _redis = redis;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000, stoppingToken);
            await _rabbitMq.StartConsumingAsync(async (message) =>
            {
                try
                {
                    var db = _redis.GetDatabase();
                    if (message.Contains("\"Price\"") && message.Contains("\"Volume\""))
                    {
                        var ticker = JsonSerializer.Deserialize<TickerData>(message);
                        if (ticker != null)
                        {
                            ticker.Symbol = ticker.Symbol.Replace("-", "");
                            await db.StringSetAsync($"ticker:{ticker.Symbol}:{ticker.Exchange}", message, TimeSpan.FromMinutes(1));
                        }
                    }
                    else if (message.Contains("\"Bids\"") && message.Contains("\"Asks\""))
                    {
                        var orderBook = JsonSerializer.Deserialize<OrderBookData>(message);
                        if (orderBook != null)
                        {
                            orderBook.Symbol = orderBook.Symbol.Replace("-", "");
                            await db.StringSetAsync($"orderbook:{orderBook.Symbol}:{orderBook.Exchange}", message, TimeSpan.FromMinutes(1));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Unknown message type");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            });
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}