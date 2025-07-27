using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Database.Entities;

/// <summary>
/// Chunk entity
/// </summary>
[Table(Core.Constants.Entity.Chunks)]
public class Chunk
{
    /// <summary>
    /// Unique chunk identifier
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Associated file identifier
    /// </summary>
    [Required]
    public Guid FileId { get; set; }

    [Required]
    /// <summary>
    /// Chunk sequence number (0-based)
    /// </summary>
    public required int SequenceNumber { get; set; }

    [Required]
    /// <summary>
    /// Chunk size in bytes
    /// </summary>
    public required int Size { get; set; }

    /// <summary>
    /// Chunk checksum (SHA256)
    /// </summary>
    [Required]
    [MaxLength(64)]
    public required string Checksum { get; set; }

    /// <summary>
    /// Storage provider identifier
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string StorageProviderId { get; set; }

    /// <summary>
    /// Storage location within the provider
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string StorageLocation { get; set; }

    [Required]
    /// <summary>
    /// Chunk creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last verification timestamp
    /// </summary>
    public DateTime? LastVerifiedAt { get; set; }

    /// <summary>
    /// Chunk status
    /// </summary>
    public Enums.ChunkStatus Status { get; set; }

    /// <summary>
    /// Associated file metadata
    /// </summary>
    [ForeignKey(nameof(FileId))]
    public virtual FileMetadata File { get; set; }
}