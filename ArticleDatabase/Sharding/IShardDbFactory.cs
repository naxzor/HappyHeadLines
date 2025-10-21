using ArticleDatabase.Data;
using Contracts;

namespace ArticleDatabase.Sharding;

public interface IShardDbFactory
{
    ArticlesDbContext Create(RegionScope scope);
    IReadOnlyList<RegionScope> AllScopes { get; }
}