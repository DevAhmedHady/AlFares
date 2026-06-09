using SharedKernel;
namespace Clients.Domain;
/// <summary>Clients domain errors.</summary>
public static class ClientErrors
{
    /// <summary>Name required.</summary>
    public static readonly Error NameRequired = Error.Validation("clients.name_required", "Client name is required.");
    /// <summary>Contact name required.</summary>
    public static readonly Error ContactNameRequired = Error.Validation("clients.contact_name_required", "Contact name is required.");
    /// <summary>Phone required.</summary>
    public static readonly Error PhoneRequired = Error.Validation("clients.phone_required", "Phone is required.");
    /// <summary>Email invalid.</summary>
    public static readonly Error EmailInvalid = Error.Validation("clients.email_invalid", "Email is invalid.");
    /// <summary>Balance invalid.</summary>
    public static readonly Error BalanceInvalid = Error.Validation("clients.balance_invalid", "Balance cannot be negative.");
    /// <summary>Client not found.</summary>
    public static Error NotFound(Guid id) => Error.NotFound("clients.not_found", $"Client '{id}' was not found.");
}
