using SharedKernel;

namespace Identity.Domain;

public static class IdentityErrors
{
    // Value objects
    public static readonly Error EmailEmpty = Error.Validation("Identity.Email.Empty", "Email is required.");
    public static readonly Error EmailInvalid = Error.Validation("Identity.Email.Invalid", "Email is not valid.");
    public static readonly Error SlugEmpty = Error.Validation("Identity.Slug.Empty", "Slug is required.");
    public static readonly Error SlugInvalid = Error.Validation("Identity.Slug.Invalid", "Slug must be lowercase alphanumerics separated by hyphens.");
    public static readonly Error DisplayNameEmpty = Error.Validation("Identity.DisplayName.Empty", "Display name is required.");
    public static readonly Error TenantNameEmpty = Error.Validation("Identity.Tenant.NameEmpty", "Tenant name is required.");

    // Users / tenants
    public static readonly Error EmailTaken = Error.Conflict("Identity.User.EmailTaken", "An account with this email already exists.");
    public static Error UserNotFound(Guid id) => Error.NotFound("Identity.User.NotFound", $"User '{id}' was not found.");
    public static readonly Error SlugTaken = Error.Conflict("Identity.Tenant.SlugTaken", "A tenant with this slug already exists.");
    public static Error TenantNotFound(Guid id) => Error.NotFound("Identity.Tenant.NotFound", $"Tenant '{id}' was not found.");
    public static readonly Error OwnerRoleMissing = Error.Failure("Identity.Tenant.OwnerRoleMissing", "Owner role template is not configured.");

    // Auth
    public static readonly Error InvalidCredentials = Error.Validation("Identity.Auth.InvalidCredentials", "Invalid email or password.");
    public static readonly Error UserInactive = Error.Validation("Identity.Auth.UserInactive", "This account is disabled.");
    public static readonly Error NotAMember = Error.NotFound("Identity.Auth.NotAMember", "User is not an active member of this tenant.");
    public static readonly Error InvalidRefreshToken = Error.Validation("Identity.Auth.InvalidRefreshToken", "Refresh token is invalid or expired.");

    // Membership / admin
    public static readonly Error AlreadyMember = Error.Conflict("Identity.Membership.AlreadyMember", "User is already a member of this tenant.");
    public static readonly Error MembershipNotFound = Error.NotFound("Identity.Membership.NotFound", "Membership was not found.");
    public static readonly Error TenantRoleNotFound = Error.NotFound("Identity.TenantRole.NotFound", "Tenant role was not found.");
}
