using System.Security.Claims;
using DataAccessLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;

namespace PresentationLayer.Pages.Admin;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class IndexModel : PageModel
{
    private readonly IUserAccountStore _users;
    private readonly IKnowledgeRepository _knowledge;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IUserAccountStore users, IKnowledgeRepository knowledge, ILogger<IndexModel> logger)
    {
        _users = users;
        _knowledge = knowledge;
        _logger = logger;
    }

    public IReadOnlyList<AdminUserRowViewModel> Users { get; private set; } = Array.Empty<AdminUserRowViewModel>();
    public IReadOnlyList<string> Roles { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<AdminSubjectOptionViewModel> SubjectOptions { get; private set; } = Array.Empty<AdminSubjectOptionViewModel>();
    public string? Query { get; private set; }
    public string? RoleFilter { get; private set; }
    public int TotalUserCount { get; private set; }
    public int AdminUserCount { get; private set; }
    public int LecturerUserCount { get; private set; }
    public int StudentUserCount { get; private set; }
    public int SubjectCount { get; private set; }
    public int AssignedSubjectCount { get; private set; }

    public async Task OnGetAsync(string? q, string? roleFilter, CancellationToken cancellationToken)
    {
        await LoadAsync(q, roleFilter, cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateUserAsync([FromForm] CreateAdminUserViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelError("Could not create this user.");
            return RedirectToPage("/Admin/Index");
        }

        try
        {
            var user = await _users.CreateLocalForAdminAsync(
                model.FullName,
                model.Email,
                model.Password,
                model.Role,
                cancellationToken);
            TempData["Success"] = $"Created {user.Email} as {user.Role}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create user {Email}", model.Email);
            TempData["Error"] = "Could not create this user.";
        }

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostCreateSubjectAsync([FromForm] CreateAdminSubjectViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelError("Could not create this subject.");
            return RedirectToPage("/Admin/Index");
        }

        try
        {
            var subject = await _knowledge.UpsertSubjectAsync(
                subjectId: null,
                code: model.Code,
                name: model.Name,
                description: model.Description,
                cancellationToken: cancellationToken);

            TempData["Success"] = $"Created subject {subject.DisplayName}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create subject {Code}", model.Code);
            TempData["Error"] = "Could not create this subject.";
        }

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostUpdateNameAsync([FromForm] UpdateUserNameViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelError("Could not update this user name.");
            return RedirectToPage("/Admin/Index");
        }

        try
        {
            var user = await _users.UpdateFullNameAsync(model.UserId, model.FullName, cancellationToken);
            TempData["Success"] = $"Updated {user.Email}'s name.";

            if (IsCurrentUser(user.Id))
            {
                await RefreshCurrentUserClaimsAsync(user);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update user name for {UserId}", model.UserId);
            TempData["Error"] = "Could not update this user name.";
        }

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostUpdateRoleAsync([FromForm] UpdateUserRoleViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _users.UpdateRoleAsync(model.UserId, model.Role, cancellationToken);
            TempData["Success"] = $"Updated {user.Email} to {user.Role}.";

            if (IsCurrentUser(user.Id) && user.Role != AppRoles.Admin)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Account/Login");
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update user role for {UserId}", model.UserId);
            TempData["Error"] = "Could not update this user role.";
        }

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostAssignLecturerSubjectAsync([FromForm] AssignLecturerSubjectViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var user = (await _users.GetAllAsync(cancellationToken))
                .FirstOrDefault(item => item.Id == model.UserId)
                ?? throw new InvalidOperationException("User not found.");
            if (user.Role != AppRoles.Lecturer)
            {
                throw new InvalidOperationException("Only lecturers can be assigned to subjects.");
            }

            var subject = (await _knowledge.GetCourseCatalogAsync(cancellationToken))
                .FirstOrDefault(item => item.Id == model.SubjectId)
                ?? throw new InvalidOperationException("Subject not found.");

            await _knowledge.UpsertSubjectAsync(
                subject.Id,
                subject.Code,
                subject.Name,
                subject.Description,
                cancellationToken,
                new SubjectOwnerInfo(user.Id, user.FullName, user.Email));

            TempData["Success"] = $"Assigned {subject.DisplayName} to {user.FullName}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not assign subject {SubjectId} to lecturer {UserId}", model.SubjectId, model.UserId);
            TempData["Error"] = "Could not assign this subject.";
        }

        return RedirectToPage("/Admin/Index");
    }

    private async Task LoadAsync(string? q, string? roleFilter, CancellationToken cancellationToken)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        var subjects = await _knowledge.GetCourseCatalogAsync(cancellationToken);
        var normalizedQuery = q?.Trim();
        var normalizedRoleFilter = roleFilter?.Trim();
        var adminCount = users.Count(user => user.Role == AppRoles.Admin);
        var assignedSubjectsByUser = subjects
            .Where(subject => subject.OwnerUserId.HasValue)
            .GroupBy(subject => subject.OwnerUserId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .OrderBy(subject => subject.Code)
                    .ThenBy(subject => subject.Name)
                    .Select(subject => subject.DisplayName)
                    .ToList());

        Roles = AppRoles.All;
        SubjectOptions = subjects
            .OrderBy(subject => subject.Code)
            .ThenBy(subject => subject.Name)
            .Select(subject => new AdminSubjectOptionViewModel
            {
                Id = subject.Id,
                DisplayName = subject.DisplayName,
                OwnerUserId = subject.OwnerUserId,
                OwnerName = subject.OwnerName,
                OwnerEmail = subject.OwnerEmail
            })
            .ToList();
        var userRows = users.Select(user => new AdminUserRowViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Provider = user.Provider,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                IsLastAdmin = user.Role == AppRoles.Admin && adminCount <= 1,
                AssignedSubjects = ResolveAssignedSubjectLabels(user, subjects, assignedSubjectsByUser)
            })
            .ToList();

        Query = normalizedQuery;
        RoleFilter = normalizedRoleFilter;
        TotalUserCount = userRows.Count;
        AdminUserCount = userRows.Count(user => user.Role == AppRoles.Admin);
        LecturerUserCount = userRows.Count(user => user.Role == AppRoles.Lecturer);
        StudentUserCount = userRows.Count(user => user.Role == AppRoles.Student);
        SubjectCount = subjects.Count;
        AssignedSubjectCount = subjects.Count(subject => subject.OwnerUserId.HasValue);

        Users = userRows
            .Where(user => string.IsNullOrWhiteSpace(normalizedQuery) || UserMatchesQuery(user, normalizedQuery))
            .Where(user => string.IsNullOrWhiteSpace(normalizedRoleFilter) || user.Role.Equals(normalizedRoleFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<string> ResolveAssignedSubjectLabels(
        UserAccount user,
        IReadOnlyList<CourseSubject> subjects,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> lecturerSubjectsByUser)
    {
        if (user.Role == AppRoles.Lecturer)
        {
            return lecturerSubjectsByUser.TryGetValue(user.Id, out var assignedSubjects)
                ? assignedSubjects
                : Array.Empty<string>();
        }

        if (user.Role != AppRoles.Student)
        {
            return Array.Empty<string>();
        }

        return new[] { "All indexed documents" };
    }

    private static bool UserMatchesQuery(AdminUserRowViewModel user, string query)
    {
        return Contains(user.FullName, query)
               || Contains(user.Email, query)
               || Contains(user.Provider, query)
               || Contains(user.Role, query)
               || user.AssignedSubjects.Any(subject => Contains(subject, query));
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCurrentUser(Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)
               && currentUserId == userId;
    }

    private async Task RefreshCurrentUserClaimsAsync(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, AppRoles.Normalize(user.Role))
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }

    private string FirstModelError(string fallback)
    {
        return ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
            ?? fallback;
    }

    private static string ToAdminUserError(string message)
    {
        if (message.Contains("Role is invalid", StringComparison.OrdinalIgnoreCase))
        {
            return "Role is invalid.";
        }

        if (message.Contains("User not found", StringComparison.OrdinalIgnoreCase))
        {
            return "User not found.";
        }

        if (message.Contains("Subject not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Subject not found.";
        }

        if (message.Contains("Subject code is required", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Subject code already exists", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Subject name", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Description", StringComparison.OrdinalIgnoreCase))
        {
            return message;
        }

        if (message.Contains("Only lecturers", StringComparison.OrdinalIgnoreCase))
        {
            return "Only lecturers can be assigned to subjects.";
        }

        if (message.Contains("Only students", StringComparison.OrdinalIgnoreCase))
        {
            return "Only students can be granted subject access.";
        }

        if (message.Contains("last admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot demote the last admin.";
        }

        if (message.Contains("seed admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot demote the seed admin.";
        }

        if (message.Contains("email is already registered", StringComparison.OrdinalIgnoreCase))
        {
            return "This email is already registered.";
        }

        if (message.Contains("Full name", StringComparison.OrdinalIgnoreCase))
        {
            return message;
        }

        return string.IsNullOrWhiteSpace(message) ? "Could not update this user role." : message;
    }
}
