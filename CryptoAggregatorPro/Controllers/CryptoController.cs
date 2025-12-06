using CryptoAggregatorPro.Models;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace CryptoAggregatorPro.Controllers
{
    [ApiController]
    [Route("api/crypto")]
    public class CryptoController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<CryptoController> _logger;

        public CryptoController(IConnectionMultiplexer redis, ILogger<CryptoController> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        [HttpGet("ticker/{symbol}")]
        public async Task<IActionResult> GetTicker(string symbol)
        {
            var db = _redis.GetDatabase();
            var exchanges = new[] { "Binance", "KuCoin", "TestExchange" };

            var result = new Dictionary<string, TickerData?>();

            foreach (var exchange in exchanges) 
            {
                var key = $"ticker:{symbol}:{exchange}";
                var value = await db.StringGetAsync(key);
                if (!value.IsNull)
                {
                    var ticker = JsonSerializer.Deserialize<TickerData>(value!);
                    result[exchange] = ticker;
                    _logger.LogInformation("Retrieved ticker from Redis: {key}", key);
                }
                else
                {
                    result[exchange] = null;
                    _logger.LogWarning("No data for {key} in Redis", key);
                }
            }

            if (result.All(r => r.Value == null))
                return NotFound("No ticker data available for symbol");

            return Ok(result);
        }

        [HttpGet("orderbook/{symbol}")]
        public async Task<IActionResult> GetOrderBook(string symbol)
        {
            var db = _redis.GetDatabase();
            var exchanges = new[] { "Binance", "KuCoin", "TestExchange" };

            var result = new Dictionary<string, OrderBookData?>();

            foreach (var exchange in exchanges)
            {
                var key = $"orderbook:{symbol}:{exchange}";
                var value = await db.StringGetAsync(key);
                if (!value.IsNull)
                {
                    var orderBook = JsonSerializer.Deserialize<OrderBookData>(value!);
                    result[exchange] = orderBook;
                    _logger.LogInformation("Retrieved orderbook from Redis: {key}", key);
                }
                else
                {
                    result[exchange] = null;
                    _logger.LogWarning("No data for {key} in Redis", key);
                }
            }

            if (result.All(r => r.Value == null))
                return NotFound("No orderbook data available for symbol");

            return Ok(result);
        }
    }
}
