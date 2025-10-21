using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommentDatabase.Data;

public class DesignTimeFactory : IDesignTimeDbContextFactory<CommentsDbContext>
{
    public CommentsDbContext CreateDbContext(string[] args)
    {
        var cs = "Host=localhost;Port=5435;Database=comments;Username=postgres;Password=1234";

        var options = new DbContextOptionsBuilder<CommentsDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new CommentsDbContext(options);
    }
}