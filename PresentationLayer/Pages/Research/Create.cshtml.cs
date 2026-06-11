using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PresentationLayer.Models;
using PresentationLayer.Security;
using ServicesLayer;

namespace PresentationLayer.Pages.Research;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class CreateModel : PageModel
{
    private readonly IResearchBenchmarkService _researchService;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IResearchBenchmarkService researchService,
        IKnowledgeRepository knowledgeRepository,
        ILogger<CreateModel> logger)
    {
        _researchService = researchService;
        _knowledgeRepository = knowledgeRepository;
        _logger = logger;
    }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    public string QuestionsText { get; set; } = string.Empty;

    [BindProperty]
    public List<Guid> EmbeddingModelIds { get; set; } = new();

    [BindProperty]
    public List<Guid> ChunkingStrategyIds { get; set; } = new();

    [BindProperty]
    public bool UseLocalFineTunedBaseline { get; set; } = true;

    [BindProperty]
    public string? FineTunedTrainingText { get; set; }

    [BindProperty]
    public string? FineTunedModelName { get; set; }

    [BindProperty]
    public string? FineTunedEndpoint { get; set; }

    public IReadOnlyList<SelectListItem> SubjectOptions { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> EmbeddingModels { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> ChunkingStrategies { get; private set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<string> CatalogLoadErrors => _catalogLoadErrors;
    public bool CanCreateExperiment => EmbeddingModels.Count > 0 && ChunkingStrategies.Count > 0;

    private readonly List<string> _catalogLoadErrors = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await BuildCreateViewModelAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Experiment name is required.");
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            ModelState.AddModelError(nameof(Subject), "Subject is required.");
        }

        var questions = ParseQuestions(QuestionsText);
        if (questions.Count == 0)
        {
            ModelState.AddModelError(nameof(QuestionsText), "Add at least one question in the format: question | expected answer.");
        }

        if (EmbeddingModelIds.Count == 0)
        {
            ModelState.AddModelError(nameof(EmbeddingModelIds), "Choose at least one embedding model before creating an experiment.");
        }

        if (ChunkingStrategyIds.Count == 0)
        {
            ModelState.AddModelError(nameof(ChunkingStrategyIds), "Choose at least one chunking strategy.");
        }

        if (!ModelState.IsValid)
        {
            await BuildCreateViewModelAsync(cancellationToken);
            return Page();
        }

        try
        {
            var trainingExamples = ParseQuestions(FineTunedTrainingText);
            var experimentId = await _researchService.CreateExperimentAsync(new CreateResearchExperimentRequest
            {
                Name = Name,
                Subject = Subject,
                EmbeddingModelIds = EmbeddingModelIds,
                ChunkingStrategyIds = ChunkingStrategyIds,
                Questions = questions,
                UseLocalFineTunedBaseline = UseLocalFineTunedBaseline,
                FineTunedTrainingExamples = trainingExamples,
                FineTunedModelName = FineTunedModelName,
                FineTunedEndpoint = FineTunedEndpoint
            }, cancellationToken);

            return RedirectToPage("/Research/Details", new { id = experimentId });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create research experiment");
            ModelState.AddModelError(string.Empty, GetSafeErrorMessage(ex));
            await BuildCreateViewModelAsync(cancellationToken);
            return Page();
        }
    }

    private async Task BuildCreateViewModelAsync(CancellationToken cancellationToken)
    {
        var catalog = await LoadResearchCatalogAsync(cancellationToken);
        var subjectOptions = await LoadSubjectOptionValuesAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(Subject) && subjectOptions.Count > 0)
        {
            Subject = subjectOptions[0];
        }

        if (EmbeddingModelIds.Count == 0)
        {
            EmbeddingModelIds = catalog.EmbeddingModels
                .Where(item => item.ModelId?.Equals("vinai/phobert-base", StringComparison.OrdinalIgnoreCase) == true
                    || item.Name.Equals("vinai/phobert-base", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Id)
                .ToList();
            if (EmbeddingModelIds.Count == 0)
            {
                EmbeddingModelIds = catalog.EmbeddingModels.Select(item => item.Id).Take(1).ToList();
            }
        }

        if (ChunkingStrategyIds.Count == 0)
        {
            ChunkingStrategyIds = catalog.ChunkingStrategies.Select(item => item.Id).ToList();
        }

        EmbeddingModels = catalog.EmbeddingModels.Select(item => new SelectListItem
        {
            Value = item.Id.ToString(),
            Text = $"{item.Name} ({item.Provider})",
            Selected = EmbeddingModelIds.Contains(item.Id)
        }).ToList();
        ChunkingStrategies = catalog.ChunkingStrategies.Select(item => new SelectListItem
        {
            Value = item.Id.ToString(),
            Text = $"{item.Name} - {item.Provider} {item.ModelId}",
            Selected = ChunkingStrategyIds.Contains(item.Id)
        }).ToList();
        SubjectOptions = subjectOptions.Select(subject => new SelectListItem
        {
            Value = subject,
            Text = subject,
            Selected = subject.Equals(Subject, StringComparison.OrdinalIgnoreCase)
        }).ToList();
    }

    private async Task<ResearchCatalog> LoadResearchCatalogAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _researchService.GetCatalogAsync(cancellationToken);
        }
        catch (Exception ex) when (IsRecoverableDataAccessFailure(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Could not load research catalog");
            AddCatalogLoadError("Không tải được cấu hình RBL từ SQL Server. Kiểm tra SQL Server, database và connection string rồi tải lại trang.");
            return new ResearchCatalog(Array.Empty<ResearchOption>(), Array.Empty<ResearchOption>());
        }
    }

    private async Task<IReadOnlyList<string>> LoadSubjectOptionValuesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await BuildSubjectOptionValuesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsRecoverableDataAccessFailure(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Could not load subject catalog");
            AddCatalogLoadError("Không tải được danh sách môn học từ SQL Server. Có thể nhập subject thủ công nếu cấu hình RBL vẫn tải được.");
            return Array.Empty<string>();
        }
    }

    private async Task<IReadOnlyList<string>> BuildSubjectOptionValuesAsync(CancellationToken cancellationToken)
    {
        var courseCatalog = await _knowledgeRepository.GetCourseCatalogAsync(cancellationToken);
        var documents = await _knowledgeRepository.GetDocumentsAsync(cancellationToken);

        return courseCatalog
            .Select(subject => subject.DisplayName)
            .Concat(documents.Select(document => document.Subject))
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(subject => subject)
            .ToList();
    }

    private static IReadOnlyList<ResearchQuestionInput> ParseQuestions(string? questionsText)
    {
        if (string.IsNullOrWhiteSpace(questionsText))
        {
            return Array.Empty<ResearchQuestionInput>();
        }

        return questionsText
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('|', 2, StringSplitOptions.TrimEntries))
            .Where(parts => !string.IsNullOrWhiteSpace(parts[0]))
            .Select(parts => new ResearchQuestionInput
            {
                Question = parts[0],
                GroundTruth = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : parts[0],
                Difficulty = "Medium"
            })
            .ToList();
    }

    private void AddCatalogLoadError(string message)
    {
        if (!_catalogLoadErrors.Contains(message, StringComparer.Ordinal))
        {
            _catalogLoadErrors.Add(message);
        }
    }

    private static string GetSafeErrorMessage(Exception exception)
    {
        return IsRecoverableDataAccessFailure(exception, CancellationToken.None)
            ? "Không tạo được thực nghiệm vì không kết nối được SQL Server. Kiểm tra SQL Server, database và connection string rồi thử lại."
            : exception.Message;
    }

    private static bool IsRecoverableDataAccessFailure(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException or TimeoutException)
            {
                return true;
            }

            var typeName = current.GetType().FullName ?? current.GetType().Name;
            if (typeName.Contains("SqlException", StringComparison.Ordinal)
                || typeName.Contains("DbException", StringComparison.Ordinal)
                || typeName.Contains("DbUpdateException", StringComparison.Ordinal))
            {
                return true;
            }

            if (current is InvalidOperationException
                && current.Message.Contains("DefaultConnection", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
