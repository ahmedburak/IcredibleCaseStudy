using FileManager.Services.Interfaces;

namespace FileManager.Services;

/// <summary>
/// File system storage provider implementation
/// </summary>
public class FileSystemStorageProvider : IStorageProvider
{
    private readonly Serilog.ILogger _logger;
    private readonly string _storageRoot;

    public string ProviderId => "FileSystem";
    public string ProviderName => "File System Storage Provider";

    public FileSystemStorageProvider(Serilog.ILogger logger, StorageSettings storageSettings)
    {
        _logger = logger;
        _storageRoot = storageSettings.FileSystemStorageRoot;

        // Ensure storage directory exists
        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
            _logger.Information("Created storage directory: {StorageRoot}", _storageRoot);
        }
    }

    public async Task<string> StoreChunkAsync(Guid chunkId, byte[] data)
    {
        try
        {
            var fileName = $"{chunkId}.chunk";
            var filePath = Path.Combine(_storageRoot, fileName);

            await File.WriteAllBytesAsync(filePath, data);

            _logger.Debug("Stored chunk {ChunkId} to {FilePath}, Size: {Size} bytes",
                chunkId, filePath, data.Length);

            return fileName;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to store chunk {ChunkId}", chunkId);
            throw;
        }
    }

    public async Task<byte[]> RetrieveChunkAsync(Guid chunkId, string storageLocation)
    {
        try
        {
            var filePath = Path.Combine(_storageRoot, storageLocation);

            if (!File.Exists(filePath))
            {
                _logger.Warning("Chunk file not found: {FilePath}", filePath);
                throw new FileNotFoundException($"Chunk file not found: {filePath}");
            }

            var data = await File.ReadAllBytesAsync(filePath);

            _logger.Debug("Retrieved chunk {ChunkId} from {FilePath}, Size: {Size} bytes",
                chunkId, filePath, data.Length);

            return data;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to retrieve chunk {ChunkId} from {StorageLocation}",
                chunkId, storageLocation);
            throw;
        }
    }

    public async Task<bool> DeleteChunkAsync(Guid chunkId, string storageLocation)
    {
        try
        {
            var filePath = Path.Combine(_storageRoot, storageLocation);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.Debug("Deleted chunk {ChunkId} from {FilePath}", chunkId, filePath);
                return true;
            }

            _logger.Warning("Chunk file not found for deletion: {FilePath}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete chunk {ChunkId} from {StorageLocation}",
                chunkId, storageLocation);
            return false;
        }
    }

    public async Task<bool> ChunkExistsAsync(Guid chunkId, string storageLocation)
    {
        try
        {
            var filePath = Path.Combine(_storageRoot, storageLocation);
            var exists = File.Exists(filePath);

            _logger.Debug("Chunk {ChunkId} exists check: {Exists}", chunkId, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check chunk existence {ChunkId} at {StorageLocation}",
                chunkId, storageLocation);
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Check if storage directory is accessible
            if (!Directory.Exists(_storageRoot))
            {
                _logger.Warning("Storage directory does not exist: {StorageRoot}", _storageRoot);
                return false;
            }

            // Try to write a test file
            var testFile = Path.Combine(_storageRoot, $"health_check_{Guid.NewGuid()}.tmp");
            await File.WriteAllTextAsync(testFile, "health_check");

            // Try to read the test file
            var content = await File.ReadAllTextAsync(testFile);

            // Clean up test file
            File.Delete(testFile);

            var isHealthy = content == "health_check";
            _logger.Debug("FileSystem storage provider health check: {IsHealthy}", isHealthy);

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "FileSystem storage provider health check failed");
            return false;
        }
    }
}