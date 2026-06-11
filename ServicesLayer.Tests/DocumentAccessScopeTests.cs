using DataAccessLayer;

namespace ServicesLayer.Tests;

public sealed class DocumentAccessScopeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "assignment2-access-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DocumentUiScope_StudentCannotListDocuments()
    {
        var repository = CreateRepository();
        await SeedDocumentsAsync(repository);

        var documents = await repository.GetDocumentsAsync(
            new DocumentAccessScope("Student", Guid.NewGuid(), "student@example.com", DocumentAccessMode.DocumentUi));

        Assert.Empty(documents);
    }

    [Fact]
    public async Task DocumentUiScope_LecturerSeesOnlyOwnUploadedDocuments()
    {
        var repository = CreateRepository();
        var lecturerA = Guid.NewGuid();
        var lecturerB = Guid.NewGuid();
        await SeedDocumentsAsync(repository, lecturerA, lecturerB);

        var documents = await repository.GetDocumentsAsync(
            new DocumentAccessScope("Lecturer", lecturerA, "lecturer-a@example.com", DocumentAccessMode.DocumentUi));

        Assert.Single(documents);
        Assert.Equal("lecturer-a.txt", documents[0].FileName);
    }

    [Fact]
    public async Task DocumentUiScope_AdminSeesAllDocuments()
    {
        var repository = CreateRepository();
        await SeedDocumentsAsync(repository);

        var documents = await repository.GetDocumentsAsync(
            new DocumentAccessScope("Admin", Guid.NewGuid(), "admin@example.com", DocumentAccessMode.DocumentUi));

        Assert.Equal(2, documents.Count);
    }

    [Fact]
    public async Task ChatScope_StudentCanReadIndexedSubjectsWithoutDocumentUi()
    {
        var repository = CreateRepository();
        await SeedDocumentsAsync(repository);

        var subjects = await repository.GetIndexedSubjectsAsync(
            new DocumentAccessScope("Student", Guid.NewGuid(), "student@example.com", DocumentAccessMode.Chat));

        Assert.Equal(new[] { "DBA103", "PRU" }, subjects.OrderBy(subject => subject).ToArray());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private JsonKnowledgeRepository CreateRepository()
    {
        Directory.CreateDirectory(_root);
        return new JsonKnowledgeRepository(Path.Combine(_root, "store.json"));
    }

    private static async Task SeedDocumentsAsync(JsonKnowledgeRepository repository)
    {
        await SeedDocumentsAsync(repository, Guid.NewGuid(), Guid.NewGuid());
    }

    private static async Task SeedDocumentsAsync(JsonKnowledgeRepository repository, Guid lecturerA, Guid lecturerB)
    {
        await repository.AddDocumentAsync(
            new IndexedDocument
            {
                Id = Guid.NewGuid(),
                FileName = "lecturer-a.txt",
                Subject = "DBA103",
                Chapter = "Intro",
                UploadedByUserId = lecturerA,
                UploadedByEmail = "lecturer-a@example.com",
                Status = DocumentIndexStatus.Indexed,
                ChunkCount = 1
            },
            Array.Empty<DocumentChunk>());
        await repository.AddDocumentAsync(
            new IndexedDocument
            {
                Id = Guid.NewGuid(),
                FileName = "lecturer-b.txt",
                Subject = "PRU",
                Chapter = "Intro",
                UploadedByUserId = lecturerB,
                UploadedByEmail = "lecturer-b@example.com",
                Status = DocumentIndexStatus.Indexed,
                ChunkCount = 1
            },
            Array.Empty<DocumentChunk>());
    }
}
