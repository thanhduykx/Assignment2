using DataAccessLayer;
using BusinessObjects;
using DataAccessLayer.Repositories;

namespace ServicesLayer;

public interface IKnowledgeService
{
    Task<IReadOnlyList<IndexedDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndexedDocument>> GetDocumentsAsync(
        DocumentAccessScope scope,
        DocumentListQuery? query = null,
        CancellationToken cancellationToken = default);
    Task<IndexedDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndexedDocument>> GetDocumentsByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(
        DocumentAccessScope scope,
        IReadOnlyCollection<string>? allowedSubjects = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetIndexedSubjectsAsync(DocumentAccessScope scope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetStaleIndexedDocumentIdsAsync(
        string embeddingModel,
        int embeddingDimensions,
        string? chunkingStrategy,
        DocumentAccessScope scope,
        CancellationToken cancellationToken = default);
    Task AddDocumentAsync(IndexedDocument document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task MarkDocumentIndexProcessingAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task CompleteDocumentIndexAsync(
        Guid documentId,
        IReadOnlyList<DocumentChunk> chunks,
        string embeddingModel,
        int embeddingDimensions,
        string chunkingStrategy,
        CancellationToken cancellationToken = default);
    Task MarkDocumentIndexFailedAsync(Guid documentId, string errorMessage, CancellationToken cancellationToken = default);
    Task<IndexedDocument> UpdateDocumentMetadataAsync(Guid documentId, string fileName, string subject, string chapter, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseSubject>> GetCourseCatalogAsync(CancellationToken cancellationToken = default);
    Task<CourseSubject> UpsertSubjectAsync(Guid? subjectId, string code, string name, string? description, CancellationToken cancellationToken = default, SubjectOwnerInfo? ownerInfo = null);
    Task DeleteSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default);
    Task<CourseChapter> UpsertChapterAsync(Guid? chapterId, Guid subjectId, string title, int sortOrder, CancellationToken cancellationToken = default);
    Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken = default);
    Task RemoveSubjectLecturerAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetSubjectStudentIdsAsync(Guid subjectId, CancellationToken cancellationToken = default);
    Task AddSubjectStudentAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default);
    Task RemoveSubjectStudentAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default);
    Task SetSubjectActiveStatusAsync(Guid subjectId, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSession>> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSession>> GetSessionsForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSessionSummary>> GetSessionSummariesForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionForOwnerAsync(Guid sessionId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<ChatSession> GetOrCreateSessionAsync(Guid sessionId, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null);
    Task<ChatSession?> RenameSessionAsync(Guid sessionId, string title, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null);
    Task<ChatSession?> SetSessionStarredAsync(Guid sessionId, bool isStarred, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null);
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null);
    Task AddMessageAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null);
}

