using Clients.Domain;
using Microsoft.EntityFrameworkCore;
namespace Clients.Persistence.Seed;
/// <summary>Seeds Clients demo data.</summary>
public static class ClientsSeeder
{
    /// <summary>Seeds 15 clients once.</summary>
    public static async Task SeedAsync(IMainDbContext db, CancellationToken ct)
    {
        if (await db.Set<Client>().AnyAsync(ct).ConfigureAwait(false)) return;
        string[] names = ["شركة النور", "مصنع الأمل", "مؤسسة الريادة", "شركة الفجر", "مجموعة الصفوة", "تجارة الوفاء", "شركة البناء", "مؤسسة الإتقان", "شركة المستقبل", "مصنع الجودة", "مجموعة التعاون", "شركة النجاح", "مؤسسة السلام", "شركة الأصالة", "مصنع التميز"];
        for (var i = 0; i < names.Length; i++)
        {
            var contact = Contact.Create($"مسؤول {i + 1}", $"0100000{i:0000}", $"client{i + 1}@alfaris.local").Value;
            var client = Client.Create(names[i], contact, (i + 1) * 1250m, (ActivityLevel)(i % 3), $"بيانات تجريبية {i + 1}").Value;
            if (i % 4 == 0) client.SetStatus(ClientStatus.Inactive);
            db.Set<Client>().Add(client);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
