using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T022-R20: فهرست/ساخت/انتشار Store-scoped کمپین Admin.
/// </summary>
[Collection("PostgresSerial")]
public sealed class MerchandisingCampaignAdminTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_merch_admin")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public void Admin_endpoints_hide_raw_amazing_code_and_qualifiers()
    {
        var root = FindRepoRoot();
        var fe = File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "admin", "campaigns", "admin-campaign-workspace.tsx"));
        Assert.DoesNotContain("AMAZING", fe);
        Assert.DoesNotContain("QualifierKind", fe);
        Assert.Contains("کمپین", File.ReadAllText(Path.Combine(root, "src", "frontend", "app", "admin", "admin-chrome-messages.ts")));
    }

    [SkippableFact]
    public async Task Admin_list_create_publish_schedule_and_archive_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "store-alpha"));
        await using var promoDb = CreatePromotionDb(cs, commerce);
        await promoDb.Database.MigrateAsync();
        var merch = new MerchandisingCampaignDirectory(promoDb);
        var storeId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");
        var type = await merch.EnsureAmazingTypeSeededAsync(CancellationToken.None);
        var now = DateTimeOffset.UtcNow;

        var draft = await merch.CreateCampaignAsync(type.Id, storeId, now.AddHours(-1), now.AddDays(2), 10, CancellationToken.None);
        await merch.UpsertCampaignTranslationAsync(draft.Id, "fa-IR", "کمپین تست ادمین", null, "Amazing", CancellationToken.None);
        await merch.UpsertCampaignTranslationAsync(draft.Id, "en-US", "Admin Test Campaign", null, "Amazing", CancellationToken.None);

        var (rows, total) = await merch.ListCampaignsAsync(storeId, "تست", MerchandisingCampaignLifecycleStatus.Draft, null, null, now, "fa-IR", 0, 20, CancellationToken.None);
        Assert.True(total >= 1);
        Assert.Contains(rows, r => r.Id == draft.Id && r.Title!.Contains("تست"));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            merch.UpdateCampaignWindowAsync(draft.Id, now.AddDays(2), now.AddDays(1), 1, CancellationToken.None));

        var future = await merch.CreateCampaignAsync(type.Id, storeId, now.AddDays(1), now.AddDays(3), 5, CancellationToken.None);
        await merch.UpsertCampaignTranslationAsync(future.Id, "fa-IR", "آینده", null, null, CancellationToken.None);
        await merch.PublishCampaignAsync(future.Id, CancellationToken.None);
        var (scheduled, _) = await merch.ListCampaignsAsync(storeId, null, null, null, "scheduled", now, "fa-IR", 0, 50, CancellationToken.None);
        Assert.Contains(scheduled, r => r.Id == future.Id && r.RuntimeLabel == "scheduled");

        await merch.PublishCampaignAsync(draft.Id, CancellationToken.None);
        var (active, _) = await merch.ListCampaignsAsync(storeId, null, null, null, "active", now, "fa-IR", 0, 50, CancellationToken.None);
        Assert.Contains(active, r => r.Id == draft.Id && r.RuntimeLabel == "active");

        await merch.ArchiveCampaignAsync(draft.Id, CancellationToken.None);
        var archived = await merch.GetCampaignAsync(draft.Id, storeId, CancellationToken.None);
        Assert.Equal(MerchandisingCampaignLifecycleStatus.Archived, archived!.LifecycleStatus);
        var activeWinner = await merch.ResolveActiveCampaignAsync(storeId, MerchandisingPromotionType.AmazingCode, now, CancellationToken.None);
        Assert.True(activeWinner is null || activeWinner.Id != draft.Id);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root");
    }

    private static PromotionDbContext CreatePromotionDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PromotionOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PromotionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PromotionDbContext.Schema, typeof(PromotionDbContext));
        options.AddInterceptors(interceptor);
        return new PromotionDbContext(options.Options);
    }
}
