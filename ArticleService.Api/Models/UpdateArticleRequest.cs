namespace ArticleService.Api.Models;

public record UpdateArticleRequest(
    string?     Title,
    string?     Body,
    Guid?       AuthorId,
    string?     Language,
    string[]?   Tags,
    DateTime?   PublishedAt
);