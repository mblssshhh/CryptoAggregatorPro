namespace CryptoAggregatorPro.Services
{
    public class DataAggregatorService : BackgroundService
    {
        private readonly RabbitMqService _rabbitMq;
        private readonly ILogger<DataAggregatorService> _logger;

        public DataAggregatorService(RabbitMqService rabbitMq, ILogger<DataAggregatorService> logger)
        {
            _rabbitMq = rabbitMq;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000, stoppingToken);

            await _rabbitMq.StartConsumingAsync(async (message) =>
            {
                _logger.LogInformation("Received from queue: {msg}", message);
            });

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
