using CryptoAggregatorPro.Controllers;
using CryptoAggregatorPro.Models;
using CryptoAggregatorPro.Services;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Crypto Aggregator Pro API", Version = "v1" });
    c.ParameterFilter<SymbolParameterFilter>();
    c.SchemaGeneratorOptions = new Swashbuckle.AspNetCore.SwaggerGen.SchemaGeneratorOptions
    {
        UseAllOfForInheritance = true
    };
});

var redisConfig = new StackExchange.Redis.ConfigurationOptions
{
    EndPoints = { "redis:6379" },
    AbortOnConnectFail = false,
    ConnectRetry = 5,
    ReconnectRetryPolicy = new ExponentialRetry(1000)
};
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(StackExchange.Redis.ConnectionMultiplexer.Connect(redisConfig));
builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddHostedService<BinanceWebSocketService>();
builder.Services.AddHostedService<KuCoinWebSocketService>();
builder.Services.AddHostedService<DataAggregatorService>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

var app = builder.Build();

app.UseWebSockets();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto Aggregator Pro API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();