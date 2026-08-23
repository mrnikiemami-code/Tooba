using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Pricing.Domain;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation پروموشن: جدا از Pricing، ترکیب قطعی، کوپن، ایزولهٔ Tenant و تصویر سفارش.
/// </summary>
[Collection("PostgresSerial")]
public sealed class PromotionFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_promo_a")
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
    /// پروموشن قیمت تألیف‌شده را مالک نیست و SDKهای بیگانه در Domain/Application نیستند.
    /// </summary>
    [Fact]
    public void Promotion_does_not_own_authored_price_and_keeps_module_boundaries()
    {
        Assert.DoesNotContain("DiscountAmount", typeof(AuthoredPrice).GetProperties().Select(p => p.Name));
        Assert.Equal("promotion", PromotionDbContext.Schema);
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Promotion", "Tooba.Promotion.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Promotion", "Tooba.Promotion.Application"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Stripe", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Pricing.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Order.Infrastructure", csproj, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Tooba.Pricing.Infrastructure", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Promotion", "Tooba.Promotion.Infrastructure", "Tooba.Promotion.Infrastructure.csproj")), StringComparison.Ordinal);
        Assert.Contains("IPromotionEvaluator", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Promotion", "Tooba.Promotion.Application", "PromotionContracts.cs")));
    }

    /// <summary>
    /// درصد، مبلغ ثابت، ارز، تاریخ، کوپن، ترکیب و ایزولهٔ Tenant روی Postgres.
    /// </summary>
    [SkippableFact]
    public async Task Promotion_evaluation_is_deterministic_and_tenant_isolated_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var exists = admin.CreateCommand();
            exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_promo_b'";
            if (await exists.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_promo_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_promo_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-promo-a", "tenant-promo-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-promo-b", "tenant-promo-b"));
        await using var dbA = CreateDb(csA, commerceA);
        await using var dbB = CreateDb(csB, commerceB);
        await dbA.Database.MigrateAsync();
        await dbB.Database.MigrateAsync();
        var dirA = new PromotionDirectory(dbA, new OpenPromotionUseCaseGuard(), new DeferredPromotionRedemptionLedger());
        var dirB = new PromotionDirectory(dbB, new OpenPromotionUseCaseGuard(), new DeferredPromotionRedemptionLedger());
        var at = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        var offer = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var variant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var seller = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");

        var percent = await dirA.CreateAsync("ده درصد", 20, at.AddMonths(-1), null, PromotionStackingPolicy.Stackable, PromotionDiscountKind.PercentageOff, 0.10m, 0m, null, null, offer, null, null, null, "IR", "Marketplace", "IRR", null, null, null, null, CancellationToken.None);
        await dirA.ActivateAsync(percent.PromotionId, CancellationToken.None);
        var applied = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, null), CancellationToken.None);
        Assert.Equal(10000m, applied.DiscountAmount);
        Assert.Equal(90000m, applied.PostDiscountTaxExclusiveAmount);

        var mismatch = await dirA.CreateAsync("مبلغ دلار", 50, at.AddMonths(-1), null, PromotionStackingPolicy.Stackable, PromotionDiscountKind.FixedAmountOff, 0m, 5m, "USD", null, offer, null, null, null, "IR", "Marketplace", null, null, null, null, null, CancellationToken.None);
        await dirA.ActivateAsync(mismatch.PromotionId, CancellationToken.None);
        var usd = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, null), CancellationToken.None);
        Assert.Equal(10000m, usd.DiscountAmount);
        Assert.Contains("CURRENCY_MISMATCH", usd.RejectionReasons);

        var exclusive = await dirA.CreateAsync("انحصاری بیست", 30, at.AddMonths(-1), null, PromotionStackingPolicy.Exclusive, PromotionDiscountKind.PercentageOff, 0.20m, 0m, null, null, offer, null, null, null, "IR", "Marketplace", "IRR", null, null, null, null, CancellationToken.None);
        await dirA.ActivateAsync(exclusive.PromotionId, CancellationToken.None);
        var onlyExclusive = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, null), CancellationToken.None);
        Assert.Equal(20000m, onlyExclusive.DiscountAmount);
        Assert.Single(onlyExclusive.Applied);
        Assert.Equal(exclusive.PromotionId, onlyExclusive.Applied[0].PromotionId);

        var coupon = await dirA.CreateAsync("کوپن", 40, at.AddMonths(-1), null, PromotionStackingPolicy.Exclusive, PromotionDiscountKind.PercentageOff, 0.50m, 0m, null, "  save-10 ", offer, null, null, null, "IR", "Marketplace", "IRR", null, null, null, null, CancellationToken.None);
        await dirA.ActivateAsync(coupon.PromotionId, CancellationToken.None);
        var withoutCode = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, null), CancellationToken.None);
        Assert.Equal(20000m, withoutCode.DiscountAmount);
        var withCode = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, "save-10"), CancellationToken.None);
        Assert.Equal(50000m, withCode.DiscountAmount);
        var badCode = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, "nope"), CancellationToken.None);
        Assert.Equal(20000m, badCode.DiscountAmount);

        var future = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, DateTimeOffset.Parse("2020-01-01T00:00:00Z"), "save-10"), CancellationToken.None);
        Assert.Equal(0m, future.DiscountAmount);

        await dirA.ExpireAsync(coupon.PromotionId, CancellationToken.None);
        var afterExpire = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, "SAVE-10"), CancellationToken.None);
        Assert.Equal(20000m, afterExpire.DiscountAmount);

        var otherOffer = await dirA.EvaluateAsync(Eval(Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"), variant, seller, 100000m, at, null), CancellationToken.None);
        Assert.Equal(0m, otherOffer.DiscountAmount);

        var cloned = await dirB.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, "SAVE-10"), CancellationToken.None);
        Assert.Equal(0m, cloned.DiscountAmount);

        var floor = await dirA.CreateAsync("سقف", 90, at.AddMonths(-1), null, PromotionStackingPolicy.Exclusive, PromotionDiscountKind.FixedAmountOff, 0m, 500000m, "IRR", null, offer, null, null, null, "IR", "Marketplace", "IRR", null, null, null, null, CancellationToken.None);
        await dirA.ActivateAsync(floor.PromotionId, CancellationToken.None);
        var capped = await dirA.EvaluateAsync(Eval(offer, variant, seller, 100000m, at, null), CancellationToken.None);
        Assert.Equal(100000m, capped.DiscountAmount);
        Assert.Equal(0m, capped.PostDiscountTaxExclusiveAmount);
    }

    private static PromotionEvaluationRequest Eval(
        Guid offerId,
        Guid variantId,
        Guid sellerId,
        decimal amount,
        DateTimeOffset at,
        string? coupon) =>
        new(offerId, variantId, null, sellerId, "IR", "Marketplace", "IRR", 1, amount, null, null, coupon, at);

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
