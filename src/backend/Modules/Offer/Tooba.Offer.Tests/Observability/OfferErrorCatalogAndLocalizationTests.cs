using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Offer.Contracts;
using Tooba.Offer.Endpoints.Errors;
using Tooba.Offer.Endpoints.Resources;
using Xunit;

namespace Tooba.Offer.Tests.Observability;

public sealed class OfferErrorCatalogAndLocalizationTests
{
    private static readonly ErrorDefinitionCatalog Catalog = new(
    [
        new FoundationErrorCatalogContributor(),
        new OfferErrorCatalogContributor(),
    ]);

    private static readonly ResourceErrorMessageLocalizer Localizer = new(
        [new FoundationErrorResourceSet(), new OfferErrorResourceSet()],
        Array.Empty<IErrorMessageContributor>());

    [Theory]
    [InlineData(OfferErrorCodes.NotFound, StatusCodes.Status404NotFound, ErrorClassification.NotFound)]
    [InlineData(OfferErrorCodes.DuplicateActiveListing, StatusCodes.Status409Conflict, ErrorClassification.Conflict)]
    [InlineData(OfferErrorCodes.ReturnPolicyOverrideDenied, StatusCodes.Status403Forbidden, ErrorClassification.Forbidden)]
    [InlineData(OfferErrorCodes.MinQuantityInvalid, StatusCodes.Status400BadRequest, ErrorClassification.Business)]
    [InlineData(OfferErrorCodes.CatalogVariantMissing, StatusCodes.Status404NotFound, ErrorClassification.NotFound)]
    [InlineData(OfferErrorCodes.ArchivedCannotActivate, StatusCodes.Status409Conflict, ErrorClassification.Conflict)]
    public void Explicit_offer_descriptor_maps_expected_status(
        string code,
        int status,
        ErrorClassification classification)
    {
        Assert.True(Catalog.TryGet(code, out var descriptor));
        Assert.Equal(status, descriptor.HttpStatus);
        Assert.Equal(classification, descriptor.Classification);
        var mapped = new SafeErrorMapper(Catalog).Map(new SemanticException(new SemanticError(code)));
        Assert.Equal(status, mapped.StatusCode);
        Assert.Equal(classification, mapped.Classification);
    }

    [Fact]
    public void Every_public_offer_error_code_constant_has_descriptor_and_english_resource()
    {
        var codes = typeof(OfferErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(codes);
        foreach (var code in codes)
        {
            Assert.True(Catalog.TryGet(code, out _), "missing descriptor: " + code);
            var en = Localizer.Localize(code, CultureInfo.GetCultureInfo("en"), new Dictionary<string, string?>(), "fallback");
            Assert.False(string.Equals(en, "fallback", StringComparison.Ordinal), "missing English resource: " + code);
            Assert.DoesNotContain("fallback", en, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Persian_resources_cover_offer_user_facing_errors()
    {
        var sample = new[]
        {
            OfferErrorCodes.NotFound,
            OfferErrorCodes.MinQuantityInvalid,
            OfferErrorCodes.DuplicateActiveListing,
            OfferErrorCodes.ReturnPolicyOverrideDenied,
            OfferErrorCodes.CustomReturnWindowOutOfRange,
        };
        foreach (var code in sample)
        {
            var args = code == OfferErrorCodes.CustomReturnWindowOutOfRange
                ? new Dictionary<string, string?> { ["min"] = "1", ["max"] = "30" }
                : new Dictionary<string, string?>();
            var fa = Localizer.Localize(code, CultureInfo.GetCultureInfo("fa"), args, "fallback");
            Assert.True(fa.Any(ch => ch is >= '\u0600' and <= '\u06ff'), "expected Persian for " + code);
        }
    }

    [Fact]
    public void Tr_TR_missing_resource_uses_default_english_not_persian()
    {
        var title = Localizer.Localize(
            OfferErrorCodes.NotFound,
            CultureInfo.GetCultureInfo("tr-TR"),
            new Dictionary<string, string?>(),
            "fallback");
        Assert.Equal("Offer was not found.", title);
        Assert.DoesNotContain(title, ch => ch is >= '\u0600' and <= '\u06ff');
    }

    [Fact]
    public void Parent_cultures_en_US_and_fa_IR_resolve_resources()
    {
        var enUs = Localizer.Localize(
            OfferErrorCodes.SellerMissing,
            CultureInfo.GetCultureInfo("en-US"),
            new Dictionary<string, string?>(),
            "fallback");
        var faIr = Localizer.Localize(
            OfferErrorCodes.SellerMissing,
            CultureInfo.GetCultureInfo("fa-IR"),
            new Dictionary<string, string?>(),
            "fallback");
        Assert.Equal("Seller was not found.", enUs);
        Assert.Contains("فروشنده", faIr, StringComparison.Ordinal);
    }

    [Fact]
    public void Named_argument_formatting_works_for_return_window()
    {
        var title = Localizer.Localize(
            OfferErrorCodes.CustomReturnWindowOutOfRange,
            CultureInfo.GetCultureInfo("en"),
            new Dictionary<string, string?> { ["min"] = "3", ["max"] = "14" },
            "fallback");
        Assert.Contains("3", title, StringComparison.Ordinal);
        Assert.Contains("14", title, StringComparison.Ordinal);
    }
}
