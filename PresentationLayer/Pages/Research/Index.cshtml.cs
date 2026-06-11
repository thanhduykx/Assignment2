using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Security;
using ServicesLayer;

namespace PresentationLayer.Pages.Research;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class IndexModel : PageModel
{
    private readonly IResearchBenchmarkService _researchService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IResearchBenchmarkService researchService, ILogger<IndexModel> logger)
    {
        _researchService = researchService;
        _logger = logger;
    }

    public IReadOnlyList<DataAccessLayer.ResearchExperimentSummary> Experiments { get; private set; } = Array.Empty<DataAccessLayer.ResearchExperimentSummary>();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Experiments = await _researchService.GetExperimentsAsync(cancellationToken);
            return Page();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Research index request was canceled by the client.");
            return new EmptyResult();
        }
    }
}
