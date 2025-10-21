using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArticleDatabase.Models;
using ArticleDatabase.Sharding;
using Contracts;

namespace ArticleService.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class ArticlesController(IShardDbFactory factory) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Article a, CancellationToken ct)
    {
        a.Id = a.Id == Guid.Empty ? Guid.NewGuid() : a.Id;
        a.UpdatedAt = DateTime.UtcNow;

        await using var db = factory.Create(a.RegionScope); 
        db.Articles.Add(a);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = a.Id }, a);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        foreach (var s in factory.AllScopes)
        {
            using var db = factory.Create(s);
            var hit = await db.Articles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (hit is not null) return Ok(hit);
        }
        return NotFound();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Article dto, CancellationToken ct)
    {
        foreach (var s in factory.AllScopes)
        {
            await using var db = factory.Create(s);
            var a = await db.Articles.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (a is null) continue;

            a.Title       = dto.Title ?? a.Title;
            a.Body        = dto.Body  ?? a.Body;
            a.AuthorId    = dto.AuthorId;
            a.Language    = dto.Language ?? a.Language;
            a.Tags        = dto.Tags ?? a.Tags;
            a.PublishedAt = dto.PublishedAt;
            a.UpdatedAt   = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Ok(a);
        }
        return NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        foreach (var s in factory.AllScopes)
        {
            await using var db = factory.Create(s);
            var a = await db.Articles.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (a is null) continue;

            db.Articles.Remove(a);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        return NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RegionScope? regionScope, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var scope = regionScope ?? RegionScope.GLOBAL;
        await using var db = factory.Create(scope);

        var items = await db.Articles.AsNoTracking()
            .Where(x => x.RegionScope == scope)
            .OrderByDescending(x => x.PublishedAt ?? x.UpdatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);

        return Ok(items);
    }
}
