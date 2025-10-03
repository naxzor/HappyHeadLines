using ArticleDatabase.Data;
using ArticleDatabase.Models;

namespace ArticleDatabase.Sharding;

public interface IShardDbFactory
{
    ArticlesDbContext Create(RegionScope scope);
    IReadOnlyList<RegionScope> AllScopes { get; }
}