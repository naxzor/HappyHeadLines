using System.Text.Json.Serialization;
using ArticleDatabase.Sharding;
using ArticleService.Api.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IShardDbFactory, ShardDbFactory>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ArticlePublishedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tp => tp
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: "ArticleService",
                            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MassTransit") 
        .AddOtlpExporter(o =>
        {
            var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                           ?? "http://jaeger:4317";
            o.Endpoint = new Uri(endpoint);
        })
    );

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IShardDbFactory>();
    foreach (var s in factory.AllScopes)
    {
        using var db = factory.Create(s);
        db.Database.Migrate();
    }
}
else
{
    app.UseHttpsRedirection();
}

app.MapGet("/healthz", () => "ok");
app.MapGet("/whoami", () => new
{
    node = Environment.MachineName,
    pid = Environment.ProcessId,
    at = DateTime.UtcNow
});
app.MapControllers();

app.Run();
