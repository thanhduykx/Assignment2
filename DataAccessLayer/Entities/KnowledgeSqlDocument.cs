using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_documents")]
public sealed class KnowledgeSqlDocument
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string StoredPath { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Chapter { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    public int ChunkCount { get; set; }

    public long FileSizeBytes { get; set; }

    public Guid? UploadedByUserId { get; set; }

    [MaxLength(255)]
    public string? UploadedByName { get; set; }

    [MaxLength(255)]
    public string? UploadedByEmail { get; set; }

    [Required, MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? IndexedAt { get; set; }

    public string? IndexError { get; set; }

    [Required, MaxLength(100)]
    public string EmbeddingModel { get; set; } = string.Empty;

    public int EmbeddingDimensions { get; set; }

    [Required, MaxLength(100)]
    public string ChunkingStrategy { get; set; } = string.Empty;
}