using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using DataAccessLayer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly IAccountEmailSender _emailSender;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUserAccountStore users,
        IKnowledgeRepository knowledge,
        IAccountEmailSender emailSender,
        ILogger<IndexModel> logger)
    {
        _users = users;
        _knowledge = knowledge;
        _emailSender = emailSender;
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
            var role = AppRoles.Normalize(model.Role);
            var selectedSubjects = await ResolveSubjectsForNewUserAsync(role, model.SubjectIds, cancellationToken);
            var user = await _users.CreateLocalForAdminAsync(
                model.FullName,
                model.Email,
                model.Password,
                role,
                cancellationToken);

            IReadOnlyList<string> assignedSubjectLabels = Array.Empty<string>();
            var warnings = new List<string>();
            try
            {
                assignedSubjectLabels = await AssignSubjectsToNewLecturerAsync(user, selectedSubjects, cancellationToken);
            }
            catch (Exception assignmentEx)
            {
                _logger.LogWarning(assignmentEx, "Created user {Email}, but lecturer subjects could not be assigned.", user.Email);
                warnings.Add("User was created, but lecturer subjects could not be assigned.");
            }

            var emailSent = false;
            try
            {
                await _emailSender.SendWelcomeEmailAsync(
                    user,
                    model.Password,
                    BuildLoginUrl(),
                    cancellationToken,
                    assignedSubjectLabels);
                emailSent = true;
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Created user {Email}, but welcome email could not be sent.", user.Email);
                warnings.Add("Welcome email could not be sent. Check SMTP configuration.");
            }

            var subjectSummary = assignedSubjectLabels.Count > 0
                ? $" Assigned {assignedSubjectLabels.Count} subject(s)."
                : string.Empty;
            var emailSummary = emailSent ? " Welcome email sent." : string.Empty;
            TempData["Success"] = $"Created {user.Email} as {user.Role}.{subjectSummary}{emailSummary}";
            if (warnings.Count > 0)
            {
                TempData["Error"] = string.Join(" ", warnings);
            }
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

    public async Task<IActionResult> OnPostImportUsersAsync([FromForm] ImportAdminUsersViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelError("Could not import users.");
            return RedirectToPage("/Admin/Index");
        }

        var errors = new List<string>();
        try
        {
            var subjects = await _knowledge.GetCourseCatalogAsync(cancellationToken);
            var existingUsers = await _users.GetAllAsync(cancellationToken);
            var drafts = BuildImportDrafts(model, subjects, errors);
            ValidateImportDrafts(drafts, existingUsers, errors);
            if (errors.Count > 0)
            {
                TempData["Error"] = BuildImportErrorSummary(errors);
                return RedirectToPage("/Admin/Index");
            }

            var createdCount = 0;
            var emailSentCount = 0;
            var assignedSubjectCount = 0;
            var warnings = new List<string>();

            foreach (var draft in drafts)
            {
                var temporaryPassword = GenerateTemporaryPassword();
                try
                {
                    var user = await _users.CreateLocalForAdminAsync(
                        BuildImportedFullName(draft.Email),
                        draft.Email,
                        temporaryPassword,
                        draft.Role,
                        cancellationToken);
                    createdCount++;

                    var assignedSubjectLabels = await AssignSubjectsToNewLecturerAsync(user, draft.Subjects, cancellationToken);
                    assignedSubjectCount += assignedSubjectLabels.Count;

                    try
                    {
                        await _emailSender.SendWelcomeEmailAsync(
                            user,
                            temporaryPassword,
                            BuildLoginUrl(),
                            cancellationToken,
                            assignedSubjectLabels);
                        emailSentCount++;
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Imported user {Email}, but welcome email could not be sent.", user.Email);
                        warnings.Add($"{user.Email}: welcome email could not be sent.");
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException)
                {
                    errors.Add($"Line {draft.LineNumber}: {ToAdminUserError(ex.Message)}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not import user {Email}", draft.Email);
                    errors.Add($"Line {draft.LineNumber}: could not import this user.");
                }
            }

            if (createdCount > 0)
            {
                var subjectSummary = assignedSubjectCount > 0
                    ? $" Assigned {assignedSubjectCount} subject(s)."
                    : string.Empty;
                TempData["Success"] = $"Imported {createdCount} user(s). Sent {emailSentCount} welcome email(s).{subjectSummary}";
            }

            if (warnings.Count > 0 || errors.Count > 0)
            {
                TempData["Error"] = BuildImportErrorSummary(warnings.Concat(errors));
            }
            else if (createdCount == 0)
            {
                TempData["Error"] = "No users were imported.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not import users.");
            TempData["Error"] = "Could not import users.";
        }

        return RedirectToPage("/Admin/Index");
    }

    private async Task<IReadOnlyList<CourseSubject>> ResolveSubjectsForNewUserAsync(
        string role,
        IEnumerable<Guid>? subjectIds,
        CancellationToken cancellationToken)
    {
        if (role != AppRoles.Lecturer)
        {
            return Array.Empty<CourseSubject>();
        }

        var selectedIds = (subjectIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (selectedIds.Count == 0)
        {
            return Array.Empty<CourseSubject>();
        }

        var subjects = await _knowledge.GetCourseCatalogAsync(cancellationToken);
        var selectedSubjects = selectedIds
            .Select(id => subjects.FirstOrDefault(subject => subject.Id == id))
            .ToList();
        if (selectedSubjects.Any(subject => subject is null))
        {
            throw new InvalidOperationException("Subject not found.");
        }

        var alreadyAssigned = selectedSubjects.FirstOrDefault(subject => subject!.OwnerUserId.HasValue);
        if (alreadyAssigned is not null)
        {
            throw new InvalidOperationException(BuildAssignedSubjectError(alreadyAssigned));
        }

        return selectedSubjects
            .OfType<CourseSubject>()
            .ToList();
    }

    private async Task<IReadOnlyList<string>> AssignSubjectsToNewLecturerAsync(
        UserAccount user,
        IReadOnlyList<CourseSubject> selectedSubjects,
        CancellationToken cancellationToken)
    {
        if (user.Role != AppRoles.Lecturer || selectedSubjects.Count == 0)
        {
            return Array.Empty<string>();
        }

        var assignedSubjectLabels = new List<string>();
        foreach (var subject in selectedSubjects)
        {
            await _knowledge.UpsertSubjectAsync(
                subject.Id,
                subject.Code,
                subject.Name,
                subject.Description,
                cancellationToken,
                new SubjectOwnerInfo(user.Id, user.FullName, user.Email));
            assignedSubjectLabels.Add(subject.DisplayName);
        }

        return assignedSubjectLabels;
    }

    private string BuildLoginUrl()
    {
        return Url.Page(
            "/Account/Login",
            pageHandler: null,
            values: null,
            protocol: Request.Scheme,
            host: Request.Host.ToUriComponent()) ?? string.Empty;
    }

    private static IReadOnlyList<ImportUserDraft> BuildImportDrafts(
        ImportAdminUsersViewModel model,
        IReadOnlyList<CourseSubject> subjects,
        List<string> errors)
    {
        var defaultRole = AppRoles.Student;
        if (AppRoles.IsKnown(model.Role))
        {
            defaultRole = AppRoles.Normalize(model.Role);
        }
        else
        {
            errors.Add("Default role is invalid.");
        }
        var defaultSubjects = ResolveSubjectIds(model.SubjectIds, subjects, errors, "default subject selection");
        var drafts = new List<ImportUserDraft>();
        var lines = (model.Emails ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = SplitImportColumns(line);
            var email = NormalizeImportEmail(columns[0]);
            if (!IsValidEmail(email))
            {
                errors.Add($"Line {lineNumber}: email is invalid.");
                continue;
            }

            var roleText = columns.Count >= 2 && !string.IsNullOrWhiteSpace(columns[1])
                ? columns[1]
                : defaultRole;
            if (!AppRoles.IsKnown(roleText))
            {
                errors.Add($"Line {lineNumber}: role is invalid.");
                continue;
            }

            var role = AppRoles.Normalize(roleText);
            var rowSubjectTokens = columns.Count >= 3
                ? SplitSubjectTokens(columns[2])
                : Array.Empty<string>();

            if (role != AppRoles.Lecturer && rowSubjectTokens.Count > 0)
            {
                errors.Add($"Line {lineNumber}: subjects are only valid for Lecturer role.");
                continue;
            }

            var selectedSubjects = role == AppRoles.Lecturer
                ? rowSubjectTokens.Count > 0
                    ? ResolveSubjectTokens(rowSubjectTokens, subjects, errors, $"Line {lineNumber}")
                    : defaultSubjects
                : Array.Empty<CourseSubject>();

            drafts.Add(new ImportUserDraft(lineNumber, email, role, selectedSubjects));
        }

        if (drafts.Count == 0 && errors.Count == 0)
        {
            errors.Add("Email list is empty.");
        }

        return drafts;
    }

    private static void ValidateImportDrafts(
        IReadOnlyList<ImportUserDraft> drafts,
        IReadOnlyList<UserAccount> existingUsers,
        List<string> errors)
    {
        var importedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reservedSubjects = new Dictionary<Guid, string>();

        foreach (var draft in drafts)
        {
            if (!importedEmails.Add(draft.Email))
            {
                errors.Add($"Line {draft.LineNumber}: email is duplicated in the import list.");
            }

            if (existingUsers.Any(user => user.Email.Equals(draft.Email, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Line {draft.LineNumber}: this email is already registered.");
            }

            foreach (var subject in draft.Subjects)
            {
                if (subject.OwnerUserId.HasValue)
                {
                    errors.Add($"Line {draft.LineNumber}: {BuildAssignedSubjectError(subject)}");
                    continue;
                }

                if (reservedSubjects.TryGetValue(subject.Id, out var reservedBy))
                {
                    errors.Add($"Line {draft.LineNumber}: subject {subject.DisplayName} is already selected for {reservedBy} in this import.");
                    continue;
                }

                reservedSubjects[subject.Id] = draft.Email;
            }
        }
    }

    private static IReadOnlyList<string> SplitImportColumns(string line)
    {
        var separator = line.Contains('\t')
            ? '\t'
            : line.Contains(';')
                ? ';'
                : ',';

        return line.Split(separator, 3, StringSplitOptions.TrimEntries).ToList();
    }

    private static IReadOnlyList<string> SplitSubjectTokens(string value)
    {
        return value
            .Split(new[] { '|', '+', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<CourseSubject> ResolveSubjectIds(
        IEnumerable<Guid>? subjectIds,
        IReadOnlyList<CourseSubject> subjects,
        List<string> errors,
        string scope)
    {
        var selectedSubjects = new List<CourseSubject>();
        foreach (var subjectId in (subjectIds ?? Array.Empty<Guid>()).Where(id => id != Guid.Empty).Distinct())
        {
            var subject = subjects.FirstOrDefault(item => item.Id == subjectId);
            if (subject is null)
            {
                errors.Add($"{scope}: subject not found.");
                continue;
            }

            selectedSubjects.Add(subject);
        }

        return selectedSubjects;
    }

    private static IReadOnlyList<CourseSubject> ResolveSubjectTokens(
        IEnumerable<string> subjectTokens,
        IReadOnlyList<CourseSubject> subjects,
        List<string> errors,
        string scope)
    {
        var selectedSubjects = new List<CourseSubject>();
        foreach (var token in subjectTokens)
        {
            var subject = FindSubjectByToken(token, subjects);
            if (subject is null)
            {
                errors.Add($"{scope}: subject '{token}' was not found.");
                continue;
            }

            selectedSubjects.Add(subject);
        }

        return selectedSubjects
            .GroupBy(subject => subject.Id)
            .Select(group => group.First())
            .ToList();
    }

    private static CourseSubject? FindSubjectByToken(string token, IReadOnlyList<CourseSubject> subjects)
    {
        var normalizedToken = token.Trim();
        if (Guid.TryParse(normalizedToken, out var subjectId))
        {
            return subjects.FirstOrDefault(subject => subject.Id == subjectId);
        }

        return subjects.FirstOrDefault(subject =>
            subject.Code.Equals(normalizedToken, StringComparison.OrdinalIgnoreCase)
            || subject.Name.Equals(normalizedToken, StringComparison.OrdinalIgnoreCase)
            || subject.DisplayName.Equals(normalizedToken, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeImportEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email)
               && new EmailAddressAttribute().IsValid(email);
    }

    private static string BuildImportedFullName(string email)
    {
        var localPart = email.Split('@', 2)[0]
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();
        var fullName = string.IsNullOrWhiteSpace(localPart) ? email : localPart;
        return fullName.Length <= 120 ? fullName : fullName[..120];
    }

    private static string GenerateTemporaryPassword()
    {
        return $"Cpms@{WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(9))}";
    }

    private static string BuildImportErrorSummary(IEnumerable<string> messages)
    {
        var items = messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (items.Count <= 6)
        {
            return string.Join(" ", items);
        }

        return $"{string.Join(" ", items.Take(6))} ... and {items.Count - 6} more issue(s).";
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
                name: model.Code,
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

    public async Task<IActionResult> OnPostUpdateRoleAsync([FromForm] UpdateUserRoleViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var existingUser = (await _users.GetAllAsync(cancellationToken))
                .FirstOrDefault(user => user.Id == model.UserId)
                ?? throw new InvalidOperationException("User not found.");
            var user = await _users.UpdateRoleAsync(model.UserId, model.Role, cancellationToken);
            var unassignedSubjectCount = 0;
            if (existingUser.Role == AppRoles.Lecturer && user.Role != AppRoles.Lecturer)
            {
                unassignedSubjectCount = await UnassignSubjectsOwnedByAsync(user.Id, cancellationToken);
            }

            var subjectSummary = unassignedSubjectCount > 0
                ? $" Unassigned {unassignedSubjectCount} subject(s)."
                : string.Empty;
            TempData["Success"] = $"Updated {user.Email} to {user.Role}.{subjectSummary}";

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

    public async Task<IActionResult> OnPostDeleteUserAsync([FromForm] DeleteAdminUserViewModel model, CancellationToken cancellationToken)
    {
        if (IsCurrentUser(model.UserId))
        {
            TempData["Error"] = "You cannot delete your own active account.";
            return RedirectToPage("/Admin/Index");
        }

        try
        {
            var existingUser = (await _users.GetAllAsync(cancellationToken))
                .FirstOrDefault(user => user.Id == model.UserId)
                ?? throw new InvalidOperationException("User not found.");
            if (existingUser.Role != AppRoles.Student)
            {
                throw new InvalidOperationException("Set role to Student before deleting this user.");
            }

            var unassignedSubjectCount = await UnassignSubjectsOwnedByAsync(model.UserId, cancellationToken);
            var deletedUser = await _users.DeleteAsync(model.UserId, cancellationToken);
            var subjectSummary = unassignedSubjectCount > 0
                ? $" Unassigned {unassignedSubjectCount} subject(s)."
                : string.Empty;
            TempData["Success"] = $"Deleted {deletedUser.Email}.{subjectSummary}";
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete user {UserId}", model.UserId);
            TempData["Error"] = "Could not delete this user.";
        }

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostRegisterLecturerSubjectAsync([FromForm] RegisterLecturerSubjectViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var lecturer = await FindUserAsync(model.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");
            if (lecturer.Role != AppRoles.Lecturer)
            {
                throw new InvalidOperationException("Only lecturers can be assigned to subjects.");
            }

            var subject = (await _knowledge.GetCourseCatalogAsync(cancellationToken))
                .FirstOrDefault(item => item.Id == model.SubjectId)
                ?? throw new InvalidOperationException("Subject not found.");

            if (subject.OwnerUserId.HasValue && subject.OwnerUserId.Value != lecturer.Id)
            {
                TempData["Error"] = $"Môn {subject.DisplayName} đã được gán cho {FormatSubjectOwner(subject)}.";
                return RedirectToPage("/Admin/Index");
            }

            await _knowledge.UpsertSubjectAsync(
                subject.Id,
                subject.Code,
                subject.Name,
                subject.Description,
                cancellationToken,
                new SubjectOwnerInfo(lecturer.Id, lecturer.FullName, lecturer.Email));

            TempData["Success"] = subject.OwnerUserId == lecturer.Id
                ? $"Môn {subject.DisplayName} đã thuộc {DisplayName(lecturer)}."
                : $"Đã đăng ký môn {subject.DisplayName} cho {DisplayName(lecturer)}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not register subject {SubjectId} for lecturer {UserId}", model.SubjectId, model.UserId);
            TempData["Error"] = "Could not register this subject.";
        }

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostUnregisterLecturerSubjectAsync([FromForm] UnregisterLecturerSubjectViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var lecturer = await FindUserAsync(model.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");
            var subject = (await _knowledge.GetCourseCatalogAsync(cancellationToken))
                .FirstOrDefault(item => item.Id == model.SubjectId)
                ?? throw new InvalidOperationException("Subject not found.");

            if (subject.OwnerUserId != lecturer.Id)
            {
                TempData["Error"] = $"Môn {subject.DisplayName} không thuộc {DisplayName(lecturer)}.";
                return RedirectToPage("/Admin/Index");
            }

            await _knowledge.UpsertSubjectAsync(
                subject.Id,
                subject.Code,
                subject.Name,
                subject.Description,
                cancellationToken,
                new SubjectOwnerInfo(null, string.Empty, string.Empty));

            TempData["Success"] = $"Đã gỡ môn {subject.DisplayName} khỏi {DisplayName(lecturer)}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not unregister subject {SubjectId} from lecturer {UserId}", model.SubjectId, model.UserId);
            TempData["Error"] = "Could not unregister this subject.";
        }

        return RedirectToPage("/Admin/Index");
    }

    private async Task<int> UnassignSubjectsOwnedByAsync(Guid userId, CancellationToken cancellationToken)
    {
        var subjectsToUnassign = (await _knowledge.GetCourseCatalogAsync(cancellationToken))
            .Where(subject => subject.OwnerUserId == userId)
            .ToList();

        foreach (var subject in subjectsToUnassign)
        {
            await _knowledge.UpsertSubjectAsync(
                subject.Id,
                subject.Code,
                subject.Name,
                subject.Description,
                cancellationToken,
                new SubjectOwnerInfo(null, string.Empty, string.Empty));
        }

        return subjectsToUnassign.Count;
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
                group => (IReadOnlyList<AdminAssignedSubjectViewModel>)group
                    .OrderBy(subject => subject.Code)
                    .ThenBy(subject => subject.Name)
                    .Select(subject => new AdminAssignedSubjectViewModel
                    {
                        Id = subject.Id,
                        DisplayName = subject.DisplayName
                    })
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
                IsCurrentUser = IsCurrentUser(user.Id),
                AssignedSubjectDetails = ResolveAssignedSubjectDetails(user, assignedSubjectsByUser),
                AssignedSubjects = ResolveAssignedSubjectLabels(user, assignedSubjectsByUser)
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
        IReadOnlyDictionary<Guid, IReadOnlyList<AdminAssignedSubjectViewModel>> lecturerSubjectsByUser)
    {
        if (user.Role == AppRoles.Lecturer)
        {
            return lecturerSubjectsByUser.TryGetValue(user.Id, out var assignedSubjects)
                ? assignedSubjects.Select(subject => subject.DisplayName).ToList()
                : Array.Empty<string>();
        }

        if (user.Role != AppRoles.Student)
        {
            return Array.Empty<string>();
        }

        return new[] { "All indexed documents" };
    }

    private static IReadOnlyList<AdminAssignedSubjectViewModel> ResolveAssignedSubjectDetails(
        UserAccount user,
        IReadOnlyDictionary<Guid, IReadOnlyList<AdminAssignedSubjectViewModel>> lecturerSubjectsByUser)
    {
        return user.Role == AppRoles.Lecturer && lecturerSubjectsByUser.TryGetValue(user.Id, out var assignedSubjects)
            ? assignedSubjects
            : Array.Empty<AdminAssignedSubjectViewModel>();
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

    private async Task<UserAccount?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return (await _users.GetAllAsync(cancellationToken))
            .FirstOrDefault(user => user.Id == userId);
    }

    private static string DisplayName(UserAccount user)
    {
        return string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName.Trim();
    }

    private static string FormatSubjectOwner(CourseSubject subject)
    {
        var name = string.IsNullOrWhiteSpace(subject.OwnerName)
            ? "giảng viên khác"
            : subject.OwnerName.Trim();
        var email = subject.OwnerEmail?.Trim();
        return string.IsNullOrWhiteSpace(email) ? name : $"{name} ({email})";
    }

    private static string BuildAssignedSubjectError(CourseSubject subject)
    {
        return $"Subject {subject.DisplayName} is already assigned to {FormatSubjectOwner(subject)}.";
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

        var assignedSubjectError = TryFormatAssignedSubjectError(message);
        if (!string.IsNullOrWhiteSpace(assignedSubjectError))
        {
            return assignedSubjectError;
        }

        if (message.Contains("Subject is already assigned", StringComparison.OrdinalIgnoreCase))
        {
            return "Môn này đã được gán cho giảng viên khác.";
        }

        if (message.Contains("Set role to Student before deleting", StringComparison.OrdinalIgnoreCase))
        {
            return "Set this user role to Student before deleting.";
        }

        if (message.Contains("Subject code is required", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Subject code already exists", StringComparison.OrdinalIgnoreCase)
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

        if (message.Contains("Cannot delete the last admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot delete the last admin.";
        }

        if (message.Contains("Cannot delete the seed admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot delete the seed admin.";
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

    private static string? TryFormatAssignedSubjectError(string message)
    {
        const string prefix = "Subject ";
        const string separator = " is already assigned to ";
        if (!message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !message.Contains(separator, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var payload = message[prefix.Length..];
        var separatorIndex = payload.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
        if (separatorIndex < 0)
        {
            return null;
        }

        var subject = payload[..separatorIndex].Trim();
        var owner = payload[(separatorIndex + separator.Length)..].Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(owner)
            ? null
            : $"Môn {subject} đã được gán cho {owner}.";
    }

    private sealed record ImportUserDraft(
        int LineNumber,
        string Email,
        string Role,
        IReadOnlyList<CourseSubject> Subjects);
}