public sealed class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeRepository _repository;

    public KnowledgeService(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<IndexedDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default) => _repository.GetDocumentsAsync(cancellationToken);
    public Task<IReadOnlyList<IndexedDocument>> GetDocumentsAsync(DocumentAccessScope scope, DocumentListQuery? query = null, CancellationToken cancellationToken = default) => _repository.GetDocumentsAsync(scope, query, cancellationToken);
    public Task<IndexedDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repository.GetDocumentAsync(documentId, cancellationToken);
    public Task<IReadOnlyList<IndexedDocument>> GetDocumentsByStatusAsync(string status, CancellationToken cancellationToken = default) => _repository.GetDocumentsByStatusAsync(status, cancellationToken);
    public Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(CancellationToken cancellationToken = default) => _repository.GetChunksAsync(cancellationToken);
    public Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(DocumentAccessScope scope, IReadOnlyCollection<string>? allowedSubjects = null, CancellationToken cancellationToken = default) => _repository.GetChunksAsync(scope, allowedSubjects, cancellationToken);
    public Task<IReadOnlyList<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default) => _repository.GetDocumentChunksAsync(documentId, cancellationToken);
    public Task<IReadOnlyList<string>> GetIndexedSubjectsAsync(DocumentAccessScope scope, CancellationToken cancellationToken = default) => _repository.GetIndexedSubjectsAsync(scope, cancellationToken);
    public Task<IReadOnlyList<Guid>> GetStaleIndexedDocumentIdsAsync(string embeddingModel, int embeddingDimensions, string? chunkingStrategy, DocumentAccessScope scope, CancellationToken cancellationToken = default) => _repository.GetStaleIndexedDocumentIdsAsync(embeddingModel, embeddingDimensions, chunkingStrategy, scope, cancellationToken);
    public Task AddDocumentAsync(IndexedDocument document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default) => _repository.AddDocumentAsync(document, chunks, cancellationToken);
    public Task MarkDocumentIndexProcessingAsync(Guid documentId, CancellationToken cancellationToken = default) => _repository.MarkDocumentIndexProcessingAsync(documentId, cancellationToken);
    public Task CompleteDocumentIndexAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, string embeddingModel, int embeddingDimensions, string chunkingStrategy, CancellationToken cancellationToken = default) => _repository.CompleteDocumentIndexAsync(documentId, chunks, embeddingModel, embeddingDimensions, chunkingStrategy, cancellationToken);
    public Task MarkDocumentIndexFailedAsync(Guid documentId, string errorMessage, CancellationToken cancellationToken = default) => _repository.MarkDocumentIndexFailedAsync(documentId, errorMessage, cancellationToken);
    public Task<IndexedDocument> UpdateDocumentMetadataAsync(Guid documentId, string fileName, string subject, string chapter, CancellationToken cancellationToken = default) => _repository.UpdateDocumentMetadataAsync(documentId, fileName, subject, chapter, cancellationToken);
    public Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repository.DeleteDocumentAsync(documentId, cancellationToken);
    public Task<IReadOnlyList<CourseSubject>> GetCourseCatalogAsync(CancellationToken cancellationToken = default) => _repository.GetCourseCatalogAsync(cancellationToken);
    public Task<CourseSubject> UpsertSubjectAsync(Guid? subjectId, string code, string name, string? description, CancellationToken cancellationToken = default, SubjectOwnerInfo? ownerInfo = null) => _repository.UpsertSubjectAsync(subjectId, code, name, description, cancellationToken, ownerInfo);
    public Task DeleteSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default) => _repository.DeleteSubjectAsync(subjectId, cancellationToken);
    public Task<CourseChapter> UpsertChapterAsync(Guid? chapterId, Guid subjectId, string title, int sortOrder, CancellationToken cancellationToken = default) => _repository.UpsertChapterAsync(chapterId, subjectId, title, sortOrder, cancellationToken);
    public Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken = default) => _repository.DeleteChapterAsync(chapterId, cancellationToken);
    public Task RemoveSubjectLecturerAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default) => _repository.RemoveSubjectLecturerAsync(subjectId, userId, cancellationToken);
    public Task<IReadOnlyList<Guid>> GetSubjectStudentIdsAsync(Guid subjectId, CancellationToken cancellationToken = default) => _repository.GetSubjectStudentIdsAsync(subjectId, cancellationToken);
    public Task AddSubjectStudentAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default) => _repository.AddSubjectStudentAsync(subjectId, userId, cancellationToken);
    public Task RemoveSubjectStudentAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default) => _repository.RemoveSubjectStudentAsync(subjectId, userId, cancellationToken);
    public Task SetSubjectActiveStatusAsync(Guid subjectId, bool isActive, CancellationToken cancellationToken = default) => _repository.SetSubjectActiveStatusAsync(subjectId, isActive, cancellationToken);
    public Task<IReadOnlyList<ChatSession>> GetSessionsAsync(CancellationToken cancellationToken = default) => _repository.GetSessionsAsync(cancellationToken);
    public Task<IReadOnlyList<ChatSession>> GetSessionsForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => _repository.GetSessionsForOwnerAsync(ownerUserId, cancellationToken);
    public Task<IReadOnlyList<ChatSessionSummary>> GetSessionSummariesForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => _repository.GetSessionSummariesForOwnerAsync(ownerUserId, cancellationToken);
    public Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) => _repository.GetSessionAsync(sessionId, cancellationToken);
    public Task<ChatSession?> GetSessionForOwnerAsync(Guid sessionId, Guid ownerUserId, CancellationToken cancellationToken = default) => _repository.GetSessionForOwnerAsync(sessionId, ownerUserId, cancellationToken);
    public Task<ChatSession> GetOrCreateSessionAsync(Guid sessionId, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null) => _repository.GetOrCreateSessionAsync(sessionId, cancellationToken, ownerInfo);
    public Task<ChatSession?> RenameSessionAsync(Guid sessionId, string title, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null) => _repository.RenameSessionAsync(sessionId, title, cancellationToken, ownerInfo);
    public Task<ChatSession?> SetSessionStarredAsync(Guid sessionId, bool isStarred, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null) => _repository.SetSessionStarredAsync(sessionId, isStarred, cancellationToken, ownerInfo);
    public Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null) => _repository.DeleteSessionAsync(sessionId, cancellationToken, ownerInfo);
    public Task AddMessageAsync(Guid sessionId, ChatMessage message, CancellationToken cancellationToken = default, ChatSessionOwnerInfo? ownerInfo = null) => _repository.AddMessageAsync(sessionId, message, cancellationToken, ownerInfo);
}
