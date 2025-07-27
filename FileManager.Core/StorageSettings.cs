namespace FileManager.Services;

/// <summary>
/// Storage configuration settings
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// Default chunk size in bytes (1MB)
    /// </summary>
    public int DefaultChunkSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Minimum chunk size in bytes (64KB)
    /// </summary>
    public int MinChunkSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Maximum chunk size in bytes (10MB)
    /// </summary>
    public int MaxChunkSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// File system storage root directory
    /// </summary>
    public string FileSystemStorageRoot { get; set; } = "./storages";

    /// <summary>
    /// File system download root directory
    /// </summary>
    public string FileSystemDownloadRoot { get; set; } = "./downloads";
}