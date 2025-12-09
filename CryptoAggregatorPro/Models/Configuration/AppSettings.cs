namespace CryptoAggregatorPro.Models
{
    public class AppSettings
    {
        public string[] Symbols { get; set; } = Environment.GetEnvironmentVariable("SYMBOLS")?.Split(',') ?? new[] { "BTCUSDT", "ETHUSDT" };
        public string[] Exchanges { get; set; } = Environment.GetEnvironmentVariable("EXCHANGES")?.Split(',') ?? new[] { "Binance", "KuCoin" };
        public int ReconnectDelaySeconds { get; set; } = int.Parse(Environment.GetEnvironmentVariable("RECONNECT_DELAY_SECONDS") ?? "5");
        public int PingIntervalMs { get; set; } = int.Parse(Environment.GetEnvironmentVariable("PING_INTERVAL_MS") ?? "18000");
    }
}