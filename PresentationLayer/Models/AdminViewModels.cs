using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;


namespace PresentationLayer.Models;

public sealed class AdminUsersViewModel
{
    public IReadOnlyList<AdminUserRowViewModel> Users { get; set; } = Array.Empty<AdminUserRowViewModel>();
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<AdminSubjectOptionViewModel> SubjectOptions { get; set; } = Array.Empty<AdminSubjectOptionViewModel>();
    public CreateAdminUserViewModel CreateUser { get; set; } = new();
    public CreateAdminSubjectViewModel CreateSubject { get; set; } = new();
}

public sealed class AdminUserRowViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsLastAdmin { get; set; }
    public bool IsCurrentUser { get; set; }
    public IReadOnlyList<string> AssignedSubjects { get; set; } = Array.Empty<string>();
    public IReadOnlyList<AdminAssignedSubjectViewModel> AssignedSubjectDetails { get; set; } = Array.Empty<AdminAssignedSubjectViewModel>();
}

public sealed class AdminAssignedSubjectViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsLeader { get; set; }
}

public sealed class AdminSubjectOptionViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? OwnerUserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int StudentCount { get; set; }
}

public sealed class UpdateUserRoleViewModel
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public sealed class DeleteAdminUserViewModel
{
    public Guid UserId { get; set; }
}

public sealed class RegisterLecturerSubjectViewModel
{
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }
    public string RoleType { get; set; } = "Teaching"; // "Leader" or "Teaching"
}

public sealed class UnregisterLecturerSubjectViewModel
{
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }
    public string RoleType { get; set; } = "Teaching"; // "Leader" or "Teaching"
}

public sealed class CreateAdminSubjectViewModel
{
    [Required(ErrorMessage = "Subject code is required.")]
    [StringLength(32, ErrorMessage = "Subject code must be 32 characters or fewer.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
    public string Description { get; set; } = string.Empty;
}

public sealed class CreateAdminUserViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, ErrorMessage = "Full name must be 120 characters or fewer.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = PresentationLayer.Security.AppRoles.Student;

    public List<Guid> SubjectIds { get; set; } = new();
}

public sealed class ImportAdminUsersViewModel
{
    [Required(ErrorMessage = "Excel file is required.")]
    public IFormFile ExcelFile { get; set; } = null!;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = PresentationLayer.Security.AppRoles.Student;

    public List<Guid> SubjectIds { get; set; } = new();
}

public sealed class ToggleSubjectActiveStatusViewModel
{
    public Guid SubjectId { get; set; }
    public bool IsActive { get; set; }
}

public sealed class RegisterStudentSubjectViewModel
{
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }
}

public sealed class UnregisterStudentSubjectViewModel
{
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }
}
