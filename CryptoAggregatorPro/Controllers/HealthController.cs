using CryptoAggregatorPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Runtime;
using System.Text.Json;

namespace CryptoAggregatorPro.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {

        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<CryptoController> _logger;
        private readonly AppSettings _settings;

        public HealthController(IConnectionMultiplexer redis, ILogger<CryptoController> logger, IOptions<AppSettings> options)
        {
            _redis = redis;
            _logger = logger;
            _settings = options.Value;
        }

        /// <summary>
        /// Gets the current connection status for all configured cryptocurrency exchanges.
        /// </summary>
        /// <returns>A dictionary where the key is the exchange name and the value contains its connection status and last update timestamp.</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(Dictionary<string, ExchangeStatus?>), 200)]
        public async Task<IActionResult> GetStatus()
        {
            var db = _redis.GetDatabase();
            var result = new Dictionary<string, ExchangeStatus?>();
            foreach (var exchange in _settings.Exchanges)
            {
                var key = $"status:{exchange}";
                var value = await db.StringGetAsync(key);
                if (!value.IsNull)
                {
                    var status = JsonSerializer.Deserialize<ExchangeStatus>(value!);
                    result[exchange] = status;
                }
                else
                {
                    result[exchange] = null;
                }
            }
            return Ok(result);
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
    }
}
