using CryptoAggregatorPro.Controllers;
using CryptoAggregatorPro.Models;
using CryptoAggregatorPro.Services;
using DotNetEnv;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);
Env.Load();
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
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
    options.RejectionStatusCode = 429;
});
var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "redis";
var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");

Console.WriteLine($"REDIS_HOST: {redisHost}");
Console.WriteLine($"REDIS_PORT: {redisPort}");
Console.WriteLine($"REDIS_PASSWORD: {redisPassword ?? "NULL"}");
Console.WriteLine($"Using Redis password: {(string.IsNullOrEmpty(redisPassword) ? "NO" : "YES")}");

var redisConfig = new StackExchange.Redis.ConfigurationOptions
{
    EndPoints = { $"{redisHost}:{redisPort}" },
    AbortOnConnectFail = false,
    ConnectRetry = 5,
    ReconnectRetryPolicy = new ExponentialRetry(1000)
};
if (!string.IsNullOrEmpty(redisPassword))
{
    redisConfig.Password = redisPassword;
}
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(StackExchange.Redis.ConnectionMultiplexer.Connect(redisConfig));
builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddHostedService<BinanceWebSocketService>();
builder.Services.AddHostedService<KuCoinWebSocketService>();
builder.Services.AddHostedService<DataAggregatorService>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
var app = builder.Build();
app.UseWebSockets();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto Aggregator Pro API v1"));
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();