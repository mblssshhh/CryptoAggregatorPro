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
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);
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
                    _logger.LogInformation("Connection to RabbitMQ... ({0}/{1})", attempt, _maxRetries);
                    var factory = new ConnectionFactory
                    {
                        HostName = "rabbitmq",
                        Port = 5672,
                        UserName = "guest",
                        Password = "guest",
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
                    _logger.LogInformation("Connection to RabbitMQ is success. Queue '{Queue}' is created.", QueueName);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error connecting to RabbitMQ. Repeat in {Delay}с...", _retryDelay.TotalSeconds);
                    if (attempt >= _maxRetries)
                    {
                        _logger.LogError("The number of connection attempts to RabbitMQ has been exhausted.");
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
            _logger.LogInformation("[RabbitMQ] Send: {Message}", message);
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
                    _logger.LogInformation("[RabbitMQ] Processed: {Message}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from RabbitMQ");
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