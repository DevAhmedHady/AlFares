using SharedKernel;
namespace Expenses.Domain;
/** <summary>Expense aggregate.</summary> */
public sealed class Expense : AggregateRoot
{
    /** <summary>Category.</summary> */
    public string Category { get; private set; } = string.Empty;
    /** <summary>Amount.</summary> */
    public decimal Amount { get; private set; }
    /** <summary>Expense date.</summary> */
    public DateOnly Date { get; private set; }
    /** <summary>Payee.</summary> */
    public string Payee { get; private set; } = string.Empty;
    /** <summary>Notes.</summary> */
    public string? Notes { get; private set; }
    /** <summary>Created UTC.</summary> */
    public DateTime CreatedAtUtc { get; private set; }
    /** <summary>Updated UTC.</summary> */
    public DateTime UpdatedAtUtc { get; private set; }
    private Expense() { }
    private Expense(string category,decimal amount,DateOnly date,string payee,string? notes):base(Guid.NewGuid())
    { Category=category; Amount=amount; Date=date; Payee=payee; Notes=notes; CreatedAtUtc=UpdatedAtUtc=DateTime.UtcNow; }
    /** <summary>Creates expense.</summary> */
    public static Result<Expense> Create(string? category,decimal amount,DateOnly date,string? payee,string? notes)
    { if(string.IsNullOrWhiteSpace(category))return ExpenseErrors.CategoryRequired; if(amount<=0)return ExpenseErrors.AmountInvalid; if(string.IsNullOrWhiteSpace(payee))return ExpenseErrors.PayeeRequired; return new Expense(category.Trim(),amount,date,payee.Trim(),Normalize(notes)); }
    /** <summary>Updates expense.</summary> */
    public Result<Expense> Update(string? category,decimal amount,DateOnly date,string? payee,string? notes)
    { if(string.IsNullOrWhiteSpace(category))return ExpenseErrors.CategoryRequired; if(amount<=0)return ExpenseErrors.AmountInvalid; if(string.IsNullOrWhiteSpace(payee))return ExpenseErrors.PayeeRequired; Category=category.Trim(); Amount=amount; Date=date; Payee=payee.Trim(); Notes=Normalize(notes); UpdatedAtUtc=DateTime.UtcNow; return this; }
    private static string? Normalize(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}

