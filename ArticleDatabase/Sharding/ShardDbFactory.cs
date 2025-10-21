using Contracts;
using ArticleDatabase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ArticleDatabase.Sharding;

public class ShardDbFactory : IShardDbFactory, IAsyncDisposable
{
    private readonly Dictionary<RegionScope, (NpgsqlDataSource DataSource, DbContextOptions<ArticlesDbContext> Options)> _shards;

    public ShardDbFactory(IConfiguration cfg)
    {
        _shards = new();

        foreach (var scope in Enum.GetValues<RegionScope>())
        {
            var key = scope.ToString();
            var cs  = cfg.GetConnectionString(key)
                      ?? throw new InvalidOperationException($"Missing ConnectionStrings:{key}");

            var ds = new NpgsqlDataSourceBuilder(cs).Build();

            var options = new DbContextOptionsBuilder<ArticlesDbContext>()
                .UseNpgsql(ds, o =>
                {
                    o.EnableRetryOnFailure(maxRetryCount: 3);
                })
                .Options;

            _shards[scope] = (ds, options);
        }
    }

    public ArticlesDbContext Create(RegionScope scope)
        => new ArticlesDbContext(_shards[scope].Options);

    public IReadOnlyList<RegionScope> AllScopes => _shards.Keys.ToList();

    public async ValueTask DisposeAsync()
    {
        foreach (var s in _shards.Values)
            await s.DataSource.DisposeAsync();
    }
}