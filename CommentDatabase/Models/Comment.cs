namespace CommentDatabase.Models;

public class Comment
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public string Author { get; set; } = default!;
    public string Body { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}