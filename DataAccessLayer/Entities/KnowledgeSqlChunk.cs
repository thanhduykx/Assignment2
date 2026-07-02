using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_chunks")]
public sealed class KnowledgeSqlChunk
{
    [Key]
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public KnowledgeSqlDocument Document { get; set; } = null!;

    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Chapter { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    [Column("EmbeddingJson")]
    public string? EmbeddingJson { get; set; }

    [MaxLength(255)]
    public string SectionTitle { get; set; } = string.Empty;

    public int CharStart { get; set; }

    public int CharEnd { get; set; }
}