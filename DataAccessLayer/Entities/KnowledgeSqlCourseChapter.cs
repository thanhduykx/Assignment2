using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_chapters")]
public sealed class KnowledgeSqlCourseChapter
{
    [Key]
    public Guid Id { get; set; }

    public Guid SubjectId { get; set; }

    [ForeignKey(nameof(SubjectId))]
    [InverseProperty(nameof(KnowledgeSqlCourseSubject.Chapters))]
    public KnowledgeSqlCourseSubject Subject { get; set; } = null!;

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}