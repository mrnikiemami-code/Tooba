using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Localization.Contracts.Ports;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167 — focused proof for the Fulfillment-owned create seam of
/// an independent canonical shipping method:
/// <list type="bullet">
/// <item>a disabled gate refuses before any database read or write;</item>
/// <item>malformed codes, duplicate codes, duplicate bindings, blank/over-long/duplicate-language
/// names and non-canonical provider metadata are refused without a write;</item>
/// <item>a valid create persists exactly one canonical row plus its real per-language names, always
/// inactive, with a server-assigned sort order and server-derived legacy binding;</item>
/// <item>no legacy service/option row is created, so the new method cannot appear in the storefront
/// projection that reads that catalog — the method is not accidentally customer-selectable;</item>
/// <item>the seven existing canonical methods and their bindings stay byte-for-byte unchanged.</item>
/// </list>
/// No web host and no live database: the in-memory provider is the store under test.
/// </summary>
public sealed class ShippingOneLevelMethodCreateTests
{
    private static readonly Guid LangFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid LangEn = Guid.Parse("01900000-0000-7000-8000-00000000aa02");
    private static readonly Guid UnknownLang = Guid.Parse("01900000-0000-7000-8000-00000000aa99");

    private static readonly Guid StandardId = Guid.Parse("11111111-0000-7000-8000-000000000001");
    private static readonly Guid ExpressId = Guid.Parse("11111111-0000-7000-8000-000000000002");
    private static readonly Guid CourierId = Guid.Parse("11111111-0000-7000-8000-000000000003");

