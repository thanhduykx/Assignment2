using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context;

public static class KnowledgeSqlDbContextOptionsFactory
{
    public static DbContextOptions<KnowledgeSqlDbContext> Create(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KnowledgeSqlDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return optionsBuilder.Options;
    }
}