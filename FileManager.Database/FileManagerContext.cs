using FileManager.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Database;

/// <summary>
/// Entity Framework DbContext for Distributed File Storage
/// </summary>
public class FileManagerContext(DbContextOptions<FileManagerContext> options) : DbContext(options)
{

    /// <summary>
    /// File metadata table
    /// </summary>
    public DbSet<FileMetadata> Files { get; set; }

    /// <summary>
    /// Chunks table
    /// </summary>
    public DbSet<Chunk> Chunks { get; set; }

    /// <summary>
    /// Chunk data table for database storage provider
    /// </summary>
    public DbSet<ChunkData> ChunkData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FileMetadata entity
        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.ToTable("Files");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.MimeType)
                .HasMaxLength(100);
            
            entity.Property(e => e.Checksum)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.Status)
                .HasConversion<int>();

            // Index for faster queries
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.Checksum);
            entity.HasIndex(e => e.UploadedAt);
        });

        // Configure Chunk entity
        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.ToTable("Chunks");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FileId)
                .IsRequired();
            
            entity.Property(e => e.Checksum)
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.StorageProviderId)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.StorageLocation)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.Status)
                .HasConversion<int>();

            // Foreign key relationship
            entity.HasOne(e => e.File)
                .WithMany(f => f.Chunks)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for faster queries
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.SequenceNumber);
            entity.HasIndex(e => e.StorageProviderId);
            entity.HasIndex(e => new { e.FileId, e.SequenceNumber }).IsUnique();
        });

        // Configure ChunkData entity
        modelBuilder.Entity<ChunkData>(entity =>
        {
            entity.ToTable("ChunkData");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Data)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}