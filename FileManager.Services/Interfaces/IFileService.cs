using FileManager.Database.Entities;

namespace FileManager.Services.Interfaces;

/// <summary>
/// File service interface for file operations
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Upload and process a file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>File metadata</returns>
    Task<FileMetadata> UploadFileAsync(string filePath, string fileName);

    /// <summary>
    /// Download and reconstruct a file
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <param name="outputPath">Output file path</param>
    /// <returns>True if download successful</returns>
    Task<bool> DownloadFileAsync(Guid fileId, string outputPath);

    /// <summary>
    /// Get file metadata
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>File metadata</returns>
    Task<FileMetadata> GetFileMetadataAsync(Guid fileId);

    /// <summary>
    /// List all files
    /// </summary>
    /// <returns>List of file metadata</returns>
    Task<List<FileMetadata>> ListFilesAsync();

    /// <summary>
    /// Delete a file and its chunks
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>True if deletion successful</returns>
    Task<bool> DeleteFileAsync(Guid fileId);

    /// <summary>
    /// Verify file integrity
    /// </summary>
    /// <param name="fileId">File identifier</param>
    /// <returns>True if file integrity is valid</returns>
    Task<bool> VerifyFileIntegrityAsync(Guid fileId);
}