using System.Diagnostics;
using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Application.Shipping.Validators;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.DependencyInjection;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Localization.Contracts.Ports;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5 — actual <see cref="FulfillmentModule"/> DI registration,
/// the adapter-backed language lifecycle for the type catalog, and real listener-observed module spans.
/// </summary>
/// <remarks>
/// <para>
/// The production registration entry <see cref="FulfillmentModule.AddServices"/> is invoked for real, so
/// <c>ShippingTypeStore</c> and its two aliases, <c>LocalizationShippingTypeLanguageFacts</c> and the
/// canonical <see cref="ModuleCallTracer"/> are the module's own descriptors. Only data boundaries are
/// replaced: the Npgsql <c>FulfillmentDbContext</c> option-config action, <see cref="IClock"/> and
/// <see cref="ILanguageLookup"/>. No store/reader/adapter/gate registration is re-created by the test.
/// </para>
/// <para>
/// EF InMemory is used and explicitly labelled as such; it is not PostgreSQL, migration, snapshot or
/// constraint proof. The language registry is a controlled <see cref="ILanguageLookup"/> fixture, not a
/// live merchant Localization database and not UI registry acceptance.
/// </para>
/// </remarks>
[CollectionDefinition(nameof(ShippingTypeRegistryCollection), DisableParallelization = true)]
public sealed class ShippingTypeRegistryCollection;

[Collection(nameof(ShippingTypeRegistryCollection))]
public sealed class ShippingTypeRegistryAndModuleDiTests
{
    private const string Sentinel = "sentinel-secret-registry-message-7b21";

    private static readonly Guid Fa = Guid.Parse("01900000-0000-7000-8000-00000000bb01");
    private static readonly Guid En = Guid.Parse("01900000-0000-7000-8000-00000000bb02");
    private static readonly Guid Ar = Guid.Parse("01900000-0000-7000-8000-00000000bb03");
    private static readonly Guid De = Guid.Parse("01900000-0000-7000-8000-00000000bb04");

    // ---------------------------------------------------------------------------------------------
    // A. Real production registration (only data boundaries overridden)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Actual_module_registration_wires_the_capability_and_scoped_aliases()
    {
        using var harness = Harness.Create([Row(Fa, "fa", "fa-IR", true, true, 1)]);

        var scope = harness.Scope;
        var concrete = scope.ServiceProvider.GetRequiredService<ShippingTypeStore>();
        var writerAlias = scope.ServiceProvider.GetRequiredService<IShippingTypeStore>();
        var readerAlias = scope.ServiceProvider.GetRequiredService<IShippingTypeReader>();

        // The module's own alias factories point at ONE scoped instance.
        Assert.Same(concrete, writerAlias);
        Assert.Same(concrete, readerAlias);

        using var otherScope = harness.Provider.CreateScope();
        Assert.NotSame(concrete, otherScope.ServiceProvider.GetRequiredService<ShippingTypeStore>());

        // The adapter and the canonical tracer are the production implementations from the same module.
        Assert.IsType<LocalizationShippingTypeLanguageFacts>(
            scope.ServiceProvider.GetRequiredService<IShippingTypeLanguageFacts>());
        Assert.IsType<ModuleCallTracer>(scope.ServiceProvider.GetRequiredService<IModuleCallTracer>());

        // Real handlers and validators resolve from the real CQRS registration.
        Assert.IsType<CreateShippingTypeHandler>(scope.ServiceProvider.GetRequiredService<
            IRequestHandler<CreateShippingTypeCommand, Result<ShippingTypeView>>>());
        Assert.IsType<UpdateShippingTypeHandler>(scope.ServiceProvider.GetRequiredService<
            IRequestHandler<UpdateShippingTypeCommand, Result<ShippingTypeView>>>());
        Assert.IsType<GetShippingTypeHandler>(scope.ServiceProvider.GetRequiredService<
            IRequestHandler<GetShippingTypeQuery, Result<ShippingTypeView>>>());
        Assert.IsType<ListShippingTypesHandler>(scope.ServiceProvider.GetRequiredService<
            IRequestHandler<ListShippingTypesQuery, Result<IReadOnlyList<ShippingTypeSummaryView>>>>());
        Assert.Contains(
            scope.ServiceProvider.GetServices<FluentValidation.IValidator<CreateShippingTypeCommand>>(),
            validator => validator is CreateShippingTypeCommandValidator);

        // The overridden data boundary is exactly one InMemory context, and the store really works on it.
        var created = await harness.CreateAsync("ordinary", 0, (Fa, "عادی", "توضیح"));
        Assert.True(created.IsSuccess, created.IsFailure ? created.FirstError.Code : null);

        var view = await harness.GetAsync(created.Value.ShippingTypeId);
        Assert.NotNull(view);
        Assert.Equal("ordinary", view!.Code);
        Assert.Equal(Fa, view.DefaultLanguageId);
        Assert.True(view.HasDefaultLanguageName);
    }

