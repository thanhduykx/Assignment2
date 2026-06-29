using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context;

public sealed class KnowledgeSqlDbContext : DbContext
{
    public KnowledgeSqlDbContext(DbContextOptions<KnowledgeSqlDbContext> options) : base(options)
    {
    }

    public DbSet<KnowledgeSqlDocument> Documents => Set<KnowledgeSqlDocument>();
    public DbSet<KnowledgeSqlChunk> Chunks => Set<KnowledgeSqlChunk>();
    public DbSet<KnowledgeSqlCourseSubject> CourseSubjects => Set<KnowledgeSqlCourseSubject>();
    public DbSet<KnowledgeSqlCourseChapter> CourseChapters => Set<KnowledgeSqlCourseChapter>();
    public DbSet<KnowledgeSqlChatSession> Sessions => Set<KnowledgeSqlChatSession>();
    public DbSet<KnowledgeSqlChatMessage> Messages => Set<KnowledgeSqlChatMessage>();
    public DbSet<KnowledgeSqlCitation> Citations => Set<KnowledgeSqlCitation>();
    public DbSet<KnowledgeSqlSubjectLecturer> SubjectLecturers => Set<KnowledgeSqlSubjectLecturer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<KnowledgeSqlDocument>(entity =>
        {
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.Subject);
            entity.HasIndex(d => d.UploadedAt);
        });

        modelBuilder.Entity<KnowledgeSqlChunk>(entity =>
        {
            entity.HasIndex(c => c.DocumentId);
            entity.HasIndex(c => c.Subject);

            entity.HasOne(c => c.Document)
                .WithMany()
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSqlCourseSubject>(entity =>
        {
            entity.HasIndex(s => s.Code).IsUnique();

            entity.HasMany(s => s.Chapters)
                .WithOne(c => c.Subject)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.TeachingLecturers)
                .WithOne(tl => tl.Subject)
                .HasForeignKey(tl => tl.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSqlSubjectLecturer>(entity =>
        {
            entity.HasIndex(tl => new { tl.SubjectId, tl.UserId }).IsUnique();
        });

        modelBuilder.Entity<KnowledgeSqlCourseChapter>(entity =>
        {
            entity.HasIndex(c => new { c.SubjectId, c.Title }).IsUnique();
        });

        modelBuilder.Entity<KnowledgeSqlChatSession>(entity =>
        {
            entity.HasIndex(s => s.OwnerUserId);
            entity.HasIndex(s => s.UpdatedAt);

            entity.HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSqlChatMessage>(entity =>
        {
            entity.HasMany(m => m.Citations)
                .WithOne(c => c.Message)
                .HasForeignKey(c => c.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSqlCitation>(entity =>
        {
            entity.HasIndex(c => c.DocumentId);
        });
    }
}