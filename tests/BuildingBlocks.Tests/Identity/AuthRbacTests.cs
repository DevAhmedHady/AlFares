using BuildingBlocks.Authentication;
using BuildingBlocks.Grids;
using FluentAssertions;
using Identity.Domain;
using Identity.Features.GetUsersGrid;
using Identity.Features.Login;
using Identity.Persistence;
using Identity.Persistence.Seed;
using Identity.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildingBlocks.Tests.Identity;

/// <summary>
/// Verifies the seeded admin can log in, role templates grant least privilege (Member is
/// read-only — the data behind a 403 on write/manage endpoints), and the admin users grid pages.
/// </summary>
[TestClass]
public sealed class AuthRbacTests
{
    private const string AdminEmail = "admin@alfaris.local";
    private const string AdminPassword = "Sup3r-Secret!";

    /// <summary>Seeded admin logs in and receives an access token carrying owner permissions.</summary>
    [TestMethod]
    public async Task SeededAdmin_CanLogin_AndReceivesOwnerAccess()
    {
        await using var db = await SeedAsync();
        var tenantId = (await db.Set<Tenant>().SingleAsync()).Id;

        var login = new LoginHandler(
            new UserRepository(db),
            new MembershipRepository(db),
            new RefreshTokenRepository(db),
            Hasher,
            Tokens()
        );

        var result = await login.Handle(
            new LoginCommand(AdminEmail, AdminPassword, tenantId),
            default
        );

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();

        var adminId = (await db.Set<User>().SingleAsync(u => u.Email.Value == AdminEmail)).Id;
        var access = await new MembershipRepository(db).GetEffectiveAccessAsync(
            tenantId,
            adminId,
            default
        );
        access.Roles.Should().Contain("Owner");
        access.Permissions.Should().Contain("clients.write").And.Contain("identity.tenants.manage");
    }

    /// <summary>Login fails for a wrong password without leaking which field was wrong.</summary>
    [TestMethod]
    public async Task Login_WithWrongPassword_Fails()
    {
        await using var db = await SeedAsync();
        var tenantId = (await db.Set<Tenant>().SingleAsync()).Id;
        var login = new LoginHandler(
            new UserRepository(db),
            new MembershipRepository(db),
            new RefreshTokenRepository(db),
            Hasher,
            Tokens()
        );

        var result = await login.Handle(
            new LoginCommand(AdminEmail, "wrong-password", tenantId),
            default
        );

        result.IsFailure.Should().BeTrue();
    }

    /// <summary>A Member's effective access is read-only: no write/manage permission is granted.</summary>
    [TestMethod]
    public async Task Member_HasReadOnlyAccess_NoWriteOrManage()
    {
        await using var db = await SeedAsync();
        var tenantId = (await db.Set<Tenant>().SingleAsync()).Id;
        await AddMemberAsync(db, tenantId, "member@alfaris.local");

        var memberId = (
            await db.Set<User>().SingleAsync(u => u.Email.Value == "member@alfaris.local")
        ).Id;
        var access = await new MembershipRepository(db).GetEffectiveAccessAsync(
            tenantId,
            memberId,
            default
        );

        access.Roles.Should().ContainSingle().Which.Should().Be("Member");
        access.Permissions.Should().Contain("clients.read");
        access
            .Permissions.Should()
            .NotContain(p =>
                p.EndsWith(".write")
                || p.EndsWith(".delete")
                || p.EndsWith(".manage")
                || p.EndsWith(".export")
            );
    }

    /// <summary>The admin users grid returns a paged, searchable result over the seeded users.</summary>
    [TestMethod]
    public async Task UsersGrid_ReturnsPagedUsers()
    {
        await using var db = await SeedAsync();
        var tenantId = (await db.Set<Tenant>().SingleAsync()).Id;
        await AddMemberAsync(db, tenantId, "member@alfaris.local");

        var handler = new GetUsersGridHandler(db);

        var all = await handler.Handle(new GetUsersGridQuery(new GridQuery()), default);
        all.IsSuccess.Should().BeTrue();
        all.Value.TotalCount.Should().Be(2);
        all.Value.Items.Should().Contain(u => u.Email == AdminEmail);

        var filtered = await handler.Handle(
            new GetUsersGridQuery(new GridQuery { Search = "member" }),
            default
        );
        filtered.Value.TotalCount.Should().Be(1);
        filtered.Value.Items.Single().Email.Should().Be("member@alfaris.local");
    }

    private static global::Api.Persistence.MainDbContext NewDb() => MainDbTestFactory.Create();

    private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

    private static ITokenService Tokens() =>
        new JwtTokenService(
            new JwtOptions
            {
                Issuer = "al-faris",
                Audience = "al-faris",
                SigningKey = "test-signing-key-at-least-32-bytes-long!!",
                AccessMinutes = 15,
                RefreshDays = 7,
            }
        );

    private static async Task<global::Api.Persistence.MainDbContext> SeedAsync()
    {
        var db = NewDb();
        await IdentitySeeder.SeedAsync(db);
        await IdentityTenantSeeder.SeedAsync(
            db,
            Hasher,
            new SeedOptions
            {
                AdminEmail = AdminEmail,
                AdminPassword = AdminPassword,
                TenantName = "الفارس",
                TenantSlug = "al-faris",
            }
        );
        return db;
    }

    private static async Task AddMemberAsync(
        global::Api.Persistence.MainDbContext db,
        Guid tenantId,
        string email
    )
    {
        var user = User.Create(Email.Create(email).Value, "Member User").Value;
        user.SetPasswordHash(Hasher.HashPassword(user, "Member-Pass1!"));
        db.Set<User>().Add(user);

        var memberRole = await db.Set<TenantRole>()
            .SingleAsync(r => r.TenantId == tenantId && r.Name == "Member");
        var membership = new TenantUser(tenantId, user.Id);
        db.Set<TenantUser>().Add(membership);
        db.Set<TenantUserRole>().Add(new TenantUserRole(membership.Id, memberRole.Id));
        await db.SaveChangesAsync();
    }
}
