using System.Text.Json;
using ArticleDatabase.Models;
using ArticleDatabase.Sharding;
using ArticleService.Api.Models;
using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ArticleService.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IShardDbFactory _factory;
    private readonly IConnectionMultiplexer? _redis;

    public ArticlesController(IShardDbFactory factory, IConnectionMultiplexer? redis = null)
    {
        _factory = factory;
        _redis = redis;
    }

    private static string ListKey(RegionScope scope, int skip, int take)
        => $"articles:{scope}:skip={skip}:take={take}";
    private static string ByIdKey(Guid id) => $"article:{id}";

    private async Task InvalidateListCachesAsync(RegionScope scope)
    {
        if (_redis is null) return;
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var db = _redis.GetDatabase();
        foreach (var key in server.Keys(pattern: $"articles:{scope}:*"))
            await db.KeyDeleteAsync(key);
    }

    private static readonly TimeSpan ArticleTtl = TimeSpan.FromDays(14);
    private static readonly TimeSpan ListTtl    = TimeSpan.FromDays(14);

    private async Task CacheSetAsync(string key, object value, TimeSpan? ttl = null)
    {
        if (_redis is null) return;
        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            key, 
            JsonSerializer.Serialize(value), 
            ttl ?? TimeSpan.FromDays(14));
    }

    private async Task<T?> CacheGetAsync<T>(string key)
    {
        if (_redis is null) return default;
        var db = _redis.GetDatabase();
        var raw = await db.StringGetAsync(key);
        return raw.HasValue ? JsonSerializer.Deserialize<T>(raw!) : default;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Article a, CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(RegionScope), a.RegionScope))
            return BadRequest("Invalid RegionScope.");

        a.Id = a.Id == Guid.Empty ? Guid.NewGuid() : a.Id;
        a.UpdatedAt = DateTime.UtcNow;

        await using var db = _factory.Create(a.RegionScope);
        db.Articles.Add(a);
        await db.SaveChangesAsync(ct);

        await InvalidateListCachesAsync(a.RegionScope);
        if (_redis is not null)
        {
            var dbCache = _redis.GetDatabase();
            await dbCache.KeyDeleteAsync(ByIdKey(a.Id));
        }

        return CreatedAtAction(nameof(GetById), new { id = a.Id }, a);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var cached = await CacheGetAsync<Article>(ByIdKey(id));
        if (cached is not null) return Ok(cached);

        foreach (var s in _factory.AllScopes)
        {
            await using var db = _factory.Create(s);
            var hit = await db.Articles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (hit is not null)
            {
                await CacheSetAsync(ByIdKey(id), hit, ArticleTtl);
                return Ok(hit);
            }
        }

        return NotFound();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateArticleRequest dto, CancellationToken ct)
    {
        foreach (var s in _factory.AllScopes)
        {
            await using var db = _factory.Create(s);
            var a = await db.Articles.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (a is null) continue;

            if (dto.Title is not null) a.Title = dto.Title;
            if (dto.Body is not null) a.Body = dto.Body;
            if (dto.AuthorId is not null) a.AuthorId = dto.AuthorId.Value;
            if (dto.Language is not null) a.Language = dto.Language;
            if (dto.Tags is not null) a.Tags = dto.Tags;
            if (dto.PublishedAt is not null) a.PublishedAt = dto.PublishedAt.Value;

            a.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            if (_redis is not null)
            {
                var dbCache = _redis.GetDatabase();
                await dbCache.KeyDeleteAsync(ByIdKey(id));
                await InvalidateListCachesAsync(a.RegionScope);
            }

            return Ok(a);
        }
        return NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        foreach (var s in _factory.AllScopes)
        {
            await using var db = _factory.Create(s);
            var a = await db.Articles.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (a is null) continue;

            db.Articles.Remove(a);
            await db.SaveChangesAsync(ct);

            await InvalidateListCachesAsync(a.RegionScope);
            if (_redis is not null)
            {
                var dbCache = _redis.GetDatabase();
                await dbCache.KeyDeleteAsync(ByIdKey(id));
            }

            return NoContent();
        }
        return NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] RegionScope? regionScope,
        [FromQuery] int take = 50,
        [FromQuery] int skip = 0,
        CancellationToken ct = default)
    {
        var scope = regionScope ?? RegionScope.GLOBAL;
        take = Math.Clamp(take, 1, 200);
        skip = Math.Max(0, skip);

        var key = ListKey(scope, skip, take);

        var cached = await CacheGetAsync<List<Article>>(key);
        if (cached is not null) return Ok(cached);

        await using var db = _factory.Create(scope);

        var items = await db.Articles.AsNoTracking()
            .Where(x => x.RegionScope == scope)
            .OrderByDescending(x => x.PublishedAt ?? x.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        await CacheSetAsync(key, items, ListTtl);

        return Ok(items);
    }
}
