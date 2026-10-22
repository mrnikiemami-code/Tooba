using Tooba.BuildingBlocks;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Errors;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188 — authoring-context read projection and fail-closed
/// readiness, proven against controlled <see cref="IShippingAuthoringContextSource"/> fixtures.
/// </summary>
/// <remarks>
/// These are handler-level proofs of the projection contract. They are not a claim that a real
/// store database was exercised; the source adapter and its real Contracts calls are proven
/// separately. Every fixture is named as fake data, and the readiness cases assert an explicit
/// failure instead of any fabricated fa/en, currency or marketplace fallback.
/// </remarks>
public sealed class ShippingAuthoringContextQueryTests
{
    private static readonly Guid Fa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid En = Guid.Parse("01900000-0000-7000-8000-00000000aa02");
    private static readonly Guid Ar = Guid.Parse("01900000-0000-7000-8000-00000000aa03");
    private static readonly Guid Ru = Guid.Parse("01900000-0000-7000-8000-00000000aa04");

    [Fact]
    public async Task One_active_non_fa_language_is_returned_from_the_source_unchanged()
    {
        var view = await Read(Source(ToobaEdition.SingleStore, "IRR", [
            Language(Ru, "ru", "ru-RU", "ru", true, true, "ltr", "Russian", "Русский", 0),
        ]));

        Assert.Equal("SingleStore", view.Edition);
        Assert.Equal("IRR", view.DefaultCurrency);
        var language = Assert.Single(view.ContentLanguages);
        Assert.Equal(Ru, language.LanguageId);
        Assert.Equal("ru", language.Code);
        Assert.Equal("ru-RU", language.Culture);
        Assert.Equal("ru", language.UrlPrefix);
        Assert.Equal("ltr", language.Direction);
        Assert.Equal("Russian", language.DisplayName);
        Assert.Equal("Русский", language.NativeName);
        Assert.Equal(0, language.SortOrder);
        Assert.True(language.IsDefault);
        Assert.Equal(Ru, view.DefaultContentLanguageId);

        // No fa/en preview fixture is injected anywhere.
        Assert.DoesNotContain(view.ContentLanguages, candidate => candidate.Code is "fa" or "en");
    }

    [Fact]
    public async Task Two_active_languages_keep_the_owner_declared_order()
    {
        var view = await Read(Source(ToobaEdition.Marketplace, "AED", [
            Language(En, "en", "en-US", "en", true, false, "ltr", "English", "English", 2),
            Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", 1),
        ]));

        Assert.Equal("Marketplace", view.Edition);
        Assert.Equal("AED", view.DefaultCurrency);
        Assert.Equal(new[] { Fa, En }, view.ContentLanguages.Select(language => language.LanguageId).ToArray());
        Assert.Equal(new[] { 1, 2 }, view.ContentLanguages.Select(language => language.SortOrder).ToArray());
        Assert.Equal(new[] { "rtl", "ltr" }, view.ContentLanguages.Select(language => language.Direction).ToArray());
    }

    [Fact]
    public async Task Three_plus_languages_include_rtl_and_ltr_and_exclude_the_inactive_row()
    {
        var view = await Read(Source(ToobaEdition.SingleStore, "USD", [
            Language(Ar, "ar", "ar-SA", "ar", true, false, "rtl", "Arabic", "العربية", 3),
            Language(En, "en", "en-US", "en", true, false, "ltr", "English", "English", 2),
            Language(Ru, "ru", "ru-RU", "ru", false, false, "ltr", "Russian", "Русский", 1),
            Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", 4),
        ]));

        Assert.Equal(new[] { En, Ar, Fa }, view.ContentLanguages.Select(language => language.LanguageId).ToArray());
        Assert.DoesNotContain(view.ContentLanguages, language => language.LanguageId == Ru);
        Assert.Equal(new[] { "ltr", "rtl", "rtl" }, view.ContentLanguages.Select(language => language.Direction).ToArray());

        // The default is NOT the first row: it is whichever row the owner flagged, never a positional
        // or locale-derived guess.
        Assert.Equal(Fa, view.DefaultContentLanguageId);
        Assert.NotEqual(view.ContentLanguages[0].LanguageId, view.DefaultContentLanguageId);
        Assert.Single(view.ContentLanguages, language => language.IsDefault);
    }

