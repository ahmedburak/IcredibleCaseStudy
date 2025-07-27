using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FileManager.Database;

/// <summary>
/// Design-time DbContext factory for migrations
/// </summary>
public class FileManagerContextFactory : IDesignTimeDbContextFactory<FileManagerContext>
{
    public FileManagerContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var databaseSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>() ?? throw new InvalidOperationException("DatabaseSettings must be configured in appsettings.json");

        var optionsBuilder = new DbContextOptionsBuilder<FileManagerContext>();
        optionsBuilder.UseNpgsql(databaseSettings.GetConnectionString());

        return new FileManagerContext(optionsBuilder.Options);
    }
}