    [Fact]
    public void Production_registration_has_no_duplicate_capability_owner()
    {
        using var harness = Harness.Create([Row(Fa, "fa", "fa-IR", true, true, 1)]);

        // Exactly one descriptor per capability seam: the module's own store, both aliases, the adapter.
        Assert.Single(harness.Services, d => d.ServiceType == typeof(ShippingTypeStore));
        Assert.Single(harness.Services, d => d.ServiceType == typeof(IShippingTypeStore));
        Assert.Single(harness.Services, d => d.ServiceType == typeof(IShippingTypeReader));
        Assert.Single(harness.Services, d => d.ServiceType == typeof(IShippingTypeLanguageFacts));
        Assert.Single(harness.Services, d => d.ServiceType == typeof(IModuleCallTracer));

        // Single owned error-catalog contributor for this module (no second resource owner).
        Assert.Single(
            harness.Services,
            d => d.ServiceType == typeof(IErrorCatalogContributor)
                 && d.ImplementationType == typeof(Infrastructure.Errors.FulfillmentErrorCatalogContributor));
    }

    [Fact]
    public void The_module_migrator_is_registered_but_never_executed_or_resolved()
    {
        using var harness = Harness.Create([Row(Fa, "fa", "fa-IR", true, true, 1)]);
        var executableMigrators = harness.Services
            .Where(descriptor => descriptor.ServiceType.Name.Contains("Migrator", StringComparison.Ordinal))
            .Select(descriptor => descriptor.ServiceType.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Registration evidence only: this slice resolves no migrator, opens no provider connection and
        // runs no schema discovery/migration.
        Assert.NotEmpty(executableMigrators);
        Assert.DoesNotContain(executableMigrators, name =>
            name.Contains("MigrationStep", StringComparison.Ordinal) && name.Contains("Exec", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------
    // B. Adapter-backed language lifecycle
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task One_active_non_fa_default_is_used_without_any_fa_fallback()
    {
        using var harness = Harness.Create([
            Row(Fa, "fa", "fa-IR", true, false, 1),
            Row(Ar, "ar", "ar-SA", true, true, 2),
        ]);

        var created = await harness.CreateAsync("ordinary", 0, (Ar, "  نوع عادی  ", "توضیح"));

        Assert.True(created.IsSuccess, created.IsFailure ? created.FirstError.Code : null);
        Assert.Equal(Ar, created.Value.DefaultLanguageId);
        Assert.True(created.Value.HasDefaultLanguageName);
        var translation = Assert.Single(created.Value.Translations);
        Assert.Equal(Ar, translation.LanguageId);
        Assert.Equal("نوع عادی", translation.Name);          // canonical trim only
        Assert.Equal("توضیح", translation.Description);
        Assert.DoesNotContain(created.Value.Translations, x => x.LanguageId == Fa);
    }

    [Fact]
    public async Task Two_active_mixed_direction_languages_keep_first_text_and_clear_only_the_selected_pair()
    {
        using var harness = Harness.Create([
            Row(Fa, "fa", "fa-IR", true, true, 1),
            Row(En, "en", "en-US", true, false, 2),
        ]);

        var created = await harness.CreateAsync("ordinary", 0, (Fa, "عادی", "توضیح فارسی"));
        Assert.True(created.IsSuccess);
        var id = created.Value.ShippingTypeId;

        // Patch the LTR language; the RTL language must stay byte-identical.
        var addedEnglish = await harness.UpdateAsync(
            id, created.Value.Revision, isActive: false, sortOrder: 0, (En, "Ordinary", "English text"));
        Assert.True(addedEnglish.IsSuccess, addedEnglish.IsFailure ? addedEnglish.FirstError.Code : null);
        Assert.Equal("عادی", Translation(addedEnglish.Value, Fa).Name);
        Assert.Equal("توضیح فارسی", Translation(addedEnglish.Value, Fa).Description);
        Assert.Equal("Ordinary", Translation(addedEnglish.Value, En).Name);

        // An explicit null description clears ONLY the selected pair.
        var cleared = await harness.UpdateAsync(
            id, addedEnglish.Value.Revision, isActive: false, sortOrder: 0, (En, "Ordinary", null));
        Assert.True(cleared.IsSuccess);
        Assert.Equal("عادی", Translation(cleared.Value, Fa).Name);
        Assert.Equal("توضیح فارسی", Translation(cleared.Value, Fa).Description);
        Assert.Null(Translation(cleared.Value, En).Description);
        Assert.Equal("Ordinary", Translation(cleared.Value, En).Name);

        // An empty patch preserves all text.
        var preserved = await harness.UpdateAsync(
            id, cleared.Value.Revision, isActive: false, sortOrder: 0);
        Assert.True(preserved.IsSuccess);
        Assert.Equal(2, preserved.Value.Translations.Count);
        Assert.Equal("عادی", Translation(preserved.Value, Fa).Name);
        Assert.Equal("توضیح فارسی", Translation(preserved.Value, Fa).Description);
    }

    [Fact]
    public async Task Three_plus_languages_use_the_declared_non_first_default_and_reject_inactive_ids()
    {
        using var harness = Harness.Create([
            Row(Fa, "fa", "fa-IR", true, false, 1),
            Row(Ar, "ar", "ar-SA", true, true, 2),      // declared default is NOT the first row
            Row(En, "en", "en-US", true, false, 3),
            Row(De, "de", "de-DE", false, false, 4),    // known, inactive
        ]);

        var created = await harness.CreateAsync("ordinary", 0, (Ar, "النوع", null));
        Assert.True(created.IsSuccess, created.IsFailure ? created.FirstError.Code : null);
        Assert.Equal(Ar, created.Value.DefaultLanguageId);
        var id = created.Value.ShippingTypeId;

        var withEnglish = await harness.UpdateAsync(
            id, created.Value.Revision, isActive: false, sortOrder: 0, (En, "Ordinary", "text"));
        Assert.True(withEnglish.IsSuccess);

        // Editing an INACTIVE registry language is rejected canonically, with no state change.
        var revisionBefore = withEnglish.Value.Revision;
        var inactive = await harness.UpdateAsync(
            id, revisionBefore, isActive: true, sortOrder: 0, (De, "Typ", null));
        Assert.True(inactive.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeLanguageInvalid, inactive.FirstError.Code);

        var afterFailure = await harness.GetAsync(id);
        Assert.Equal(revisionBefore, afterFailure!.Revision);
        Assert.False(afterFailure.IsActive);
        Assert.DoesNotContain(afterFailure.Translations, x => x.LanguageId == De);
        Assert.Equal("النوع", Translation(afterFailure, Ar).Name);
    }

    [Fact]
    public async Task Deactivating_a_registry_language_retains_stored_text_and_blocks_edits_until_reactivated()
    {
        using var harness = Harness.Create([
            Row(Fa, "fa", "fa-IR", true, true, 1),
            Row(En, "en", "en-US", true, false, 2),
        ]);

        var created = await harness.CreateAsync("ordinary", 0, (Fa, "عادی", "فارسی"), (En, "Ordinary", "English"));
        Assert.True(created.IsSuccess);
        var id = created.Value.ShippingTypeId;

        harness.Registry.Deactivate(En);

        // GET keeps the exact raw text of the now-inactive language; no delete and no auto-translation.
        var afterDeactivate = await harness.GetAsync(id);
        Assert.Equal("Ordinary", Translation(afterDeactivate!, En).Name);
        Assert.Equal("English", Translation(afterDeactivate, En).Description);
        Assert.Equal(Fa, afterDeactivate.DefaultLanguageId);

        var revision = afterDeactivate.Revision;
        var edited = await harness.UpdateAsync(
            id, revision, isActive: true, sortOrder: 0, (En, "Ordinary", "changed"));
        Assert.True(edited.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeLanguageInvalid, edited.FirstError.Code);

        var unchanged = await harness.GetAsync(id);
        Assert.Equal(revision, unchanged!.Revision);
        Assert.Equal("English", Translation(unchanged, En).Description);

        harness.Registry.Activate(En);

        var afterReactivate = await harness.GetAsync(id);
        Assert.Equal("Ordinary", Translation(afterReactivate!, En).Name);
        Assert.Equal("English", Translation(afterReactivate, En).Description);
    }

    [Fact]
    public async Task Changing_the_registry_default_requires_the_new_default_name_before_activation()
    {
        using var harness = Harness.Create([
            Row(Fa, "fa", "fa-IR", true, true, 1),
            Row(En, "en", "en-US", true, false, 2),
        ]);

        var created = await harness.CreateAsync("ordinary", 0, (Fa, "عادی", null));
        Assert.True(created.IsSuccess);
        var id = created.Value.ShippingTypeId;

        harness.Registry.MakeDefault(En);

        var afterSwitch = await harness.GetAsync(id);
        Assert.Equal(En, afterSwitch!.DefaultLanguageId);
        Assert.False(afterSwitch.HasDefaultLanguageName);

        var activation = await harness.UpdateAsync(
            id, afterSwitch.Revision, isActive: true, sortOrder: 0);
        Assert.True(activation.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeDefaultLanguageNameRequired, activation.FirstError.Code);

        var stillInactive = await harness.GetAsync(id);
        Assert.False(stillInactive!.IsActive);

        var withName = await harness.UpdateAsync(
            id, stillInactive.Revision, isActive: true, sortOrder: 0, (En, "Ordinary", null));
        Assert.True(withName.IsSuccess, withName.IsFailure ? withName.FirstError.Code : null);
        Assert.True(withName.Value.IsActive);
        Assert.True(withName.Value.HasDefaultLanguageName);

        // Activation here is catalog state only; it makes no shipping method quote-ready.
        Assert.Equal(En, withName.Value.DefaultLanguageId);
        Assert.Equal("عادی", Translation(withName.Value, Fa).Name);
    }

    [Theory]
    [InlineData("missing-active", FulfillmentErrorCodes.ShippingTypeLanguageInvalid)]
    [InlineData("missing-sort", FulfillmentErrorCodes.ShippingTypeLanguageInvalid)]
    [InlineData("empty-id", FulfillmentErrorCodes.ShippingTypeLanguageInvalid)]
    [InlineData("duplicate-id", FulfillmentErrorCodes.ShippingTypeLanguageInvalid)]
    [InlineData("no-active", FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable)]
    [InlineData("no-default", FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable)]
    [InlineData("inactive-default", FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable)]
    [InlineData("multiple-defaults", FulfillmentErrorCodes.ShippingTypeDefaultLanguageAmbiguous)]
    public async Task Invalid_registry_is_rejected_canonically_without_guessing_or_persisting(
        string scenario,
        string expectedCode)
    {
        using var harness = Harness.Create(InvalidRows(scenario));

        var result = await harness.CreateAsync("ordinary", 0, (Fa, "عادی", null));

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.FirstError.Code);
        Assert.Equal(0, await harness.CountAsync());
    }

    [Fact]
    public async Task Raw_language_ids_are_stable_across_interface_culture()
    {
        using var harness = Harness.Create([
            Row(Fa, "fa", "fa-IR", true, true, 1),
            Row(En, "en", "en-US", true, false, 2),
        ]);

        var created = await harness.CreateAsync("ordinary", 0, (Fa, "عادی", null), (En, "Ordinary", null));
        Assert.True(created.IsSuccess);
        var id = created.Value.ShippingTypeId;

        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
            CultureInfo.CurrentUICulture = new CultureInfo("fa-IR");
            var persian = await harness.GetAsync(id);

            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var english = await harness.GetAsync(id);

            Assert.Equal(
                persian!.Translations.Select(x => (x.LanguageId, x.Name, x.Description)),
                english!.Translations.Select(x => (x.LanguageId, x.Name, x.Description)));
            Assert.Equal(new[] { Fa, En }.OrderBy(x => x),
                english.Translations.Select(x => x.LanguageId).OrderBy(x => x));
            Assert.Equal("عادی", Translation(english, Fa).Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // C. Real ActivityListener proof through the canonical tracer
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task One_real_module_span_per_successful_lookup_with_parent_correlation_and_restoration()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        using var harness = Harness.Create([Row(Fa, "fa", "fa-IR", true, true, 1)]);

        var correlationId = Guid.NewGuid().ToString("N");
        using var correlationScope = CorrelationIdContext.BeginScope(correlationId);
        using var parent = ToobaTelemetry.ActivitySource.StartActivity("test.fulfillment.parent");
        Assert.NotNull(parent);

        var facts = await harness.LanguageFacts.ReadAsync(CancellationToken.None);
        Assert.Single(facts.Languages);

        var span = Assert.Single(spans);
        Assert.Equal("module.Fulfillment.Localization.ListLanguages", span.OperationName);
        Assert.Equal("Fulfillment", Tag(span, TracingTagNames.ModuleSource));
        Assert.Equal("Localization", Tag(span, TracingTagNames.ModuleTarget));
        Assert.Equal("Localization", Tag(span, TracingTagNames.Module));
        Assert.Equal("ListLanguages", Tag(span, TracingTagNames.Operation));
        Assert.Equal(TracingRequestKinds.ModuleCall, Tag(span, TracingTagNames.RequestKind));
        Assert.Equal(correlationId, Tag(span, TracingTagNames.CorrelationId));
        Assert.Equal(ActivityStatusCode.Ok, span.Status);

        // One child of the ambient parent, sharing its trace; the ambient parent is restored afterwards.
        Assert.Equal(parent!.SpanId, span.ParentSpanId);
        Assert.Equal(parent.TraceId, span.TraceId);
        Assert.Same(parent, Activity.Current);
        Assert.NotEqual(default, span.Duration);
        Assert.Null(Tag(span, TracingTagNames.ExceptionType));

        // Span count tracks the fixture's real lookups, not a guessed handler count.
        await harness.LanguageFacts.ReadAsync(CancellationToken.None);
        await harness.LanguageFacts.ReadAsync(CancellationToken.None);
        Assert.Equal(3, spans.Count);
        Assert.Equal(3, harness.Registry.Calls);
    }

    [Fact]
    public async Task Invalid_registry_facts_are_a_business_failure_after_a_successful_lookup_span()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        using var harness = Harness.Create([
            new LanguageLookupSnapshot(Fa, "fa", "fa-IR", "fa", IsDefault: true)
            {
                IsActive = true,
                SortOrder = null,     // registry metadata incomplete
            },
        ]);

        var result = await harness.CreateAsync("ordinary", 0, (Fa, "عادی", null));

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeLanguageInvalid, result.FirstError.Code);
        var span = Assert.Single(spans);
        // The dependency call itself completed: this is not a dependency exception.
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Null(Tag(span, TracingTagNames.ExceptionType));
        Assert.Equal(0, await harness.CountAsync());
    }

    [Fact]
    public async Task Throwing_lookup_is_recorded_by_exception_type_only_and_rethrown_unchanged()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        using var harness = Harness.Create([Row(Fa, "fa", "fa-IR", true, true, 1)]);
        var failure = new InvalidOperationException(Sentinel);
        harness.Registry.ThrowOnRead = failure;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.LanguageFacts.ReadAsync(CancellationToken.None));

        Assert.Same(failure, thrown);
        AssertNoLeak(Assert.Single(spans), typeof(InvalidOperationException));
    }

