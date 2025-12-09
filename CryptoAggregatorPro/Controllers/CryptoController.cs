using CryptoAggregatorPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
        private readonly AppSettings _settings;

        public CryptoController(IConnectionMultiplexer redis, ILogger<CryptoController> logger, IOptions<AppSettings> options)
        {
            _redis = redis;
            _logger = logger;
            _settings = options.Value;
        }

        [HttpGet("ticker/{symbol}")]
        public async Task<IActionResult> GetTicker(string symbol)
        {
            var db = _redis.GetDatabase();
            var result = new Dictionary<string, TickerData?>();
            foreach (var exchange in _settings.Exchanges)
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
            var result = new Dictionary<string, OrderBookData?>();
            foreach (var exchange in _settings.Exchanges)
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