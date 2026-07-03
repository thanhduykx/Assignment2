using BusinessObjects;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Mapping;

public static class KnowledgeSqlMapper
{
    public static IndexedDocument ToModel(KnowledgeSqlDocument entity)
    {
        return new IndexedDocument
        {
            Id = entity.Id,
            FileName = entity.FileName,
            StoredPath = entity.StoredPath,
            Subject = entity.Subject,
            Chapter = entity.Chapter,
            ContentType = entity.ContentType,
            UploadedAt = entity.UploadedAt,
            ChunkCount = entity.ChunkCount,
            FileSizeBytes = entity.FileSizeBytes,
            UploadedByUserId = entity.UploadedByUserId,
            
            // Sửa warning dòng 22, 23
            UploadedByName = entity.UploadedByName ?? string.Empty,
            UploadedByEmail = entity.UploadedByEmail ?? string.Empty,
            
            Status = entity.Status,
            IndexedAt = entity.IndexedAt,
            
            // Sửa warning dòng 26
            IndexError = entity.IndexError ?? string.Empty,
            
            EmbeddingModel = entity.EmbeddingModel,
            EmbeddingDimensions = entity.EmbeddingDimensions,
            ChunkingStrategy = entity.ChunkingStrategy
        };
    }

    public static KnowledgeSqlDocument ToEntity(IndexedDocument model)
    {
        return new KnowledgeSqlDocument
        {
            Id = model.Id,
            FileName = model.FileName,
            StoredPath = model.StoredPath,
            Subject = model.Subject,
            Chapter = model.Chapter,
            ContentType = model.ContentType,
            UploadedAt = model.UploadedAt,
            ChunkCount = model.ChunkCount,
            FileSizeBytes = model.FileSizeBytes,
            UploadedByUserId = model.UploadedByUserId,
            UploadedByName = string.IsNullOrEmpty(model.UploadedByName) ? null : model.UploadedByName,
            UploadedByEmail = string.IsNullOrEmpty(model.UploadedByEmail) ? null : model.UploadedByEmail,
            Status = model.Status,
            IndexedAt = model.IndexedAt,
            IndexError = string.IsNullOrEmpty(model.IndexError) ? null : model.IndexError,
            EmbeddingModel = model.EmbeddingModel,
            EmbeddingDimensions = model.EmbeddingDimensions,
            ChunkingStrategy = model.ChunkingStrategy
        };
    }

    public static DocumentChunk ToModel(KnowledgeSqlChunk entity)
    {
        var chunk = new DocumentChunk
        {
            Id = entity.Id,
            DocumentId = entity.DocumentId,
            FileName = entity.FileName,
            Subject = entity.Subject,
            Chapter = entity.Chapter,
            ChunkIndex = entity.ChunkIndex,
            Text = entity.Text,
            SectionTitle = entity.SectionTitle,
            CharStart = entity.CharStart,
            CharEnd = entity.CharEnd
        };

        if (!string.IsNullOrEmpty(entity.EmbeddingJson))
        {
            chunk.Embedding = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, double>>(entity.EmbeddingJson) ?? new();
        }

        return chunk;
    }

    public static KnowledgeSqlChunk ToEntity(DocumentChunk model)
    {
        return new KnowledgeSqlChunk
        {
            Id = model.Id,
            DocumentId = model.DocumentId,
            FileName = model.FileName,
            Subject = model.Subject,
            Chapter = model.Chapter,
            ChunkIndex = model.ChunkIndex,
            Text = model.Text,
            SectionTitle = model.SectionTitle,
            CharStart = model.CharStart,
            CharEnd = model.CharEnd,
            EmbeddingJson = model.Embedding != null ? System.Text.Json.JsonSerializer.Serialize(model.Embedding) : "{}"
        };
    }

    public static CourseSubject ToModel(KnowledgeSqlCourseSubject entity)
    {
        return new CourseSubject
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            OwnerUserId = entity.OwnerUserId,
            
            // Sửa warning dòng 102, 103
            OwnerName = entity.OwnerName ?? string.Empty,
            OwnerEmail = entity.OwnerEmail ?? string.Empty,
            
            Chapters = entity.Chapters
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Title)
                .Select(ToModel)
                .ToList()
        };
    }

    public static CourseChapter ToModel(KnowledgeSqlCourseChapter entity)
    {
        return new CourseChapter
        {
            Id = entity.Id,
            SubjectId = entity.SubjectId,
            SubjectCode = entity.Subject?.Code ?? string.Empty,
            SubjectName = entity.Subject?.Name ?? string.Empty,
            Title = entity.Title,
            SortOrder = entity.SortOrder
        };
    }

    public static ChatSession ToModel(KnowledgeSqlChatSession entity)
    {
        return new ChatSession
        {
            Id = entity.Id,
            Title = entity.Title,
            IsStarred = entity.IsStarred,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            OwnerUserId = entity.OwnerUserId,
            OwnerName = entity.OwnerName,
            OwnerEmail = entity.OwnerEmail,
            Messages = entity.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(ToModel)
                .ToList()
        };
    }

    public static ChatMessage ToModel(KnowledgeSqlChatMessage entity)
    {
        return new ChatMessage
        {
            Role = entity.Role,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt,
            Citations = entity.Citations
                .Select(ToModel)
                .ToList()
        };
    }

    public static SourceCitation ToModel(KnowledgeSqlCitation entity)
    {
        return new SourceCitation
        {
            DocumentId = entity.DocumentId,
            FileName = entity.FileName,
            Subject = entity.Subject,
            Chapter = entity.Chapter,
            ChunkIndex = entity.ChunkIndex,
            Score = entity.Score,
            Excerpt = entity.Excerpt
        };
    }

    public static KnowledgeSqlChatMessage ToEntity(Guid sessionId, ChatMessage model)
    {
        return new KnowledgeSqlChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = model.Role,
            Content = model.Content,
            CreatedAt = model.CreatedAt,
            Citations = model.Citations.Select(c => ToEntity(Guid.NewGuid(), c)).ToList()
        };
    }

    public static KnowledgeSqlCitation ToEntity(Guid messageId, SourceCitation model)
    {
        return new KnowledgeSqlCitation
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            DocumentId = model.DocumentId,
            FileName = model.FileName,
            Subject = model.Subject,
            Chapter = model.Chapter,
            ChunkIndex = model.ChunkIndex,
            Score = model.Score,
            Excerpt = model.Excerpt
        };
    }
}