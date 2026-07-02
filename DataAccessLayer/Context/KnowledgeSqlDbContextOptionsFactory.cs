using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context;

public static class KnowledgeSqlDbContextOptionsFactory
{
    public static DbContextOptions<KnowledgeSqlDbContext> Create(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (builder.ConnectTimeout <= 0 || builder.ConnectTimeout > 5)
        {
            builder.ConnectTimeout = 5;
        }

        return new DbContextOptionsBuilder<KnowledgeSqlDbContext>()
            .UseSqlServer(builder.ConnectionString, options => options.CommandTimeout(15))
            .Options;
    }
}