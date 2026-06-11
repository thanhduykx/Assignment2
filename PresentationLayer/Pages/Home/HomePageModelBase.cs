using System.Security.Claims;
using System.Text;
using DataAccessLayer;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Home;

public abstract class HomePageModelBase : PageModel
{
    protected readonly ILogger<HomePageModelBase> _logger;
    protected readonly IKnowledgeRepository _repository;
    protected readonly IDocumentIndexingService _indexingService;
    protected readonly IWebPageTextExtractor _webPageTextExtractor;
    protected readonly IRagChatService _chatService;
    protected readonly IUserAccountStore _users;
    protected readonly IWebHostEnvironment _environment;
    protected readonly IDocumentIndexJobQueue _indexJobQueue;

    protected HomePageModelBase(
        ILogger<HomePageModelBase> logger,
        IKnowledgeRepository repository,
        IDocumentIndexingService indexingService,
        IWebPageTextExtractor webPageTextExtractor,
        IRagChatService chatService,
        IUserAccountStore users,
        IWebHostEnvironment environment,
        IDocumentIndexJobQueue indexJobQueue)
    {
        _logger = logger;
        _repository = repository;
        _indexingService = indexingService;
        _webPageTextExtractor = webPageTextExtractor;
        _chatService = chatService;
        _users = users;
        _environment = environment;
        _indexJobQueue = indexJobQueue;
    }

        protected static object ToSessionSummary(ChatSession session)
        {
            return new
            {
                id = session.Id,
                title = GetSessionTitle(session),
                isStarred = session.IsStarred,
                createdAt = session.CreatedAt,
                updatedAt = session.UpdatedAt,
                messageCount = session.Messages.Count
            };
        }

        protected string CurrentRole()
        {
            return AppRoles.Normalize(User.FindFirstValue(ClaimTypes.Role));
        }

        protected bool IsAdmin()
        {
            return CurrentRole() == AppRoles.Admin;
        }

        protected bool IsLecturer()
        {
            return CurrentRole() == AppRoles.Lecturer;
        }

        protected bool CanManageDocuments()
        {
            return IsAdmin() || IsLecturer();
        }

        protected DocumentAccessScope BuildDocumentAccessScope(DocumentAccessMode mode)
        {
            return new DocumentAccessScope(
                CurrentRole(),
                CurrentUserId(),
                User.FindFirstValue(ClaimTypes.Email),
                mode);
        }

