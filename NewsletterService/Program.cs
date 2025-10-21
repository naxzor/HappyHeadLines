using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var b = WebApplication.CreateBuilder(args);
b.Services.AddControllers();
b.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h => { h.Username("guest"); h.Password("guest"); });
        cfg.ConfigureEndpoints(ctx);
    });
});

b.Services.AddOpenTelemetry().WithTracing(tp => tp
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("NewsletterService"))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(o => o.Endpoint = new Uri(
        Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://jaeger:4317")));

var app = b.Build();
app.MapGet("/healthz", () => "ok");
app.MapControllers();
app.Run();