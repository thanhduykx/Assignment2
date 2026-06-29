using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using BusinessObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PresentationLayer.Models;
using PresentationLayer.Security;
using PresentationLayer.Services;
using ServicesLayer;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PresentationLayer.Pages.Admin;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class IndexModel : PageModel
{
    private readonly IUserAccountStore _users;
    private readonly IKnowledgeService _knowledge;
    private readonly IKnowledgeRepository _repository;
    private readonly IAccountEmailSender _emailSender;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUserAccountStore users,
        IKnowledgeService knowledge,
        IKnowledgeRepository repository,
        IAccountEmailSender emailSender,
        ILogger<IndexModel> logger)
    {
        _users = users;
        _knowledge = knowledge;
        _repository = repository;
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

    public enum AdminSection { Directory, CreateAccount, BulkImport, CreateSubject, LecturerTable }

    public AdminSection CurrentSection { get; private set; } = AdminSection.Directory;

    public async Task OnGetAsync(string? section, string? q, string? roleFilter, CancellationToken cancellationToken)
    {
        CurrentSection = ParseSection(section);
        await LoadAsync(q, roleFilter, cancellationToken);
    }

    private static AdminSection ParseSection(string? section)
    {
        return section?.ToLowerInvariant() switch
        {
            "create-account" or "createaccount" => AdminSection.CreateAccount,
            "bulk-import" or "bulkimport" => AdminSection.BulkImport,
            "create-subject" or "createsubject" => AdminSection.CreateSubject,
            "lecturer-table" or "lecturertable" => AdminSection.LecturerTable,
            _ => AdminSection.Directory
        };
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
            var drafts = BuildImportDraftsFromExcel(model, subjects, errors);
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

            // For bulk import: distribute subjects round-robin among lecturers
            var availableSubjects = new Queue<CourseSubject>(
                subjects
                    .Where(subject => !subject.OwnerUserId.HasValue)
                    .OrderBy(subject => subject.DisplayName));
            var importSubjectIndex = 0;
            var selectedSubjectList = subjects
                .Where(subject => model.SubjectIds?.Contains(subject.Id) == true)
                .OrderBy(subject => subject.DisplayName)
                .ToList();

            foreach (var draft in drafts)
            {
                var temporaryPassword = "12345678";
                try
                {
                    var user = await _users.CreateLocalForAdminAsync(
                        draft.FullName,
                        draft.Email,
                        temporaryPassword,
                        draft.Role,
                        cancellationToken);
                    createdCount++;

                    // Assign subjects in round-robin: each lecturer gets one subject (if available)
                    IReadOnlyList<string> assignedSubjectLabels = Array.Empty<string>();
                    if (draft.Role == AppRoles.Lecturer && importSubjectIndex < selectedSubjectList.Count)
                    {
                        var subjectForThisLecturer = selectedSubjectList[importSubjectIndex];
                        if (!subjectForThisLecturer.OwnerUserId.HasValue)
                        {
                            assignedSubjectLabels = await AssignSubjectsToNewLecturerAsync(
                                user, new[] { subjectForThisLecturer }, cancellationToken);
                            assignedSubjectCount += assignedSubjectLabels.Count;
                        }
                        importSubjectIndex++;
                    }

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

    private static IReadOnlyList<ImportUserDraft> BuildImportDraftsFromExcel(
        ImportAdminUsersViewModel model,
        IReadOnlyList<CourseSubject> subjects,
        List<string> errors)
    {
        var drafts = new List<ImportUserDraft>();
        if (model.ExcelFile == null || model.ExcelFile.Length == 0)
        {
            errors.Add("Excel file is empty or missing.");
            return drafts;
        }

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

        try
        {
            using var stream = model.ExcelFile.OpenReadStream();
            using var doc = SpreadsheetDocument.Open(stream, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null)
            {
                errors.Add("Invalid Excel file format.");
                return drafts;
            }

            var sheet = workbookPart.Workbook.Sheets?.Cast<Sheet>().FirstOrDefault();
            if (sheet == null)
            {
                errors.Add("Excel file has no worksheets.");
                return drafts;
            }

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData == null)
            {
                errors.Add("Worksheet has no data.");
                return drafts;
            }

            var rows = sheetData.Elements<Row>().ToList();
            if (rows.Count == 0)
            {
                errors.Add("Worksheet has no rows.");
                return drafts;
            }

            // Detect headers from row 1
            var headerRow = rows[0];
            string? nameColumn = null;
            string? emailColumn = null;

            foreach (Cell cell in headerRow.Elements<Cell>())
            {
                string headerText = GetCellValue(doc, cell);
                string colLetter = GetColumnLetter(cell.CellReference?.Value);

                if (headerText.Equals("Họ & Tên", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("Họ và Tên", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("Tên", StringComparison.OrdinalIgnoreCase))
                {
                    nameColumn = colLetter;
                }
                else if (nameColumn == null && (
                    headerText.Contains("Họ & Tên", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Contains("Họ và Tên", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Contains("Name", StringComparison.OrdinalIgnoreCase)))
                {
                    nameColumn = colLetter;
                }

                if (headerText.Equals("Email", StringComparison.OrdinalIgnoreCase))
                {
                    emailColumn = colLetter;
                }
                else if (emailColumn == null && headerText.Contains("Email", StringComparison.OrdinalIgnoreCase))
                {
                    emailColumn = colLetter;
                }
            }

            if (string.IsNullOrEmpty(nameColumn)) nameColumn = "A";
            if (string.IsNullOrEmpty(emailColumn)) emailColumn = "D";

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var lineNumber = i + 1;

                string fullName = "";
                string email = "";

                foreach (Cell cell in row.Elements<Cell>())
                {
                    string colLetter = GetColumnLetter(cell.CellReference?.Value);
                    if (colLetter == nameColumn)
                    {
                        fullName = GetCellValue(doc, cell);
                    }
                    else if (colLetter == emailColumn)
                    {
                        email = GetCellValue(doc, cell);
                    }
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var normalizedEmail = NormalizeImportEmail(email);
                if (!IsValidEmail(normalizedEmail))
                {
                    errors.Add($"Row {lineNumber}: Email '{email}' is invalid.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = BuildImportedFullName(normalizedEmail);
                }

                drafts.Add(new ImportUserDraft(lineNumber, fullName, normalizedEmail, defaultRole, defaultSubjects));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse Excel file: {ex.Message}");
        }

        if (drafts.Count == 0 && errors.Count == 0)
        {
            errors.Add("No valid user rows found in the Excel file.");
        }

        return drafts;
    }

    private static string GetColumnLetter(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference)) return string.Empty;
        return new string(cellReference.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
    }

    private static string GetCellValue(SpreadsheetDocument doc, Cell cell)
    {
        if (cell == null) return string.Empty;
        string val = cell.CellValue?.Text ?? string.Empty;
        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
        {
            var sstPart = doc.WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
            if (sstPart?.SharedStringTable != null && int.TryParse(val, out int id))
            {
                val = sstPart.SharedStringTable.ChildElements[id].InnerText;
            }
        }
        return val.Trim();
    }

    private static void ValidateImportDrafts(
        IReadOnlyList<ImportUserDraft> drafts,
        IReadOnlyList<UserAccount> existingUsers,
        List<string> errors)
    {
        var importedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        }
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

    public IActionResult OnPostUpdateRole([FromForm] UpdateUserRoleViewModel model)
    {
        TempData["Error"] = "Role changes are disabled after account creation.";
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
            if (existingUser.Role != AppRoles.Student && existingUser.Role != AppRoles.Lecturer)
            {
                throw new InvalidOperationException("Set role to Student or Lecturer before deleting this user.");
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

            if (model.RoleType == "Leader")
            {
                // Gán làm Trưởng bộ môn (Subject Leader) — OwnerUserId
                if (subject.OwnerUserId.HasValue && subject.OwnerUserId.Value != lecturer.Id)
                {
                    TempData["Error"] = $"Trưởng bộ môn {subject.DisplayName} đã được gán cho {FormatSubjectOwner(subject)}. Vui lòng gỡ trước khi chuyển.";
                    return RedirectToPage("/Admin/Index");
                }

                await _knowledge.UpsertSubjectAsync(
                    subject.Id,
                    subject.Code,
                    subject.Name,
                    subject.Description,
                    cancellationToken,
                    new SubjectOwnerInfo(lecturer.Id, lecturer.FullName, lecturer.Email));

                TempData["Success"] = $"Đã gán {DisplayName(lecturer)} làm trưởng bộ môn {subject.DisplayName}.";
            }
            else
            {
                // Gán làm Giảng viên giảng dạy (Teaching Lecturer) — bảng trung gian
                await _repository.AddSubjectLecturerAsync(subject.Id, lecturer.Id, cancellationToken);
                TempData["Success"] = $"Đã thêm {DisplayName(lecturer)} vào danh sách giảng viên giảng dạy môn {subject.DisplayName}.";
            }
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

        return RedirectToPage("/Admin/Index", new { section = "lecturer-table" });
    }

    public async Task<IActionResult> OnPostUnregisterLecturerSubjectAsync([FromForm] UnregisterLecturerSubjectViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var lecturer = await FindUserAsync(model.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");
            if (lecturer.Role != AppRoles.Lecturer)
            {
                throw new InvalidOperationException("Only lecturers can teach subjects.");
            }

            var subject = (await _knowledge.GetCourseCatalogAsync(cancellationToken))
                .FirstOrDefault(item => item.Id == model.SubjectId)
                ?? throw new InvalidOperationException("Subject not found.");

            // Call a single repository method that handles both:
            // 1. Remove teaching lecturer from rag_subject_lecturers (junction table)
            // 2. If this lecturer is also the Subject Leader (OwnerUserId), revoke that role too
            await _repository.RemoveSubjectLecturerAsync(subject.Id, lecturer.Id, cancellationToken);

            var leaderRevoked = subject.OwnerUserId == lecturer.Id;
            if (leaderRevoked)
            {
                TempData["Success"] = $"Đã gỡ {DisplayName(lecturer)} khỏi danh sách giảng dạy và thu hồi quyền trưởng bộ môn của môn {subject.DisplayName}.";
            }
            else
            {
                TempData["Success"] = $"Đã gỡ {DisplayName(lecturer)} khỏi danh sách giảng dạy môn {subject.DisplayName}.";
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            TempData["Error"] = ToAdminUserError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not unregister subject {SubjectId} for user {UserId}", model.SubjectId, model.UserId);
            TempData["Error"] = "Could not unregister this subject.";
        }

        return RedirectToPage("/Admin/Index", new { section = "lecturer-table" });
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

        // 1. Dùng Dictionary để ánh xạ Môn học cho Giảng viên và gán cờ IsLeader
        var subjectMap = new Dictionary<Guid, Dictionary<Guid, AdminAssignedSubjectViewModel>>();

        // 2. Lặp qua danh sách môn học để gom nhóm Giảng viên (tối ưu hơn lặp qua toàn bộ User)
        foreach (var subject in subjects)
        {
            // Nguồn A: Trưởng bộ môn (OwnerUserId)
            if (subject.OwnerUserId.HasValue)
            {
                var leaderId = subject.OwnerUserId.Value;
                if (!subjectMap.ContainsKey(leaderId)) subjectMap[leaderId] = new();
                subjectMap[leaderId][subject.Id] = new AdminAssignedSubjectViewModel
                {
                    Id = subject.Id,
                    DisplayName = subject.DisplayName,
                    IsLeader = true
                };
            }

            // Nguồn B: Các giảng viên giảng dạy từ bảng trung gian
            // Hàm này nhận vào SubjectId và nhả ra danh sách UserId
            var teachingLecturerIds = await _repository.GetSubjectLecturerIdsAsync(subject.Id, cancellationToken);
            foreach (var lecturerId in teachingLecturerIds)
            {
                if (!subjectMap.ContainsKey(lecturerId)) subjectMap[lecturerId] = new();
                if (!subjectMap[lecturerId].ContainsKey(subject.Id))
                {
                    subjectMap[lecturerId][subject.Id] = new AdminAssignedSubjectViewModel
                    {
                        Id = subject.Id,
                        DisplayName = subject.DisplayName,
                        IsLeader = false
                    };
                }
            }
        }

        // 3. Chuyển đổi sang định dạng IReadOnlyDictionary theo đúng kiểu dữ liệu cũ yêu cầu
        var assignedSubjectsByUser = subjectMap.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<AdminAssignedSubjectViewModel>)kvp.Value.Values
                .OrderBy(s => s.DisplayName)
                .ToList()
        );

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

        if (message.Contains("Set role to Student or Lecturer before deleting", StringComparison.OrdinalIgnoreCase))
        {
            return message;
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
        string FullName,
        string Email,
        string Role,
        IReadOnlyList<CourseSubject> Subjects);
}
