namespace CryptoAggregatorPro.Models
{
    public class TickerData
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public DateTime Timestamp { get; set; }
        public string Exchange { get; set; } = string.Empty;
    }
}
