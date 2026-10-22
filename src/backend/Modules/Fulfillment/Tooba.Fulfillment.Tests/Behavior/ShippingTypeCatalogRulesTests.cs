using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Application.Shipping.Validators;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Endpoints.Admin;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1 — گاردهای دامنه، حقایق زبان، خوانندهٔ بدنه و درز ذخیره.
/// </summary>
/// <remarks>
/// EF InMemory صریحاً جانشین اثبات رابطه‌ای نیست: این تست‌ها فقط قاعده/جریان درون‌حافظه‌ای را اثبات می‌کنند و
/// اثبات PostgreSQL، هم‌زمانی واقعی، harness مسیرها/مجوز و ActivityListener واقعی در RESULT صادقانه به‌عنوان
/// اجرانشده ثبت شده است.
/// </remarks>
public sealed class ShippingTypeCatalogRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid DefaultLanguage = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondLanguage = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ThirdLanguage = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // ---- R196-05: canonical identity is rejected, never trimmed/renamed -----------------------

    [Theory]
    [InlineData("ordinary")]
    [InlineData("heavy_2")]
    [InlineData("a")]
    public void Canonical_ascii_codes_are_accepted(string code) =>
        Assert.Equal(code, ShippingType.TryNormalizeCode(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" Ordinary")]
    [InlineData("ordinary ")]
    [InlineData("Ordinary")]
    [InlineData("2heavy")]
    [InlineData("_heavy")]
    [InlineData("heavy-type")]
    [InlineData("سنگین")]
    public void Malformed_or_whitespace_codes_are_rejected_not_renamed(string? code) =>
        Assert.Null(ShippingType.TryNormalizeCode(code));

    [Fact]
    public void Code_over_64_characters_is_rejected() =>
        Assert.Null(ShippingType.TryNormalizeCode(new string('a', ShippingType.CodeMaxLength + 1)));

    // ---- R196-02: no partial assignment in the domain ----------------------------------------

    [Fact]
    public void Create_is_inactive_with_revision_one()
    {
        var entity = CreateEntity();

        Assert.False(entity.IsActive);
        Assert.Equal(1, entity.Revision);
        Assert.Equal(Now, entity.CreatedAt);
        Assert.Equal(Now, entity.UpdatedAt);
        Assert.Single(entity.Translations);
    }

    [Fact]
    public void Replace_is_atomic_and_does_not_validate_in_domain()
    {
        var entity = CreateEntity();
        var translation = entity.Translations.Single();

        // اعتبارسنجی متن (طول/خالی‌بودن) در قاعدهٔ واحد Application است؛ دامنه فقط انتساب هم‌زمان انجام می‌دهد.
        translation.Replace("نام تازه", null);

        Assert.Equal("نام تازه", translation.Name);
        Assert.Null(translation.Description);
    }

    [Fact]
    public void Application_rule_rejects_overlength_description_without_touching_inputs()
    {
        var exception = Assert.Throws<SemanticException>(() => ShippingTypePreparation.NormalizeText(
            DefaultLanguage,
            "نام تازه",
            new string('x', ShippingType.DescriptionMaxLength + 1)));

        Assert.Equal(FulfillmentErrorCodes.ShippingTypeTranslationInvalid, exception.Error.Code);
    }

    [Fact]
    public void Replace_clears_only_the_selected_description()
    {
        var entity = ShippingType.Create(
            UuidV7.New(),
            "ordinary",
            10,
            [
                ShippingTypeTranslation.Create(UuidV7.New(), DefaultLanguage, "عادی", "قبلی"),
                ShippingTypeTranslation.Create(UuidV7.New(), SecondLanguage, "Ordinary", "kept"),
            ],
            Now);

        entity.ReplaceTranslation(DefaultLanguage, "عادی", null);

        Assert.Null(entity.Translations.Single(x => x.LanguageId == DefaultLanguage).Description);
        Assert.Equal("kept", entity.Translations.Single(x => x.LanguageId == SecondLanguage).Description);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ShippingType.SortOrderMaxValue + 1)]
    public void Sort_order_outside_bounds_is_rejected(int sortOrder) =>
        Assert.ThrowsAny<ArgumentException>(() => ShippingType.Create(
            UuidV7.New(),
            "ordinary",
            sortOrder,
            [ShippingTypeTranslation.Create(UuidV7.New(), DefaultLanguage, "عادی", null)],
            Now));

    [Fact]
    public void Blank_name_and_empty_language_are_rejected_by_the_application_rule()
    {
        var blank = Assert.Throws<SemanticException>(
            () => ShippingTypePreparation.NormalizeText(DefaultLanguage, "   ", null));
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeTranslationInvalid, blank.Error.Code);

        var empty = Assert.Throws<SemanticException>(
            () => ShippingTypePreparation.NormalizeText(Guid.Empty, "عادی", null));
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeTranslationInvalid, empty.Error.Code);
    }

    // ---- R196-03: registry cardinality is validated, never guessed ----------------------------

    [Fact]
    public void Single_active_default_resolves()
    {
        var resolution = Facts(
            (DefaultLanguage, active: true, isDefault: true, order: 1),
            (SecondLanguage, active: true, isDefault: false, order: 2),
            (ThirdLanguage, active: false, isDefault: false, order: 3)).Resolve();

        Assert.Equal(DefaultLanguage, resolution.ActiveDefaultLanguageId);
        Assert.Equal([DefaultLanguage, SecondLanguage], resolution.ActiveLanguageIds);
        Assert.False(resolution.IsActive(ThirdLanguage));
    }

    [Fact]
    public void Non_first_default_is_respected()
    {
        var resolution = Facts(
            (SecondLanguage, active: true, isDefault: false, order: 1),
            (ThirdLanguage, active: true, isDefault: true, order: 7),
            (DefaultLanguage, active: true, isDefault: false, order: 3)).Resolve();

        Assert.Equal(ThirdLanguage, resolution.ActiveDefaultLanguageId);
    }

    [Fact]
    public void Multiple_active_defaults_are_rejected_not_ordered_first()
    {
        var exception = Assert.Throws<SemanticException>(() => Facts(
            (DefaultLanguage, active: true, isDefault: true, order: 1),
            (SecondLanguage, active: true, isDefault: true, order: 2)).Resolve());

        Assert.Equal(FulfillmentErrorCodes.ShippingTypeDefaultLanguageAmbiguous, exception.Error.Code);
    }

    [Fact]
    public void Zero_default_or_no_active_language_is_rejected()
    {
        var noDefault = Assert.Throws<SemanticException>(() => Facts(
            (DefaultLanguage, active: true, isDefault: false, order: 1)).Resolve());
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable, noDefault.Error.Code);

        var noActive = Assert.Throws<SemanticException>(() => Facts(
            (DefaultLanguage, active: false, isDefault: true, order: 1)).Resolve());
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable, noActive.Error.Code);
    }

    [Fact]
    public void Duplicate_or_empty_language_identity_is_rejected()
    {
        var duplicate = Assert.Throws<SemanticException>(() => Facts(
            (DefaultLanguage, active: true, isDefault: true, order: 1),
            (DefaultLanguage, active: true, isDefault: false, order: 2)).Resolve());
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeLanguageInvalid, duplicate.Error.Code);

        var empty = Assert.Throws<SemanticException>(() => Facts(
            (Guid.Empty, active: true, isDefault: true, order: 1)).Resolve());
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeLanguageInvalid, empty.Error.Code);
    }

    // ---- R196-01: lossless transport input ---------------------------------------------------

    [Fact]
    public async Task Body_reader_preserves_missing_null_and_null_elements()
    {
        var body = await ReadAsync("""
            {"sortOrder":null,"isActive":null,
             "translations":[{"languageId":"11111111-1111-1111-1111-111111111111","name":"عادی"},null]}
            """);

        Assert.Null(body.Code);
        Assert.Null(body.SortOrder);
        Assert.Null(body.IsActive);
        Assert.Null(body.ExpectedRevision);
        Assert.NotNull(body.Translations);
        Assert.Equal(2, body.Translations!.Count);
        Assert.Null(body.Translations[1]);
    }

    [Fact]
    public async Task Body_reader_keeps_null_collection_as_null()
    {
        var body = await ReadAsync("""{"translations":null}""");
        Assert.Null(body.Translations);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("""{"sortOrder":"ten"}""")]
    [InlineData("""{"translations":{}}""")]
    [InlineData("""{"translations":[{"languageId":"not-a-guid","name":"x"}]}""")]
    public async Task Malformed_body_is_rejected_canonically(string json)
    {
        var exception = await Assert.ThrowsAsync<SemanticException>(() => ReadAsync(json));
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeBodyMalformed, exception.Error.Code);
    }

    [Fact]
    public void Create_validator_rejects_absent_sort_order_and_null_last_translation()
    {
        var validator = new CreateShippingTypeCommandValidator();

        Assert.False(validator.Validate(new CreateShippingTypeCommand(new ShippingTypeWriteModel(
            "ordinary", null, null, null, [Input(DefaultLanguage, "عادی")]))).IsValid);

        Assert.False(validator.Validate(new CreateShippingTypeCommand(new ShippingTypeWriteModel(
            "ordinary", 10, null, null, [Input(DefaultLanguage, "عادی"), null]))).IsValid);

        Assert.True(validator.Validate(new CreateShippingTypeCommand(new ShippingTypeWriteModel(
            "ordinary", 10, null, null, [Input(DefaultLanguage, "عادی")]))).IsValid);
    }

    [Fact]
    public void Update_validator_requires_explicit_active_sort_revision_translations()
    {
        var validator = new UpdateShippingTypeCommandValidator();
        var id = UuidV7.New();

        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, 0, null, 1, []))).IsValid);
        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, null, false, 1, []))).IsValid);
        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, 0, false, null, []))).IsValid);
        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, 0, false, 1, null))).IsValid);
        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            Guid.Empty, new ShippingTypeWriteModel(null, 0, false, 1, []))).IsValid);
        Assert.True(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, 0, false, 1, []))).IsValid);
    }

    [Fact]
    public void Update_validator_rejects_duplicate_blank_and_overlength_text()
    {
        var validator = new UpdateShippingTypeCommandValidator();
        var id = UuidV7.New();

        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, 0, false, 1,
                [Input(DefaultLanguage, "عادی"), Input(DefaultLanguage, "تکراری")]))).IsValid);

        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, 0, false, 1, [Input(DefaultLanguage, "  ")]))).IsValid);

        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            id, new ShippingTypeWriteModel(null, 0, false, 1,
                [Input(DefaultLanguage, "عادی", new string('x', ShippingType.DescriptionMaxLength + 1))]))).IsValid);
    }

    [Fact]
    public void Get_query_validator_rejects_empty_id()
    {
        var validator = new GetShippingTypeQueryValidator();
        Assert.False(validator.Validate(new GetShippingTypeQuery(Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new GetShippingTypeQuery(UuidV7.New())).IsValid);
    }

    // ---- R196-02: store no-op / invalid last entry / revision (EF InMemory) ------------------

    [Fact]
    public async Task Identical_nonempty_patch_is_a_true_noop_for_revision_and_timestamp()
    {
        using var db = NewDbContext();
        var clock = new MutableClock(Now);
        var store = new ShippingTypeStore(db, clock, new UuidV7IdGenerator());
        var id = await CreateAsync(store);

        clock.UtcNow = Now.AddHours(3);

        var view = await store.UpdateAsync(
            id,
            1,
            null,
            10,
            false,
            [new ShippingTypePreparedTranslation(DefaultLanguage, "عادی", null)],
            DefaultLanguage,
            CancellationToken.None);

        Assert.Equal(1, view.Revision);
        Assert.Equal(Now, await UpdatedAtAsync(db, id));
    }

    [Fact]
    public async Task Stale_identical_request_is_still_a_conflict()
    {
        using var db = NewDbContext();
        var store = new ShippingTypeStore(db, new FixedClock(Now), new UuidV7IdGenerator());
        var id = await CreateAsync(store);

        var exception = await Assert.ThrowsAsync<SemanticException>(() => store.UpdateAsync(
            id,
            99,
            null,
            10,
            false,
            [new ShippingTypePreparedTranslation(DefaultLanguage, "عادی", null)],
            DefaultLanguage,
            CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.ShippingTypeRevisionConflict, exception.Error.Code);
    }

    [Fact]
    public async Task Invalid_last_entry_leaves_no_pending_edit_for_a_later_save()
    {
        using var db = NewDbContext();
        var store = new ShippingTypeStore(db, new FixedClock(Now), new UuidV7IdGenerator());
        var id = await CreateAsync(store);

        var exception = await Assert.ThrowsAsync<SemanticException>(() => store.UpdateAsync(
            id,
            1,
            null,
            10,
            false,
            [
                new ShippingTypePreparedTranslation(SecondLanguage, "Ordinary", null),
                new ShippingTypePreparedTranslation(DefaultLanguage, " ", null),
            ],
            DefaultLanguage,
            CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.ShippingTypeTranslationInvalid, exception.Error.Code);
        Assert.Empty(db.ChangeTracker.Entries<ShippingType>().Where(e => e.State != EntityState.Unchanged));

        await db.SaveChangesAsync(CancellationToken.None);
        var entity = await db.ShippingTypes.AsNoTracking()
            .Include(x => x.Translations)
            .SingleAsync(x => x.ShippingTypeId == id);
        Assert.Equal(1, entity.Revision);
        Assert.Equal(1, entity.Translations.Count);
    }

    [Fact]
    public async Task Selected_language_patch_preserves_other_text()
    {
        using var db = NewDbContext();
        var store = new ShippingTypeStore(db, new FixedClock(Now), new UuidV7IdGenerator());
        var id = await CreateAsync(
            store,
            new ShippingTypePreparedTranslation(SecondLanguage, "Ordinary", "kept"));

        var view = await store.UpdateAsync(
            id,
            1,
            null,
            10,
            false,
            [new ShippingTypePreparedTranslation(DefaultLanguage, "عادی ویرایش", null)],
            DefaultLanguage,
            CancellationToken.None);

        Assert.Equal(2, view.Revision);
        Assert.Equal("عادی ویرایش", view.Translations.Single(x => x.LanguageId == DefaultLanguage).Name);
        Assert.Equal("kept", view.Translations.Single(x => x.LanguageId == SecondLanguage).Description);
    }

    [Fact]
    public async Task Activation_requires_default_language_name()
    {
        using var db = NewDbContext();
        var store = new ShippingTypeStore(db, new FixedClock(Now), new UuidV7IdGenerator());
        var id = await CreateAsync(store);

        var exception = await Assert.ThrowsAsync<SemanticException>(() => store.UpdateAsync(
            id,
            1,
            null,
            10,
            true,
            [],
            SecondLanguage,
            CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.ShippingTypeDefaultLanguageNameRequired, exception.Error.Code);
    }

    [Fact]
    public async Task Immutable_code_rejects_a_different_body_code()
    {
        using var db = NewDbContext();
        var store = new ShippingTypeStore(db, new FixedClock(Now), new UuidV7IdGenerator());
        var id = await CreateAsync(store);

        var exception = await Assert.ThrowsAsync<SemanticException>(() => store.UpdateAsync(
            id,
            1,
            "renamed",
            10,
            false,
            [],
            DefaultLanguage,
            CancellationToken.None));

        Assert.Equal(FulfillmentErrorCodes.ShippingTypeCodeInvalid, exception.Error.Code);
    }

    [Fact]
    public async Task Contract_reader_lists_only_active_and_returns_inactive_by_id()
    {
        using var db = NewDbContext();
        var store = new ShippingTypeStore(db, new FixedClock(Now), new UuidV7IdGenerator());
        var id = await CreateAsync(store);

        Assert.Empty(await store.ListActiveAsync(CancellationToken.None));

        await store.UpdateAsync(id, 1, null, 10, true, [], DefaultLanguage, CancellationToken.None);
        Assert.Single(await store.ListActiveAsync(CancellationToken.None));
        Assert.Single(await store.GetByIdsAsync([id], CancellationToken.None));

        await store.UpdateAsync(id, 2, null, 10, false, [], DefaultLanguage, CancellationToken.None);
        Assert.Empty(await store.ListActiveAsync(CancellationToken.None));
        var inactive = await store.GetByIdsAsync([id], CancellationToken.None);
        Assert.False(inactive.Single().IsActive);
    }

    // ---- helpers -----------------------------------------------------------------------------

    private static ShippingType CreateEntity() => ShippingType.Create(
        UuidV7.New(),
        "ordinary",
        10,
        [ShippingTypeTranslation.Create(UuidV7.New(), DefaultLanguage, "عادی", null)],
        Now);

    private static ShippingTypeLanguageFacts Facts(
        params (Guid LanguageId, bool Active, bool Default, int Order)[] languages) =>
        new(languages
            .Select(x => new ShippingTypeLanguageFact(x.LanguageId, x.Active, x.Default, x.Order))
            .ToList());

    private static ShippingTypeTranslationInput Input(
        Guid languageId,
        string? name,
        string? description = null) => new(languageId, name, description);

    private static async Task<ShippingTypeWriteModel> ReadAsync(string json)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return await ShippingTypeBodyReader.ReadAsync(context.Request, CancellationToken.None);
    }

    private static FulfillmentDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase($"tb196r1-{Guid.NewGuid():N}")
            .Options);

    private static async Task<Guid> CreateAsync(
        ShippingTypeStore store,
        params ShippingTypePreparedTranslation[] extra)
    {
        var translations = new List<ShippingTypePreparedTranslation>
        {
            new(DefaultLanguage, "عادی", null),
        };
        translations.AddRange(extra);

        return await store.CreateAsync("ordinary", 10, translations, CancellationToken.None);
    }

    private static async Task<DateTimeOffset> UpdatedAtAsync(FulfillmentDbContext db, Guid id) =>
        (await db.ShippingTypes.AsNoTracking().SingleAsync(x => x.ShippingTypeId == id)).UpdatedAt;

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
