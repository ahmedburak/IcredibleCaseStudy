using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Database.Entities;

/// <summary>
/// File metadata entity
/// </summary>
[Table(Core.Constants.Entity.FileMetadatas)]
public class FileMetadata
{
    /// <summary>
    /// Unique file identifier
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string FileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File MIME type
    /// </summary>
    [MaxLength(100)]
    public required string MimeType { get; set; }

    /// <summary>
    /// File checksum (SHA256)
    /// </summary>
    [Required]
    [MaxLength(64)]
    public required string Checksum { get; set; }

    /// <summary>
    /// Total number of chunks
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Chunk size used for this file
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    /// File upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Last access timestamp
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// File status
    /// </summary>
    public Enums.FileStatus Status { get; set; }

    /// <summary>
    /// Associated chunks
    /// </summary>
    public virtual ICollection<Chunk> Chunks { get; set; } = [];
}