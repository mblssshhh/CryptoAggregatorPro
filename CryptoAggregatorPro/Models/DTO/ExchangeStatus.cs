namespace CryptoAggregatorPro.Models.DTO
{
    public class ExchangeStatus
    {
        public string Status { get; set; } = "Disconnected";
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }
}
