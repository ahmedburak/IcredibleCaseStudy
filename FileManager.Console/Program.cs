using FileManager.Core;
using FileManager.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FileManager.Console;

/// <summary>
/// Main program class
/// </summary>
public class Program
{
    private static IServiceProvider _serviceProvider;

    public static async Task Main(string[] args)
    {
        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Constants.Configuration.AppSettingsName, false, true)
                .Build();

            // Configure Serilog
            Log.Logger = ServiceCollectionExtensions.ConfigureSerilog(configuration).CreateLogger();

            // Build service provider
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddDistributedFileStorage(configuration);

            services.AddSingleton(Log.Logger); // Serilog.ILogger olarak ekler

            _serviceProvider = services.BuildServiceProvider();

            Log.Information("=== Distributed File Storage System Started ===");

            // Initialize system
            await InitializeSystemAsync();

            // Run main application
            await RunApplicationAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            System.Console.ReadKey();
        }
    }

    public static async Task InitializeSystemAsync()
    {
        try
        {
            Log.Information("Initializing system...");

            // Ensure storage directories exist
            ServiceCollectionExtensions.EnsureStorageDirectoriesExist(_serviceProvider);

            // Ensure database is created and migrated
            await ServiceCollectionExtensions.EnsureDatabaseCreatedAsync(_serviceProvider);

            // Validate storage providers
            await ServiceCollectionExtensions.ValidateStorageProvidersAsync(_serviceProvider);

            Log.Information("System initialization completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "System initialization failed");
            throw;
        }
    }

    public static async Task RunApplicationAsync()
    {
        var fileService = _serviceProvider.GetRequiredService<IFileService>();
        var managerService = _serviceProvider.GetRequiredService<IManagerService>();

        while (true)
        {
            try
            {
                System.Console.WriteLine();
                System.Console.WriteLine("=== Distributed File Storage System ===");
                System.Console.WriteLine("1. Upload File");
                System.Console.WriteLine("2. Download File");
                System.Console.WriteLine("3. List Files");
                System.Console.WriteLine("4. Delete File");
                System.Console.WriteLine("5. Verify File Integrity");
                System.Console.WriteLine("6. Exit");
                System.Console.Write("Select an option (1-6): ");

                var choice = System.Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await managerService.UploadFileAsync(fileService);
                        break;
                    case "2":
                        await managerService.DownloadFileAsync(fileService);
                        break;
                    case "3":
                        await managerService.ListFilesAsync(fileService);
                        break;
                    case "4":
                        await managerService.DeleteFileAsync(fileService);
                        break;
                    case "5":
                        await managerService.VerifyFileIntegrityAsync(fileService);
                        break;
                    case "6":
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        Log.Information("Application shutting down...");
                        System.Console.WriteLine("Application shutting down...");
                        return;
                    default:
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine("Invalid option. Please try again.");
                        System.Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during operation");
                System.Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}