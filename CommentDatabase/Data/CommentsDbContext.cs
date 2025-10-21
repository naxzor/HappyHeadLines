using CommentDatabase.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentDatabase.Data;

public class CommentsDbContext : DbContext
{
    public CommentsDbContext(DbContextOptions<CommentsDbContext> options) : base(options) { }

    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Comment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ArticleId);
            e.Property(x => x.Author).HasMaxLength(200);
            e.Property(x => x.Body).HasMaxLength(10_000);
        });
    }
}