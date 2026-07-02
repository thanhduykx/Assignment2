using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("Citations")]
public sealed class KnowledgeSqlCitation
{
    [Key]
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    [ForeignKey(nameof(MessageId))]
    public KnowledgeSqlChatMessage Message { get; set; } = null!;

    public Guid DocumentId { get; set; }

    [MaxLength(512)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(128)]
    public string Chapter { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public double Score { get; set; }

    public string Excerpt { get; set; } = string.Empty;
}