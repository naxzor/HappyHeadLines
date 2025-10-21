namespace Contracts;

public record ArticlePublished(
    Guid Id,
    string Title,
    string Body,
    Guid AuthorId,
    string Language,
    string[] Tags,
    RegionScope RegionScope,
    DateTime? PublishedAt
);