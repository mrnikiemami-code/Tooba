using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش پنل فروشنده/ادمین پروموشن و اعمال کوپن در ارزیابی.
/// </summary>
[Collection("PostgresSerial")]
public sealed class PromotionPanelTests : IAsyncLifetime
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
                .WithDatabase("tooba_promo_panel")
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

    /// <summary>
    /// فروشنده فقط پروموشن خود را می‌سازد/می‌بیند؛ فروشندهٔ خارجی رد می‌شود؛ ادمین فهرست و غیرفعال می‌کند.
    /// </summary>
    [SkippableFact]
    public async Task Seller_own_create_list_activate_and_admin_deactivate_with_foreign_deny()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-promo-panel", "tenant-promo-panel"));
        await using var db = CreateDb(_container.GetConnectionString(), commerce);
        await db.Database.MigrateAsync();
        var dir = new PromotionDirectory(db, new OpenPromotionUseCaseGuard(), new DeferredPromotionRedemptionLedger());

        var sellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var sellerB = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c6");
        var at = DateTimeOffset.Parse("2026-06-01T00:00:00Z");

        var created = await dir.CreateForSellerAsync(
            null,
            sellerA,
            "کوپن تابستان",
            at.AddDays(-1),
            at.AddMonths(1),
            PromotionDiscountKind.PercentageOff,
            0.15m,
            0m,
            null,
            "SUMMER15",
            50000m,
            CancellationToken.None);
        Assert.Equal(sellerA, created.SellerPartyId);
        Assert.Equal(PromotionStatus.Draft, created.Status);
        Assert.Equal("SUMMER15", created.CouponCode);

        var listA = await dir.ListBySellerAsync(null, sellerA, CancellationToken.None);
        Assert.Contains(listA, x => x.PromotionId == created.PromotionId);
        var listB = await dir.ListBySellerAsync(null, sellerB, CancellationToken.None);
        Assert.DoesNotContain(listB, x => x.PromotionId == created.PromotionId);
        Assert.Null(await dir.GetForSellerAsync(null, sellerB, created.PromotionId, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.ActivateForSellerAsync(null, sellerB, created.PromotionId, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.UpdateForSellerAsync(
                null,
                sellerB,
                created.PromotionId,
                "هک",
                at,
                null,
                PromotionDiscountKind.PercentageOff,
                0.99m,
                0m,
                null,
                "HACK",
                null,
                CancellationToken.None));

        await dir.ActivateForSellerAsync(null, sellerA, created.PromotionId, CancellationToken.None);
        var active = await dir.GetForSellerAsync(null, sellerA, created.PromotionId, CancellationToken.None);
        Assert.Equal(PromotionStatus.Active, active!.Status);

        var offer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var variant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var withCoupon = await dir.EvaluateAsync(
            new PromotionEvaluationRequest(
                offer,
                variant,
                null,
                sellerA,
                "IR",
                "Marketplace",
                "IRR",
                1,
                100000m,
                null,
                null,
                "summer15",
                at),
            CancellationToken.None);
        Assert.Equal(15000m, withCoupon.DiscountAmount);
        Assert.Single(withCoupon.Applied);
        Assert.Equal(created.PromotionId, withCoupon.Applied[0].PromotionId);

        var withoutCoupon = await dir.EvaluateAsync(
            new PromotionEvaluationRequest(
                offer,
                variant,
                null,
                sellerA,
                "IR",
                "Marketplace",
                "IRR",
                1,
                100000m,
                null,
                null,
                null,
                at),
            CancellationToken.None);
        Assert.Equal(0m, withoutCoupon.DiscountAmount);

        var adminAll = await dir.ListForAdminAsync(null, null, CancellationToken.None);
        Assert.Contains(adminAll, x => x.PromotionId == created.PromotionId);
        var adminFiltered = await dir.ListForAdminAsync(null, sellerA, CancellationToken.None);
        Assert.All(adminFiltered, x => Assert.Equal(sellerA, x.SellerPartyId));

        await dir.DeactivateForAdminAsync(null, created.PromotionId, CancellationToken.None);
        var expired = await dir.GetForAdminAsync(null, created.PromotionId, CancellationToken.None);
        Assert.Equal(PromotionStatus.Expired, expired!.Status);
        var afterExpire = await dir.EvaluateAsync(
            new PromotionEvaluationRequest(
                offer,
                variant,
                null,
                sellerA,
                "IR",
                "Marketplace",
                "IRR",
                1,
                100000m,
                null,
                null,
                "SUMMER15",
                at),
            CancellationToken.None);
        Assert.Equal(0m, afterExpire.DiscountAmount);
    }

    /// <summary>
    /// مسیرهای Host پروموشن و دسترسی پنل در سورس ثبت شده‌اند.
    /// </summary>
    [Fact]
    public void Host_registers_seller_and_admin_promotion_routes_with_panel_access()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Promotion", "PromotionEndpoints.cs"));
        var program = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        var checkout = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontCheckoutComposer.cs"));
        Assert.Contains("/v1/seller/promotions", endpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/promotions", endpoints, StringComparison.Ordinal);
        Assert.Contains("SellerPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPromotionEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("couponCode", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("CouponCode: null", checkout, StringComparison.Ordinal);
    }

    private static PromotionDbContext CreateDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PromotionOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PromotionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PromotionDbContext.Schema, typeof(PromotionDbContext));
        options.AddInterceptors(interceptor);
        return new PromotionDbContext(options.Options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
