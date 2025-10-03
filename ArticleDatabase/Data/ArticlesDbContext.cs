using Microsoft.EntityFrameworkCore;
using ArticleDatabase.Models;

namespace ArticleDatabase.Data;

public class ArticlesDbContext(DbContextOptions<ArticlesDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Article>(e =>
        {
            e.ToTable("articles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(250);
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.Tags).HasColumnType("text[]");
            e.Property(x => x.RegionScope)
                .HasConversion<string>()
                .ValueGeneratedNever();
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.RegionScope, x.PublishedAt });
        });
    }
}