        protected Guid? CurrentUserId()
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : null;
        }

        protected static UserOptionViewModel ToUserOption(UserAccount user)
        {
            return new UserOptionViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };
        }

        protected async Task<SubjectOwnerInfo> BuildSubjectOwnerInfoAsync(Guid? ownerUserId, CancellationToken cancellationToken)
        {
            if (!ownerUserId.HasValue)
            {
                return new SubjectOwnerInfo(null, string.Empty, string.Empty);
            }

            var lecturer = (await _users.GetByRoleAsync(AppRoles.Lecturer, cancellationToken))
                .FirstOrDefault(user => user.Id == ownerUserId.Value);
            if (lecturer is null)
            {
                throw new InvalidOperationException("Lecturer owner not found.");
            }

            return new SubjectOwnerInfo(lecturer.Id, lecturer.FullName, lecturer.Email);
        }

        protected DocumentUploaderInfo BuildDocumentUploaderInfo()
        {
            return new DocumentUploaderInfo(
                CurrentUserId(),
                User.FindFirstValue(ClaimTypes.Name),
                User.FindFirstValue(ClaimTypes.Email));
        }

        protected ChatSessionOwnerInfo BuildChatSessionOwnerInfo()
        {
            return new ChatSessionOwnerInfo(
                CurrentUserId(),
                User.FindFirstValue(ClaimTypes.Name),
                User.FindFirstValue(ClaimTypes.Email));
        }

        protected async Task<UserAccount?> GetCurrentUserAccountAsync(CancellationToken cancellationToken)
        {
            if (CurrentUserId() is not { } userId)
            {
                return null;
            }

            return (await _users.GetAllAsync(cancellationToken))
                .FirstOrDefault(user => user.Id == userId);
        }

        protected IReadOnlyList<CourseSubject> FilterCourseCatalogForCurrentUser(IReadOnlyList<CourseSubject> catalog)
        {
            if (IsAdmin())
            {
                return catalog;
            }

            if (!IsLecturer() || CurrentUserId() is not { } userId)
            {
                return Array.Empty<CourseSubject>();
            }

            return catalog
                .Where(subject => subject.OwnerUserId == userId)
                .ToList();
        }

        protected IReadOnlyList<IndexedDocument> FilterDocumentsForCurrentUser(
            IReadOnlyList<IndexedDocument> documents,
            IReadOnlyList<CourseSubject> catalog,
            UserAccount? currentUser)
        {
            if (IsAdmin())
            {
                return documents;
            }

            if (currentUser is null)
            {
                return Array.Empty<IndexedDocument>();
            }

            if (IsLecturer())
            {
                return documents
                    .Where(DocumentBelongsToCurrentUser)
                    .ToList();
            }

            return Array.Empty<IndexedDocument>();
        }

        protected async Task<bool> CanManageSubjectAsync(Guid subjectId, CancellationToken cancellationToken)
        {
            if (IsAdmin())
            {
                return true;
            }

            if (!IsLecturer() || CurrentUserId() is not { } userId)
            {
                return false;
            }

            var subject = (await _repository.GetCourseCatalogAsync(cancellationToken))
                .FirstOrDefault(item => item.Id == subjectId);
            return subject?.OwnerUserId == userId;
        }

        protected async Task<bool> CanManageSubjectAsync(string subjectText, CancellationToken cancellationToken)
        {
            if (IsAdmin())
            {
                return true;
            }

            if (!IsLecturer() || CurrentUserId() is not { } userId)
            {
                return false;
            }

            var subject = FindSubjectForDocumentSubject(await _repository.GetCourseCatalogAsync(cancellationToken), subjectText);
            return subject?.OwnerUserId == userId;
        }

        protected async Task<bool> CanManageDocumentAsync(IndexedDocument document, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return IsAdmin() || (IsLecturer() && DocumentBelongsToCurrentUser(document));
        }

        protected async Task<bool> CanViewDocumentAsync(IndexedDocument document, CancellationToken cancellationToken)
        {
            return await CanManageDocumentAsync(document, cancellationToken);
        }

        protected bool DocumentBelongsToCurrentUser(IndexedDocument document)
        {
            if (CurrentUserId() is { } userId && document.UploadedByUserId == userId)
            {
                return true;
            }

            var currentEmail = User.FindFirstValue(ClaimTypes.Email);
            return !document.UploadedByUserId.HasValue
                   && !string.IsNullOrWhiteSpace(currentEmail)
                   && document.UploadedByEmail.Equals(currentEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        protected static bool IsDataAccessTimeout(Exception exception)
        {
            return exception is TaskCanceledException or TimeoutException
                   || exception.InnerException is not null && IsDataAccessTimeout(exception.InnerException)
                   || exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                   || exception.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                   || exception.GetType().FullName?.Contains("SqlException", StringComparison.OrdinalIgnoreCase) == true;
        }

        protected async Task<bool> CanManageChapterAsync(Guid chapterId, CancellationToken cancellationToken)
        {
            if (IsAdmin())
            {
                return true;
            }

            if (!IsLecturer() || CurrentUserId() is not { } userId)
            {
                return false;
            }

            var subject = (await _repository.GetCourseCatalogAsync(cancellationToken))
                .FirstOrDefault(item => item.Chapters.Any(chapter => chapter.Id == chapterId));
            return subject?.OwnerUserId == userId;
        }

        protected async Task<(string Name, string Email)> ResolveSubjectOwnerAsync(string subjectText, CancellationToken cancellationToken)
        {
            var subject = FindSubjectForDocumentSubject(await _repository.GetCourseCatalogAsync(cancellationToken), subjectText);
            return subject is null
                ? (string.Empty, string.Empty)
                : (subject.OwnerName, subject.OwnerEmail);
        }

        protected static CourseSubject? FindSubjectForDocumentSubject(IEnumerable<CourseSubject> catalog, string subjectText)
        {
            var normalizedSubject = (subjectText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedSubject))
            {
                return null;
            }

            var parsed = ParseSubjectForCatalog(normalizedSubject);
            return catalog.FirstOrDefault(subject =>
                subject.DisplayName.Equals(normalizedSubject, StringComparison.OrdinalIgnoreCase)
                || subject.Code.Equals(normalizedSubject, StringComparison.OrdinalIgnoreCase)
                || subject.Name.Equals(normalizedSubject, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(parsed.Code)
                    && subject.Code.Equals(parsed.Code, StringComparison.OrdinalIgnoreCase)));
        }

        protected static IReadOnlyList<CourseSubject> BuildSynchronizedCourseCatalogForView(
            IEnumerable<CourseSubject> catalog,
            IEnumerable<IndexedDocument> documents)
        {
            var synchronized = catalog
                .Select(CloneCourseSubject)
                .ToList();

            foreach (var document in documents.Where(item => !string.IsNullOrWhiteSpace(item.Subject)))
            {
                var parsed = ParseSubjectForCatalog(document.Subject);
                if (string.IsNullOrWhiteSpace(parsed.Code))
                {
                    continue;
                }

                var subject = synchronized.FirstOrDefault(item =>
                    item.Code.Equals(parsed.Code, StringComparison.OrdinalIgnoreCase)
                    || item.DisplayName.Equals(document.Subject.Trim(), StringComparison.OrdinalIgnoreCase));
                if (subject is null)
                {
                    subject = new CourseSubject
                    {
                        Id = CreateStableCatalogId(parsed.Code),
                        Code = parsed.Code,
                        Name = string.IsNullOrWhiteSpace(parsed.Name) ? parsed.Code : parsed.Name,
                        Description = "Tự đồng bộ từ tài liệu đã index.",
                        CreatedAt = document.UploadedAt
                    };
                    synchronized.Add(subject);
                }
                else if (string.IsNullOrWhiteSpace(subject.Name)
                         || subject.Name.Equals(subject.Code, StringComparison.OrdinalIgnoreCase))
                {
                    subject.Name = string.IsNullOrWhiteSpace(parsed.Name) ? parsed.Code : parsed.Name;
                }

                var chapterTitle = document.Chapter.Trim();
                if (string.IsNullOrWhiteSpace(chapterTitle)
                    || subject.Chapters.Any(item => item.Title.Equals(chapterTitle, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var nextSortOrder = subject.Chapters.Count == 0
                    ? 1
                    : subject.Chapters.Max(item => item.SortOrder) + 1;
                subject.Chapters.Add(new CourseChapter
                {
                    Id = CreateStableCatalogId($"{subject.Code}:{chapterTitle}"),
                    SubjectId = subject.Id,
                    SubjectCode = subject.Code,
                    SubjectName = subject.Name,
                    Title = chapterTitle,
                    SortOrder = nextSortOrder
                });
            }

            return synchronized
                .OrderBy(item => item.Code)
                .ToList();
        }

        protected static CourseSubject CloneCourseSubject(CourseSubject subject)
        {
            return new CourseSubject
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name,
                Description = subject.Description,
                CreatedAt = subject.CreatedAt,
                OwnerUserId = subject.OwnerUserId,
                OwnerName = subject.OwnerName,
                OwnerEmail = subject.OwnerEmail,
                Chapters = subject.Chapters
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Title)
                    .Select(chapter => new CourseChapter
                    {
                        Id = chapter.Id,
                        SubjectId = chapter.SubjectId,
                        SubjectCode = chapter.SubjectCode,
                        SubjectName = chapter.SubjectName,
                        Title = chapter.Title,
                        SortOrder = chapter.SortOrder
                    })
                    .ToList()
            };
        }

        protected static Guid CreateStableCatalogId(string value)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant()));
            return new Guid(bytes[..16]);
        }

        protected static string ToVietnameseUploadError(string message)
        {
            if (message.Contains("Only PDF, DOCX, PPTX, and TXT files", StringComparison.OrdinalIgnoreCase))
            {
                return "Chỉ hỗ trợ file PDF, DOCX, PPTX và TXT.";
            }

            if (message.Contains("selected file is empty", StringComparison.OrdinalIgnoreCase))
            {
                return "File đã chọn đang trống nên không thể index.";
            }

            if (message.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                return "Tài liệu này đã tồn tại trong kho.";
            }

            return string.IsNullOrWhiteSpace(message) ? "Không thể xử lý tài liệu." : message;
        }

        protected async Task SyncCourseCatalogFromDocumentsAsync(
            IReadOnlyList<IndexedDocument> documents,
            CancellationToken cancellationToken)
        {
            foreach (var document in documents.Where(item => !string.IsNullOrWhiteSpace(item.Subject)))
            {
                try
                {
                    await SyncCourseCatalogFromDocumentAsync(document, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Could not sync course catalog from document {DocumentId}", document.Id);
                }
            }
        }

        protected async Task SyncCourseCatalogFromDocumentAsync(IndexedDocument document, CancellationToken cancellationToken)
        {
            var parsed = ParseSubjectForCatalog(document.Subject);
            if (string.IsNullOrWhiteSpace(parsed.Code))
            {
                return;
            }

            var catalog = await _repository.GetCourseCatalogAsync(cancellationToken);
            var subject = catalog.FirstOrDefault(item =>
                item.Code.Equals(parsed.Code, StringComparison.OrdinalIgnoreCase)
                || item.DisplayName.Equals(document.Subject.Trim(), StringComparison.OrdinalIgnoreCase));

            if (subject is null)
            {
                subject = await _repository.UpsertSubjectAsync(
                    subjectId: null,
                    code: parsed.Code,
                    name: parsed.Name,
                    description: "Tự đồng bộ từ tài liệu đã index.",
                    cancellationToken);
            }
            else if (string.IsNullOrWhiteSpace(subject.Name)
                     || subject.Name.Equals(subject.Code, StringComparison.OrdinalIgnoreCase))
            {
                subject = await _repository.UpsertSubjectAsync(
                    subject.Id,
                    subject.Code,
                    parsed.Name,
                    subject.Description,
                    cancellationToken);
            }

            var chapterTitle = document.Chapter.Trim();
            if (string.IsNullOrWhiteSpace(chapterTitle)
                || subject.Chapters.Any(item => item.Title.Equals(chapterTitle, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var nextSortOrder = subject.Chapters.Count == 0
                ? 1
                : subject.Chapters.Max(item => item.SortOrder) + 1;
            await _repository.UpsertChapterAsync(
                chapterId: null,
                subject.Id,
                chapterTitle,
                nextSortOrder,
                cancellationToken);
        }

        protected static (string Code, string Name) ParseSubjectForCatalog(string subject)
        {
            var trimmed = subject.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return (string.Empty, string.Empty);
            }

            var separatorIndex = trimmed.IndexOf(" - ", StringComparison.Ordinal);
            var separatorLength = 3;
            if (separatorIndex < 0)
            {
                separatorIndex = trimmed.IndexOf('-', StringComparison.Ordinal);
                separatorLength = 1;
            }

            if (separatorIndex > 0)
            {
                var codeCandidate = NormalizeCatalogCode(trimmed[..separatorIndex]);
                var nameCandidate = trimmed[(separatorIndex + separatorLength)..].Trim();
                if (!string.IsNullOrWhiteSpace(codeCandidate) && !string.IsNullOrWhiteSpace(nameCandidate))
                {
                    return (codeCandidate, nameCandidate);
                }
            }

            var firstToken = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? trimmed;
            var code = NormalizeCatalogCode(firstToken);
            return string.IsNullOrWhiteSpace(code)
                ? (string.Empty, string.Empty)
                : (code, trimmed);
        }

        protected static bool SubjectMatchesFilter(string documentSubject, string subjectFilter)
        {
            var normalizedDocumentSubject = (documentSubject ?? string.Empty).Trim();
            var normalizedSubjectFilter = (subjectFilter ?? string.Empty).Trim();
            if (normalizedDocumentSubject.Equals(normalizedSubjectFilter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var documentCode = ParseSubjectForCatalog(normalizedDocumentSubject).Code;
            var filterCode = ParseSubjectForCatalog(normalizedSubjectFilter).Code;
            return !string.IsNullOrWhiteSpace(documentCode)
                && documentCode.Equals(filterCode, StringComparison.OrdinalIgnoreCase);
        }

        protected static string NormalizeCatalogCode(string code)
        {
            return new string((code ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Where(character => char.IsLetterOrDigit(character) || character is '_' or '.')
                .Take(32)
                .ToArray());
        }

        protected static string ToVietnameseCatalogError(string message)
        {
            if (message.Contains("Subject code is required", StringComparison.OrdinalIgnoreCase))
            {
                return "Mã môn học là bắt buộc.";
            }

            if (message.Contains("Subject code already exists", StringComparison.OrdinalIgnoreCase))
            {
                return "Mã môn học đã tồn tại.";
            }

            if (message.Contains("Lecturer owner not found", StringComparison.OrdinalIgnoreCase))
            {
                return "Không tìm thấy giảng viên phụ trách hợp lệ.";
            }

            if (message.Contains("Chapter title is required", StringComparison.OrdinalIgnoreCase))
            {
                return "Tên chương là bắt buộc.";
            }

            if (message.Contains("Chapter already exists", StringComparison.OrdinalIgnoreCase))
            {
                return "Chương này đã tồn tại trong môn học.";
            }

            return string.IsNullOrWhiteSpace(message) ? "Không thể lưu danh mục môn/chương." : message;
        }

        protected static string ToVietnameseDocumentError(string message)
        {
            if (message.Contains("Document not found", StringComparison.OrdinalIgnoreCase))
            {
                return "Không tìm thấy tài liệu.";
            }

            if (message.Contains("File name is required", StringComparison.OrdinalIgnoreCase))
            {
                return "Tên file là bắt buộc.";
            }

            if (message.Contains("Subject is required", StringComparison.OrdinalIgnoreCase))
            {
                return "Subject là bắt buộc.";
            }

            if (message.Contains("Chapter is required", StringComparison.OrdinalIgnoreCase))
            {
                return "Chapter là bắt buộc.";
            }

            return string.IsNullOrWhiteSpace(message) ? "Không thể cập nhật tài liệu." : message;
        }

        protected string GetUploadsRoot()
        {
            return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "uploads"));
        }

        protected void TryDeleteStoredFile(IndexedDocument document)
        {
            try
            {
                var storedPath = Path.GetFullPath(document.StoredPath);
                if (IsPathUnderDirectory(storedPath, GetUploadsRoot()) && System.IO.File.Exists(storedPath))
                {
                    System.IO.File.Delete(storedPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete stored file for document {DocumentId}", document.Id);
            }
        }

        protected static bool IsPathUnderDirectory(string path, string directory)
        {
            var fullPath = Path.GetFullPath(path);
            var fullDirectory = Path.GetFullPath(directory);
            if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar))
            {
                fullDirectory += Path.DirectorySeparatorChar;
            }

            return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
        }

        protected static string ResolveContentType(IndexedDocument document)
        {
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            if (extension == ".txt")
            {
                return "text/plain; charset=utf-8";
            }

            if (!string.IsNullOrWhiteSpace(document.ContentType) && document.ContentType != "application/octet-stream")
            {
                if (document.ContentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase)
                    || document.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    return "text/plain; charset=utf-8";
                }

                return document.ContentType;
            }

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain; charset=utf-8",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };
        }

        protected static bool IsTextDocument(IndexedDocument document)
        {
            var extension = Path.GetExtension(document.FileName);
            return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                   || document.ContentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase)
                   || document.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
        }

        protected static string GetSessionTitle(ChatSession session)
        {
            if (!string.IsNullOrWhiteSpace(session.Title))
            {
                return session.Title.Trim();
            }

            var firstQuestion = session.Messages
                .FirstOrDefault(message => message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                ?.Content
                .Trim();

            if (string.IsNullOrWhiteSpace(firstQuestion))
            {
                return "Phiên chưa có câu hỏi";
            }

            return firstQuestion.Length <= 56 ? firstQuestion : $"{firstQuestion[..56]}...";
        }

}
