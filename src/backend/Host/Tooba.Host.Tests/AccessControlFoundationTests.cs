using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.AccessControl.Infrastructure;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Access Control: کاتالوگ، سقف، escalation، Mobile/Books.
/// </summary>
[Collection("PostgresSerial")]
public sealed class AccessControlFoundationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_access")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>مرز ماژول و مسیرهای HTTP.</summary>
    [Fact]
    public void AccessControl_module_boundary_static_checks()
    {
        Assert.Equal("access_control", AccessControlDbContext.Schema);
        Assert.True(PermissionCatalog.All.Count >= 40);
        Assert.Contains(PermissionCatalog.All, p => p.PermissionId == "accesscontrol.manage");
        Assert.False(PermissionCatalog.IsDelegable("admin.dashboard.view"));
        Assert.True(PermissionCatalog.IsDelegable("order.handle"));
        Assert.Contains(ToobaModuleComposition.Modules, module => module is AccessControlModule);

        var endpoints = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "AccessControl", "AccessControlEndpoints.cs"));
        Assert.Contains("/v1/admin/access-control", endpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/seller/access-control", endpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/sellers/{sellerId:guid}/access-control", endpoints, StringComparison.Ordinal);
        Assert.Contains("/scope-resources/categories", endpoints, StringComparison.Ordinal);
        Assert.Contains("/me/capabilities", endpoints, StringComparison.Ordinal);
    }

    /// <summary>سقف، منع escalation و Mobile ALLOW / Books DENY در لایهٔ مجوز.</summary>
    [SkippableFact]
    public async Task Ceiling_escalation_and_category_scope_policy()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();

        var authz = CreateInMemoryAuthz();
        var sellerA = Guid.Parse("a1a1a1a1-a1a1-41a1-81a1-a1a1a1a1a1a1");
        var sellerB = Guid.Parse("b2b2b2b2-b2b2-42b2-82b2-b2b2b2b2b2b2");
        var ownerActor = Guid.Parse("c3c3c3c3-c3c3-43c3-83c3-c3c3c3c3c3c3");
        var employee = Guid.Parse("d4d4d4d4-d4d4-44d4-84d4-d4d4d4d4d4d4");
        var mobileCategory = Guid.Parse("e5e5e5e5-e5e5-45e5-85e5-e5e5e5e5e5e5");
        var booksCategory = Guid.Parse("f6f6f6f6-f6f6-46f6-86f6-f6f6f6f6f6f6");
        var admin = Guid.Parse("17171717-1717-4171-8171-171717171717");
        var catalog = new FakeCatalogLookupGateway();
        catalog.AddCategory(mobileCategory, "موبایل");
        catalog.AddCategory(booksCategory, "کتاب");
        var directory = new AccessControlDirectory(db, authz, new AccessControlInstrumentation(), catalog);

        await directory.EnsureBootstrapAsync(admin, [sellerA, sellerB], "tenant-test", CancellationToken.None);

        var sellerOwner = new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerA);
        var foreign = new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerB);

        // ceiling without order.handle -> cannot grant
        await directory.SetSellerCeilingAsync(
            sellerA,
            PermissionCatalog.All.Where(p => p.Delegable && p.PermissionId != "order.handle")
                .Select(p => (p.PermissionId, true, AccessScopeKind.GlobalWithinOwner, (Guid?)null)).ToList(),
            admin,
            "t1",
            CancellationToken.None);

        var role = await directory.CreateRoleAsync(
            sellerOwner,
            new CreateAccessRoleCommand("Mobile Order Operator", "mobile-order-op", "scoped"),
            ownerActor,
            "t2",
            CancellationToken.None);

        var escalatePlatform = await Assert.ThrowsAsync<AccessControlException>(() =>
            directory.SetRolePermissionsAsync(
                role.Id,
                sellerOwner,
                [new RolePermissionGrant("admin.dashboard.view", AccessScopeKind.GlobalWithinOwner, null, true)],
                ownerActor,
                "t3a",
                CancellationToken.None));
        Assert.Contains("platform_permission", escalatePlatform.Code, StringComparison.Ordinal);

        var escalateCeiling = await Assert.ThrowsAsync<AccessControlException>(() =>
            directory.SetRolePermissionsAsync(
                role.Id,
                sellerOwner,
                [new RolePermissionGrant("order.handle", AccessScopeKind.Category, mobileCategory, true)],
                ownerActor,
                "t3b",
                CancellationToken.None));
        Assert.Contains("ceiling", escalateCeiling.Code, StringComparison.Ordinal);

        // enable ceiling for order.handle + order.view
        await directory.SetSellerCeilingAsync(
            sellerA,
            [
                ("order.view", true, AccessScopeKind.GlobalWithinOwner, null),
                ("order.handle", true, AccessScopeKind.GlobalWithinOwner, null),
                ("accesscontrol.view", true, AccessScopeKind.GlobalWithinOwner, null),
                ("accesscontrol.manage", true, AccessScopeKind.GlobalWithinOwner, null),
            ],
            admin,
            "t4",
            CancellationToken.None);

        await directory.SetRolePermissionsAsync(
            role.Id,
            sellerOwner,
            [
                new RolePermissionGrant("order.view", AccessScopeKind.Category, mobileCategory, true),
                new RolePermissionGrant("order.handle", AccessScopeKind.Category, mobileCategory, true),
            ],
            ownerActor,
            "t5",
            CancellationToken.None);

        await directory.AssignRoleAsync(sellerOwner, employee, role.Id, ownerActor, "t6", CancellationToken.None);

        var effective = await directory.GetEffectiveAccessAsync(employee, sellerOwner, CancellationToken.None);
        Assert.Contains(effective.Permissions, p => p.PermissionId == "order.handle" && p.ScopeResourceId == mobileCategory);

        // foreign seller cannot see role
        await Assert.ThrowsAsync<AccessControlException>(() =>
            directory.GetRoleAsync(role.Id, foreign, CancellationToken.None));

        // SpiceDB/InMemory category policy
        var mobileAllow = await authz.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(employee),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Category, Id = mobileCategory.ToString("D") },
                Permission = AuthorizationRelations.HandleOrders,
                CallContext = new AuthorizationCallContext { Edition = ToobaEdition.SingleStore, TenantId = "tenant-test" },
            },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Allow, mobileAllow.Kind);

        var booksDeny = await authz.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(employee),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Category, Id = booksCategory.ToString("D") },
                Permission = AuthorizationRelations.HandleOrders,
                CallContext = new AuthorizationCallContext { Edition = ToobaEdition.SingleStore, TenantId = "tenant-test" },
            },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Deny, booksDeny.Kind);

        // revoke ceiling -> effective deny after sync
        await directory.SetSellerCeilingAsync(
            sellerA,
            [("accesscontrol.view", true, AccessScopeKind.GlobalWithinOwner, null)],
            admin,
            "t7",
            CancellationToken.None);
        var afterRevoke = await directory.GetEffectiveAccessAsync(employee, sellerOwner, CancellationToken.None);
        Assert.DoesNotContain(afterRevoke.Permissions, p => p.PermissionId == "order.handle");

        var systemRoles = await directory.ListRolesAsync(sellerOwner, false, CancellationToken.None);
        var system = Assert.Single(systemRoles, r => r.Code == "seller-owner");
        await Assert.ThrowsAsync<AccessControlException>(() =>
            directory.UpdateRoleAsync(system.Id, sellerOwner, new UpdateAccessRoleCommand("x", "y"), ownerActor, "t8", CancellationToken.None));
    }

    private static AccessControlDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, AccessControlDbContext.Schema, typeof(AccessControlDbContext));
        return new AccessControlDbContext(options.Options);
    }

    private static InMemoryAuthorizationAdapter CreateInMemoryAuthz()
    {
        var telemetry = new AuthorizationInstrumentation();
        var audit = new InMemoryAuthorizationSecurityEventSink();
        return new InMemoryAuthorizationAdapter(telemetry, audit);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }
}
