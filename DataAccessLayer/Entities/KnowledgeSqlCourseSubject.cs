using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_subjects")]
public sealed class KnowledgeSqlCourseSubject
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Subject Leader (tối đa 1)
    public Guid? OwnerUserId { get; set; }

    [MaxLength(255)]
    public string? OwnerName { get; set; }

    [MaxLength(255)]
    public string? OwnerEmail { get; set; }

    // Teaching Lecturers (N-N)
    [InverseProperty(nameof(KnowledgeSqlSubjectLecturer.Subject))]
    public List<KnowledgeSqlSubjectLecturer> TeachingLecturers { get; set; } = new();

    [InverseProperty(nameof(KnowledgeSqlCourseChapter.Subject))]
    public List<KnowledgeSqlCourseChapter> Chapters { get; set; } = new();
}