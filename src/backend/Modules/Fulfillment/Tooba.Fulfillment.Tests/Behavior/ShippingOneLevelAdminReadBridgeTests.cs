using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Localization.Contracts.Ports;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-085 — focused proof for the trusted, authenticated, read-only admin
/// bridge to the one-level shipping catalog: default-OFF gate evaluation from authoritative server
/// configuration, no database read while OFF, controlled NOT_READY for an unmigrated table (never a
/// 500 and never a fake "ready/empty" success), and no second merchant level in the projection.
/// No web host, no live database.
/// </summary>
public sealed class ShippingOneLevelAdminReadBridgeTests
{
    private static readonly Guid LangFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");

    [Fact]
    public async Task Gate_is_disabled_by_absence_and_the_database_is_never_read()
    {
        var reader = new RecordingReader();
        var read = new ShippingOneLevelCatalogAdminRead(
            Options.Create(new ShippingOneLevelCatalogOptions()), reader);

        var result = await read.ReadAsync(CancellationToken.None);

        Assert.Equal(ShippingOneLevelCatalogGate.Disabled, result.Gate);
        Assert.False(result.CatalogReady);
        Assert.Empty(result.Methods);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Gate_is_disabled_by_an_explicit_false_and_the_database_is_never_read()
    {
        var reader = new RecordingReader();
        var read = new ShippingOneLevelCatalogAdminRead(
            Options.Create(new ShippingOneLevelCatalogOptions { Enabled = false }), reader);

        var result = await read.ReadAsync(CancellationToken.None);

        Assert.Equal(ShippingOneLevelCatalogGate.Disabled, result.Gate);
        Assert.False(result.CatalogReady);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Only_an_explicit_true_enables_the_read_and_keeps_inactive_rows()
    {
        var reader = new RecordingReader
        {
            Rows =
            [
                Snapshot("post:standard", isActive: true, sortOrder: 12, label: "پست معمولی"),
                Snapshot("post:express", isActive: false, sortOrder: 11, label: null),
                Snapshot("snapp_courier", isActive: false, sortOrder: 30, label: null),
            ],
        };
        var read = new ShippingOneLevelCatalogAdminRead(
            Options.Create(new ShippingOneLevelCatalogOptions { Enabled = true }), reader);

        var result = await read.ReadAsync(CancellationToken.None);

        Assert.Equal(ShippingOneLevelCatalogGate.Enabled, result.Gate);
        Assert.True(result.CatalogReady);
        Assert.Equal(1, reader.Calls);
        Assert.Equal(3, result.Methods.Count);
        Assert.Contains(result.Methods, x => x.MethodCode == "post:express" && !x.IsActive);
        Assert.Contains(result.Methods, x => x.MethodCode == "snapp_courier" && !x.IsActive);
    }

    [Fact]
    public async Task An_unmigrated_catalog_reports_a_controlled_not_ready_instead_of_failing()
    {
        var reader = new RecordingReader
        {
            Failure = new PostgresException(
                "relation \"fulfillment.shipping_methods\" does not exist",
                "ERROR",
                "ERROR",
                PostgresErrorCodes.UndefinedTable),
        };
        var read = new ShippingOneLevelCatalogAdminRead(
            Options.Create(new ShippingOneLevelCatalogOptions { Enabled = true }), reader);

        var result = await read.ReadAsync(CancellationToken.None);

        // Enabled gate, but the catalog is honestly NOT ready: no 500, no fake empty success.
        Assert.Equal(ShippingOneLevelCatalogGate.Enabled, result.Gate);
        Assert.False(result.CatalogReady);
        Assert.Empty(result.Methods);
    }

    [Fact]
    public async Task Unexpected_database_failures_are_not_swallowed()
    {
        var reader = new RecordingReader
        {
            Failure = new PostgresException("boom", "ERROR", "ERROR", PostgresErrorCodes.InsufficientPrivilege),
        };
        var read = new ShippingOneLevelCatalogAdminRead(
            Options.Create(new ShippingOneLevelCatalogOptions { Enabled = true }), reader);

        await Assert.ThrowsAsync<PostgresException>(() => read.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Handler_reports_the_effective_gate_and_never_exposes_a_second_merchant_level()
    {
        var read = new StubRead(
            ShippingOneLevelCatalogGate.Enabled,
            CatalogReady: true,
            [
                Snapshot("post:standard", isActive: true, sortOrder: 12, label: "پست معمولی"),
                Snapshot("post:express", isActive: false, sortOrder: 11, label: null),
            ]);
        var handler = new ListOneLevelShippingMethodsHandler(read, new StubLanguages());

        var result = await handler.Handle(new ListOneLevelShippingMethodsQuery("fa"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal("Enabled", dto.Gate);
        Assert.True(dto.Ready);
        Assert.Equal(2, dto.Methods.Count);

        // Inactive methods stay visible and inactive; the label is never invented.
        var express = Assert.Single(dto.Methods, x => x.MethodCode == "post:express");
        Assert.False(express.IsActive);
        Assert.Equal(string.Empty, express.Name);
        var standard = Assert.Single(dto.Methods, x => x.MethodCode == "post:standard");
        Assert.True(standard.IsActive);
        Assert.Equal("پست معمولی", standard.Name);

        // No option count, no provider kind and no legacy binding on the wire: one level only.
        var payload = System.Text.Json.JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("providerKind", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legacyServiceCode", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legacyOptionCode", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("optionCount", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("options", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handler_reports_a_disabled_not_ready_catalog_without_pretending_success()
    {
        var handler = new ListOneLevelShippingMethodsHandler(
            new StubRead(ShippingOneLevelCatalogGate.Disabled, CatalogReady: false, []),
            new StubLanguages());

        var result = await handler.Handle(new ListOneLevelShippingMethodsQuery(null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Disabled", result.Value!.Gate);
        Assert.False(result.Value!.Ready);
        Assert.Empty(result.Value!.Methods);
    }

    private static ShippingOneLevelMethodSnapshot Snapshot(
        string code,
        bool isActive,
        int sortOrder,
        string? label) =>
        new(
            Guid.NewGuid(),
            code,
            "post",
            "post",
            "standard",
            isActive,
            sortOrder,
            label is null
                ? []
                : [new ShippingOneLevelMethodTranslationSnapshot(LangFa, label, null)]);

    private sealed class RecordingReader : IShippingOneLevelCatalogReader
    {
        public int Calls { get; private set; }
        public IReadOnlyList<ShippingOneLevelMethodSnapshot> Rows { get; init; } = [];
        public Exception? Failure { get; init; }

        public Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Failure is not null
                ? Task.FromException<IReadOnlyList<ShippingOneLevelMethodSnapshot>>(Failure)
                : Task.FromResult(Rows);
        }

        public Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            ListAsync(cancellationToken);

        public Task<ShippingOneLevelMethodSnapshot?> GetByCodeAsync(string methodCode, CancellationToken cancellationToken) =>
            Task.FromResult<ShippingOneLevelMethodSnapshot?>(null);
    }

    private sealed class StubRead(
        ShippingOneLevelCatalogGate gate,
        bool CatalogReady,
        IReadOnlyList<ShippingOneLevelMethodSnapshot> methods) : IShippingOneLevelCatalogAdminRead
    {
        public Task<ShippingOneLevelCatalogAdminSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ShippingOneLevelCatalogAdminSnapshot(gate, CatalogReady, methods));
    }

    private sealed class StubLanguages : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>(
                [new LanguageLookupSnapshot(LangFa, "fa", "fa-IR", "", true)]);
    }
}
