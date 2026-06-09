using SharedKernel;
namespace Expenses.Domain;
/** <summary>Expense errors.</summary> */
public static class ExpenseErrors
{
    /** <summary>Category required.</summary> */
    public static readonly Error CategoryRequired=Error.Validation("expenses.category_required","Category is required.");
    /** <summary>Amount invalid.</summary> */
    public static readonly Error AmountInvalid=Error.Validation("expenses.amount_invalid","Amount must be positive.");
    /** <summary>Payee required.</summary> */
    public static readonly Error PayeeRequired=Error.Validation("expenses.payee_required","Payee is required.");
    /** <summary>Not found.</summary> */
    public static Error NotFound(Guid id)=>Error.NotFound("expenses.not_found",$"Expense '{id}' was not found.");
}

