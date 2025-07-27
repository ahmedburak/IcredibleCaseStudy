namespace FileManager.Database;

public class Enums
{
    /// <summary>
    /// Chunk status enumeration
    /// </summary>
    public enum ChunkStatus
    {
        Pending = 0,
        Stored = 1,
        Verified = 2,
        Corrupted = 3,
        Missing = 4
    }

    /// <summary>
    /// File status enumeration
    /// </summary>
    public enum FileStatus
    {
        Uploading = 0,
        Completed = 1,
        Corrupted = 2,
        Deleted = 3
    }
}