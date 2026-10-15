using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping;
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
/// TB-SHIPPING-ONE-LEVEL-TABS-094 — focused proof for the narrow, Fulfillment-owned admin write seam
/// that edits only the per-language display name of an existing canonical one-level method:
/// <list type="bullet">
/// <item>a disabled gate refuses before any database read or write (fail-closed);</item>
/// <item>an unknown/malformed code and a blank/over-long label are refused without a write;</item>
/// <item>a valid update writes exactly the translation name and preserves identity, binding,
/// activation, sort order and the optional description;</item>
/// <item>a repeated save is idempotent — one row per (method, language), never a duplicate;</item>
/// <item>the write never touches <c>method_code</c>, the legacy binding or the provider kind, and it
/// never creates a second-level option;</item>
/// <item>an unknown language is refused at the Application boundary before the write seam runs.</item>
/// </list>
/// No web host and no live database: the in-memory provider is the store under test.
/// </summary>
public sealed class ShippingOneLevelAdminWriteTests
{
    private static readonly Guid LangFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid LangEn = Guid.Parse("01900000-0000-7000-8000-00000000aa02");
    private static readonly Guid UnknownLang = Guid.Parse("01900000-0000-7000-8000-00000000aa99");

    private static readonly Guid StandardId = Guid.Parse("11111111-0000-7000-8000-000000000001");
    private static readonly Guid ExpressId = Guid.Parse("11111111-0000-7000-8000-000000000002");

