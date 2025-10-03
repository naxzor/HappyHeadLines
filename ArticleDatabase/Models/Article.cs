using System.ComponentModel.DataAnnotations;

namespace ArticleDatabase.Models;

public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(250)]
    public string Title { get; set; } = default!;

    [Required]
    public string Body { get; set; } = default!;

    public Guid? AuthorId { get; set; }

    [Required, MaxLength(10)]
    public string Language { get; set; } = "en";

    public string[] Tags { get; set; } = Array.Empty<string>();
    
    public RegionScope RegionScope { get; set; } = RegionScope.GLOBAL;

    public DateTime? PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}