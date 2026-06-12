using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using PresentationLayer.Models;
using PresentationLayer.Security;

namespace PresentationLayer.Services;

public interface IUserAccountStore
{
    Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAccount>> GetByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserAccount> CreateLocalAsync(string fullName, string email, string password, CancellationToken cancellationToken = default);
    Task<UserAccount> CreateLocalForAdminAsync(string fullName, string email, string password, string role, CancellationToken cancellationToken = default);
    Task<UserAccount> GetOrCreateExternalAsync(string fullName, string email, string provider, CancellationToken cancellationToken = default);
    Task<UserAccount> UpdateFullNameAsync(Guid userId, string fullName, CancellationToken cancellationToken = default);
    Task<UserAccount> UpdateRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
    Task<UserAccount> DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(UserAccount Account, string Token, DateTimeOffset ExpiresAt)?> CreatePasswordResetTokenAsync(
        string email,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);
    Task<UserAccount> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
    Task<UserAccount> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
    bool VerifyPassword(UserAccount account, string password);
}

public sealed record SeedAdminOptions(bool Enabled, string FullName, string Email, string Password);

public sealed class UserAccountStore : IUserAccountStore
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _connectionString;
    private readonly SeedAdminOptions _seedAdmin;

    public UserAccountStore(string connectionString, SeedAdminOptions? seedAdmin = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        _connectionString = connectionString;
        _seedAdmin = NormalizeSeedAdminOptions(seedAdmin);
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken))
                .OrderBy(user => user.Email)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<UserAccount>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var normalizedRole = AppRoles.Normalize(role);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken))
                .Where(user => user.Role == normalizedRole)
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Email)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Count > 0;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            return users.FirstOrDefault(user => string.Equals(user.Email, NormalizeEmail(email), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            return users.FirstOrDefault(user => user.Id == userId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> CreateLocalAsync(string fullName, string email, string password, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var normalizedEmail = NormalizeEmail(email);
            if (users.Any(user => string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("This email is already registered.");
            }

            var user = new UserAccount
            {
                FullName = fullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = HashPassword(password),
                Provider = "Local",
                Role = IsSeedAdminEmail(normalizedEmail) || users.Count == 0 ? AppRoles.Admin : AppRoles.Student
            };

            users.Add(user);
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> CreateLocalForAdminAsync(
        string fullName,
        string email,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!AppRoles.IsKnown(role))
        {
            throw new InvalidOperationException("Role is invalid.");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var normalizedEmail = NormalizeEmail(email);
            if (users.Any(user => string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("This email is already registered.");
            }

            var user = new UserAccount
            {
                FullName = NormalizeFullName(fullName),
                Email = normalizedEmail,
                PasswordHash = HashPassword(password),
                Provider = "Local",
                Role = IsSeedAdminEmail(normalizedEmail) ? AppRoles.Admin : AppRoles.Normalize(role)
            };

            users.Add(user);
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> GetOrCreateExternalAsync(string fullName, string email, string provider, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var normalizedEmail = NormalizeEmail(email);
            var existing = users.FirstOrDefault(user => string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                var changed = EnsureSeedAdminRole(existing);
                if (TrackExternalProvider(existing, provider))
                {
                    changed = true;
                }

                if (ShouldUseExternalName(existing.FullName, fullName))
                {
                    existing.FullName = fullName.Trim();
                    changed = true;
                }

                if (changed)
                {
                    await SaveAsync(users, cancellationToken);
                }

                return existing;
            }

            var user = new UserAccount
            {
                FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedEmail : fullName.Trim(),
                Email = normalizedEmail,
                Provider = provider,
                Role = IsSeedAdminEmail(normalizedEmail) || users.Count == 0 ? AppRoles.Admin : AppRoles.Student
            };

            users.Add(user);
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> UpdateRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        if (!AppRoles.IsKnown(role))
        {
            throw new InvalidOperationException("Role is invalid.");
        }

        var normalizedRole = AppRoles.Normalize(role);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var user = users.FirstOrDefault(item => item.Id == userId)
                ?? throw new InvalidOperationException("User not found.");

            if (user.Role == AppRoles.Admin
                && normalizedRole != AppRoles.Admin
                && users.Count(item => item.Role == AppRoles.Admin) <= 1)
            {
                throw new InvalidOperationException("Cannot demote the last admin.");
            }

            if (IsSeedAdminEmail(user.Email) && normalizedRole != AppRoles.Admin)
            {
                throw new InvalidOperationException("Cannot demote the seed admin.");
            }

            user.Role = normalizedRole;
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> UpdateFullNameAsync(Guid userId, string fullName, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var user = users.FirstOrDefault(item => item.Id == userId)
                ?? throw new InvalidOperationException("User not found.");

            user.FullName = NormalizeFullName(fullName);
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var user = users.FirstOrDefault(item => item.Id == userId)
                ?? throw new InvalidOperationException("User not found.");

            if (user.Role != AppRoles.Student)
            {
                throw new InvalidOperationException("Set role to Student before deleting this user.");
            }

            if (user.Role == AppRoles.Admin && users.Count(item => item.Role == AppRoles.Admin) <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last admin.");
            }

            if (IsSeedAdminEmail(user.Email))
            {
                throw new InvalidOperationException("Cannot delete the seed admin.");
            }

            users.Remove(user);
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<(UserAccount Account, string Token, DateTimeOffset ExpiresAt)?> CreatePasswordResetTokenAsync(
        string email,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : lifetime);
        var token = CreatePasswordResetToken();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var user = users.FirstOrDefault(item => string.Equals(item.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return null;
            }

            user.PasswordResetTokenHash = HashResetToken(token);
            user.PasswordResetTokenExpiresAt = expiresAt;
            await SaveAsync(users, cancellationToken);
            return (user, token, expiresAt);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ValidatePassword(newPassword);
        var normalizedEmail = NormalizeEmail(email);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var user = users.FirstOrDefault(item => string.Equals(item.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Password reset link is invalid or expired.");

            if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash)
                || user.PasswordResetTokenExpiresAt is null
                || user.PasswordResetTokenExpiresAt <= DateTimeOffset.UtcNow
                || !ResetTokenMatches(user.PasswordResetTokenHash, token))
            {
                throw new InvalidOperationException("Password reset link is invalid or expired.");
            }

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.PasswordChangedAt = DateTimeOffset.UtcNow;
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserAccount> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ValidatePassword(newPassword);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var user = users.FirstOrDefault(item => item.Id == userId)
                ?? throw new InvalidOperationException("User not found.");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                throw new InvalidOperationException("This account does not have a local password. Use forgot password to set one.");
            }

            if (!VerifyPassword(user, currentPassword))
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.PasswordChangedAt = DateTimeOffset.UtcNow;
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool VerifyPassword(UserAccount account, string password)
    {
        if (string.IsNullOrWhiteSpace(account.PasswordHash))
        {
            return false;
        }

        var parts = account.PasswordHash.Split('.', 3);
        if (parts.Length != 3 || parts[0] != "PBKDF2")
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var actual = pbkdf2.GetBytes(KeySize);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);
        return $"PBKDF2.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    private static string CreatePasswordResetToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashResetToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static bool ResetTokenMatches(string expectedHash, string token)
    {
        try
        {
            var expected = Convert.FromHexString(expectedHash);
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return expected.Length == actual.Length
                   && CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters.");
        }
    }

    private async Task<List<UserAccount>> LoadAsync(CancellationToken cancellationToken)
    {
        await EnsureCreatedAsync(cancellationToken);
        var users = await LoadUsersCoreAsync(cancellationToken);
        var changed = false;

        foreach (var user in users)
        {
            var normalizedRole = AppRoles.Normalize(user.Role);
            if (user.Role != normalizedRole)
            {
                user.Role = normalizedRole;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = user.Email;
                changed = true;
            }

            if (user.PasswordResetTokenExpiresAt is { } resetExpiresAt
                && resetExpiresAt <= DateTimeOffset.UtcNow
                && !string.IsNullOrWhiteSpace(user.PasswordResetTokenHash))
            {
                user.PasswordResetTokenHash = null;
                user.PasswordResetTokenExpiresAt = null;
                changed = true;
            }
        }

        if (EnsureSeedAdmin(users))
        {
            changed = true;
        }

        if (changed)
        {
            await SaveAsync(users, cancellationToken);
        }

        return users;
    }

    private async Task SaveAsync(List<UserAccount> users, CancellationToken cancellationToken)
    {
        await EnsureCreatedAsync(cancellationToken);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM app_users";
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var user in users)
            {
                await using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO app_users (
                        Id, Email, FullName, PasswordHash, PasswordResetTokenHash,
                        PasswordResetTokenExpiresAt, PasswordChangedAt, Provider, Role, CreatedAt)
                    VALUES (
                        @Id, @Email, @FullName, @PasswordHash, @PasswordResetTokenHash,
                        @PasswordResetTokenExpiresAt, @PasswordChangedAt, @Provider, @Role, @CreatedAt)
                    """;
                AddUserParameters(insertCommand, user);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<UserAccount>> LoadUsersCoreAsync(CancellationToken cancellationToken)
    {
        var users = new List<UserAccount>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Email, FullName, PasswordHash, PasswordResetTokenHash,
                   PasswordResetTokenExpiresAt, PasswordChangedAt, Provider, Role, CreatedAt
            FROM app_users
            ORDER BY Email
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new UserAccount
            {
                Id = reader.GetGuid(0),
                Email = reader.GetString(1),
                FullName = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                PasswordResetTokenHash = reader.IsDBNull(4) ? null : reader.GetString(4),
                PasswordResetTokenExpiresAt = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
                PasswordChangedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                Provider = reader.GetString(7),
                Role = reader.GetString(8),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(9)
            });
        }

        return users;
    }

    private async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID('app_users', 'U') IS NULL
            BEGIN
                CREATE TABLE app_users (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_app_users PRIMARY KEY,
                    Email NVARCHAR(320) NOT NULL,
                    FullName NVARCHAR(120) NOT NULL,
                    PasswordHash NVARCHAR(512) NOT NULL,
                    PasswordResetTokenHash NVARCHAR(128) NULL,
                    PasswordResetTokenExpiresAt DATETIMEOFFSET NULL,
                    PasswordChangedAt DATETIMEOFFSET NULL,
                    Provider NVARCHAR(120) NOT NULL,
                    Role NVARCHAR(32) NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL
                );
            END;

            IF COL_LENGTH('app_users', 'PasswordResetTokenHash') IS NULL
                ALTER TABLE app_users ADD PasswordResetTokenHash NVARCHAR(128) NULL;
            IF COL_LENGTH('app_users', 'PasswordResetTokenExpiresAt') IS NULL
                ALTER TABLE app_users ADD PasswordResetTokenExpiresAt DATETIMEOFFSET NULL;
            IF COL_LENGTH('app_users', 'PasswordChangedAt') IS NULL
                ALTER TABLE app_users ADD PasswordChangedAt DATETIMEOFFSET NULL;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'UX_app_users_Email'
                  AND object_id = OBJECT_ID('app_users'))
            BEGIN
                CREATE UNIQUE INDEX UX_app_users_Email ON app_users (Email);
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_app_users_Role'
                  AND object_id = OBJECT_ID('app_users'))
            BEGIN
                CREATE INDEX IX_app_users_Role ON app_users (Role);
            END;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddUserParameters(SqlCommand command, UserAccount user)
    {
        command.Parameters.AddWithValue("@Id", user.Id);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.Parameters.AddWithValue("@FullName", user.FullName);
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@PasswordResetTokenHash", (object?)user.PasswordResetTokenHash ?? DBNull.Value);
        command.Parameters.AddWithValue("@PasswordResetTokenExpiresAt", (object?)user.PasswordResetTokenExpiresAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@PasswordChangedAt", (object?)user.PasswordChangedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@Provider", user.Provider);
        command.Parameters.AddWithValue("@Role", user.Role);
        command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Full name is required.");
        }

        var normalized = fullName.Trim();
        return normalized.Length <= 120
            ? normalized
            : throw new InvalidOperationException("Full name must be 120 characters or fewer.");
    }

    private bool EnsureSeedAdmin(List<UserAccount> users)
    {
        if (!_seedAdmin.Enabled)
        {
            return false;
        }

        var seedEmail = _seedAdmin.Email;
        var existing = users.FirstOrDefault(user => string.Equals(user.Email, seedEmail, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(existing.FullName))
            {
                existing.FullName = _seedAdmin.FullName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Provider))
            {
                existing.Provider = "Local";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.PasswordHash))
            {
                existing.PasswordHash = HashPassword(_seedAdmin.Password);
                changed = true;
            }

            if (existing.Role != AppRoles.Admin)
            {
                existing.Role = AppRoles.Admin;
                changed = true;
            }

            return changed;
        }

        if (users.Any(user => user.Role == AppRoles.Admin))
        {
            return false;
        }

        users.Add(new UserAccount
        {
            FullName = _seedAdmin.FullName,
            Email = seedEmail,
            PasswordHash = HashPassword(_seedAdmin.Password),
            Provider = "Local",
            Role = AppRoles.Admin
        });

        return true;
    }

    private bool IsSeedAdminEmail(string email)
    {
        return _seedAdmin.Enabled
               && string.Equals(NormalizeEmail(email), _seedAdmin.Email, StringComparison.OrdinalIgnoreCase);
    }

    private bool EnsureSeedAdminRole(UserAccount user)
    {
        if (!IsSeedAdminEmail(user.Email) || user.Role == AppRoles.Admin)
        {
            return false;
        }

        user.Role = AppRoles.Admin;
        return true;
    }

    private static bool TrackExternalProvider(UserAccount user, string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.Provider))
        {
            user.Provider = provider.Trim();
            return true;
        }

        var providers = user.Provider
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (providers.Any(item => item.Equals(provider, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        providers.Add(provider.Trim());
        user.Provider = string.Join(", ", providers);
        return true;
    }

    private static bool ShouldUseExternalName(string currentName, string externalName)
    {
        return !string.IsNullOrWhiteSpace(externalName)
               && (string.IsNullOrWhiteSpace(currentName)
                   || currentName.Equals("System Admin", StringComparison.OrdinalIgnoreCase)
                   || currentName.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                   || currentName.Contains('@'));
    }

    private static SeedAdminOptions NormalizeSeedAdminOptions(SeedAdminOptions? options)
    {
        var fullName = string.IsNullOrWhiteSpace(options?.FullName) ? "System Admin" : options.FullName.Trim();
        var email = string.IsNullOrWhiteSpace(options?.Email) ? "admin@eduvietrag.local" : NormalizeEmail(options.Email);
        var password = string.IsNullOrWhiteSpace(options?.Password) ? "Admin@12345" : options.Password;
        return new SeedAdminOptions(options?.Enabled ?? true, fullName, email, password);
    }
}
