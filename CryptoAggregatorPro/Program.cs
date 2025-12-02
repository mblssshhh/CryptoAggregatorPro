using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Crypto Aggregator Pro API", Version = "v1" });
});

var redisConfig = new StackExchange.Redis.ConfigurationOptions
{
    EndPoints = { "localhost:6379" },
    AbortOnConnectFail = false, 
    ConnectRetry = 5, 
    ReconnectRetryPolicy = new ExponentialRetry(1000)
};
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(StackExchange.Redis.ConnectionMultiplexer.Connect(redisConfig));

builder.Services.AddSingleton<CryptoAggregatorPro.Services.RabbitMqService>(); 

builder.Services.AddHostedService<CryptoAggregatorPro.Services.BinanceWebSocketService>();
builder.Services.AddHostedService<CryptoAggregatorPro.Services.KuCoinWebSocketService>();
builder.Services.AddHostedService<CryptoAggregatorPro.Services.DataAggregatorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto Aggregator Pro API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();