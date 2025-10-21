using CommentDatabase.Data;
using CommentService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using CommentDatabase.Models;

namespace CommentService.Api.Controllers;

[ApiController]
[Route("comments")]
public class CommentsController : ControllerBase
{
    private readonly CommentsDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public CommentsController(CommentsDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    [HttpGet]
    public Task<ActionResult<IEnumerable<CommentDto>>> GetQuery([FromQuery] 
        Guid articleId, 
        int take = 50, 
        int skip = 0)
        => GetForArticle(articleId, take, skip);
    
    [HttpGet("article/{articleId:guid}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetForArticle(
        Guid articleId, 
        int take = 50, 
        int skip = 0)
    {
        var key = $"comments:{articleId}:skip={skip}:take={take}";
        var db = _redis.GetDatabase();

        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
            return Ok(JsonSerializer.Deserialize<List<CommentDto>>(cached!)!);

        var data = await _db.Comments
            .Where(c => c.ArticleId == articleId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(c => new CommentDto(c.Id, 
                c.ArticleId, 
                c.Author, 
                c.Body, 
                c.CreatedAt
                ))
            .ToListAsync();

        await db.StringSetAsync(key, JsonSerializer.Serialize(data), TimeSpan.FromSeconds(60));

        return Ok(data);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CreateCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Author) || string.IsNullOrWhiteSpace(req.Body))
            return BadRequest("Author and Body are required.");

        var entity = new Comment
        {
            Id = Guid.NewGuid(),
            ArticleId = req.ArticleId,
            Author = req.Author.Trim(),
            Body = req.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(entity);
        await _db.SaveChangesAsync();

        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var db = _redis.GetDatabase();
        foreach (var key in server.Keys(pattern: $"comments:{req.ArticleId}:*"))
            await db.KeyDeleteAsync(key);

        return CreatedAtAction(nameof(GetForArticle), 
            new { articleId = entity.ArticleId }, 
            entity.ToDto());
    }
}
