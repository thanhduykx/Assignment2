using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_subject_lecturers")]
public sealed class KnowledgeSqlSubjectLecturer
{
    [Key]
    public Guid Id { get; set; }

    public Guid SubjectId { get; set; }

    [ForeignKey(nameof(SubjectId))]
    [InverseProperty(nameof(KnowledgeSqlCourseSubject.TeachingLecturers))]
    public KnowledgeSqlCourseSubject Subject { get; set; } = null!;

    public Guid UserId { get; set; }
}