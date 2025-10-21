using Contracts;
using MassTransit;
namespace NewsletterService.Consumers;

public class NewArticleConsumer : IConsumer<ArticlePublished>
{
    private readonly ILogger<NewArticleConsumer> _logger;

    public NewArticleConsumer(ILogger<NewArticleConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<ArticlePublished> ctx)
    {
        _logger.LogInformation("Immediate newsletter: {Title} ({Region})",
            ctx.Message.Title, ctx.Message.RegionScope);

        return Task.CompletedTask;
    }
}