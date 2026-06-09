using Expenses.Contracts; using Expenses.Domain; using Mapster;
namespace Expenses.Mapping; /** <summary>Expense mappings.</summary> */ public sealed class ExpensesMappingConfig:IRegister{/** <inheritdoc/> */ public void Register(TypeAdapterConfig config)=>config.NewConfig<Expense,ExpenseResponse>();}
