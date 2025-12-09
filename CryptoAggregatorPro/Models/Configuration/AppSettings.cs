namespace CryptoAggregatorPro.Models
{
    public class AppSettings
    {
        public string[] Symbols { get; set; } = { "BTCUSDT", "ETHUSDT" };
        public string[] Exchanges { get; set; } = { "Binance", "KuCoin" };
        public int ReconnectDelaySeconds { get; set; } = 5;
        public int PingIntervalMs { get; set; } = 18000;
    }
}
