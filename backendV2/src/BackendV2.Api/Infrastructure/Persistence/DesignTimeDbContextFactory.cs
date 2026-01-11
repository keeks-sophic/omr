using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NetTopologySuite;

namespace BackendV2.Api.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("BACKENDV2_DB") 
                   ?? "Host=localhost;Port=5432;Database=skyback;Username=postgres;Password=1234";
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(conn, o => o.UseNetTopologySuite());
        return new AppDbContext(builder.Options);
    }
}
