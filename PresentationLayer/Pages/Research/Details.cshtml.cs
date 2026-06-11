using System.Collections.Concurrent;
using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;

namespace PresentationLayer.Pages.Research;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class DetailsModel : PageModel
{
    private static readonly ConcurrentDictionary<Guid, byte> RunningBenchmarks = new();

    private readonly IResearchBenchmarkService _researchService;
    private readonly IResearchReportPdfService _reportPdfService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IResearchBenchmarkService researchService,
        IResearchReportPdfService reportPdfService,
        IServiceScopeFactory scopeFactory,
        ILogger<DetailsModel> logger)
    {
        _researchService = researchService;
        _reportPdfService = reportPdfService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ResearchExperimentDetail Experiment { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await _researchService.GetExperimentAsync(id, cancellationToken);
        if (experiment is null)
        {
            return NotFound();
        }

        Experiment = experiment;
        return Page();
    }

    public async Task<IActionResult> OnGetReportPdfAsync(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await _researchService.GetExperimentAsync(id, cancellationToken);
        if (experiment is null)
        {
            return NotFound();
        }

        var bytes = _reportPdfService.BuildReport(experiment);
        var fileName = $"rbl-report-{SanitizeFileName(experiment.Name)}-{DateTime.UtcNow:yyyyMMddHHmm}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    public async Task<IActionResult> OnPostRunAsync(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await _researchService.GetExperimentAsync(id, cancellationToken);
        if (experiment is null)
        {
            return NotFound();
        }

        if (!RunningBenchmarks.TryAdd(id, 0))
        {
            TempData["Success"] = "Benchmark is already running. You can leave this page and come back later.";
            return RedirectToPage("/Research/Details", new { id });
        }

        QueueBenchmark(id);
        TempData["Success"] = "Benchmark started in background. You can leave this page and continue working.";
        return RedirectToPage("/Research/Details", new { id });
    }

    private void QueueBenchmark(Guid experimentId)
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IResearchBenchmarkService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DetailsModel>>();

            try
            {
                await service.RunExperimentAsync(experimentId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background research benchmark failed for experiment {ExperimentId}", experimentId);
            }
            finally
            {
                RunningBenchmarks.TryRemove(experimentId, out _);
            }
        });
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
        cleaned = string.Join("-", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(cleaned) ? "experiment" : cleaned.ToLowerInvariant();
    }
}