    [Fact]
    public async Task Caller_cancellation_keeps_the_same_exception_and_token_and_is_never_swallowed()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        using var harness = Harness.Create([Row(Fa, "fa", "fa-IR", true, true, 1)]);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancellationException = new OperationCanceledException(cancellation.Token);
        harness.Registry.ThrowOnRead = cancellationException;

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(
            () => harness.LanguageFacts.ReadAsync(cancellation.Token));

        Assert.Same(cancellationException, thrown);
        Assert.Equal(cancellation.Token, thrown.CancellationToken);
        Assert.True(thrown.CancellationToken.IsCancellationRequested);
        // The caller's exact token struct (not a substituted/default one) reached the real lookup.
        Assert.Equal(cancellation.Token, harness.Registry.ReceivedToken);

        var span = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(
            typeof(OperationCanceledException).FullName,
            Tag(span, TracingTagNames.ExceptionType));
    }

    [Fact]
    public async Task Exported_span_carries_no_registry_text_identifier_or_token_values()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        using var harness = Harness.Create([Row(Fa, "fa", "fa-IR", true, true, 1)]);

        var created = await harness.CreateAsync("sensitive_code", 0, (Fa, "SENSITIVE-NAME-9f3c", "SENSITIVE-DESC-9f3c"));
        Assert.True(created.IsSuccess);

        var span = Assert.Single(spans);
        var exported = span.TagObjects
            .Select(tag => $"{tag.Key}={tag.Value}")
            .Concat(span.Events.Select(e => e.Name))
            .ToArray();

        foreach (var forbidden in new[]
                 {
                     "SENSITIVE-NAME-9f3c",
                     "SENSITIVE-DESC-9f3c",
                     "sensitive_code",
                     Fa.ToString(),
                     Fa.ToString("N"),
                     "tenant",
                     "IRR",
                     Sentinel,
                 })
        {
            Assert.DoesNotContain(exported, value => value.Contains(forbidden, StringComparison.Ordinal));
        }

        // Sanity: the span is still the module call (so the absence above is meaningful).
        Assert.Equal("module.Fulfillment.Localization.ListLanguages", span.OperationName);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static LanguageLookupSnapshot Row(
        Guid id,
        string code,
        string culture,
        bool isActive,
        bool isDefault,
        int sortOrder) =>
        new(id, code, culture, code, isDefault)
        {
            IsActive = isActive,
            SortOrder = sortOrder,
            Direction = culture.StartsWith("fa", StringComparison.Ordinal)
                        || culture.StartsWith("ar", StringComparison.Ordinal)
                ? "rtl"
                : "ltr",
            DisplayName = code.ToUpperInvariant(),
            NativeName = code,
        };

    private static LanguageLookupSnapshot[] InvalidRows(string scenario) => scenario switch
    {
        "missing-active" => [RowMissingActive()],
        "missing-sort" => [RowMissingSort()],
        "empty-id" => [Row(Guid.Empty, "fa", "fa-IR", true, true, 1)],
        "duplicate-id" => [Row(Fa, "fa", "fa-IR", true, true, 1), Row(Fa, "fa", "fa-IR", true, false, 2)],
        "no-active" => [Row(Fa, "fa", "fa-IR", false, true, 1)],
        "no-default" => [Row(Fa, "fa", "fa-IR", true, false, 1)],
        "inactive-default" => [Row(Fa, "fa", "fa-IR", true, false, 1), Row(En, "en", "en-US", false, true, 2)],
        "multiple-defaults" => [Row(Fa, "fa", "fa-IR", true, true, 1), Row(En, "en", "en-US", true, true, 2)],
        _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
    };

    private static LanguageLookupSnapshot RowMissingActive() =>
        new(Fa, "fa", "fa-IR", "fa", IsDefault: true) { SortOrder = 1 };

    private static LanguageLookupSnapshot RowMissingSort() =>
        new(Fa, "fa", "fa-IR", "fa", IsDefault: true) { IsActive = true };

    private static ShippingTypeTranslationSnapshot Translation(ShippingTypeView view, Guid languageId) =>
        view.Translations.Single(x => x.LanguageId == languageId);

    private static string? Tag(Activity activity, string key) => activity.GetTagItem(key)?.ToString();

    private static void AssertNoLeak(Activity span, Type? exceptionType)
    {
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(exceptionType?.FullName, Tag(span, TracingTagNames.ExceptionType));
        var values = span.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
        Assert.DoesNotContain(values, value => value.Contains(Sentinel, StringComparison.Ordinal));
        Assert.Empty(span.Events);
    }

    private static ActivityListener CreateListener(List<Activity> sink)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ToobaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                // Isolate this capability's spans from unrelated tests sharing the process-wide source.
                if (activity.OperationName.StartsWith(
                        "module.Fulfillment.Localization.", StringComparison.Ordinal))
                {
                    lock (sink)
                    {
                        sink.Add(activity);
                    }
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    /// <summary>Controlled, mutable registry fixture standing in for the owner's Localization rows.</summary>
    private sealed class MutableLanguageRegistry(params LanguageLookupSnapshot[] rows) : ILanguageLookup
    {
        private readonly List<LanguageLookupSnapshot> _rows = [.. rows];

        public int Calls { get; private set; }

        public Exception? ThrowOnRead { get; set; }

        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            ReceivedToken = cancellationToken;

            if (ThrowOnRead is { } failure)
            {
                throw failure;
            }

            return Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>(_rows.ToArray());
        }

        public void Deactivate(Guid languageId) => Mutate(languageId, row => row with { IsActive = false });

        public void Activate(Guid languageId) => Mutate(languageId, row => row with { IsActive = true });

        public void MakeDefault(Guid languageId)
        {
            for (var index = 0; index < _rows.Count; index++)
            {
                _rows[index] = _rows[index] with { IsDefault = _rows[index].LanguageId == languageId };
            }
        }

        private void Mutate(Guid languageId, Func<LanguageLookupSnapshot, LanguageLookupSnapshot> change)
        {
            var index = _rows.FindIndex(row => row.LanguageId == languageId);
            _rows[index] = change(_rows[index]);
        }
    }

    private sealed class MutableClock(DateTimeOffset start) : IClock
    {
        private DateTimeOffset _now = start;

        public DateTimeOffset UtcNow => _now;
    }

    /// <summary>Real composition: production module registration with only data boundaries replaced.</summary>
    private sealed class Harness : IDisposable
    {
        private Harness(
            ServiceProvider provider,
            IServiceScope scope,
            MutableLanguageRegistry registry,
            IShippingTypeLanguageFacts languageFacts,
            ISender sender)
        {
            Provider = provider;
            Scope = scope;
            Registry = registry;
            LanguageFacts = languageFacts;
            Sender = sender;
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public MutableLanguageRegistry Registry { get; }

        public IShippingTypeLanguageFacts LanguageFacts { get; }

        public ISender Sender { get; }

        public IReadOnlyList<ServiceDescriptor> Services { get; private init; } = [];

        public static Harness Create(params LanguageLookupSnapshot[] rows)
        {
            var registry = new MutableLanguageRegistry(rows);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddToobaObservabilityFoundation();
            services.AddToobaCqrsFoundation(typeof(CreateShippingTypeCommand).Assembly);

            // The ACTUAL module registration entry (not a hand-copied capability composition).
            new FulfillmentModule().AddServices(
                services,
                new ConfigurationBuilder().Build(),
                new StubHostEnvironment());

            var snapshot = services.ToArray();

            // Data boundaries only: replace the Npgsql option-config action with EF InMemory, and the
            // platform clock plus the language registry with controlled fixtures. The module's store,
            // reader, adapter and gate descriptors are left untouched.
            ReplaceDbContext(services);
            Replace<IClock>(services, new MutableClock(new DateTimeOffset(2026, 10, 22, 11, 0, 0, TimeSpan.Zero)));
            services.AddSingleton<ILanguageLookup>(registry);

            var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = false,
            });
            var scope = provider.CreateScope();

            return new Harness(
                provider,
                scope,
                registry,
                scope.ServiceProvider.GetRequiredService<IShippingTypeLanguageFacts>(),
                scope.ServiceProvider.GetRequiredService<ISender>())
            {
                Services = snapshot,
            };
        }

        public async Task<Result<ShippingTypeView>> CreateAsync(
            string code,
            int sortOrder,
            params (Guid LanguageId, string Name, string? Description)[] translations)
        {
            var model = new ShippingTypeWriteModel(
                code,
                sortOrder,
                null,
                null,
                translations
                    .Select(x => (ShippingTypeTranslationInput?)new ShippingTypeTranslationInput(
                        x.LanguageId, x.Name, x.Description))
                    .ToArray());

            return await Sender.Send(new CreateShippingTypeCommand(model));
        }

        public async Task<Result<ShippingTypeView>> UpdateAsync(
            Guid id,
            long expectedRevision,
            bool isActive,
            int sortOrder,
            params (Guid LanguageId, string Name, string? Description)[] translations)
        {
            var model = new ShippingTypeWriteModel(
                null,
                sortOrder,
                isActive,
                expectedRevision,
                translations
                    .Select(x => (ShippingTypeTranslationInput?)new ShippingTypeTranslationInput(
                        x.LanguageId, x.Name, x.Description))
                    .ToArray());

            return await Sender.Send(new UpdateShippingTypeCommand(id, model));
        }

        public async Task<ShippingTypeView?> GetAsync(Guid id)
        {
            var result = await Sender.Send(new GetShippingTypeQuery(id));
            return result.IsSuccess ? result.Value : null;
        }

        public async Task<int> CountAsync()
        {
            var result = await Sender.Send(new ListShippingTypesQuery());
            Assert.True(result.IsSuccess);
            return result.Value.Count;
        }

        public void Dispose()
        {
            Scope.Dispose();
            Provider.Dispose();
        }

        private static void ReplaceDbContext(IServiceCollection services)
        {
            for (var index = services.Count - 1; index >= 0; index--)
            {
                var serviceType = services[index].ServiceType;
                if (serviceType == typeof(FulfillmentDbContext)
                    || serviceType == typeof(DbContextOptions<FulfillmentDbContext>)
                    || serviceType == typeof(DbContextOptions))
                {
                    services.RemoveAt(index);
                }
            }

            services.AddDbContext<FulfillmentDbContext>(options =>
                options.UseInMemoryDatabase($"tb196r5-{Guid.NewGuid():N}"));
        }

        private static void Replace<TService>(IServiceCollection services, TService instance)
            where TService : class
        {
            for (var index = services.Count - 1; index >= 0; index--)
            {
                if (services[index].ServiceType == typeof(TService))
                {
                    services.RemoveAt(index);
                }
            }

            services.AddSingleton(instance);
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tooba.Fulfillment.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
