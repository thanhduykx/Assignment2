using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_chat_sessions")]
public sealed class KnowledgeSqlChatSession
{
    [Key]
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? OwnerUserId { get; set; }

    [MaxLength(255)]
    public string OwnerName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string OwnerEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public bool IsStarred { get; set; }

    [InverseProperty(nameof(KnowledgeSqlChatMessage.Session))]
    public List<KnowledgeSqlChatMessage> Messages { get; set; } = new();
}