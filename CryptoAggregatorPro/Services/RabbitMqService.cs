using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
namespace CryptoAggregatorPro.Services
{
    public class RabbitMqService : IAsyncDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private const string QueueName = "crypto_data_queue";
        private readonly ILogger<RabbitMqService> _logger;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(10);
        private const int _maxRetries = 30;
        public RabbitMqService(ILogger<RabbitMqService> logger)
        {
            _logger = logger;
            _ = InitializeAsync();
        }
        private async Task InitializeAsync()
        {
            int attempt = 0;
            while (_connection == null || !_connection.IsOpen)
            {
                attempt++;
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
                        Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
                        UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
                        Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
                        AutomaticRecoveryEnabled = true,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                    };
                    _connection = await factory.CreateConnectionAsync();
                    _channel = await _connection.CreateChannelAsync();
                    await _channel.QueueDeclareAsync(
                        queue: QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to connect to RabbitMQ, attempt {attempt}");
                    if (attempt >= _maxRetries)
                    {
                        throw;
                    }
                    await Task.Delay(_retryDelay);
                }
            }
        }
        public async Task SendMessageAsync<T>(T data)
        {
            await EnsureConnectedAsync();
            var message = JsonSerializer.Serialize(data);
            var body = Encoding.UTF8.GetBytes(message);
            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueName,
                mandatory: false,
                body: body);
        }
        public async Task StartConsumingAsync(Func<string, Task> onMessageReceived)
        {
            await EnsureConnectedAsync();
            var consumer = new AsyncEventingBasicConsumer(_channel!);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    await onMessageReceived(message);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };
            await _channel!.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);
        }
        private async Task EnsureConnectedAsync()
        {
            while (_channel == null || !_channel.IsOpen)
            {
                await Task.Delay(500);
            }
        }
        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}