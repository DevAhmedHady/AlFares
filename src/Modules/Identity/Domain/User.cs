using SharedKernel;

namespace Identity.Domain;

public sealed class User : AggregateRoot
{
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private User() { } // EF materialization

    private User(Guid id, Email email, string displayName, DateTime createdAtUtc)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = string.Empty; // set immediately via SetPasswordHash
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<User> Create(Email email, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return IdentityErrors.DisplayNameEmpty;

        return new User(Guid.NewGuid(), email, displayName.Trim(), DateTime.UtcNow);
    }

    // Hash is produced by the application layer (PasswordHasher<User>), which needs the instance.
    public void SetPasswordHash(string hash) => PasswordHash = hash;
}
