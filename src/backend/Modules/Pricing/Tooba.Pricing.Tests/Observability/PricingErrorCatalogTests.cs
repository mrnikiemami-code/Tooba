using System.Globalization;
using System.Reflection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Endpoints.Errors;
using Tooba.Pricing.Endpoints.Resources;
using Xunit;

namespace Tooba.Pricing.Tests.Observability;

public sealed class PricingErrorCatalogTests
{
    private static readonly ErrorDefinitionCatalog Catalog = new(
    [
        new FoundationErrorCatalogContributor(),
        new PricingErrorCatalogContributor(),
    ]);

    private static readonly ResourceErrorMessageLocalizer Localizer = new(
        [new FoundationErrorResourceSet(), new PricingErrorResourceSet()],
        Array.Empty<IErrorMessageContributor>());

    [Fact]
    public void Every_public_pricing_error_code_has_descriptor_and_both_locales()
    {
        var codes = typeof(PricingErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
        Assert.NotEmpty(codes);
        foreach (var code in codes)
        {
            Assert.True(Catalog.TryGet(code, out var descriptor), "missing descriptor: " + code);
            Assert.True(descriptor.HttpStatus is >= 400 and <= 499, code);
            var en = Localizer.Localize(code, CultureInfo.GetCultureInfo("en"), new Dictionary<string, string?>(), "fallback");
            var fa = Localizer.Localize(code, CultureInfo.GetCultureInfo("fa"), new Dictionary<string, string?>(), "fallback");
            Assert.NotEqual("fallback", en);
            Assert.NotEqual("fallback", fa);
            Assert.NotEqual(en, fa);
        }
    }
}
