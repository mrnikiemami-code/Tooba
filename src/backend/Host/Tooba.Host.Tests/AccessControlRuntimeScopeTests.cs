using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.AccessControl.Infrastructure;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.BuildingBlocks;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش runtime scope: فیلتر سفارش Mobile/Books، رد Category ناشناس، تقاطع سقف.
/// </summary>
[Collection("PostgresSerial")]
public sealed class AccessControlRuntimeScopeTests : IAsyncLifetime
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
                .WithDatabase("tooba_access_runtime")
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

    /// <summary>Category ناشناس در SetRolePermissions رد می‌شود.</summary>
    [SkippableFact]
    public async Task Unknown_category_scope_is_rejected()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateAccessDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var catalog = new FakeCatalogLookupGateway();
        var mobile = Guid.Parse("e5e5e5e5-e5e5-45e5-85e5-e5e5e5e5e5e5");
        catalog.AddCategory(mobile, "موبایل");
        var directory = new AccessControlDirectory(db, CreateInMemoryAuthz(), new AccessControlInstrumentation(), catalog);

        var seller = Guid.Parse("a1a1a1a1-a1a1-41a1-81a1-a1a1a1a1a1a1");
        var admin = Guid.Parse("17171717-1717-4171-8171-171717171717");
        var actor = Guid.Parse("c3c3c3c3-c3c3-43c3-83c3-c3c3c3c3c3c3");
        await directory.EnsureBootstrapAsync(admin, [seller], "tenant-test", CancellationToken.None);
        await directory.SetSellerCeilingAsync(
            seller,
            [
                ("order.view", true, AccessScopeKind.GlobalWithinOwner, null),
                ("accesscontrol.view", true, AccessScopeKind.GlobalWithinOwner, null),
                ("accesscontrol.manage", true, AccessScopeKind.GlobalWithinOwner, null),
            ],
            admin,
            "t1",
            CancellationToken.None);

        var owner = new AccessOwnerScope(AccessOwnerScopeKind.Seller, seller);
        var role = await directory.CreateRoleAsync(
            owner,
            new CreateAccessRoleCommand("op", "op", "d"),
            actor,
            "t2",
            CancellationToken.None);

        var fakeCategory = Guid.Parse("99999999-9999-4999-8999-999999999999");
        var ex = await Assert.ThrowsAsync<AccessControlException>(() =>
            directory.SetRolePermissionsAsync(
                role.Id,
                owner,
                [new RolePermissionGrant("order.view", AccessScopeKind.Category, fakeCategory, true)],
                actor,
                "t3",
                CancellationToken.None));
        Assert.Equal("access.scope.unknown_resource", ex.Code);
    }

    /// <summary>سقف Category فقط همان منبع را برای grant اجازه می‌دهد.</summary>
    [SkippableFact]
    public async Task Category_ceiling_intersection_blocks_outside_scope()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateAccessDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var mobile = Guid.Parse("e5e5e5e5-e5e5-45e5-85e5-e5e5e5e5e5e5");
        var books = Guid.Parse("f6f6f6f6-f6f6-46f6-86f6-f6f6f6f6f6f6");
        var catalog = new FakeCatalogLookupGateway();
        catalog.AddCategory(mobile, "موبایل");
        catalog.AddCategory(books, "کتاب");
        var directory = new AccessControlDirectory(db, CreateInMemoryAuthz(), new AccessControlInstrumentation(), catalog);

        var seller = Guid.Parse("a1a1a1a1-a1a1-41a1-81a1-a1a1a1a1a1a1");
        var admin = Guid.Parse("17171717-1717-4171-8171-171717171717");
        var actor = Guid.Parse("c3c3c3c3-c3c3-43c3-83c3-c3c3c3c3c3c3");
        await directory.EnsureBootstrapAsync(admin, [seller], "tenant-test", CancellationToken.None);

        await directory.SetSellerCeilingAsync(
            seller,
            [
                ("order.view", true, AccessScopeKind.Category, mobile),
                ("order.handle", true, AccessScopeKind.Category, mobile),
                ("accesscontrol.view", true, AccessScopeKind.GlobalWithinOwner, null),
                ("accesscontrol.manage", true, AccessScopeKind.GlobalWithinOwner, null),
            ],
            admin,
            "t1",
            CancellationToken.None);

        var owner = new AccessOwnerScope(AccessOwnerScopeKind.Seller, seller);
        var role = await directory.CreateRoleAsync(
            owner,
            new CreateAccessRoleCommand("mobile-op", "mobile-op", "d"),
            actor,
            "t2",
            CancellationToken.None);

        await directory.SetRolePermissionsAsync(
            role.Id,
            owner,
            [new RolePermissionGrant("order.view", AccessScopeKind.Category, mobile, true)],
            actor,
            "t3",
            CancellationToken.None);

        var denyBooks = await Assert.ThrowsAsync<AccessControlException>(() =>
            directory.SetRolePermissionsAsync(
                role.Id,
                owner,
                [new RolePermissionGrant("order.view", AccessScopeKind.Category, books, true)],
                actor,
                "t4",
                CancellationToken.None));
        Assert.Contains("ceiling", denyBooks.Code, StringComparison.Ordinal);

        var denyGlobal = await Assert.ThrowsAsync<AccessControlException>(() =>
            directory.SetRolePermissionsAsync(
                role.Id,
                owner,
                [new RolePermissionGrant("order.view", AccessScopeKind.GlobalWithinOwner, null, true)],
                actor,
                "t5",
                CancellationToken.None));
        Assert.Contains("ceiling", denyGlobal.Code, StringComparison.Ordinal);
    }

    /// <summary>لیست/جزئیات سفارش: Mobile allow، Books deny، mixed بدون نشت خط Books.</summary>
    [SkippableFact]
    public async Task Seller_order_list_and_detail_respect_category_scope()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        await using var accessDb = CreateAccessDb(cs);
        await using var orderDb = CreateOrderDb(cs);
        await accessDb.Database.MigrateAsync();
        await orderDb.Database.MigrateAsync();

        var mobile = Guid.Parse("e5e5e5e5-e5e5-45e5-85e5-e5e5e5e5e5e5");
        var books = Guid.Parse("f6f6f6f6-f6f6-46f6-86f6-f6f6f6f6f6f6");
        var seller = Guid.Parse("a1a1a1a1-a1a1-41a1-81a1-a1a1a1a1a1a1");
        var admin = Guid.Parse("17171717-1717-4171-8171-171717171717");
        var ownerActor = Guid.Parse("c3c3c3c3-c3c3-43c3-83c3-c3c3c3c3c3c3");
        var employee = Guid.Parse("d4d4d4d4-d4d4-44d4-84d4-d4d4d4d4d4d4");
        var catalog = new FakeCatalogLookupGateway();
        catalog.AddCategory(mobile, "موبایل");
        catalog.AddCategory(books, "کتاب");
        var access = new AccessControlDirectory(accessDb, CreateInMemoryAuthz(), new AccessControlInstrumentation(), catalog);
        await access.EnsureBootstrapAsync(admin, [seller], "tenant-test", CancellationToken.None);
        await access.SetSellerCeilingAsync(
            seller,
            [
                ("order.view", true, AccessScopeKind.GlobalWithinOwner, null),
                ("order.handle", true, AccessScopeKind.GlobalWithinOwner, null),
                ("accesscontrol.view", true, AccessScopeKind.GlobalWithinOwner, null),
                ("accesscontrol.manage", true, AccessScopeKind.GlobalWithinOwner, null),
            ],
            admin,
            "c1",
            CancellationToken.None);

        var owner = new AccessOwnerScope(AccessOwnerScopeKind.Seller, seller);
        var role = await access.CreateRoleAsync(
            owner,
            new CreateAccessRoleCommand("Mobile Order Operator", "mobile-order-op", "scoped"),
            ownerActor,
            "c2",
            CancellationToken.None);
        await access.SetRolePermissionsAsync(
            role.Id,
            owner,
            [
                new RolePermissionGrant("order.view", AccessScopeKind.Category, mobile, true),
                new RolePermissionGrant("order.handle", AccessScopeKind.Category, mobile, true),
            ],
            ownerActor,
            "c3",
            CancellationToken.None);
        await access.AssignRoleAsync(owner, employee, role.Id, ownerActor, "c4", CancellationToken.None);

        var now = DateTimeOffset.Parse("2026-08-27T12:00:00Z");
        var mobileOrderId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var booksOrderId = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var mixedOrderId = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var checkoutMobile = Guid.NewGuid();
        var checkoutBooks = Guid.NewGuid();
        var checkoutMixed = Guid.NewGuid();

        var mobileLine = OrderLine.FromCheckout(
            mobileOrderId, Guid.NewGuid(), Guid.NewGuid(), seller, 1, 100m, "IRR", true, Guid.NewGuid(), null,
            "Taxable", 0m, 0m, 100m, null, categoryIdSnapshot: mobile);
        var booksLine = OrderLine.FromCheckout(
            booksOrderId, Guid.NewGuid(), Guid.NewGuid(), seller, 1, 50m, "IRR", true, Guid.NewGuid(), null,
            "Taxable", 0m, 0m, 50m, null, categoryIdSnapshot: books);
        var mixedMobile = OrderLine.FromCheckout(
            mixedOrderId, Guid.NewGuid(), Guid.NewGuid(), seller, 1, 100m, "IRR", true, Guid.NewGuid(), null,
            "Taxable", 0m, 0m, 100m, null, categoryIdSnapshot: mobile);
        var mixedBooks = OrderLine.FromCheckout(
            mixedOrderId, Guid.NewGuid(), Guid.NewGuid(), seller, 1, 50m, "IRR", true, Guid.NewGuid(), null,
            "Taxable", 0m, 0m, 50m, null, categoryIdSnapshot: books);

        var soMobile = SellerOrder.Open(checkoutMobile, seller, "SO-MOBILE", OrderMode.OnlinePurchase, "IRR", [mobileLine]);
        var soBooks = SellerOrder.Open(checkoutBooks, seller, "SO-BOOKS", OrderMode.OnlinePurchase, "IRR", [booksLine]);
        var soMixed = SellerOrder.Open(checkoutMixed, seller, "SO-MIXED", OrderMode.OnlinePurchase, "IRR", [mixedMobile, mixedBooks]);

        orderDb.Checkouts.Add(CheckoutGroup.Submit(
            checkoutMobile, $"idem-{checkoutMobile:N}", Guid.NewGuid(), OrderMode.OnlinePurchase, Guid.NewGuid(), employee,
            "IR", "IRR", SalesChannel.Marketplace, [soMobile], now));
        orderDb.Checkouts.Add(CheckoutGroup.Submit(
            checkoutBooks, $"idem-{checkoutBooks:N}", Guid.NewGuid(), OrderMode.OnlinePurchase, Guid.NewGuid(), employee,
            "IR", "IRR", SalesChannel.Marketplace, [soBooks], now));
        orderDb.Checkouts.Add(CheckoutGroup.Submit(
            checkoutMixed, $"idem-{checkoutMixed:N}", Guid.NewGuid(), OrderMode.OnlinePurchase, Guid.NewGuid(), employee,
            "IR", "IRR", SalesChannel.Marketplace, [soMixed], now));
        await orderDb.SaveChangesAsync();

        var effective = await access.GetEffectiveAccessAsync(employee, owner, CancellationToken.None);
        Assert.Contains(effective.Permissions, p =>
            p.PermissionId == "order.view"
            && p.ScopeResourceId == mobile
            && p.ScopeDisplayName == "موبایل");

        var allowed = effective.Permissions
            .Where(p => p.PermissionId == "order.view" && p.ScopeKind == AccessScopeKind.Category && p.ScopeResourceId is not null)
            .Select(p => p.ScopeResourceId!.Value)
            .ToHashSet();
        Assert.DoesNotContain(effective.Permissions, p =>
            p.PermissionId == "order.view" && p.ScopeKind == AccessScopeKind.GlobalWithinOwner);

        var orders = await orderDb.SellerOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.SellerPartyId == seller)
            .ToListAsync();
        var visible = orders.Where(o =>
            o.Lines.Any(l => l.CategoryIdSnapshot is Guid cid && allowed.Contains(cid))).ToList();
        Assert.Contains(visible, o => o.SellerOrderId == mobileOrderId);
        Assert.DoesNotContain(visible, o => o.SellerOrderId == booksOrderId);
        Assert.Contains(visible, o => o.SellerOrderId == mixedOrderId);

        var mixed = Assert.Single(orders, o => o.SellerOrderId == mixedOrderId);
        var authorizedMixedLines = mixed.Lines
            .Where(l => l.CategoryIdSnapshot is Guid cid && allowed.Contains(cid))
            .ToList();
        Assert.Single(authorizedMixedLines);
        Assert.Equal(mobile, authorizedMixedLines[0].CategoryIdSnapshot);

        var booksOnly = Assert.Single(orders, o => o.SellerOrderId == booksOrderId);
        Assert.DoesNotContain(booksOnly.Lines, l => l.CategoryIdSnapshot is Guid cid && allowed.Contains(cid));
    }

    private static AccessControlDbContext CreateAccessDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, AccessControlDbContext.Schema, typeof(AccessControlDbContext));
        return new AccessControlDbContext(options.Options);
    }

    private static OrderDbContext CreateOrderDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }

    private static InMemoryAuthorizationAdapter CreateInMemoryAuthz()
    {
        var telemetry = new AuthorizationInstrumentation();
        var audit = new InMemoryAuthorizationSecurityEventSink();
        return new InMemoryAuthorizationAdapter(telemetry, audit);
    }
}