    [Fact]
    public async Task A_disabled_gate_refuses_before_any_read_or_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: false);

        var result = await write.UpdateMethodNameAsync("post:standard", LangFa, "پست معمولی", CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.GateDisabled, result.Status);
        Assert.Equal(string.Empty, result.MethodCode);
        Assert.Equal(string.Empty, result.Name);
        // Nothing was written and nothing was even read: the name stays exactly as seeded.
        Assert.Equal("معمولی", await NameAsync(db, StandardId, LangFa));
        Assert.Equal(2, await db.ShippingMethodTranslations.AsNoTracking().CountAsync());
    }

    [Theory]
    [InlineData("post:standard:extra")]
    [InlineData("post:")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("post standard;drop")]
    [InlineData("POST:STANDARD!")]
    public async Task A_malformed_code_is_refused_without_a_write(string supplied)
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        var result = await write.UpdateMethodNameAsync(supplied, LangFa, "پست معمولی", CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.InvalidMethodCode, result.Status);
        Assert.Equal(string.Empty, result.MethodCode);
        Assert.Equal("معمولی", await NameAsync(db, StandardId, LangFa));
        Assert.Equal(2, await db.ShippingMethodTranslations.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task A_canonical_code_that_does_not_exist_is_refused_as_not_found()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        var result = await write.UpdateMethodNameAsync("tipax:express", LangFa, "تیپاکس پیشتاز", CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.MethodNotFound, result.Status);
        // The canonical (normalized) code is echoed; it is never split into a fabricated binding.
        Assert.Equal("tipax:express", result.MethodCode);
        Assert.Equal(2, await db.ShippingMethodTranslations.AsNoTracking().CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_label_is_refused_without_a_write(string? supplied)
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        var result = await write.UpdateMethodNameAsync("post:standard", LangFa, supplied, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.NameRequired, result.Status);
        Assert.Equal("معمولی", await NameAsync(db, StandardId, LangFa));
    }

    [Fact]
    public async Task A_label_beyond_the_stable_column_width_is_refused_without_a_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        var tooLong = new string('ا', ShippingOneLevelMethodNameLimits.MaxNameLength + 1);
        var result = await write.UpdateMethodNameAsync("post:standard", LangFa, tooLong, CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.NameTooLong, result.Status);
        Assert.Equal("معمولی", await NameAsync(db, StandardId, LangFa));

        // Exactly at the boundary is accepted (the persisted column is varchar(128)).
        var atLimit = new string('ا', ShippingOneLevelMethodNameLimits.MaxNameLength);
        var accepted = await write.UpdateMethodNameAsync("post:standard", LangFa, atLimit, CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, accepted.Status);
    }

    [Fact]
    public async Task A_valid_update_writes_only_the_name_and_preserves_identity_binding_and_activation()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        var result = await write.UpdateMethodNameAsync("post:standard", LangFa, "  پست معمولی  ", CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, result.Status);
        Assert.Equal("post:standard", result.MethodCode);
        Assert.Equal(LangFa, result.LanguageId);
        Assert.Equal("پست معمولی", result.Name);

        // The human label is trimmed and persisted for exactly that language.
        Assert.Equal("پست معمولی", await NameAsync(db, StandardId, LangFa));
        // A different language is untouched.
        Assert.Equal("Standard", await NameAsync(db, StandardId, LangEn));

        // Identity, binding, provider kind, activation and sort order are byte-for-byte unchanged.
        var method = await db.ShippingMethods.AsNoTracking().SingleAsync(x => x.ShippingMethodId == StandardId);
        Assert.Equal("post:standard", method.MethodCode);
        Assert.Equal("post", method.LegacyServiceCode);
        Assert.Equal("standard", method.LegacyOptionCode);
        Assert.Equal("post", method.ProviderKind);
        Assert.True(method.IsActive);
        Assert.Equal(12, method.SortOrder);

        // An inactive method stays inactive: editing a name never activates anything.
        var express = await db.ShippingMethods.AsNoTracking().SingleAsync(x => x.ShippingMethodId == ExpressId);
        Assert.False(express.IsActive);
    }

    [Fact]
    public async Task The_optional_description_of_an_existing_row_is_never_rewritten()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        var result = await write.UpdateMethodNameAsync("post:standard", LangFa, "پست معمولی", CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, result.Status);
        var row = await db.ShippingMethodTranslations.AsNoTracking()
            .SingleAsync(x => x.ShippingMethodId == StandardId && x.LanguageId == LangFa);
        Assert.Equal("پست معمولی", row.Name);
        // The description seeded alongside the original name survives an edit of the name only.
        Assert.Equal("توضیح اصلی", row.Description);
    }

    [Fact]
    public async Task A_repeated_save_is_idempotent_and_never_creates_a_duplicate_row()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        var first = await write.UpdateMethodNameAsync("post:standard", LangFa, "پست معمولی", CancellationToken.None);
        var second = await write.UpdateMethodNameAsync("post:standard", LangFa, "پست معمولی", CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, first.Status);
        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, second.Status);
        // Exactly one row per (method, language) — the canonical uniqueness key.
        Assert.Single(await db.ShippingMethodTranslations.AsNoTracking()
            .Where(x => x.ShippingMethodId == StandardId && x.LanguageId == LangFa).ToListAsync());
        Assert.Equal("پست معمولی", await NameAsync(db, StandardId, LangFa));
    }

    [Fact]
    public async Task A_missing_translation_is_inserted_once_on_the_canonical_uniqueness_key()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        // post:express has no translation at all yet.
        Assert.Empty(await db.ShippingMethodTranslations.AsNoTracking()
            .Where(x => x.ShippingMethodId == ExpressId).ToListAsync());

        var created = await write.UpdateMethodNameAsync("post:express", LangFa, "پست پیشتاز", CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, created.Status);
        Assert.Equal("پست پیشتاز", await NameAsync(db, ExpressId, LangFa));

        var again = await write.UpdateMethodNameAsync("post:express", LangFa, "پست پیشتاز", CancellationToken.None);
        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, again.Status);
        Assert.Single(await db.ShippingMethodTranslations.AsNoTracking()
            .Where(x => x.ShippingMethodId == ExpressId && x.LanguageId == LangFa).ToListAsync());
    }

    [Fact]
    public async Task A_legacy_composite_code_updates_the_existing_row_without_creating_a_second_level()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);

        // Legacy composite identity resolves to the existing row; nothing is split or fabricated.
        var result = await write.UpdateMethodNameAsync("POST:Standard", LangFa, "پست معمولی", CancellationToken.None);

        Assert.Equal(ShippingOneLevelMethodNameWriteStatus.Updated, result.Status);
        Assert.Equal("post:standard", result.MethodCode);
        Assert.Equal("پست معمولی", await NameAsync(db, StandardId, LangFa));
        // Only one method row and one translation row per language: no option/second level was created.
        Assert.Equal(2, await db.ShippingMethods.AsNoTracking().CountAsync());
        Assert.Equal(2, await db.ShippingMethodTranslations.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task An_unknown_language_is_refused_at_the_application_boundary_before_any_write()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var write = Write(db, enabled: true);
        var handler = new UpdateOneLevelShippingMethodTranslationHandler(write, new StubLanguages());

        var result = await handler.Handle(
            new UpdateOneLevelShippingMethodTranslationCommand("post:standard", UnknownLang, "پست معمولی"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingServiceLanguageInvalid, result.Errors[0].Code);
        Assert.Equal("معمولی", await NameAsync(db, StandardId, LangFa));
        Assert.Equal(2, await db.ShippingMethodTranslations.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task The_handler_maps_every_fail_closed_outcome_to_a_stable_error_code()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var languages = new StubLanguages();

        var disabled = await new UpdateOneLevelShippingMethodTranslationHandler(Write(db, enabled: false), languages)
            .Handle(new UpdateOneLevelShippingMethodTranslationCommand("post:standard", LangFa, "x"), CancellationToken.None);
        Assert.True(disabled.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.Rejected, disabled.Errors[0].Code);

        var missing = await new UpdateOneLevelShippingMethodTranslationHandler(Write(db, enabled: true), languages)
            .Handle(new UpdateOneLevelShippingMethodTranslationCommand("tipax:express", LangFa, "x"), CancellationToken.None);
        Assert.Equal(FulfillmentErrorCodes.ShippingMethodNotFound, missing.Errors[0].Code);

        var blank = await new UpdateOneLevelShippingMethodTranslationHandler(Write(db, enabled: true), languages)
            .Handle(new UpdateOneLevelShippingMethodTranslationCommand("post:standard", LangFa, "  "), CancellationToken.None);
        Assert.Equal(FulfillmentErrorCodes.ShippingMethodNameRequired, blank.Errors[0].Code);

        var malformed = await new UpdateOneLevelShippingMethodTranslationHandler(Write(db, enabled: true), languages)
            .Handle(new UpdateOneLevelShippingMethodTranslationCommand("post:standard!", LangFa, "x"), CancellationToken.None);
        Assert.Equal(FulfillmentErrorCodes.ShippingMethodCodeInvalid, malformed.Errors[0].Code);
    }

    [Fact]
    public async Task A_valid_command_returns_the_server_owned_saved_name()
    {
        await using var db = CreateDb();
        SeedMethods(db);
        var handler = new UpdateOneLevelShippingMethodTranslationHandler(Write(db, enabled: true), new StubLanguages());

        var result = await handler.Handle(
            new UpdateOneLevelShippingMethodTranslationCommand("post:standard", LangFa, " پست معمولی "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("post:standard", result.Value!.MethodCode);
        Assert.Equal(LangFa, result.Value!.LanguageId);
        Assert.Equal("پست معمولی", result.Value!.Name);
        Assert.Equal("پست معمولی", await NameAsync(db, StandardId, LangFa));
    }

    private static ShippingOneLevelCatalogAdminWrite Write(FulfillmentDbContext db, bool enabled) =>
        new(
            Options.Create(new ShippingOneLevelCatalogOptions { Enabled = enabled }),
            db,
            new FixedIdGenerator(Enumerable.Range(0, 16)
                .Select(i => Guid.Parse($"33333333-0000-7000-8000-{i:D12}")).ToArray()));

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
            ShippingMethod.Create(ExpressId, "post:express", "post", "post", "express", 11, false, now));

        db.ShippingMethodTranslations.AddRange(
            ShippingMethodTranslation.Create(Guid.NewGuid(), StandardId, LangFa, "معمولی", "توضیح اصلی"),
            ShippingMethodTranslation.Create(Guid.NewGuid(), StandardId, LangEn, "Standard", null));

        db.SaveChanges();
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
