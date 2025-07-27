using FileManager.Database;
using FileManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Services;

/// <summary>
/// Database storage provider implementation
/// </summary>
public class DatabaseStorageProvider(Serilog.ILogger logger, FileManagerContext context) : IStorageProvider
{
    public string ProviderId => "Database";
    public string ProviderName => "Database Storage Provider";

    public async Task<string> StoreChunkAsync(Guid chunkId, byte[] data)
    {
        try
        {
            var chunkData = new Database.Entities.ChunkData
            {
                Id = chunkId,
                Data = data,
                CreatedAt = DateTime.UtcNow
            };

            context.ChunkData.Add(chunkData);
            await context.SaveChangesAsync();

            logger.Debug("Stored chunk {ChunkId} in database, Size: {Size} bytes",
                chunkId, data.Length);

            return chunkId.ToString();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to store chunk {ChunkId} in database", chunkId);
            throw;
        }
    }

    public async Task<byte[]> RetrieveChunkAsync(Guid chunkId, string storageLocation)
    {
        try
        {
            var chunkData = await context.ChunkData
                .FirstOrDefaultAsync(cd => cd.Id == chunkId);

            if (chunkData == null)
            {
                logger.Warning("Chunk not found in database: {ChunkId}", chunkId);
                throw new InvalidOperationException($"Chunk not found in database: {chunkId}");
            }

            logger.Debug("Retrieved chunk {ChunkId} from database, Size: {Size} bytes",
                chunkId, chunkData.Data.Length);

            return chunkData.Data;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to retrieve chunk {ChunkId} from database", chunkId);
            throw;
        }
    }

    public async Task<bool> DeleteChunkAsync(Guid chunkId, string storageLocation)
    {
        try
        {
            var chunkData = await context.ChunkData
                .FirstOrDefaultAsync(cd => cd.Id == chunkId);

            if (chunkData != null)
            {
                context.ChunkData.Remove(chunkData);
                await context.SaveChangesAsync();

                logger.Debug("Deleted chunk {ChunkId} from database", chunkId);
                return true;
            }

            logger.Warning("Chunk not found for deletion in database: {ChunkId}", chunkId);
            return false;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to delete chunk {ChunkId} from database", chunkId);
            return false;
        }
    }

    public async Task<bool> ChunkExistsAsync(Guid chunkId, string storageLocation)
    {
        try
        {
            var exists = await context.ChunkData
                .AnyAsync(cd => cd.Id == chunkId);

            logger.Debug("Chunk {ChunkId} exists check in database: {Exists}", chunkId, exists);

            return exists;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to check chunk existence {ChunkId} in database", chunkId);
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Try to execute a simple query to check database connectivity
            await context.Database.ExecuteSqlRawAsync("SELECT 1");

            logger.Debug("Database storage provider health check: Healthy");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Database storage provider health check failed");
            return false;
        }
    }
}