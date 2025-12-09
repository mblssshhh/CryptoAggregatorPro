namespace CryptoAggregatorPro.Models
{
    public class AggregatedTicker
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal AveragePrice { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public DateTime Timestamp { get; set; }
        public int ExchangesCount { get; set; }
    }
}
