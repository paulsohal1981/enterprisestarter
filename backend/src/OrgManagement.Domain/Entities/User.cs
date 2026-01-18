using OrgManagement.Domain.Common;
using OrgManagement.Domain.Enums;

namespace OrgManagement.Domain.Entities;

public class User : BaseEntity, ISoftDelete, IAuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.PendingActivation;

    // Authentication
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public bool MustChangePassword { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }

    // Foreign keys
    public Guid OrganizationId { get; private set; }
    public Guid? SubOrganizationId { get; private set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public Organization Organization { get; private set; } = null!;
    public SubOrganization? SubOrganization { get; private set; }
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private User() { }

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        Guid organizationId,
        Guid? subOrganizationId = null,
        string? phoneNumber = null,
        bool mustChangePassword = true)
    {
        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            OrganizationId = organizationId,
            SubOrganizationId = subOrganizationId,
            MustChangePassword = mustChangePassword,
            Status = UserStatus.Active
        };
    }

    public string FullName => $"{FirstName} {LastName}";

    public void Update(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    public void UpdateEmail(string email)
    {
        Email = email.ToLowerInvariant();
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public void GeneratePasswordResetToken()
    {
        PasswordResetToken = Guid.NewGuid().ToString("N");
        PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);
    }

    public bool ValidatePasswordResetToken(string token)
    {
        return PasswordResetToken == token &&
               PasswordResetTokenExpiry.HasValue &&
               PasswordResetTokenExpiry.Value > DateTime.UtcNow;
    }

    public void SetRefreshToken(string refreshToken, DateTime expiryTime)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
    }

    public void RecordLoginSuccess()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        LastLoginAt = DateTime.UtcNow;
    }

    public void RecordLoginFailure()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(30);
            Status = UserStatus.Locked;
        }
    }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public void Activate()
    {
        Status = UserStatus.Active;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        RevokeRefreshToken();
    }

    public void Unlock()
    {
        if (Status == UserStatus.Locked)
        {
            Status = UserStatus.Active;
            FailedLoginAttempts = 0;
            LockoutEnd = null;
        }
    }

    public void AssignToSubOrganization(Guid? subOrganizationId)
    {
        SubOrganizationId = subOrganizationId;
    }

    public void AddRole(Role role)
    {
        if (!UserRoles.Any(ur => ur.RoleId == role.Id))
        {
            UserRoles.Add(new UserRole { UserId = Id, RoleId = role.Id });
        }
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
        {
            UserRoles.Remove(userRole);
        }
    }

    public void ClearRoles()
    {
        UserRoles.Clear();
    }
}
