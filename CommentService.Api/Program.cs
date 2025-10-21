using System.Text.Json.Serialization;
using CommentDatabase.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cs = builder.Configuration.GetConnectionString("Comments")
         ?? Environment.GetEnvironmentVariable("COMMENTS_DB")
         ?? "Host=host.docker.internal;Port=5435;Database=comments;Username=postgres;Password=1234";
builder.Services.AddDbContext<CommentsDbContext>(opt => opt.UseNpgsql(cs));

var redisConn = builder.Configuration.GetConnectionString("Redis")
               ?? Environment.GetEnvironmentVariable("REDIS_URL")
               ?? "redis:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

builder.Services.AddOpenTelemetry()
    .WithTracing(tp => tp
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(
                    serviceName: "CommentService",
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.EnrichWithIDbCommand = (activity, cmd) =>
            {
                activity.SetTag("db.system", "postgresql");
            };
        })
        .AddOtlpExporter(o =>
        {
            var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                           ?? "http://jaeger:4317";
            o.Endpoint = new Uri(endpoint);
        }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CommentsDbContext>();
    db.Database.Migrate();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/healthz", () => "ok");
app.MapGet("/comments/healthz", () => "ok");
app.MapGet("/whoami", () => new
{
    node = Environment.MachineName, 
    pid = Environment.ProcessId, 
    at = DateTime.UtcNow
});

app.MapControllers();

app.Run();
