using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_chat_messages")]
public sealed class KnowledgeSqlChatMessage
{
    [Key]
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    [ForeignKey(nameof(SessionId))]
    [InverseProperty(nameof(KnowledgeSqlChatSession.Messages))]
    public KnowledgeSqlChatSession Session { get; set; } = null!;

    [Required, MaxLength(32)]
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [InverseProperty(nameof(KnowledgeSqlCitation.Message))]
    public List<KnowledgeSqlCitation> Citations { get; set; } = new();
}