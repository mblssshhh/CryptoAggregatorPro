using CryptoAggregatorPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

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

        /// <summary>
        /// Ping the API to check if it's running
        /// </summary>
        /// <returns>Pong response</returns>
        [HttpGet("ping")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult Ping()
        {
            return Ok("Pong");
        }

        /// <summary>
        /// Get the current ticker for the specified symbol
        /// </summary>
        /// <param name="symbol">Cryptocurrency symbol (e.g., BTCUSDT, ETHUSDT)</param>
        /// <returns>A dictionary containing ticker data for each exchange</returns>
        [HttpGet("ticker/{symbol}")]
        [ProducesResponseType(typeof(Dictionary<string, TickerData?>), 200)]
        [ProducesResponseType(404)]
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
                }
                else
                {
                    result[exchange] = null;
                }
            }
            if (result.All(r => r.Value == null))
                return NotFound("No ticker data available for symbol");
            return Ok(result);
        }

        /// <summary>
        /// Get the current order book for the specified symbol
        /// </summary>
        /// <param name="symbol">Cryptocurrency symbol (e.g., BTCUSDT, ETHUSDT)</param>
        /// <returns>A dictionary containing order book data for each exchange</returns>
        [HttpGet("orderbook/{symbol}")]
        [ProducesResponseType(typeof(Dictionary<string, OrderBookData?>), 200)]
        [ProducesResponseType(404)]
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
                }
                else
                {
                    result[exchange] = null;
                }
            }
            if (result.All(r => r.Value == null))
                return NotFound("No orderbook data available for symbol");
            return Ok(result);
        }
    }

    /// <summary>
    /// Swagger hints for the 'symbol' parameter
    /// </summary>
    public class SymbolParameterFilter : IParameterFilter
    {
        private readonly AppSettings _settings;
        public SymbolParameterFilter(IOptions<AppSettings> options)
        {
            _settings = options.Value;
        }
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter.Name == "symbol")
            {
                parameter.Schema.Enum = _settings.Symbols
                    .Distinct()
                    .Select(s => new OpenApiString(s) as IOpenApiAny)
                    .ToList();
            }
        }
    }
}