    [Fact]
    public async Task A_disabled_gate_refuses_before_any_read_or_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: false);

        var outcome = await create.CreateMethodAsync(
            "tipax:sameday", null, Names(("همان‌روز", LangFa)), CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.GateDisabled, outcome.Status);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
        Assert.Equal(0, await db.ShippingMethodTranslations.AsNoTracking()
            .CountAsync(x => x.LanguageId == LangFa && x.Name == "همان‌روز"));
    }

    [Theory]
    [InlineData("tipax:sameday:extra")]
    [InlineData("tipax:")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tipax sameday;drop")]
    [InlineData("TIPAX:SAMEDAY!")]
    public async Task A_malformed_code_is_refused_without_a_write(string supplied)
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            supplied, null, Names(("x", LangFa)), CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.InvalidMethodCode, outcome.Status);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task A_duplicate_canonical_code_is_refused_and_the_existing_row_is_untouched()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            "POST:Standard", null, Names(("تکراری", LangFa)), CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.MethodCodeDuplicate, outcome.Status);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
        Assert.Equal("post:standard", (await db.ShippingMethods.AsNoTracking()
            .SingleAsync(x => x.ShippingMethodId == StandardId)).MethodCode);
    }

    [Fact]
    public async Task A_legacy_binding_owned_by_another_row_is_refused_even_with_a_free_code()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        // A legacy/backfilled row whose canonical code differs but whose binding is exactly
        // (tipax, sameday): the code is free, the binding is not.
        db.ShippingMethods.Add(ShippingMethod.Create(
            Guid.NewGuid(), "tipax_sameday_legacy", "tipax", "tipax", "sameday", 50, false,
            DateTimeOffset.Parse("2026-10-15T00:00:00Z")));
        await db.SaveChangesAsync();

        var outcome = await create.CreateMethodAsync(
            "tipax:sameday", null, Names(("همان‌روز", LangFa)), CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.BindingDuplicate, outcome.Status);
        Assert.Equal(4, await db.ShippingMethods.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task A_non_canonical_provider_metadata_is_refused_without_a_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            "tipax:sameday", "Tipax Express!", Names(("همان‌روز", LangFa)), CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.InvalidProviderKind, outcome.Status);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_name_is_refused_without_a_write(string? supplied)
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            "tipax:sameday", null, Names((supplied!, LangFa)), CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.NameRequired, outcome.Status);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task A_name_beyond_the_stable_column_width_is_refused_without_a_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            "tipax:sameday", null,
            Names((new string('ا', ShippingOneLevelMethodNameLimits.MaxNameLength + 1), LangFa)),
            CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.NameTooLong, outcome.Status);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task The_same_language_twice_is_refused_without_a_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            "tipax:sameday", null,
            [new ShippingOneLevelMethodCreateName(LangFa, "یک"), new ShippingOneLevelMethodCreateName(LangFa, "دو")],
            CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.DuplicateLanguage, outcome.Status);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task A_valid_create_persists_one_inactive_method_with_real_language_names_and_no_legacy_rows()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            " TIPAX:Sameday ",
            null,
            [new ShippingOneLevelMethodCreateName(LangFa, "  تیپاکس همان‌روز "),
             new ShippingOneLevelMethodCreateName(LangEn, "Tipax Same-day")],
            CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.Created, outcome.Status);
        Assert.Equal("tipax:sameday", outcome.MethodCode);
        // Provider metadata falls back to the service segment of the canonical identity.
        Assert.Equal("tipax", outcome.ProviderKind);
        // The legacy binding is derived server-side with the existing backfill rule.
        Assert.Equal("tipax", outcome.LegacyServiceCode);
        Assert.Equal("sameday", outcome.LegacyOptionCode);
        // A new method is never created active and never gets a client sort order.
        Assert.False(outcome.IsActive);
        Assert.Equal(61, outcome.SortOrder);

        var stored = await db.ShippingMethods.AsNoTracking()
            .SingleAsync(x => x.MethodCode == "tipax:sameday");
        Assert.False(stored.IsActive);
        Assert.Equal("tipax", stored.ProviderKind);
        Assert.Equal("tipax", stored.LegacyServiceCode);
        Assert.Equal("sameday", stored.LegacyOptionCode);

        Assert.Equal("تیپاکس همان‌روز", await NameAsync(db, stored.ShippingMethodId, LangFa));
        Assert.Equal("Tipax Same-day", await NameAsync(db, stored.ShippingMethodId, LangEn));

        // No legacy catalog row is fabricated, so the storefront tree (which reads only
        // shipping_services/options) cannot contain the new method: it is not customer-selectable.
        Assert.Equal(0, await db.ShippingServices.AsNoTracking().CountAsync());
        Assert.Equal(0, await db.ShippingServiceOptions.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task An_optionless_canonical_code_is_accepted_with_a_null_option_binding()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var outcome = await create.CreateMethodAsync(
            "mahex", "mahex", Names(("ماهکس", LangFa)), CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodCreateStatus.Created, outcome.Status);
        Assert.Equal("mahex", outcome.LegacyServiceCode);
        Assert.Null(outcome.LegacyOptionCode);
    }

    [Fact]
    public async Task The_seven_recognized_methods_and_their_bindings_are_unchanged_by_a_create()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var create = Create(db, enabled: true);

        var before = await db.ShippingMethods.AsNoTracking()
            .OrderBy(x => x.MethodCode)
            .Select(x => new { x.MethodCode, x.LegacyServiceCode, x.LegacyOptionCode, x.IsActive, x.SortOrder })
            .ToListAsync();

        await create.CreateMethodAsync("tipax:sameday", null, Names(("همان‌روز", LangFa)), CancellationToken.None);

        var after = await db.ShippingMethods.AsNoTracking()
            .Where(x => x.MethodCode != "tipax:sameday")
            .OrderBy(x => x.MethodCode)
            .Select(x => new { x.MethodCode, x.LegacyServiceCode, x.LegacyOptionCode, x.IsActive, x.SortOrder })
            .ToListAsync();

        Assert.Equal(before, after);
    }

    [Fact]
    public async Task The_handler_rejects_an_unknown_language_before_any_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var handler = new CreateOneLevelShippingMethodHandler(Create(db, enabled: true), new StubLanguages());

        var result = await handler.Handle(
            new CreateOneLevelShippingMethodCommand(
                "tipax:sameday", null, [new(LangFa, "همان‌روز"), new(UnknownLang, "Same-day")]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingServiceLanguageInvalid, result.Errors[0].Code);
        Assert.Equal(3, await db.ShippingMethods.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task The_handler_rejects_an_empty_translation_set_and_maps_every_fail_closed_outcome()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var languages = new StubLanguages();

        var empty = await new CreateOneLevelShippingMethodHandler(Create(db, enabled: true), languages)
            .Handle(new CreateOneLevelShippingMethodCommand("tipax:sameday", null, []), CancellationToken.None);
        Assert.Equal(FulfillmentErrorCodes.ShippingMethodNameRequired, empty.Errors[0].Code);

        var duplicate = await new CreateOneLevelShippingMethodHandler(Create(db, enabled: true), languages)
            .Handle(new CreateOneLevelShippingMethodCommand(
                "post:standard", null, [new(LangFa, "تکراری")]), CancellationToken.None);
        Assert.Equal(FulfillmentErrorCodes.ShippingMethodCodeDuplicate, duplicate.Errors[0].Code);

        var malformed = await new CreateOneLevelShippingMethodHandler(Create(db, enabled: true), languages)
            .Handle(new CreateOneLevelShippingMethodCommand(
                "post:standard!", null, [new(LangFa, "x")]), CancellationToken.None);
        Assert.Equal(FulfillmentErrorCodes.ShippingMethodCodeInvalid, malformed.Errors[0].Code);

        var disabled = await new CreateOneLevelShippingMethodHandler(Create(db, enabled: false), languages)
            .Handle(new CreateOneLevelShippingMethodCommand(
                "tipax:sameday", null, [new(LangFa, "x")]), CancellationToken.None);
        Assert.Equal(FulfillmentErrorCodes.Rejected, disabled.Errors[0].Code);
    }

    [Fact]
    public async Task A_valid_command_returns_the_server_owned_created_method()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var handler = new CreateOneLevelShippingMethodHandler(Create(db, enabled: true), new StubLanguages());

        var result = await handler.Handle(
            new CreateOneLevelShippingMethodCommand(
                "tipax:sameday", "tipax", [new(LangFa, " تیپاکس همان‌روز ")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("tipax:sameday", result.Value!.MethodCode);
        Assert.Equal("tipax", result.Value!.ProviderKind);
        Assert.False(result.Value!.IsActive);
        Assert.Equal("تیپاکس همان‌روز", result.Value!.Translations.Single().Name);
        Assert.Single(await db.ShippingMethods.AsNoTracking()
            .Where(x => x.MethodCode == "tipax:sameday").ToListAsync());
    }

    private static IReadOnlyList<ShippingOneLevelMethodCreateName> Names((string Name, Guid LanguageId) entry) =>
        [new ShippingOneLevelMethodCreateName(entry.LanguageId, entry.Name)];

    private static ShippingOneLevelCatalogAdminCreate Create(FulfillmentDbContext db, bool enabled) =>
        new(Options.Create(new ShippingOneLevelCatalogOptions { Enabled = enabled }), db,
            new FixedIdGenerator(Enumerable.Range(0, 32)
                .Select(i => Guid.Parse($"55555555-0000-7000-8000-{i:D12}")).ToArray()),
            new FixedClock());

    private static async Task<string> NameAsync(FulfillmentDbContext db, Guid methodId, Guid languageId) =>
        (await db.ShippingMethodTranslations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShippingMethodId == methodId && x.LanguageId == languageId))?.Name
        ?? string.Empty;

    private static FulfillmentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static void SeedMethods(FulfillmentDbContext db)
    {
        var now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");
        db.ShippingMethods.AddRange(
            ShippingMethod.Create(StandardId, "post:standard", "post", "post", "standard", 12, true, now),
            ShippingMethod.Create(ExpressId, "post:express", "post", "post", "express", 11, false, now),
            ShippingMethod.Create(CourierId, "store_courier", "store_courier", "store_courier", null, 60, true, now));

        db.ShippingMethodTranslations.AddRange(
            ShippingMethodTranslation.Create(Guid.NewGuid(), StandardId, LangFa, "معمولی", null),
            ShippingMethodTranslation.Create(Guid.NewGuid(), ExpressId, LangFa, "پیشتاز", null));

        db.SaveChanges();
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.Parse("2026-10-15T12:00:00Z");
    }

    private sealed class StubLanguages : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>(
            [
                new LanguageLookupSnapshot(LangFa, "fa", "fa-IR", "", true),
                new LanguageLookupSnapshot(LangEn, "en", "en-US", "", false),
            ]);
    }
}
