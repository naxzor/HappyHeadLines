using Contracts;

namespace CommentService.Api.Models;

public record CreateCommentRequest(
    Guid ArticleId, 
    string Author, 
    string Body);

public record CommentDto(
    Guid Id, 
    Guid ArticleId, 
    string Author, 
    string Body, 
    DateTime CreatedAt);