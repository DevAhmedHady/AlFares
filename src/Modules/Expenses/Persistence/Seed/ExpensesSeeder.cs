using System.Text;
using Expenses.Domain;
using Microsoft.EntityFrameworkCore;

namespace Expenses.Persistence.Seed;

/// <summary>Creates and repairs default expense data.</summary>
public static class ExpensesSeeder
{
    /// <summary>Seeds defaults and repairs legacy encoded seed values.</summary>
    public static async Task SeedAsync(IMainDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        (string Name, ExpenseScope Scope)[] seeds =
        [
            ("مواد خام", ExpenseScope.General),
            ("رواتب وسلف", ExpenseScope.General),
            ("صيانة", ExpenseScope.General),
            ("نقل", ExpenseScope.General),
            ("مرافق", ExpenseScope.General),
            ("وقود", ExpenseScope.Car),
            ("صيانة سيارة", ExpenseScope.Car),
            ("تأمين", ExpenseScope.Car),
            ("رخصة", ExpenseScope.Car)
        ];

        var expenseTypes = await db.Set<ExpenseType>().ToListAsync(cancellationToken).ConfigureAwait(false);
        var expenses = await db.Set<Expense>().ToListAsync(cancellationToken).ConfigureAwait(false);
        var mergedTypeIds = new Dictionary<Guid, Guid>();
        var duplicateTypes = new List<ExpenseType>();

        foreach (var seed in seeds)
        {
            var matches = expenseTypes
                .Where(type => type.Name == seed.Name ||
                               type.Name == ToTrimmedLegacyMojibake(seed.Name) ||
                               DecodeLegacyMojibake(type.Name) == seed.Name)
                .OrderByDescending(type => type.Name == seed.Name)
                .ToList();

            var primary = matches.FirstOrDefault();
            if (primary is null)
            {
                primary = ExpenseType.Create(seed.Name, seed.Scope).Value;
                db.Set<ExpenseType>().Add(primary);
                expenseTypes.Add(primary);
            }
            else
            {
                primary.Update(seed.Name, seed.Scope, true);
            }

            foreach (var duplicate in matches.Skip(1))
            {
                mergedTypeIds[duplicate.Id] = primary.Id;
                duplicateTypes.Add(duplicate);
                expenseTypes.Remove(duplicate);
            }
        }

        foreach (var expense in expenses)
        {
            if (!mergedTypeIds.TryGetValue(expense.ExpenseTypeId, out var primaryTypeId))
                continue;

            var update = expense.Update(
                primaryTypeId, expense.Amount, expense.Date, expense.Payee, expense.Notes,
                expense.OwnerType, expense.OwnerId);
            if (update.IsFailure)
                throw new InvalidOperationException(update.Error.Description);
        }

        foreach (var duplicateType in duplicateTypes)
            db.Set<ExpenseType>().Remove(duplicateType);

        RepairSeededExpenses(expenses);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (expenses.Count != 0)
            return;

        var generalTypes = expenseTypes.Where(type => type.Scope == ExpenseScope.General).ToArray();
        for (var index = 0; index < 30; index++)
        {
            db.Set<Expense>().Add(Expense.Create(
                generalTypes[index % generalTypes.Length].Id,
                500 + (index * 125),
                DateOnly.FromDateTime(DateTime.UtcNow.Date).AddMonths(-(index % 6)).AddDays(-(index % 20)),
                $"مستفيد {index + 1}",
                $"مصروف تجريبي {index + 1}").Value);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void RepairSeededExpenses(IEnumerable<Expense> expenses)
    {
        for (var index = 1; index <= 30; index++)
        {
            var payee = $"مستفيد {index}";
            var notes = $"مصروف تجريبي {index}";
            foreach (var expense in expenses.Where(expense =>
                         DecodeLegacyMojibake(expense.Payee) == payee ||
                         DecodeLegacyMojibake(expense.Notes) == notes))
            {
                expense.Update(
                    expense.ExpenseTypeId, expense.Amount, expense.Date,
                    DecodeLegacyMojibake(expense.Payee) == payee ? payee : expense.Payee,
                    DecodeLegacyMojibake(expense.Notes) == notes ? notes : expense.Notes,
                    expense.OwnerType, expense.OwnerId);
            }
        }
    }

    private static string? DecodeLegacyMojibake(string? value)
    {
        if (value is null)
            return null;

        var decoded = value;
        if (ContainsArabic(decoded))
            return decoded;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var candidate = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(decoded));
            if (candidate == decoded || candidate.Contains('\uFFFD'))
                break;
            if (ContainsArabic(candidate))
                return candidate;
            decoded = candidate;
        }

        return decoded;
    }

    private static bool ContainsArabic(string value) =>
        value.Any(character => character is >= '\u0600' and <= '\u06FF');

    private static string ToTrimmedLegacyMojibake(string value) =>
        Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(value)).Trim();
}
