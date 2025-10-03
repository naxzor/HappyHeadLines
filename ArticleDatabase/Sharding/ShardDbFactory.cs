using ArticleDatabase.Data;
using ArticleDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ArticleDatabase.Sharding;

public class ShardDbFactory : IShardDbFactory
{
    private readonly Dictionary<RegionScope, DbContextOptions<ArticlesDbContext>> _opts = new();

    public ShardDbFactory(IConfiguration cfg)
    {
        foreach (var scope in Enum.GetValues<RegionScope>())
        {
            var key = scope.ToString();
            var cs = cfg.GetConnectionString(key)
                     ?? throw new InvalidOperationException($"Missing ConnectionStrings:{key}");
            var ob = new DbContextOptionsBuilder<ArticlesDbContext>().UseNpgsql(cs);
            _opts[scope] = ob.Options;
        }
    }

    public ArticlesDbContext Create(RegionScope scope) => new ArticlesDbContext(_opts[scope]);
    public IReadOnlyList<RegionScope> AllScopes => _opts.Keys.ToList();
}