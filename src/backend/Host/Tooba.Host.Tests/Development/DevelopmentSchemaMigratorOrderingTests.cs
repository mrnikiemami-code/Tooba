using Microsoft.Extensions.DependencyInjection;
using Tooba.Host.Development;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests.Development;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-110 — تنظیم ترتیب DevelopmentSchemaMigrator.
/// مهاجرت‌های همهٔ ماژول‌ها باید پیش از هر گام پس از مهاجرت تمام شوند تا گامی که از طریق درزهای
/// Contracts به schema ماژول‌های دیرتر (مثل Party برای گام Catalog) نیاز دارد، شکست نخورد.
/// این تست‌ها فقط با مهاجرت‌گر و گام ساختگی کار می‌کنند: بدون DbContext، بدون دیتابیس.
/// </summary>
public sealed class DevelopmentSchemaMigratorOrderingTests
{
    [Fact]
    public async Task Post_migration_step_runs_only_after_every_registered_module_migration()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        // Catalog-like owner: its step needs a schema owned by the module that migrates later (Party-like).
        services.AddModuleSchemaMigrator("Owner", 1, (_, _) =>
        {
            trace.Add("migrate:Owner");
            return Task.CompletedTask;
        });
        services.AddModuleSchemaMigrator("Dependency", 6, (_, _) =>
        {
            trace.Add("migrate:Dependency");
            return Task.CompletedTask;
        });
        services.AddModuleSchemaMigrationStep("Owner", 1, (_, _) =>
        {
            if (!trace.Contains("migrate:Dependency", StringComparer.Ordinal))
            {
                throw new InvalidOperationException("post-migration step ran before its dependency schema migrated.");
            }

            trace.Add("step:Owner");
            return Task.CompletedTask;
        });

        using var provider = services.BuildServiceProvider();
        await DevelopmentSchemaMigrator.MigrateOrderedAsync(provider, CancellationToken.None);

        Assert.Equal(["migrate:Owner", "migrate:Dependency", "step:Owner"], trace);
    }

    [Fact]
    public async Task Migration_order_and_post_step_order_stay_canonical_and_run_exactly_once()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        // Registered out of order on purpose: the seam must sort by Order.
        services.AddModuleSchemaMigrator("Third", 9, (_, _) => Record(trace, "migrate:Third"));
        services.AddModuleSchemaMigrator("First", 2, (_, _) => Record(trace, "migrate:First"));
        services.AddModuleSchemaMigrator("Second", 6, (_, _) => Record(trace, "migrate:Second"));
        // Two steps share AfterOrder 6: tie-break stays (AfterOrder, then Module ordinal).
        services.AddModuleSchemaMigrationStep("Zulu", 6, (_, _) => Record(trace, "step:Zulu"));
        services.AddModuleSchemaMigrationStep("Alpha", 6, (_, _) => Record(trace, "step:Alpha"));
        services.AddModuleSchemaMigrationStep("First", 2, (_, _) => Record(trace, "step:First"));

        using var provider = services.BuildServiceProvider();
        await DevelopmentSchemaMigrator.MigrateOrderedAsync(provider, CancellationToken.None);

        Assert.Equal(
            [
                "migrate:First",
                "migrate:Second",
                "migrate:Third",
                "step:First",
                "step:Alpha",
                "step:Zulu",
            ],
            trace);
        Assert.Equal(trace.Count, trace.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Failing_migration_short_circuits_every_post_migration_step()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        services.AddModuleSchemaMigrator("First", 2, (_, _) => Record(trace, "migrate:First"));
        services.AddModuleSchemaMigrator("Broken", 6, (_, _) => throw new InvalidOperationException("schema failed"));
        services.AddModuleSchemaMigrationStep("First", 2, (_, _) => Record(trace, "step:First"));

        using var provider = services.BuildServiceProvider();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DevelopmentSchemaMigrator.MigrateOrderedAsync(provider, CancellationToken.None));

        Assert.Equal("schema failed", error.Message);
        Assert.DoesNotContain("step:First", trace);
        Assert.Equal(["migrate:First"], trace);
    }

    [Fact]
    public async Task Canceled_migration_short_circuits_every_post_migration_step()
    {
        var trace = new List<string>();
        using var cts = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddModuleSchemaMigrator("First", 2, (_, ct) =>
        {
            Record(trace, "migrate:First");
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });
        services.AddModuleSchemaMigrationStep("First", 2, (_, _) => Record(trace, "step:First"));

        using var provider = services.BuildServiceProvider();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DevelopmentSchemaMigrator.MigrateOrderedAsync(provider, cts.Token));

        Assert.Equal(["migrate:First"], trace);
    }

    [Fact]
    public async Task Step_without_a_registered_migration_order_keeps_its_previous_no_run_behavior()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        services.AddModuleSchemaMigrator("First", 2, (_, _) => Record(trace, "migrate:First"));
        services.AddModuleSchemaMigrationStep("Orphan", 42, (_, _) => Record(trace, "step:Orphan"));

        using var provider = services.BuildServiceProvider();
        await DevelopmentSchemaMigrator.MigrateOrderedAsync(provider, CancellationToken.None);

        Assert.Equal(["migrate:First"], trace);
    }

    [Fact]
    public async Task No_step_registration_still_migrates_every_module_once()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        services.AddModuleSchemaMigrator("First", 2, (_, _) => Record(trace, "migrate:First"));
        services.AddModuleSchemaMigrator("Second", 6, (_, _) => Record(trace, "migrate:Second"));

        using var provider = services.BuildServiceProvider();
        await DevelopmentSchemaMigrator.MigrateOrderedAsync(provider, CancellationToken.None);

        Assert.Equal(["migrate:First", "migrate:Second"], trace);
    }

    [Fact]
    public async Task Duplicate_migration_order_is_rejected_before_anything_runs()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        services.AddModuleSchemaMigrator("First", 2, (_, _) => Record(trace, "migrate:First"));
        services.AddModuleSchemaMigrator("Clash", 2, (_, _) => Record(trace, "migrate:Clash"));

        using var provider = services.BuildServiceProvider();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DevelopmentSchemaMigrator.MigrateOrderedAsync(provider, CancellationToken.None));

        Assert.Empty(trace);
    }

    private static Task Record(List<string> trace, string entry)
    {
        trace.Add(entry);
        return Task.CompletedTask;
    }
}
