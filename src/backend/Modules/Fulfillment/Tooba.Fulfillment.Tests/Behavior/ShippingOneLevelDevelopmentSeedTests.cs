using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-120 — focused proof for the Fulfillment-owned Development bootstrap that
/// invokes the EXISTING one-level backfill. The bootstrap is a pure orchestration seam: it adds no
/// mapping rule and no duplicate algorithm, it reuses <see cref="ShippingMethodBackfillPlanner"/> and
/// <see cref="IShippingOneLevelBackfillStore"/>. These facts pin the three acceptance clauses without a
/// live Host or a scratch database: population from a canonical legacy source with identity/localized
/// labels preserved, idempotent rerun, and fail-closed behaviour for a disabled gate, a non-Development
/// environment and a non-executable plan.
/// </summary>
public sealed class ShippingOneLevelDevelopmentSeedTests
{
    private static readonly Guid LangFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid LangEn = Guid.Parse("01900000-0000-7000-8000-00000000aa02");

    [Fact]
    public async Task Enabled_development_run_populates_the_catalog_from_the_legacy_source()
    {
        var seed = new RecordingSeedPort();
        var reader = new RecordingCatalogReader(LiveCatalog());
        var store = new RecordingBackfillStore();

        await using var provider = Build(
            new FakeEnvironment("Development"), seed, reader, store, enabled: true);

        await ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync(provider);

        Assert.Equal(1, seed.Calls);
        Assert.Equal(1, reader.Calls);
        Assert.Equal(1, store.ListCalls);
        Assert.Equal(1, store.ApplyCalls);

        var plan = Assert.IsType<ShippingMethodBackfillPlan>(store.LastPlan);
        Assert.True(plan.IsExecutable, string.Join("; ", plan.Violations));
        Assert.Equal(7, plan.Items.Count);
        Assert.All(plan.Items, x => Assert.Equal(ShippingMethodPlanAction.Create, x.Action));

        var desired = plan.Items.ToDictionary(x => x.Desired.MethodCode, x => x.Desired, StringComparer.Ordinal);
        Assert.Equal("post", desired["post:standard"].LegacyServiceCode);
        Assert.Equal("standard", desired["post:standard"].LegacyOptionCode);
        Assert.Null(desired["in_person"].LegacyOptionCode);
        // Service+option identity is preserved as its own field: the composite is never split into a service code.
        Assert.Equal("post:standard", desired["post:standard"].MethodCode);
        Assert.Equal("معمولی", desired["post:standard"].Translations.Single(x => x.LanguageId == LangFa).Name);
        Assert.Equal("Standard", desired["post:standard"].Translations.Single(x => x.LanguageId == LangEn).Name);

        // The backfill store is the only writer and it materialized exactly the desired rows.
        Assert.Equal(7, store.Rows.Count);
    }

    [Fact]
    public async Task Second_run_is_idempotent_and_writes_nothing()
    {
        var seed = new RecordingSeedPort();
        var reader = new RecordingCatalogReader(LiveCatalog());
        var store = new RecordingBackfillStore();

        await using var provider = Build(
            new FakeEnvironment("Development"), seed, reader, store, enabled: true);

        await ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync(provider);
        var rowsAfterFirst = store.Rows.Count;
        var applyAfterFirst = store.ApplyCalls;

        await ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync(provider);

        Assert.Equal(7, rowsAfterFirst);
        Assert.Equal(1, applyAfterFirst);
        // Second pass re-reads but finds only Skip items, so it never opens a transaction.
        Assert.Equal(2, store.ListCalls);
        Assert.Equal(1, store.ApplyCalls);
        Assert.Equal(rowsAfterFirst, store.Rows.Count);
    }

    [Fact]
    public async Task Disabled_gate_fails_closed_without_touching_the_database()
    {
        var seed = new RecordingSeedPort();
        var reader = new RecordingCatalogReader(LiveCatalog());
        var store = new RecordingBackfillStore();

        await using var provider = Build(
            new FakeEnvironment("Development"), seed, reader, store, enabled: false);

        await ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync(provider);

        Assert.Equal(0, seed.Calls);
        Assert.Equal(0, reader.Calls);
        Assert.Equal(0, store.ListCalls);
        Assert.Equal(0, store.ApplyCalls);
        Assert.Empty(store.Rows);
    }

    [Fact]
    public async Task Non_development_environment_fails_closed()
    {
        var seed = new RecordingSeedPort();
        var reader = new RecordingCatalogReader(LiveCatalog());
        var store = new RecordingBackfillStore();

        await using var provider = Build(
            new FakeEnvironment("Production"), seed, reader, store, enabled: true);

        await ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync(provider);

        Assert.Equal(0, seed.Calls);
        Assert.Equal(0, reader.Calls);
        Assert.Equal(0, store.ListCalls);
        Assert.Equal(0, store.ApplyCalls);
    }