    [Fact]
    public async Task Language_identity_and_default_are_independent_of_the_interface_locale()
    {
        // Same authoritative data, no locale input exists at all: the projection cannot vary with it.
        var source = Source(ToobaEdition.SingleStore, "IRR", [
            Language(En, "en", "en-US", "en", true, true, "ltr", "English", "English", 0),
        ]);

        var first = await Read(source);
        var second = await Read(source);

        Assert.Equal(first.DefaultContentLanguageId, second.DefaultContentLanguageId);
        Assert.Equal("en", Assert.Single(second.ContentLanguages).Code);
        Assert.DoesNotContain(typeof(GetShippingAuthoringContextQuery).GetProperties(), property =>
            property.Name.Contains("Locale", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Culture", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Language", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Currency", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Mode", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task The_query_carries_no_caller_controlled_input()
    {
        // Zero members: no owner/mode/tenant/language/currency override can even be expressed.
        Assert.Empty(typeof(GetShippingAuthoringContextQuery).GetProperties());
        Assert.Empty(typeof(GetShippingAuthoringContextQuery).GetConstructors()
            .Single(constructor => constructor.GetParameters().Length == 0)
            .GetParameters());
        Assert.Contains(
            typeof(GetShippingAuthoringContextQuery).GetInterfaces(),
            contract => contract.IsGenericType
                        && contract.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>));
    }

    [Theory]
    [InlineData("no-active")]
    [InlineData("active-flag-absent")]
    [InlineData("inactive-only-default")]
    [InlineData("no-active-default")]
    [InlineData("multiple-active-defaults")]
    [InlineData("missing-direction")]
    [InlineData("unknown-direction")]
    [InlineData("missing-display-name")]
    [InlineData("missing-native-name")]
    [InlineData("missing-sort-order")]
    [InlineData("empty-language-id")]
    public async Task Unusable_or_ambiguous_configuration_is_explicitly_unavailable(string scenario)
    {
        var source = scenario switch
        {
            "no-active" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", false, true, "rtl", "Persian", "فارسی", 0),
            ]),
            "active-flag-absent" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", null, true, "rtl", "Persian", "فارسی", 0),
            ]),
            "inactive-only-default" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(En, "en", "en-US", "en", true, false, "ltr", "English", "English", 0),
                Language(Fa, "fa", "fa-IR", "fa", false, true, "rtl", "Persian", "فارسی", 1),
            ]),
            "no-active-default" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(En, "en", "en-US", "en", true, false, "ltr", "English", "English", 0),
            ]),
            "multiple-active-defaults" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", 0),
                Language(En, "en", "en-US", "en", true, true, "ltr", "English", "English", 1),
            ]),
            "missing-direction" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", true, true, null, "Persian", "فارسی", 0),
            ]),
            "unknown-direction" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", true, true, "sideways", "Persian", "فارسی", 0),
            ]),
            "missing-display-name" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", null, "فارسی", 0),
            ]),
            "missing-native-name" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", " ", 0),
            ]),
            "missing-sort-order" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", null),
            ]),
            "empty-language-id" => Source(ToobaEdition.SingleStore, "IRR", [
                Language(Guid.Empty, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", 0),
            ]),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        var result = await new GetShippingAuthoringContextHandler(source)
            .Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringConfigurationUnavailable, result.FirstError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_effective_currency_is_unavailable(string? currency)
    {
        var result = await new GetShippingAuthoringContextHandler(
                Source(ToobaEdition.SingleStore, currency, [
                    Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", 0),
                ]))
            .Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringConfigurationUnavailable, result.FirstError.Code);
    }

    [Theory]
    [InlineData(ToobaEdition.Unset)]
    [InlineData((ToobaEdition)99)]
    public async Task Unresolved_or_inconsistent_edition_is_unavailable(ToobaEdition edition)
    {
        var result = await new GetShippingAuthoringContextHandler(
                Source(edition, "IRR", [
                    Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", 0),
                ]))
            .Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringConfigurationUnavailable, result.FirstError.Code);
    }

    [Fact]
    public async Task A_dependency_exception_propagates_instead_of_becoming_a_fake_success()
    {
        var failure = new InvalidOperationException("dependency unavailable");
        var handler = new GetShippingAuthoringContextHandler(new ThrowingSource(failure));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None));

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task Caller_cancellation_is_preserved()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var handler = new GetShippingAuthoringContextHandler(
            new ThrowingSource(new OperationCanceledException(cancellation.Token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.Handle(new GetShippingAuthoringContextQuery(), cancellation.Token));
    }

    [Fact]
    public async Task Two_independently_resolved_sources_do_not_leak_into_each_other()
    {
        var singleStore = new GetShippingAuthoringContextHandler(Source(ToobaEdition.SingleStore, "IRR", [
            Language(Fa, "fa", "fa-IR", "fa", true, true, "rtl", "Persian", "فارسی", 0),
        ]));
        var marketplace = new GetShippingAuthoringContextHandler(Source(ToobaEdition.Marketplace, "EUR", [
            Language(En, "en", "en-US", "en", true, true, "ltr", "English", "English", 0),
            Language(Ru, "ru", "ru-RU", "ru", true, false, "ltr", "Russian", "Русский", 1),
        ]));

        var first = await singleStore.Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None);
        var second = await marketplace.Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None);
        var firstAgain = await singleStore.Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None);

        Assert.Equal("SingleStore", first.Value.Edition);
        Assert.Equal("IRR", first.Value.DefaultCurrency);
        Assert.Equal(new[] { "fa" }, first.Value.ContentLanguages.Select(language => language.Code).ToArray());

        Assert.Equal("Marketplace", second.Value.Edition);
        Assert.Equal("EUR", second.Value.DefaultCurrency);
        Assert.Equal(new[] { "en", "ru" }, second.Value.ContentLanguages.Select(language => language.Code).ToArray());

        // No shared/cached state: the first handler still reports its own authority.
        Assert.Equal(first.Value.Edition, firstAgain.Value.Edition);
        Assert.Equal(first.Value.DefaultCurrency, firstAgain.Value.DefaultCurrency);
        Assert.Equal(first.Value.DefaultContentLanguageId, firstAgain.Value.DefaultContentLanguageId);
        Assert.Equal(
            first.Value.ContentLanguages.Select(language => language.Code).ToArray(),
            firstAgain.Value.ContentLanguages.Select(language => language.Code).ToArray());
    }

    [Fact]
    public void Edition_values_are_explicit_stable_strings()
    {
        // The view must not leak enum names/numbers; only the two accepted editions are expressible.
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Application",
            "Authoring",
            "Queries",
            "GetShippingAuthoringContextQuery.cs"));

        Assert.Contains("\"SingleStore\"", source, StringComparison.Ordinal);
        Assert.Contains("\"Marketplace\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("canManageSellerRules", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("canSelectOwner", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OwnerId", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SellerId", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TenantId", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionReference", source, StringComparison.Ordinal);
    }

    private static async Task<Tooba.Shipping.Application.Authoring.Models.ShippingAuthoringContextView> Read(
        IShippingAuthoringContextSource source)
    {
        var result = await new GetShippingAuthoringContextHandler(source)
            .Handle(new GetShippingAuthoringContextQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess, result.IsFailure ? result.FirstError.Code : null);
        return result.Value;
    }

    private static FixedSource Source(
        ToobaEdition edition,
        string? currency,
        IReadOnlyList<ShippingAuthoringLanguageSource> languages) =>
        new(new ShippingAuthoringContextSourceSnapshot(edition, currency, languages));

    private static ShippingAuthoringLanguageSource Language(
        Guid id,
        string code,
        string culture,
        string urlPrefix,
        bool? isActive,
        bool isDefault,
        string? direction,
        string? displayName,
        string? nativeName,
        int? sortOrder) =>
        new(id, code, culture, urlPrefix, isActive, isDefault, direction, displayName, nativeName, sortOrder);

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class FixedSource(ShippingAuthoringContextSourceSnapshot snapshot) : IShippingAuthoringContextSource
    {
        public Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class ThrowingSource(Exception failure) : IShippingAuthoringContextSource
    {
        public Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromException<ShippingAuthoringContextSourceSnapshot>(failure);
    }
}
