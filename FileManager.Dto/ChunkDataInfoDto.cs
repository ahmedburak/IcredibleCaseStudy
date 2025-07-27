using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Dto;

/// <summary>
/// Chunk data for retrieval
/// </summary>
public class ChunkDataInfoDto
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
    /// Storage provider identifier
    /// </summary>
    public string StorageProviderId { get; set; }

    /// <summary>
    /// Storage location
    /// </summary>
    public string StorageLocation { get; set; }
}