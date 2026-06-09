using SharedKernel;
namespace Clients.Domain;

/// <summary>Client engagement level.</summary>
public enum ActivityLevel { Low, Medium, High }
/// <summary>Client lifecycle status.</summary>
public enum ClientStatus { Active, Inactive }

/// <summary>Client contact information.</summary>
public sealed class Contact : ValueObject
{
    /// <summary>Contact name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Phone number.</summary>
    public string Phone { get; private set; } = string.Empty;
    /// <summary>Email address.</summary>
    public string? Email { get; private set; }
    private Contact() { }
    private Contact(string name, string phone, string? email) { Name = name; Phone = phone; Email = email; }
    /// <summary>Creates validated contact information.</summary>
    public static Result<Contact> Create(string? name, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name)) return ClientErrors.ContactNameRequired;
        if (string.IsNullOrWhiteSpace(phone)) return ClientErrors.PhoneRequired;
        if (!string.IsNullOrWhiteSpace(email) && (!email.Contains('@') || !email.Contains('.'))) return ClientErrors.EmailInvalid;
        return new Contact(name.Trim(), phone.Trim(), string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant());
    }
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Name; yield return Phone; yield return Email; }
}

/// <summary>Client aggregate root.</summary>
public sealed class Client : AggregateRoot
{
    /// <summary>Business name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Primary contact.</summary>
    public Contact Contact { get; private set; } = default!;
    /// <summary>Current account balance.</summary>
    public decimal AccountBalance { get; private set; }
    /// <summary>Engagement level.</summary>
    public ActivityLevel ActivityLevel { get; private set; }
    /// <summary>Lifecycle status.</summary>
    public ClientStatus Status { get; private set; }
    /// <summary>Optional notes.</summary>
    public string? Notes { get; private set; }
    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; private set; }
    /// <summary>Last update timestamp.</summary>
    public DateTime UpdatedAtUtc { get; private set; }
    private Client() { }
    private Client(Guid id, string name, Contact contact, decimal balance, ActivityLevel level, string? notes) : base(id)
    { Name = name; Contact = contact; AccountBalance = balance; ActivityLevel = level; Notes = notes; Status = ClientStatus.Active; CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow; }
    /// <summary>Creates a client.</summary>
    public static Result<Client> Create(string? name, Contact contact, decimal balance, ActivityLevel level, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) return ClientErrors.NameRequired;
        if (balance < 0) return ClientErrors.BalanceInvalid;
        return new Client(Guid.NewGuid(), name.Trim(), contact, balance, level, Normalize(notes));
    }
    /// <summary>Updates client details.</summary>
    public Result<Client> Update(string? name, Contact contact, decimal balance, ActivityLevel level, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) return ClientErrors.NameRequired;
        if (balance < 0) return ClientErrors.BalanceInvalid;
        Name = name.Trim(); Contact = contact; AccountBalance = balance; ActivityLevel = level; Notes = Normalize(notes); UpdatedAtUtc = DateTime.UtcNow; return this;
    }
    /// <summary>Changes lifecycle status.</summary>
    public Result<Client> SetStatus(ClientStatus status) { Status = status; UpdatedAtUtc = DateTime.UtcNow; return this; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
