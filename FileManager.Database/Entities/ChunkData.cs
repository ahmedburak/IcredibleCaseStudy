using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileManager.Database.Entities;

/// <summary>
/// Chunk data entity for database storage
/// </summary>
[Table(Core.Constants.Entity.ChunkDatas)]
public class ChunkData
{
    [Key]
    public required Guid Id { get; set; }
    [Required]
    public required byte[] Data { get; set; }
    [Required]
    public required DateTime CreatedAt { get; set; }
}