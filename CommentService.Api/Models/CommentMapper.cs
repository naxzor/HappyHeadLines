using CommentDatabase.Models;

namespace CommentService.Api.Models;

public static class CommentMapper
{
    public static CommentDto ToDto(this Comment c) =>
        new(c.Id, c.ArticleId, c.Author, c.Body, c.CreatedAt);
}