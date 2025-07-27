using FileManager.Database;
using FileManager.Services;
using FileManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using static FileManager.Core.Constants;

namespace FileManager.Console;

/// <summary>
/// Service collection extensions for dependency injection configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add distributed file storage services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddDistributedFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration settings
        var databaseSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
        var storageSettings = configuration.GetSection("StorageSettings").Get<StorageSettings>();

        if (databaseSettings == null || storageSettings == null)
        {
            throw new InvalidOperationException("DatabaseSettings and StorageSettings must be configured in appsettings.json");
        }

        services.AddSingleton(databaseSettings);
        services.AddSingleton(storageSettings);

        var useInMemory = configuration.GetValue<bool>("DatabaseSettings:UseInMemory");
        if (useInMemory)
        {
            // Use SQLite for testing
            services.AddDbContext<FileManagerContext>(options =>
                options.UseSqlite($"Data Source={databaseSettings.Database}"));
        }
        else
        {
            // Use PostgreSQL for production
            services.AddDbContext<FileManagerContext>(options =>
                options.UseNpgsql(databaseSettings.GetConnectionString()));
        }

        // Core services
        services.AddScoped<IChecksumService, ChecksumService>();
        services.AddScoped<IChunkService, ChunkService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IManagerService, ManagerService>();

        // Storage providers
        services.AddScoped<IStorageProvider, FileSystemStorageProvider>();
        services.AddScoped<IStorageProvider, DatabaseStorageProvider>();

        return services;
    }

    /// <summary>
    /// Configure Serilog logging
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <returns>Logger configuration</returns>
    public static LoggerConfiguration ConfigureSerilog(IConfiguration configuration)
    {
        return new LoggerConfiguration().ReadFrom.Configuration(configuration);
    }

    /// <summary>
    /// Ensure database is created and migrated
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Task</returns>
    public static async Task EnsureDatabaseCreatedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileManagerContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();

        try
        {
            logger.Information("Ensuring database is created...");

            // For testing, use EnsureCreated instead of migrations
            var created = await context.Database.EnsureCreatedAsync();
            if (created)
            {
                logger.Information("Database created successfully");
            }
            else
            {
                logger.Information("Database already exists");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create database");
            throw;
        }
    }

    /// <summary>
    /// Ensure storage directories exist
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    public static void EnsureStorageDirectoriesExist(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var storageSettings = scope.ServiceProvider.GetRequiredService<StorageSettings>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();

        try
        {
            if (!Directory.Exists(storageSettings.FileSystemStorageRoot))
            {
                Directory.CreateDirectory(storageSettings.FileSystemStorageRoot);
                logger.Information("Created storage directory: {StorageRoot}", storageSettings.FileSystemStorageRoot);
            }

            if (!Directory.Exists(storageSettings.FileSystemDownloadRoot))
            {
                Directory.CreateDirectory(storageSettings.FileSystemDownloadRoot);
                logger.Information("Created download directory: {DownloadRoot}", storageSettings.FileSystemDownloadRoot);
            }

            // Create logs directory
            var logsDirectory = LogConfigurations.LogsFolderName;
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
                logger.Information("Created logs directory: {LogsDirectory}", logsDirectory);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create storage directories");
            throw;
        }
    }

    /// <summary>
    /// Validate storage providers health
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Task</returns>
    public static async Task ValidateStorageProvidersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var storageProviders = scope.ServiceProvider.GetServices<IStorageProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();

        foreach (var provider in storageProviders)
        {
            try
            {
                var isHealthy = await provider.IsHealthyAsync();
                if (isHealthy)
                {
                    logger.Information("Storage provider {ProviderId} is healthy", provider.ProviderId);
                }
                else
                {
                    logger.Warning("Storage provider {ProviderId} is not healthy", provider.ProviderId);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to check health of storage provider {ProviderId}", provider.ProviderId);
            }
        }
    }
}