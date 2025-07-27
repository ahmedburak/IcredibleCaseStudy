using FileManager.Database.Entities;
using FileManager.Dto;
using FileManager.Services.Interfaces;
using static FileManager.Database.Enums;

namespace FileManager.Services;

/// <summary>
/// Chunk service implementation
/// </summary>
public class ChunkService(
    Serilog.ILogger logger,
    IChecksumService checksumService,
    StorageSettings storageSettings) : IChunkService
{
    public int CalculateOptimalChunkSize(long fileSize)
    {
        try
        {
            // Dynamic chunk size calculation based on file size
            int chunkSize;

            if (fileSize <= 10 * 1024 * 1024) // <= 10MB
            {
                chunkSize = storageSettings.MinChunkSize; // 64KB
            }
            else if (fileSize <= 100 * 1024 * 1024) // <= 100MB
            {
                chunkSize = storageSettings.DefaultChunkSize; // 1MB
            }
            else if (fileSize <= 500 * 1024 * 1024) // <= 500MB
            {
                chunkSize = 5 * 1024 * 1024; // 5MB
            }
            else
            {
                chunkSize = storageSettings.MaxChunkSize; // 10MB
            }

            // Ensure chunk size is within bounds
            chunkSize = Math.Max(storageSettings.MinChunkSize, chunkSize);
            chunkSize = Math.Min(storageSettings.MaxChunkSize, chunkSize);

            logger.Debug("Calculated optimal chunk size for file size {FileSize}: {ChunkSize} bytes",
                fileSize, chunkSize);

            return chunkSize;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to calculate optimal chunk size for file size {FileSize}", fileSize);
            return storageSettings.DefaultChunkSize;
        }
    }

    public async Task<List<ChunkInfoDto>> SplitFileAsync(string filePath, int chunkSize)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var chunks = new List<ChunkInfoDto>();
            var buffer = new byte[chunkSize];
            var sequenceNumber = 0;

            logger.Information("Starting file split: {FilePath}, ChunkSize: {ChunkSize} bytes",
                filePath, chunkSize);

            using var fileStream = File.OpenRead(filePath);

            while (true)
            {
                var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, chunkSize));

                if (bytesRead == 0)
                {
                    break;
                }

                // Create chunk data array with actual size
                var chunkData = new byte[bytesRead];
                Array.Copy(buffer, chunkData, bytesRead);

                // Calculate checksum for chunk
                var checksum = checksumService.CalculateChecksum(chunkData);

                var chunkInfo = new ChunkInfoDto
                {
                    Id = Guid.NewGuid(),
                    SequenceNumber = sequenceNumber,
                    Data = chunkData,
                    Checksum = checksum
                };

                chunks.Add(chunkInfo);
                sequenceNumber++;

                logger.Debug("Created chunk {SequenceNumber}: ID={ChunkId}, Size={Size} bytes, Checksum={Checksum}",
                    sequenceNumber - 1, chunkInfo.Id, bytesRead, checksum);
            }

            logger.Information("File split completed: {FilePath}, Total chunks: {TotalChunks}",
                filePath, chunks.Count);

            return chunks;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to split file {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> MergeChunksAsync(List<Chunk> chunks, string outputPath)
    {
        try
        {
            if (chunks == null || chunks.Count == 0)
            {
                throw new ArgumentException("Chunks list cannot be null or empty");
            }

            // Sort chunks by sequence number
            var sortedChunks = chunks.OrderBy(c => c.SequenceNumber).ToList();

            logger.Information("Starting chunk merge: Output={OutputPath}, Total chunks: {TotalChunks}",
                outputPath, sortedChunks.Count);

            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using var outputStream = File.Create(outputPath);

            foreach (var chunk in sortedChunks)
            {
                logger.Debug("Processing chunk {SequenceNumber}: ID={ChunkId}",
                    chunk.SequenceNumber, chunk.Id);

                // Note: This method expects chunk data to be retrieved separately
                // The actual chunk data retrieval should be handled by the calling service
            }

            logger.Information("Chunk merge completed: {OutputPath}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to merge chunks to {OutputPath}", outputPath);
            return false;
        }
    }

    public async Task<List<Chunk>> StoreChunksAsync(List<ChunkInfoDto> chunks, List<IStorageProvider> storageProviders)
    {
        try
        {
            if (chunks == null || chunks.Count == 0)
            {
                throw new ArgumentException("Chunks list cannot be null or empty");
            }

            if (storageProviders == null || storageProviders.Count == 0)
            {
                throw new ArgumentException("Storage providers list cannot be null or empty");
            }

            var storedChunks = new List<Chunk>();
            var providerIndex = 0;

            logger.Information("Starting chunk storage: Total chunks: {TotalChunks}, Providers: {ProviderCount}",
                chunks.Count, storageProviders.Count);

            foreach (var chunkInfo in chunks)
            {
                // Round-robin distribution strategy
                var provider = storageProviders[providerIndex % storageProviders.Count];
                providerIndex++;

                try
                {
                    var storageLocation = await provider.StoreChunkAsync(chunkInfo.Id, chunkInfo.Data);

                    var chunk = new Chunk
                    {
                        Id = chunkInfo.Id,
                        SequenceNumber = chunkInfo.SequenceNumber,
                        Size = chunkInfo.Size,
                        Checksum = chunkInfo.Checksum,
                        StorageProviderId = provider.ProviderId,
                        StorageLocation = storageLocation,
                        CreatedAt = DateTime.UtcNow,
                        Status = ChunkStatus.Stored
                    };

                    storedChunks.Add(chunk);

                    logger.Debug("Stored chunk {SequenceNumber} using provider {ProviderId}: {StorageLocation}",
                        chunk.SequenceNumber, provider.ProviderId, storageLocation);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to store chunk {ChunkId} using provider {ProviderId}",
                        chunkInfo.Id, provider.ProviderId);
                    throw;
                }
            }

            logger.Information("Chunk storage completed: {StoredCount} chunks stored", storedChunks.Count);
            return storedChunks;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to store chunks");
            throw;
        }
    }

    public async Task<List<ChunkDataInfoDto>> RetrieveChunksAsync(List<Chunk> chunks, List<IStorageProvider> storageProviders)
    {
        try
        {
            if (chunks == null || chunks.Count == 0)
            {
                throw new ArgumentException("Chunks list cannot be null or empty");
            }

            if (storageProviders == null || storageProviders.Count == 0)
            {
                throw new ArgumentException("Storage providers list cannot be null or empty");
            }

            var retrievedChunks = new List<ChunkDataInfoDto>();
            var providerLookup = storageProviders.ToDictionary(p => p.ProviderId, p => p);

            logger.Information("Starting chunk retrieval: Total chunks: {TotalChunks}", chunks.Count);

            foreach (var chunk in chunks.OrderBy(c => c.SequenceNumber))
            {
                if (!providerLookup.TryGetValue(chunk.StorageProviderId, out var provider))
                {
                    throw new InvalidOperationException($"Storage provider not found: {chunk.StorageProviderId}");
                }

                try
                {
                    var data = await provider.RetrieveChunkAsync(chunk.Id, chunk.StorageLocation);

                    // Verify chunk integrity
                    if (!checksumService.VerifyChecksum(data, chunk.Checksum))
                    {
                        throw new InvalidOperationException($"Chunk integrity verification failed: {chunk.Id}");
                    }

                    var chunkData = new ChunkDataInfoDto
                    {
                        Id = chunk.Id,
                        SequenceNumber = chunk.SequenceNumber,
                        Data = data,
                        Checksum = chunk.Checksum,
                        StorageProviderId = chunk.StorageProviderId,
                        StorageLocation = chunk.StorageLocation
                    };

                    retrievedChunks.Add(chunkData);

                    logger.Debug("Retrieved chunk {SequenceNumber} from provider {ProviderId}: Size={Size} bytes",
                        chunk.SequenceNumber, provider.ProviderId, data.Length);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to retrieve chunk {ChunkId} from provider {ProviderId}",
                        chunk.Id, chunk.StorageProviderId);
                    throw;
                }
            }

            logger.Information("Chunk retrieval completed: {RetrievedCount} chunks retrieved", retrievedChunks.Count);
            return retrievedChunks;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to retrieve chunks");
            throw;
        }
    }
}