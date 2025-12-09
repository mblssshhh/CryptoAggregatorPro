namespace CryptoAggregatorPro.Models
{
    public class OrderBookData
    {
        public string Symbol { get; set; } = string.Empty;
        public List<OrderBookEntry> Bids { get; set; } = new List<OrderBookEntry>();
        public List<OrderBookEntry> Asks { get; set; } = new List<OrderBookEntry>();
        public DateTime Timestamp { get; set; }
        public string Exchange { get; set; } = string.Empty;
    }

    public class OrderBookEntry
    {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
