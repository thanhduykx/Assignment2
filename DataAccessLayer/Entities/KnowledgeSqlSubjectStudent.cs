using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

[Table("rag_subject_students")]
public sealed class KnowledgeSqlSubjectStudent
{
    [Key]
    public Guid Id { get; set; }

    public Guid SubjectId { get; set; }

    [ForeignKey(nameof(SubjectId))]
    [InverseProperty(nameof(KnowledgeSqlCourseSubject.EnrolledStudents))]
    public KnowledgeSqlCourseSubject Subject { get; set; } = null!;

    public Guid UserId { get; set; }
}
