using MassTransit;
using ArticleDatabase.Models;
using ArticleDatabase.Sharding;
using Contracts;

namespace ArticleService.Api.Consumers;

public class ArticlePublishedConsumer (IShardDbFactory factory) : IConsumer<ArticlePublished>
{
    public async Task Consume(ConsumeContext<ArticlePublished> context)
    {
        var e = context.Message;

        await using var db = factory.Create(e.RegionScope);

        var entity = new Article
        {
            Id          = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id,
            Title       = e.Title,
            Body        = e.Body,
            AuthorId    = e.AuthorId,
            Language    = e.Language,
            Tags        = e.Tags,
            RegionScope = e.RegionScope,
            PublishedAt = e.PublishedAt,
            UpdatedAt   = DateTime.UtcNow
        };

        db.Articles.Add(entity);
        await db.SaveChangesAsync();
    }
}