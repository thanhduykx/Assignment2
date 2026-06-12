using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context;

internal sealed class KnowledgeSqlDbContext(DbContextOptions<KnowledgeSqlDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeSqlDocument> Documents => Set<KnowledgeSqlDocument>();
    public DbSet<KnowledgeSqlChunk> Chunks => Set<KnowledgeSqlChunk>();
    public DbSet<KnowledgeSqlChatSession> Sessions => Set<KnowledgeSqlChatSession>();
    public DbSet<KnowledgeSqlChatMessage> Messages => Set<KnowledgeSqlChatMessage>();
    public DbSet<KnowledgeSqlCitation> Citations => Set<KnowledgeSqlCitation>();
    public DbSet<KnowledgeSqlCourseSubject> CourseSubjects => Set<KnowledgeSqlCourseSubject>();
    public DbSet<KnowledgeSqlCourseChapter> CourseChapters => Set<KnowledgeSqlCourseChapter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeSqlDocument>(entity =>
        {
            entity.ToTable("rag_documents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.StoredPath).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.Subject).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Chapter).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.FileSizeBytes).HasDefaultValue(0L);
            entity.Property(item => item.UploadedByName).HasMaxLength(255);
            entity.Property(item => item.UploadedByEmail).HasMaxLength(255);
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired().HasDefaultValue(DocumentIndexStatus.Indexed);
            entity.Property(item => item.IndexError).HasColumnType("nvarchar(max)");
            entity.Property(item => item.EmbeddingModel).HasMaxLength(100).IsRequired().HasDefaultValue(string.Empty);
            entity.Property(item => item.EmbeddingDimensions).HasDefaultValue(0);
            entity.Property(item => item.ChunkingStrategy).HasMaxLength(100).IsRequired().HasDefaultValue(string.Empty);
            entity.HasIndex(item => item.FileName);
            entity.HasIndex(item => item.UploadedByUserId);
            entity.HasIndex(item => new { item.Status, item.UploadedAt });
        });

        modelBuilder.Entity<KnowledgeSqlChunk>(entity =>
        {
            entity.ToTable("rag_chunks");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Subject).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Chapter).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Text).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(item => item.SectionTitle).HasMaxLength(255).IsRequired().HasDefaultValue(string.Empty);
            entity.Property(item => item.CharStart).HasDefaultValue(0);
            entity.Property(item => item.CharEnd).HasDefaultValue(0);
            entity.Property(item => item.EmbeddingJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(item => new { item.DocumentId, item.ChunkIndex }).IsUnique();
            entity.HasIndex(item => new { item.Subject, item.Chapter });
            entity.HasOne(item => item.Document)
                .WithMany(item => item.Chunks)
                .HasForeignKey(item => item.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSqlChatSession>(entity =>
        {
            entity.ToTable("rag_chat_sessions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(200).IsRequired().HasDefaultValue(string.Empty);
            entity.Property(item => item.IsStarred).HasDefaultValue(false);
            entity.Property(item => item.OwnerName).HasMaxLength(255);
            entity.Property(item => item.OwnerEmail).HasMaxLength(255);
            entity.HasIndex(item => item.UpdatedAt);
            entity.HasIndex(item => item.OwnerUserId);
            entity.HasIndex(item => new { item.OwnerUserId, item.IsStarred, item.UpdatedAt });
        });

        modelBuilder.Entity<KnowledgeSqlChatMessage>(entity =>
        {
            entity.ToTable("rag_chat_messages");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Role).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Content).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(item => new { item.SessionId, item.CreatedAt });
            entity.HasOne(item => item.Session)
                .WithMany(item => item.Messages)
                .HasForeignKey(item => item.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSqlCitation>(entity =>
        {
            entity.ToTable("rag_citations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Subject).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Chapter).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Excerpt).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasOne(item => item.Message)
                .WithMany(item => item.Citations)
                .HasForeignKey(item => item.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSqlCourseSubject>(entity =>
        {
            entity.ToTable("rag_subjects");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.OwnerName).HasMaxLength(255);
            entity.Property(item => item.OwnerEmail).HasMaxLength(255);
            entity.HasIndex(item => item.Code).IsUnique();
            entity.HasIndex(item => item.OwnerUserId);
        });

        modelBuilder.Entity<KnowledgeSqlCourseChapter>(entity =>
        {
            entity.ToTable("rag_chapters");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(255).IsRequired();
            entity.HasIndex(item => new { item.SubjectId, item.Title }).IsUnique();
            entity.HasOne(item => item.Subject)
                .WithMany(item => item.Chapters)
                .HasForeignKey(item => item.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
