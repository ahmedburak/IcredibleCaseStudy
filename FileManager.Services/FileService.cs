using FileManager.Database;
using FileManager.Database.Entities;
using FileManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static FileManager.Database.Enums;

namespace FileManager.Services;

/// <summary>
/// File service implementation
/// </summary>
public class FileService(
    Serilog.ILogger logger,
    IChunkService chunkService,
    IChecksumService checksumService,
    FileManagerContext context,
    IEnumerable<IStorageProvider> storageProviders) : IFileService
{
    private readonly List<IStorageProvider> _storageProviders = [.. storageProviders];

    public async Task<FileMetadata> UploadFileAsync(string filePath, string fileName)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            logger.Information("Starting file upload: {FileName}, Size: {FileSize} bytes", fileName, fileSize);

            // Calculate file checksum
            var fileChecksum = await checksumService.CalculateFileChecksumAsync(filePath);

            // Check if file already exists
            var existingFile = await context.Files
                .FirstOrDefaultAsync(f => f.Checksum == fileChecksum);

            if (existingFile != null)
            {
                logger.Information("File already exists with same checksum: {ExistingFileName}", existingFile.FileName);
                return existingFile;
            }

            // Calculate optimal chunk size
            var chunkSize = chunkService.CalculateOptimalChunkSize(fileSize);

            // Split file into chunks
            var chunkInfos = await chunkService.SplitFileAsync(filePath, chunkSize);

            // Create file metadata
            var fileMetadata = new FileMetadata
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                FileSize = fileSize,
                MimeType = GetMimeType(fileName),
                Checksum = fileChecksum,
                TotalChunks = chunkInfos.Count,
                ChunkSize = chunkSize,
                UploadedAt = DateTime.UtcNow,
                Status = FileStatus.Uploading
            };

            // Store chunks
            var storedChunks = await chunkService.StoreChunksAsync(chunkInfos, _storageProviders);

            // Set file ID for chunks
            foreach (var chunk in storedChunks)
            {
                chunk.FileId = fileMetadata.Id;
            }

            // Save to database
            context.Files.Add(fileMetadata);
            context.Chunks.AddRange(storedChunks);

            fileMetadata.Status = FileStatus.Completed;
            await context.SaveChangesAsync();

            logger.Information("File upload completed: {FileName}, ID: {FileId}, Chunks: {ChunkCount}",
                fileName, fileMetadata.Id, storedChunks.Count);

            return fileMetadata;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to upload file {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DownloadFileAsync(Guid fileId, string outputPath)
    {
        try
        {
            var fileMetadata = await context.Files
                .Include(f => f.Chunks)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileMetadata == null)
            {
                logger.Warning("File not found: {FileId}", fileId);
                return false;
            }

            logger.Information("Starting file download: {FileName}, ID: {FileId}, Output: {OutputPath}",
                fileMetadata.FileName, fileId, outputPath);

            // Retrieve chunks
            var retrievedChunks = await chunkService.RetrieveChunksAsync([.. fileMetadata.Chunks], _storageProviders);

            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Merge chunks to recreate file
            using (var outputStream = File.Create(outputPath))
            {
                foreach (var chunkData in retrievedChunks.OrderBy(c => c.SequenceNumber))
                {
                    await outputStream.WriteAsync(chunkData.Data);
                }
            } // Stream is closed here

            // Verify file integrity after stream is closed
            var downloadedChecksum = await checksumService.CalculateFileChecksumAsync(outputPath);
            if (!string.Equals(downloadedChecksum, fileMetadata.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                logger.Error("File integrity verification failed after download: {FileId}", fileId);
                File.Delete(outputPath);
                return false;
            }

            // Update last accessed timestamp
            fileMetadata.LastAccessedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            logger.Information("File download completed: {FileName}, ID: {FileId}",
                fileMetadata.FileName, fileId);

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to download file {FileId}", fileId);
            return false;
        }
    }

    public async Task<FileMetadata> GetFileMetadataAsync(Guid fileId)
    {
        try
        {
            var fileMetadata = await context.Files
                .Include(f => f.Chunks)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileMetadata == null)
            {
                logger.Warning("File metadata not found: {FileId}", fileId);

                return default;
            }

            return fileMetadata;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get file metadata {FileId}", fileId);
            throw;
        }
    }

    public async Task<List<FileMetadata>> ListFilesAsync()
    {
        try
        {
            var files = await context.Files
                .Where(f => f.Status != FileStatus.Deleted)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            logger.Debug("Listed {FileCount} files", files.Count);

            return files;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to list files");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(Guid fileId)
    {
        try
        {
            var fileMetadata = await context.Files
                .Include(f => f.Chunks)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileMetadata == null)
            {
                logger.Warning("File not found for deletion: {FileId}", fileId);
                return false;
            }

            logger.Information("Starting file deletion: {FileName}, ID: {FileId}",
                fileMetadata.FileName, fileId);

            // Delete chunks from storage providers
            var deletionTasks = fileMetadata.Chunks.Select(async chunk =>
            {
                var provider = _storageProviders.FirstOrDefault(p => p.ProviderId == chunk.StorageProviderId);
                if (provider != null)
                {
                    try
                    {
                        await provider.DeleteChunkAsync(chunk.Id, chunk.StorageLocation);
                        logger.Debug("Deleted chunk {ChunkId} from provider {ProviderId}",
                            chunk.Id, provider.ProviderId);
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, "Failed to delete chunk {ChunkId} from provider {ProviderId}",
                            chunk.Id, provider.ProviderId);
                    }
                }
            });

            await Task.WhenAll(deletionTasks);

            // Mark file as deleted
            fileMetadata.Status = FileStatus.Deleted;
            await context.SaveChangesAsync();

            logger.Information("File deletion completed: {FileName}, ID: {FileId}",
                fileMetadata.FileName, fileId);

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to delete file {FileId}", fileId);
            return false;
        }
    }

    public async Task<bool> VerifyFileIntegrityAsync(Guid fileId)
    {
        try
        {
            var fileMetadata = await context.Files
                .Include(f => f.Chunks)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileMetadata == null)
            {
                logger.Warning("File not found for integrity verification: {FileId}", fileId);
                return false;
            }

            logger.Information("Starting file integrity verification: {FileName}, ID: {FileId}",
                fileMetadata.FileName, fileId);

            // Verify each chunk
            var verificationTasks = fileMetadata.Chunks.Select(async chunk =>
            {
                var provider = _storageProviders.FirstOrDefault(p => p.ProviderId == chunk.StorageProviderId);
                if (provider == null)
                {
                    logger.Warning("Storage provider not found for chunk {ChunkId}: {ProviderId}",
                        chunk.Id, chunk.StorageProviderId);
                    return false;
                }

                try
                {
                    var chunkData = await provider.RetrieveChunkAsync(chunk.Id, chunk.StorageLocation);
                    var isValid = checksumService.VerifyChecksum(chunkData, chunk.Checksum);

                    if (!isValid)
                    {
                        logger.Warning("Chunk integrity verification failed: {ChunkId}", chunk.Id);
                        chunk.Status = ChunkStatus.Corrupted;
                    }
                    else
                    {
                        chunk.Status = ChunkStatus.Verified;
                        chunk.LastVerifiedAt = DateTime.UtcNow;
                    }

                    return isValid;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to verify chunk {ChunkId}", chunk.Id);
                    chunk.Status = ChunkStatus.Missing;
                    return false;
                }
            });

            var verificationResults = await Task.WhenAll(verificationTasks);
            var allValid = verificationResults.All(r => r);

            if (!allValid)
            {
                fileMetadata.Status = FileStatus.Corrupted;
            }

            await context.SaveChangesAsync();

            logger.Information("File integrity verification completed: {FileName}, ID: {FileId}, Valid: {IsValid}",
                fileMetadata.FileName, fileId, allValid);

            return allValid;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to verify file integrity {FileId}", fileId);
            return false;
        }
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };
    }
}