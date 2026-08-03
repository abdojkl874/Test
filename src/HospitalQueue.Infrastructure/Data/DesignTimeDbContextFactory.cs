using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HospitalQueue.Infrastructure.Data;

/// <summary>
/// Lets `dotnet ef migrations add` run against this project directly using a
/// fallback connection string, without needing the Web host to build.
/// The real connection string at runtime comes from appsettings.json in HospitalQueue.Web.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("HOSPITALQUEUE_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=hospital_queue;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
