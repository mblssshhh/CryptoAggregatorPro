namespace CryptoAggregatorPro.Models
{
    public class BestOrderBook
    {
        public string Symbol { get; set; } = string.Empty;
        public OrderBookEntry? BestBid { get; set; }
        public OrderBookEntry? BestAsk { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
