using System.Text.Json.Serialization;
using ArticleDatabase.Data;
using ArticleDatabase.Sharding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IShardDbFactory, ShardDbFactory>();

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
app.MapControllers();
app.MapGet("/whoami", () => new { node = Environment.MachineName, pid = Environment.ProcessId, at = DateTime.UtcNow });
app.Run();