    [Fact]
    public async Task A_non_executable_plan_is_never_applied_and_mutates_nothing()
    {
        var seed = new RecordingSeedPort();
        // Two option-less services with the same code collide on the binding, so the planner reports a
        // violation. The bootstrap must refuse the plan rather than inventing rows to work around it.
        var reader = new RecordingCatalogReader(
        [
            Optionless("snapp_courier", "courier", 10),
            Optionless("snapp_courier", "courier", 20),
        ]);
        var store = new RecordingBackfillStore();

        await using var provider = Build(
            new FakeEnvironment("Development"), seed, reader, store, enabled: true);

        await ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync(provider);

        Assert.Equal(1, store.ListCalls);
        Assert.Equal(0, store.ApplyCalls);
        Assert.Empty(store.Rows);
    }

    private static ServiceProvider Build(
        IHostEnvironment environment,
        RecordingSeedPort seed,
        RecordingCatalogReader reader,
        RecordingBackfillStore store,
        bool enabled)
    {
        var services = new ServiceCollection();
        services.AddSingleton(environment);
        services.AddSingleton<IShippingCatalogSeedPort>(seed);
        services.AddSingleton<IShippingCatalogReader>(reader);
        services.AddSingleton<IShippingOneLevelBackfillStore>(store);
        services.Configure<ShippingOneLevelCatalogOptions>(options => options.Enabled = enabled);
        return services.BuildServiceProvider();
    }

    /// <summary>The live Development legacy shape: post(2) + tipax(2) + 3 option-less services = 7 methods.</summary>
    private static IReadOnlyList<ShippingCatalogServiceSnapshot> LiveCatalog() =>
    [
        WithOptions("post", "post", 10,
        [
            Option("express", false, 10, ("پیشتاز", "Express")),
            Option("standard", true, 20, ("معمولی", "Standard")),
        ]),
        WithOptions("tipax", "tipax", 20,
        [
            Option("express", true, 10, ("پیشتاز", "Express")),
            Option("standard", true, 20, ("معمولی", "Standard")),
        ]),
        Optionless("snapp_courier", "courier", 30),
        Optionless("store_courier", "store_courier", 40),
        Optionless("in_person", "in_person", 50),
    ];

    private static ShippingCatalogServiceSnapshot WithOptions(
        string code,
        string providerKind,
        int sortOrder,
        IReadOnlyList<ShippingCatalogOptionSnapshot> options) =>
        new(Guid.NewGuid(), code, providerKind, code, code, true, sortOrder,
            [
                new ShippingCatalogTranslationSnapshot(LangFa, $"نام {code}", null),
                new ShippingCatalogTranslationSnapshot(LangEn, $"Name {code}", null),
            ],
            options);

    private static ShippingCatalogServiceSnapshot Optionless(string code, string providerKind, int sortOrder) =>
        new(Guid.NewGuid(), code, providerKind, code, code, true, sortOrder,
            [
                new ShippingCatalogTranslationSnapshot(LangFa, $"نام {code}", null),
                new ShippingCatalogTranslationSnapshot(LangEn, $"Name {code}", null),
            ],
            []);

    private static ShippingCatalogOptionSnapshot Option(
        string code,
        bool isActive,
        int sortOrder,
        (string Fa, string En) names) =>
        new(Guid.NewGuid(), Guid.NewGuid(), code, isActive, sortOrder,
            [
                new ShippingCatalogOptionTranslationSnapshot(LangFa, names.Fa),
                new ShippingCatalogOptionTranslationSnapshot(LangEn, names.En),
            ]);

    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tooba.Fulfillment.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class RecordingSeedPort : IShippingCatalogSeedPort
    {
        public int Calls { get; private set; }

        public Task EnsureSeedAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingCatalogReader(IReadOnlyList<ShippingCatalogServiceSnapshot> services)
        : IShippingCatalogReader
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(services);
        }

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            Task.FromResult(services.FirstOrDefault(x => x.ShippingServiceId == serviceId));
    }

    /// <summary>
    /// In-memory double for the persisted seam. It records the plan it was asked to apply and, on
    /// <see cref="ApplyAsync"/>, materializes the Create rows so a rerun observes matching identity
    /// (the transactional apply itself is proven against real PostgreSQL in the task evidence).
    /// </summary>
    private sealed class RecordingBackfillStore : IShippingOneLevelBackfillStore
    {
        private readonly List<ShippingMethodExistingRow> _rows = [];

        public int ListCalls { get; private set; }
        public int ApplyCalls { get; private set; }
        public ShippingMethodBackfillPlan? LastPlan { get; private set; }
        public IReadOnlyList<ShippingMethodExistingRow> Rows => _rows;

        public Task<IReadOnlyList<ShippingMethodExistingRow>> ListExistingAsync(CancellationToken cancellationToken)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyList<ShippingMethodExistingRow>>(_rows.ToArray());
        }

        public Task ApplyAsync(ShippingMethodBackfillPlan plan, CancellationToken cancellationToken)
        {
            ApplyCalls++;
            LastPlan = plan;
            Assert.True(plan.IsExecutable, string.Join("; ", plan.Violations));
            foreach (var item in plan.Items)
            {
                if (item.Action == ShippingMethodPlanAction.Create)
                {
                    _rows.Add(new ShippingMethodExistingRow(
                        Guid.NewGuid(),
                        item.Desired.MethodCode,
                        item.Desired.LegacyServiceCode,
                        item.Desired.LegacyOptionCode,
                        item.Desired.IsActive));
                }
            }

            return Task.CompletedTask;
        }
    }
}
