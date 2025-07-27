namespace FileManager.Dto;

/// <summary>
/// Chunk information for processing
/// </summary>
public class ChunkInfoDto
{
    /// <summary>
    /// Chunk identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Sequence number
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Chunk data
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Chunk checksum
    /// </summary>
    public string Checksum { get; set; }

    /// <summary>
    /// Chunk size
    /// </summary>
    public int Size => Data?.Length ?? 0;
}