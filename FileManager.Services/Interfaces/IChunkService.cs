using FileManager.Database.Entities;
using FileManager.Dto;

namespace FileManager.Services.Interfaces;

/// <summary>
/// Chunk service interface for file chunking operations
/// </summary>
public interface IChunkService
{
    /// <summary>
    /// Calculate optimal chunk size based on file size
    /// </summary>
    /// <param name="fileSize">File size in bytes</param>
    /// <returns>Optimal chunk size in bytes</returns>
    int CalculateOptimalChunkSize(long fileSize);

    /// <summary>
    /// Split file into chunks
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="chunkSize">Chunk size in bytes</param>
    /// <returns>List of chunk information</returns>
    Task<List<ChunkInfoDto>> SplitFileAsync(string filePath, int chunkSize);

    /// <summary>
    /// Merge chunks back to original file
    /// </summary>
    /// <param name="chunks">List of chunks to merge</param>
    /// <param name="outputPath">Output file path</param>
    /// <returns>True if merge successful</returns>
    Task<bool> MergeChunksAsync(List<Chunk> chunks, string outputPath);

    /// <summary>
    /// Store chunks using storage providers
    /// </summary>
    /// <param name="chunks">List of chunk information</param>
    /// <param name="storageProviders">Available storage providers</param>
    /// <returns>List of stored chunks with storage information</returns>
    Task<List<Chunk>> StoreChunksAsync(List<ChunkInfoDto> chunks, List<IStorageProvider> storageProviders);

    /// <summary>
    /// Retrieve chunks from storage providers
    /// </summary>
    /// <param name="chunks">List of chunks to retrieve</param>
    /// <param name="storageProviders">Available storage providers</param>
    /// <returns>List of chunk data</returns>
    Task<List<ChunkDataInfoDto>> RetrieveChunksAsync(List<Chunk> chunks, List<IStorageProvider> storageProviders);
}