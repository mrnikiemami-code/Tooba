using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.ShippingTypes;
using Tooba.Fulfillment.Application.ShippingTypes.Validators;
using Tooba.Fulfillment.Domain.Aggregates;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196 — گاردهای bounded نوع ارسال عملیاتی.
/// </summary>
/// <remarks>
/// این تست فقط قواعد دامنه و شکل transport را اثبات می‌کند. اثبات مسیرها/یونیکت‌کد/همزمانی واقعی
/// PostgreSQL در این گذر اجرا نشده و در RESULT صادقانه به‌عنوان محدودیت ثبت شده است.
/// </remarks>
public sealed class ShippingTypeCatalogRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid DefaultLanguage = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondLanguage = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData("ordinary")]
    [InlineData("heavy_2")]
    [InlineData("a")]
    public void Canonical_ascii_codes_are_accepted(string code) =>
        Assert.Equal(code, ShippingType.TryNormalizeCode(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ordinary")]
    [InlineData("2heavy")]
    [InlineData("_heavy")]
    [InlineData("heavy-type")]
    [InlineData("heavy type")]
    [InlineData("سنگین")]
    public void Malformed_codes_are_rejected_not_renamed(string? code) =>
        Assert.Null(ShippingType.TryNormalizeCode(code));

    [Fact]
    public void Code_over_64_characters_is_rejected() =>
        Assert.Null(ShippingType.TryNormalizeCode(new string('a', ShippingType.CodeMaxLength + 1)));

    [Fact]
    public void Create_is_inactive_with_revision_one()
    {
        var entity = ShippingType.Create(
            UuidV7.New(),
            "ordinary",
            10,
            [ShippingTypeTranslation.Create(DefaultLanguage, "عادی", null)],
            Now);

        Assert.False(entity.IsActive);
        Assert.Equal(1, entity.Revision);
        Assert.Equal(Now, entity.CreatedAt);
        Assert.Equal(Now, entity.UpdatedAt);
        Assert.Single(entity.Translations);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ShippingType.SortOrderMaxValue + 1)]
    public void Sort_order_outside_bounds_is_rejected(int sortOrder) =>
        Assert.Throws<SemanticException>(() => ShippingType.Create(
            UuidV7.New(),
            "ordinary",
            sortOrder,
            [ShippingTypeTranslation.Create(DefaultLanguage, "عادی", null)],
            Now));

    [Fact]
    public void Blank_name_and_empty_language_are_rejected()
    {
        Assert.Throws<SemanticException>(() =>
            ShippingTypeTranslation.Create(DefaultLanguage, "   ", null));
        Assert.Throws<SemanticException>(() =>
            ShippingTypeTranslation.Create(Guid.Empty, "عادی", null));
    }

    [Fact]
    public void Update_bumps_revision_and_replaces_only_selected_text()
    {
        var entity = ShippingType.Create(
            UuidV7.New(),
            "ordinary",
            10,
            [
                ShippingTypeTranslation.Create(DefaultLanguage, "عادی", "متن قبلی"),
                ShippingTypeTranslation.Create(SecondLanguage, "Ordinary", "kept"),
            ],
            Now);

        entity.ReplaceTranslation(SecondLanguage, "Ordinary", "کاملاً متفاوت");
        entity.Update(20, true, Now.AddMinutes(5));

        Assert.Equal(2, entity.Revision);
        Assert.True(entity.IsActive);
        Assert.Equal(20, entity.SortOrder);
        Assert.Equal("عادی", entity.Translations.Single(x => x.LanguageId == DefaultLanguage).Name);
        Assert.Equal("متن قبلی", entity.Translations.Single(x => x.LanguageId == DefaultLanguage).Description);
        Assert.Equal("کاملاً متفاوت", entity.Translations.Single(x => x.LanguageId == SecondLanguage).Description);
    }

    [Fact]
    public void Create_validator_accepts_well_formed_body()
    {
        var validator = new CreateShippingTypeCommandValidator();
        var command = new CreateShippingTypeCommand(new ShippingTypeCreateModel(
            "ordinary",
            10,
            [new ShippingTypeTranslationInput(SecondLanguage, "Ordinary", null)]));

        Assert.True(validator.Validate(command).IsValid);
    }

    [Fact]
    public void Update_validator_rejects_absent_revision_and_bad_sort()
    {
        var validator = new UpdateShippingTypeCommandValidator();
        var bad = validator.Validate(new UpdateShippingTypeCommand(
            Guid.Empty,
            new ShippingTypeUpdateModel(0, null, -1, false, [])));
        Assert.False(bad.IsValid);

        var good = validator.Validate(new UpdateShippingTypeCommand(
            UuidV7.New(),
            new ShippingTypeUpdateModel(1, null, 0, false, [])));
        Assert.True(good.IsValid);
    }

    [Fact]
    public void Update_validator_rejects_duplicate_languages()
    {
        var validator = new UpdateShippingTypeCommandValidator();
        var result = validator.Validate(new UpdateShippingTypeCommand(
            UuidV7.New(),
            new ShippingTypeUpdateModel(
                1,
                null,
                0,
                false,
                [
                    new ShippingTypeTranslationInput(DefaultLanguage, "عادی", null),
                    new ShippingTypeTranslationInput(DefaultLanguage, "تکراری", null),
                ])));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Update_validator_rejects_blank_name_and_overlength_description()
    {
        var validator = new UpdateShippingTypeCommandValidator();

        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            UuidV7.New(),
            new ShippingTypeUpdateModel(
                1, null, 0, false,
                [new ShippingTypeTranslationInput(DefaultLanguage, "  ", null)]))).IsValid);

        Assert.False(validator.Validate(new UpdateShippingTypeCommand(
            UuidV7.New(),
            new ShippingTypeUpdateModel(
                1, null, 0, false,
                [
                    new ShippingTypeTranslationInput(
                        DefaultLanguage,
                        "عادی",
                        new string('x', ShippingType.DescriptionMaxLength + 1)),
                ]))).IsValid);
    }
}
