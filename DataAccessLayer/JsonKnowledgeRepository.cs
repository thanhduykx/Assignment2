using System.Text.Json;

namespace DataAccessLayer;

public sealed class JsonKnowledgeRepository : IKnowledgeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public JsonKnowledgeRepository(string storePath)
    {
        _storePath = storePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
    }

    public async Task<IReadOnlyList<IndexedDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Documents
                .OrderByDescending(document => document.UploadedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<IndexedDocument>> GetDocumentsAsync(
        DocumentAccessScope scope,
        DocumentListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return ApplyDocumentListQuery(
                    ApplyDocumentScope((await LoadAsync(cancellationToken)).Documents, scope),
                    query)
                .OrderByDescending(document => document.UploadedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IndexedDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Documents.FirstOrDefault(document => document.Id == documentId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<IndexedDocument>> GetDocumentsByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var normalizedStatus = NormalizeRequiredText(status, "Status is required.");
            return (await LoadAsync(cancellationToken)).Documents
                .Where(document => string.Equals(document.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
                .OrderBy(document => document.UploadedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var indexedDocumentIds = store.Documents
                .Where(document => string.Equals(document.Status, DocumentIndexStatus.Indexed, StringComparison.OrdinalIgnoreCase))
                .Select(document => document.Id)
                .ToHashSet();
            return store.Chunks
                .Where(chunk => indexedDocumentIds.Contains(chunk.DocumentId))
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetChunksAsync(
        DocumentAccessScope scope,
        IReadOnlyCollection<string>? allowedSubjects = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var indexedDocuments = ApplyDocumentScope(store.Documents, scope)
                .Where(document => document.Status.Equals(DocumentIndexStatus.Indexed, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(document => document.Id);
            var subjects = NormalizeSubjectFilters(allowedSubjects);
            return store.Chunks
                .Where(chunk => indexedDocuments.ContainsKey(chunk.DocumentId))
                .Where(chunk => subjects.Count == 0 || subjects.Contains(chunk.Subject))
                .OrderBy(chunk => chunk.DocumentId)
                .ThenBy(chunk => chunk.ChunkIndex)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Chunks
                .Where(chunk => chunk.DocumentId == documentId)
                .OrderBy(chunk => chunk.ChunkIndex)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetIndexedSubjectsAsync(
        DocumentAccessScope scope,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return ApplyDocumentScope((await LoadAsync(cancellationToken)).Documents, scope)
                .Where(document => document.Status.Equals(DocumentIndexStatus.Indexed, StringComparison.OrdinalIgnoreCase))
                .Select(document => document.Subject)
                .Where(subject => !string.IsNullOrWhiteSpace(subject))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(subject => subject)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetStaleIndexedDocumentIdsAsync(
        string embeddingModel,
        int embeddingDimensions,
        string? chunkingStrategy,
        DocumentAccessScope scope,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var normalizedModel = (embeddingModel ?? string.Empty).Trim();
            var normalizedChunkingStrategy = (chunkingStrategy ?? string.Empty).Trim();
            return ApplyDocumentScope((await LoadAsync(cancellationToken)).Documents, scope)
                .Where(document => document.Status.Equals(DocumentIndexStatus.Indexed, StringComparison.OrdinalIgnoreCase))
                .Where(document => document.ChunkCount == 0
                                   || !document.EmbeddingModel.Equals(normalizedModel, StringComparison.Ordinal)
                                   || document.EmbeddingDimensions != embeddingDimensions
                                   || (!string.IsNullOrWhiteSpace(normalizedChunkingStrategy)
                                       && !document.ChunkingStrategy.Equals(normalizedChunkingStrategy, StringComparison.Ordinal)))
                .OrderBy(document => document.UploadedAt)
                .Select(document => document.Id)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddDocumentAsync(IndexedDocument document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            store.Documents.Add(document);
            store.Chunks.AddRange(chunks);
            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkDocumentIndexProcessingAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var document = store.Documents.FirstOrDefault(item => item.Id == documentId)
                ?? throw new InvalidOperationException("Document not found.");

            document.Status = DocumentIndexStatus.Processing;
            document.IndexError = string.Empty;
            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CompleteDocumentIndexAsync(
        Guid documentId,
        IReadOnlyList<DocumentChunk> chunks,
        string embeddingModel,
        int embeddingDimensions,
        string chunkingStrategy,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var document = store.Documents.FirstOrDefault(item => item.Id == documentId)
                ?? throw new InvalidOperationException("Document not found.");

            store.Chunks.RemoveAll(item => item.DocumentId == documentId);
            store.Chunks.AddRange(chunks);

            document.Status = DocumentIndexStatus.Indexed;
            document.ChunkCount = chunks.Count;
            document.IndexedAt = DateTimeOffset.UtcNow;
            document.IndexError = string.Empty;
            document.EmbeddingModel = embeddingModel.Trim();
            document.EmbeddingDimensions = Math.Max(0, embeddingDimensions);
            document.ChunkingStrategy = chunkingStrategy.Trim();

            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkDocumentIndexFailedAsync(Guid documentId, string errorMessage, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var document = store.Documents.FirstOrDefault(item => item.Id == documentId);
            if (document is null)
            {
                return;
            }

            document.Status = DocumentIndexStatus.Failed;
            document.IndexError = (errorMessage ?? string.Empty).Trim();
            document.IndexedAt = null;
            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IndexedDocument> UpdateDocumentMetadataAsync(
        Guid documentId,
        string fileName,
        string subject,
        string chapter,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var document = store.Documents.FirstOrDefault(item => item.Id == documentId)
                ?? throw new InvalidOperationException("Document not found.");
            var normalizedFileName = NormalizeFileName(fileName);
            var normalizedSubject = NormalizeRequiredText(subject, "Subject is required.");
            var normalizedChapter = NormalizeRequiredText(chapter, "Chapter is required.");

            document.FileName = normalizedFileName;
            document.Subject = normalizedSubject;
            document.Chapter = normalizedChapter;

            foreach (var chunk in store.Chunks.Where(item => item.DocumentId == documentId))
            {
                chunk.FileName = normalizedFileName;
                chunk.Subject = normalizedSubject;
                chunk.Chapter = normalizedChapter;
            }

            await SaveAsync(store, cancellationToken);
            return document;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            store.Documents.RemoveAll(item => item.Id == documentId);
            store.Chunks.RemoveAll(item => item.DocumentId == documentId);
            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<CourseSubject>> GetCourseCatalogAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).CourseSubjects
                .OrderBy(subject => subject.Code)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CourseSubject> UpsertSubjectAsync(
        Guid? subjectId,
        string code,
        string name,
        string? description,
        CancellationToken cancellationToken = default,
        SubjectOwnerInfo? ownerInfo = null)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var normalizedCode = NormalizeCode(code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                throw new InvalidOperationException("Subject code is required.");
            }

            if (store.CourseSubjects.Any(item => item.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase)
                && (!subjectId.HasValue || item.Id != subjectId.Value)))
            {
                throw new InvalidOperationException("Subject code already exists.");
            }

            var subject = subjectId.HasValue
                ? store.CourseSubjects.FirstOrDefault(item => item.Id == subjectId.Value)
                : null;
            if (subject is null)
            {
                subject = new CourseSubject { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
                store.CourseSubjects.Add(subject);
            }

            subject.Code = normalizedCode;
            subject.Name = string.IsNullOrWhiteSpace(name) ? normalizedCode : name.Trim();
            subject.Description = description?.Trim() ?? string.Empty;
            if (ownerInfo is not null)
            {
                subject.OwnerUserId = ownerInfo.UserId;
                subject.OwnerName = ownerInfo.Name?.Trim() ?? string.Empty;
                subject.OwnerEmail = ownerInfo.Email?.Trim() ?? string.Empty;
            }

            await SaveAsync(store, cancellationToken);
            return subject;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            store.CourseSubjects.RemoveAll(item => item.Id == subjectId);
            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CourseChapter> UpsertChapterAsync(
        Guid? chapterId,
        Guid subjectId,
        string title,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var subject = store.CourseSubjects.FirstOrDefault(item => item.Id == subjectId)
                ?? throw new InvalidOperationException("Subject not found.");
            var trimmedTitle = title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                throw new InvalidOperationException("Chapter title is required.");
            }

            if (subject.Chapters.Any(item => item.Title.Equals(trimmedTitle, StringComparison.OrdinalIgnoreCase)
                && (!chapterId.HasValue || item.Id != chapterId.Value)))
            {
                throw new InvalidOperationException("Chapter already exists for this subject.");
            }

            var chapter = chapterId.HasValue
                ? subject.Chapters.FirstOrDefault(item => item.Id == chapterId.Value)
                : null;
            if (chapter is null)
            {
                chapter = new CourseChapter { Id = Guid.NewGuid(), SubjectId = subject.Id };
                subject.Chapters.Add(chapter);
            }

            chapter.SubjectId = subject.Id;
            chapter.SubjectCode = subject.Code;
            chapter.SubjectName = subject.Name;
            chapter.Title = trimmedTitle;
            chapter.SortOrder = sortOrder;
            await SaveAsync(store, cancellationToken);
            return chapter;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            foreach (var subject in store.CourseSubjects)
            {
                subject.Chapters.RemoveAll(item => item.Id == chapterId);
            }

            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ChatSession>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Sessions
                .OrderByDescending(session => session.IsStarred)
                .ThenByDescending(session => session.UpdatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ChatSession>> GetSessionsForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Sessions
                .Where(session => session.OwnerUserId == ownerUserId)
                .OrderByDescending(session => session.IsStarred)
                .ThenByDescending(session => session.UpdatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ChatSessionSummary>> GetSessionSummariesForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Sessions
                .Where(session => session.OwnerUserId == ownerUserId)
                .OrderByDescending(session => session.IsStarred)
                .ThenByDescending(session => session.UpdatedAt)
                .Select(ToSessionSummary)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Sessions.FirstOrDefault(item => item.Id == sessionId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ChatSession?> GetSessionForOwnerAsync(Guid sessionId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Sessions.FirstOrDefault(item => item.Id == sessionId && item.OwnerUserId == ownerUserId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ChatSession> GetOrCreateSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default,
        ChatSessionOwnerInfo? ownerInfo = null)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is not null)
            {
                EnsureSessionOwner(session, ownerInfo);
                await SaveAsync(store, cancellationToken);
                return session;
            }

            session = new ChatSession
            {
                Id = sessionId,
                OwnerUserId = ownerInfo?.UserId,
                OwnerName = ownerInfo?.Name?.Trim() ?? string.Empty,
                OwnerEmail = ownerInfo?.Email?.Trim() ?? string.Empty
            };
            store.Sessions.Add(session);
            await SaveAsync(store, cancellationToken);
            return session;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ChatSession?> RenameSessionAsync(
        Guid sessionId,
        string title,
        CancellationToken cancellationToken = default,
        ChatSessionOwnerInfo? ownerInfo = null)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                return null;
            }

            EnsureSessionOwner(session, ownerInfo);
            session.Title = NormalizeSessionTitle(title);
            session.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveAsync(store, cancellationToken);
            return session;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ChatSession?> SetSessionStarredAsync(
        Guid sessionId,
        bool isStarred,
        CancellationToken cancellationToken = default,
        ChatSessionOwnerInfo? ownerInfo = null)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                return null;
            }

            EnsureSessionOwner(session, ownerInfo);
            session.IsStarred = isStarred;
            session.UpdatedAt = DateTimeOffset.UtcNow;
            await SaveAsync(store, cancellationToken);
            return session;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default,
        ChatSessionOwnerInfo? ownerInfo = null)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                return false;
            }

            EnsureSessionOwner(session, ownerInfo);
            store.Sessions.Remove(session);
            await SaveAsync(store, cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task ImportFromJsonIfEmptyAsync(string jsonStorePath, CancellationToken cancellationToken = default)
    {
        // JSON repository already uses JSON storage; nothing to import.
        return Task.CompletedTask;
    }

    public Task AddSubjectLecturerAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default)
    {
        // JSON repository does not support subject lecturer management.
        return Task.CompletedTask;
    }

    public Task RemoveSubjectLecturerAsync(Guid subjectId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetSubjectLecturerIdsAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }

    public async Task AddMessageAsync(
        Guid sessionId,
        ChatMessage message,
        CancellationToken cancellationToken = default,
        ChatSessionOwnerInfo? ownerInfo = null)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                session = new ChatSession
                {
                    Id = sessionId,
                    Title = IsUserMessage(message) ? BuildSessionTitle(message.Content) : string.Empty,
                    OwnerUserId = ownerInfo?.UserId,
                    OwnerName = ownerInfo?.Name?.Trim() ?? string.Empty,
                    OwnerEmail = ownerInfo?.Email?.Trim() ?? string.Empty
                };
                store.Sessions.Add(session);
            }
            else
            {
                EnsureSessionOwner(session, ownerInfo);
            }

            session.Messages.Add(message);
            session.UpdatedAt = DateTimeOffset.UtcNow;
            if (string.IsNullOrWhiteSpace(session.Title) && IsUserMessage(message))
            {
                session.Title = BuildSessionTitle(message.Content);
            }

            await SaveAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<KnowledgeStore> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new KnowledgeStore();
        }

        await using var stream = File.OpenRead(_storePath);
        return await JsonSerializer.DeserializeAsync<KnowledgeStore>(stream, JsonOptions, cancellationToken) ?? new KnowledgeStore();
    }

    private async Task SaveAsync(KnowledgeStore store, CancellationToken cancellationToken)
    {
        var tempPath = $"{_storePath}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, store, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, _storePath, true);
    }

    private static string NormalizeCode(string code)
    {
        return string.Join(string.Empty, (code ?? string.Empty).Trim().ToUpperInvariant().Where(character => !char.IsWhiteSpace(character)));
    }

    private static string NormalizeFileName(string fileName)
    {
        var normalized = Path.GetFileName((fileName ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("File name is required.");
        }

        return normalized;
    }

    private static string NormalizeRequiredText(string value, string errorMessage)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }

    private static IEnumerable<IndexedDocument> ApplyDocumentScope(
        IEnumerable<IndexedDocument> documents,
        DocumentAccessScope scope)
    {
        if (scope.IsAdmin)
        {
            return documents;
        }

        if (scope.Mode == DocumentAccessMode.Chat && scope.IsStudent)
        {
            return documents;
        }

        if (scope.IsLecturer && scope.UserId is { } userId)
        {
            var email = scope.NormalizedEmail;
            return documents.Where(document =>
                document.UploadedByUserId == userId
                || (!document.UploadedByUserId.HasValue
                    && !string.IsNullOrWhiteSpace(email)
                    && document.UploadedByEmail.Equals(email, StringComparison.OrdinalIgnoreCase)));
        }

        return Array.Empty<IndexedDocument>();
    }

    private static IEnumerable<IndexedDocument> ApplyDocumentListQuery(
        IEnumerable<IndexedDocument> documents,
        DocumentListQuery? query)
    {
        if (!string.IsNullOrWhiteSpace(query?.Query))
        {
            var search = query.Query.Trim();
            documents = documents.Where(document =>
                Contains(document.FileName, search)
                || Contains(document.Subject, search)
                || Contains(document.Chapter, search)
                || Contains(document.UploadedByName, search)
                || Contains(document.UploadedByEmail, search)
                || Contains(document.ContentType, search));
        }

        if (!string.IsNullOrWhiteSpace(query?.SubjectFilter))
        {
            var subjectFilter = query.SubjectFilter.Trim();
            documents = documents.Where(document => SubjectMatchesFilter(document.Subject, subjectFilter));
        }

        if (!string.IsNullOrWhiteSpace(query?.StatusFilter))
        {
            var status = query.StatusFilter.Trim();
            documents = documents.Where(document => document.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        return documents;
    }

    private static HashSet<string> NormalizeSubjectFilters(IReadOnlyCollection<string>? subjects)
    {
        return subjects is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : subjects
                .Where(subject => !string.IsNullOrWhiteSpace(subject))
                .Select(subject => subject.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool Contains(string value, string search)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SubjectMatchesFilter(string documentSubject, string subjectFilter)
    {
        var normalizedDocumentSubject = (documentSubject ?? string.Empty).Trim();
        var normalizedSubjectFilter = (subjectFilter ?? string.Empty).Trim();
        if (normalizedDocumentSubject.Equals(normalizedSubjectFilter, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var documentCode = ParseSubjectCode(normalizedDocumentSubject);
        var filterCode = ParseSubjectCode(normalizedSubjectFilter);
        return !string.IsNullOrWhiteSpace(documentCode)
               && documentCode.Equals(filterCode, StringComparison.OrdinalIgnoreCase);
    }

    private static string ParseSubjectCode(string subject)
    {
        var trimmed = (subject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        var separatorIndex = trimmed.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            separatorIndex = trimmed.IndexOf('-', StringComparison.Ordinal);
        }

        var candidate = separatorIndex > 0
            ? trimmed[..separatorIndex]
            : trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? trimmed;

        return NormalizeCatalogCode(candidate);
    }

    private static string NormalizeCatalogCode(string code)
    {
        return new string((code ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character is '_' or '.')
            .Take(32)
            .ToArray());
    }

    private static bool IsUserMessage(ChatMessage message)
    {
        return message.Role.Equals("user", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSessionTitle(string content)
    {
        var normalized = NormalizeSessionTitle(content);
        return normalized.Length <= 56 ? normalized : $"{normalized[..56]}...";
    }

    private static ChatSessionSummary ToSessionSummary(ChatSession session)
    {
        return new ChatSessionSummary
        {
            Id = session.Id,
            Title = session.Title,
            IsStarred = session.IsStarred,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            MessageCount = session.Messages.Count,
            FirstUserMessagePreview = session.Messages
                .OrderBy(message => message.CreatedAt)
                .FirstOrDefault(IsUserMessage)
                ?.Content
                ?.Trim() ?? string.Empty
        };
    }

    private static string NormalizeSessionTitle(string title)
    {
        var normalized = string.Join(' ', (title ?? string.Empty)
            .Trim()
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Session title is required.");
        }

        return normalized.Length <= 200 ? normalized : normalized[..200];
    }

    private static void EnsureSessionOwner(ChatSession session, ChatSessionOwnerInfo? ownerInfo)
    {
        if (ownerInfo?.UserId is not { } ownerUserId)
        {
            return;
        }

        if (session.OwnerUserId.HasValue && session.OwnerUserId.Value != ownerUserId)
        {
            throw new InvalidOperationException("Chat session is not available for this user.");
        }

        if (!session.OwnerUserId.HasValue)
        {
            session.OwnerUserId = ownerUserId;
            session.OwnerName = ownerInfo.Name?.Trim() ?? string.Empty;
            session.OwnerEmail = ownerInfo.Email?.Trim() ?? string.Empty;
        }
    }
}
