namespace FileManager.Services.Interfaces;

/// <summary>
/// Storage provider interface for different storage implementations
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Storage provider unique identifier
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Storage provider name
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Store chunk data
    /// </summary>
    /// <param name="chunkId">Unique chunk identifier</param>
    /// <param name="data">Chunk data</param>
    /// <returns>Storage location identifier</returns>
    Task<string> StoreChunkAsync(Guid chunkId, byte[] data);

    /// <summary>
    /// Retrieve chunk data
    /// </summary>
    /// <param name="chunkId">Unique chunk identifier</param>
    /// <param name="storageLocation">Storage location identifier</param>
    /// <returns>Chunk data</returns>
    Task<byte[]> RetrieveChunkAsync(Guid chunkId, string storageLocation);

    /// <summary>
    /// Delete chunk data
    /// </summary>
    /// <param name="chunkId">Unique chunk identifier</param>
    /// <param name="storageLocation">Storage location identifier</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteChunkAsync(Guid chunkId, string storageLocation);

    /// <summary>
    /// Check if chunk exists
    /// </summary>
    /// <param name="chunkId">Unique chunk identifier</param>
    /// <param name="storageLocation">Storage location identifier</param>
    /// <returns>True if chunk exists</returns>
    Task<bool> ChunkExistsAsync(Guid chunkId, string storageLocation);

    /// <summary>
    /// Get storage provider health status
    /// </summary>
    /// <returns>True if provider is healthy</returns>
    Task<bool> IsHealthyAsync();
}