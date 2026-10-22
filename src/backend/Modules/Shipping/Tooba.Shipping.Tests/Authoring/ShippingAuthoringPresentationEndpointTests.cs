using System.Globalization;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Localization.Contracts.Ports;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Authoring.Validation;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.StoreContext.Contracts.Current;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1 — the owner-local multilingual presentation
/// surface, executed through the REAL HTTP delegates.
/// </summary>
/// <remarks>
/// <para>
/// R192-01 closed: every fact here goes through the production composition entry
/// (<see cref="ShippingEndpointModule"/>), the real <see cref="ShippingAuthoringOwnerAuthorizer"/>, the
/// real body reader, the real MediatR validation pipeline, the real presentation query/command handlers
/// and shared projection, the real <see cref="ShippingAuthoringDraftDirectory"/> over the real
/// <see cref="ShippingDbContext"/>, and the real response factory/error catalog. Only the platform
/// authorization seams and the Contracts language/commerce sources are controlled fixtures.
/// </para>
/// <para>
/// <b>Fixture honesty.</b> Language ids and metadata are test-owned fixtures fed through a stub
/// <see cref="ILanguageLookup"/> into the real <see cref="ShippingAuthoringContextSource"/>. This proves
/// the lawful adapter path and handler behavior; it is NOT a live merchant Localization database or a
/// browser session.
/// </para>
/// <para>
/// <b>Dispatch vs execution.</b> <see cref="Harness.SentCount"/> counts real <c>ISender</c> dispatch (a
/// behavior registered BEFORE the validation behavior) and <see cref="Harness.HandlerReached"/> counts
/// arrival past validation (a behavior registered AFTER it). A denied request asserts both are zero; a
/// malformed body asserts dispatch may happen while no handler is reached.
/// </para>
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringPresentationEndpointTests
{
    private const string AdminDrafts = "/v1/admin/shipping-services/authoring-drafts";
    private const string SellerDrafts = "/v1/seller/shipping-services/authoring-drafts";
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly Guid Fa = Guid.Parse("01900000-0000-7000-8000-000000009201");
    private static readonly Guid En = Guid.Parse("01900000-0000-7000-8000-000000009202");
    private static readonly Guid Ru = Guid.Parse("01900000-0000-7000-8000-000000009203");
    private static readonly Guid De = Guid.Parse("01900000-0000-7000-8000-000000009204");

    [Fact]
    public async Task One_active_non_fa_language_roundtrips_through_the_real_presentation_routes()
    {
        using var harness = Harness.Create("pres-http-a", languages:
        [
            Language(Ru, "ru", "ru-RU", "ltr", isDefault: true, sortOrder: 0, displayName: "Russian", nativeName: "Русский"),
        ]);
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/presentation";

        var (patchStatus, _, patched) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Ru, "  Доставка  ", "На следующий день"), ""));

        Assert.Equal(StatusCodes.Status200OK, patchStatus);
        Assert.Equal(2, patched.GetProperty("revision").GetInt64());
        var translated = Assert.Single(patched.GetProperty("methodTranslations").EnumerateArray());
        Assert.Equal(Ru, translated.GetProperty("languageId").GetGuid());
        Assert.Equal("Доставка", translated.GetProperty("name").GetString());
        Assert.Equal("На следующий день", translated.GetProperty("description").GetString());
        Assert.Empty(patched.GetProperty("groupTitles").EnumerateArray());
        var active = Assert.Single(patched.GetProperty("contentLanguages").EnumerateArray());
        Assert.Equal(Ru, active.GetProperty("languageId").GetGuid());
        Assert.Equal("ltr", active.GetProperty("direction").GetString());
        Assert.Equal(Ru, patched.GetProperty("defaultContentLanguageId").GetGuid());
        Assert.True(patched.GetProperty("hasDefaultLanguageName").GetBoolean());

        // A NEW GET through the same real pipeline returns the same raw text, and the read really travelled
        // the Shipping -> Localization adapter (its call counter advanced).
        var readsBefore = harness.Languages.Calls;
        var (getStatus, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, getStatus);
        Assert.Equal(2, read.GetProperty("revision").GetInt64());
        Assert.Equal("Доставка", Assert.Single(read.GetProperty("methodTranslations").EnumerateArray())
            .GetProperty("name").GetString());
        Assert.True(harness.Languages.Calls > readsBefore);

        // Nothing but the presentation changed: the region tree and the draft binding are untouched.
        var (_, _, draft) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal("post", draft.GetProperty("serviceCode").GetString());
        Assert.Single(draft.GetProperty("regionGroups").EnumerateArray());
        Assert.Equal(2, draft.GetProperty("revision").GetInt64());
    }

    [Fact]
    public async Task Two_mixed_rtl_ltr_languages_are_written_independently_and_returned_raw_with_registry_order()
    {
        using var harness = Harness.Create("pres-http-b", languages:
        [
            Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی"),
            Language(En, "en", "en-US", "ltr", isDefault: false, sortOrder: 1, displayName: "English", nativeName: "English"),
        ]);
        var (draftId, firstGroup, secondGroup) = await harness.CreateTwoGroupDraftAsync(Tehran, Isfahan);
        var path = $"{AdminDrafts}/{draftId}/presentation";

        var (firstStatus, _, _) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Fa, "ارسال اکسپرس", "یک روزه"), Title(firstGroup, Fa, "گروه تهران")));
        Assert.Equal(StatusCodes.Status200OK, firstStatus);

        // English is written by a SECOND patch: the Persian bytes must survive it unchanged.
        var (secondStatus, _, second) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(2, Entry(En, "Express delivery"), Title(secondGroup, En, "Isfahan group")));
        Assert.Equal(StatusCodes.Status200OK, secondStatus);
        Assert.Equal(3, second.GetProperty("revision").GetInt64());

        var (_, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        var translations = read.GetProperty("methodTranslations").EnumerateArray()
            .ToDictionary(item => item.GetProperty("languageId").GetGuid());
        Assert.Equal(2, translations.Count);
        Assert.Equal("ارسال اکسپرس", translations[Fa].GetProperty("name").GetString());
        Assert.Equal("یک روزه", translations[Fa].GetProperty("description").GetString());
        Assert.Equal("Express delivery", translations[En].GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, translations[En].GetProperty("description").ValueKind);

        var titles = read.GetProperty("groupTitles").EnumerateArray().ToArray();
        Assert.Equal(2, titles.Length);
        Assert.Contains(titles, title => title.GetProperty("groupId").GetGuid() == firstGroup
                                         && title.GetProperty("languageId").GetGuid() == Fa
                                         && title.GetProperty("title").GetString() == "گروه تهران");
        Assert.Contains(titles, title => title.GetProperty("groupId").GetGuid() == secondGroup
                                         && title.GetProperty("languageId").GetGuid() == En
                                         && title.GetProperty("title").GetString() == "Isfahan group");

        // Active registry identity, owner order and default come from the source rows, not from the body.
        var languages = read.GetProperty("contentLanguages").EnumerateArray().ToArray();
        Assert.Equal([Fa, En], languages.Select(item => item.GetProperty("languageId").GetGuid()).ToArray());
        Assert.Equal(["rtl", "ltr"], languages.Select(item => item.GetProperty("direction").GetString()!).ToArray());
        Assert.Equal(Fa, read.GetProperty("defaultContentLanguageId").GetGuid());
        Assert.True(read.GetProperty("hasDefaultLanguageName").GetBoolean());

        // The geographic tree is exactly what create proved: editing text never rewrites a selection.
        var (_, _, draft) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal(
            [Tehran, Isfahan],
            draft.GetProperty("regionGroups").EnumerateArray()
                .SelectMany(group => group.GetProperty("areas").EnumerateArray())
                .Select(area => area.GetProperty("id").GetGuid())
                .ToArray());
    }

    [Fact]
    public async Task Three_plus_languages_with_non_first_default_and_an_inactive_stored_language_retain_text()
    {
        using var harness = Harness.Create("pres-http-c", languages:
        [
            Language(Ru, "ru", "ru-RU", "ltr", isDefault: false, sortOrder: 0, displayName: "Russian", nativeName: "Русский"),
            Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 1, displayName: "Persian", nativeName: "فارسی"),
            Language(En, "en", "en-US", "ltr", isDefault: false, sortOrder: 2, displayName: "English", nativeName: "English"),
            Language(De, "de", "de-DE", "ltr", isDefault: false, sortOrder: 3, displayName: "German", nativeName: "Deutsch"),
        ]);
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/presentation";

        var (seedStatus, _, _) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Ru, "Доставка") + "," + Entry(De, "Lieferung"), ""));
        Assert.Equal(StatusCodes.Status200OK, seedStatus);

        // Default is NOT the first row: the projection must follow the flag, not position.
        var (_, _, initial) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(Fa, initial.GetProperty("defaultContentLanguageId").GetGuid());
        Assert.False(initial.GetProperty("hasDefaultLanguageName").GetBoolean());
        Assert.Equal(
            [Ru, Fa, En, De],
            initial.GetProperty("contentLanguages").EnumerateArray()
                .Select(item => item.GetProperty("languageId").GetGuid())
                .ToArray());

        // Deactivate the language that already has stored text.
        harness.Languages.Deactivate(De);
        var (_, _, afterDeactivate) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(
            [Ru, Fa, En],
            afterDeactivate.GetProperty("contentLanguages").EnumerateArray()
                .Select(item => item.GetProperty("languageId").GetGuid())
                .ToArray());
        var retained = Assert.Single(
            afterDeactivate.GetProperty("methodTranslations").EnumerateArray(),
            item => item.GetProperty("languageId").GetGuid() == De);
        Assert.Equal("Lieferung", retained.GetProperty("name").GetString());
        Assert.Equal(2, afterDeactivate.GetProperty("revision").GetInt64());

        // A write to the inactive id fails closed and changes nothing.
        var (inactiveStatus, _, inactiveBody) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(2, Entry(De, "Neu"), ""));
        Assert.Equal(StatusCodes.Status400BadRequest, inactiveStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringPresentationLanguageInvalid, inactiveBody.GetProperty("errorCode").GetString());
        Assert.Equal(2, (await harness.ReadDraftStampAsync(draftId)).Revision);

        // Reactivate: the exact previous bytes come back with no auto-translation.
        harness.Languages.Activate(De);
        var (_, _, afterReactivate) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal("Lieferung", Assert.Single(
            afterReactivate.GetProperty("methodTranslations").EnumerateArray(),
            item => item.GetProperty("languageId").GetGuid() == De).GetProperty("name").GetString());

        // Change BOTH the default (to a language with no stored name) and the owner order.
        harness.Languages.SetDefault(En);
        harness.Languages.Reorder((Ru, 3), (Fa, 2), (En, 0), (De, 1));
        var (_, _, afterDefault) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(En, afterDefault.GetProperty("defaultContentLanguageId").GetGuid());
        Assert.False(afterDefault.GetProperty("hasDefaultLanguageName").GetBoolean());
        Assert.Equal(
            [En, De, Fa, Ru],
            afterDefault.GetProperty("contentLanguages").EnumerateArray()
                .Select(item => item.GetProperty("languageId").GetGuid())
                .ToArray());

        // Writing the new default language flips completeness truthfully.
        var (writeStatus, _, written) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(2, Entry(En, "Delivery"), ""));
        Assert.Equal(StatusCodes.Status200OK, writeStatus);
        Assert.True(written.GetProperty("hasDefaultLanguageName").GetBoolean());
        Assert.Equal(3, written.GetProperty("revision").GetInt64());
    }

    [Fact]
    public async Task Selected_clear_and_true_noops_keep_revision_and_timestamp_and_stale_revision_conflicts()
    {
        using var harness = Harness.Create("pres-http-d", languages:
        [
            Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی"),
            Language(En, "en", "en-US", "ltr", isDefault: false, sortOrder: 1, displayName: "English", nativeName: "English"),
        ]);
        var (draftId, firstGroup, _) = await harness.CreateTwoGroupDraftAsync(Tehran, Isfahan);
        var path = $"{AdminDrafts}/{draftId}/presentation";

        var (seedStatus, _, _) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(
                1,
                Entry(Fa, "ارسال", "توضیح") + "," + Entry(En, "Delivery", "Note"),
                Title(firstGroup, Fa, "تهران") + "," + Title(firstGroup, En, "Tehran")));
        Assert.Equal(StatusCodes.Status200OK, seedStatus);

        // Clear ONLY the selected-language description and title.
        var (clearStatus, _, cleared) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(2, ClearDescription(En, "Delivery"), ClearTitle(firstGroup, En)));
        Assert.Equal(StatusCodes.Status200OK, clearStatus);
        Assert.Equal(3, cleared.GetProperty("revision").GetInt64());

        var translations = cleared.GetProperty("methodTranslations").EnumerateArray()
            .ToDictionary(item => item.GetProperty("languageId").GetGuid());
        Assert.Equal("Delivery", translations[En].GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, translations[En].GetProperty("description").ValueKind);
        Assert.Equal("ارسال", translations[Fa].GetProperty("name").GetString());
        Assert.Equal("توضیح", translations[Fa].GetProperty("description").GetString());
        var title = Assert.Single(cleared.GetProperty("groupTitles").EnumerateArray());
        Assert.Equal(Fa, title.GetProperty("languageId").GetGuid());
        Assert.Equal("تهران", title.GetProperty("title").GetString());

        // No-op #1: empty lists.
        var (revision, updatedAt) = await harness.ReadDraftStampAsync(draftId);
        var (emptyStatus, _, empty) = await harness.SendAsync(HttpMethod.Patch, path, PatchBody(revision, "", ""));
        Assert.Equal(StatusCodes.Status200OK, emptyStatus);
        Assert.Equal(revision, empty.GetProperty("revision").GetInt64());
        Assert.Equal((revision, updatedAt), await harness.ReadDraftStampAsync(draftId));

        // No-op #2: an identical active entry is not a change either.
        var (identicalStatus, _, identical) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(revision, Entry(Fa, "ارسال", "توضیح"), ""));
        Assert.Equal(StatusCodes.Status200OK, identicalStatus);
        Assert.Equal(revision, identical.GetProperty("revision").GetInt64());
        Assert.Equal((revision, updatedAt), await harness.ReadDraftStampAsync(draftId));

        // A stale request can never overwrite the newer state.
        var (staleStatus, _, stale) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(revision - 1, ClearDescription(Fa, "تغییر کهنه"), ""));
        Assert.Equal(StatusCodes.Status409Conflict, staleStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, stale.GetProperty("errorCode").GetString());
        Assert.Equal((revision, updatedAt), await harness.ReadDraftStampAsync(draftId));
        var (_, _, after) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal("ارسال", Assert.Single(
            after.GetProperty("methodTranslations").EnumerateArray(),
            item => item.GetProperty("languageId").GetGuid() == Fa).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Interface_culture_and_accept_language_never_select_content_language()
    {
        using var harness = Harness.Create("pres-http-e", languages:
        [
            Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی"),
            Language(En, "en", "en-US", "ltr", isDefault: false, sortOrder: 1, displayName: "English", nativeName: "English"),
        ]);
        var (draftId, firstGroup, _) = await harness.CreateTwoGroupDraftAsync(Tehran, Isfahan);
        var path = $"{AdminDrafts}/{draftId}/presentation";
        var (seedStatus, _, _) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(
                1,
                Entry(Fa, "ارسال") + "," + Entry(En, "Delivery"),
                Title(firstGroup, En, "Tehran")));
        Assert.Equal(StatusCodes.Status200OK, seedStatus);

        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
            CultureInfo.CurrentUICulture = new CultureInfo("fa-IR");
            harness.AcceptLanguage = "fa-IR,fa;q=0.9";
            var (_, _, persianInterface) = await harness.SendAsync(HttpMethod.Get, path);

            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            harness.AcceptLanguage = "en-US,en;q=0.9";
            var (_, _, englishInterface) = await harness.SendAsync(HttpMethod.Get, path);

            // Raw stored content and the configured identities are identical: the interface locale only
            // changed the caller's culture, never which language the content is read as.
            Assert.Equal(persianInterface.GetRawText(), englishInterface.GetRawText());
            var translations = englishInterface.GetProperty("methodTranslations").EnumerateArray()
                .ToDictionary(
                    item => item.GetProperty("languageId").GetGuid(),
                    item => item.GetProperty("name").GetString());
            Assert.Equal("ارسال", translations[Fa]);
            Assert.Equal("Delivery", translations[En]);
            Assert.Equal(2, translations.Count);

            // A PATCH under a different interface culture still writes exactly the selected LanguageId.
            var (patchStatus, _, patched) = await harness.SendAsync(
                HttpMethod.Patch,
                path,
                PatchBody(2, Entry(Fa, "ارسال سریع"), ""));
            Assert.Equal(StatusCodes.Status200OK, patchStatus);
            Assert.Equal("Delivery", Assert.Single(
                patched.GetProperty("methodTranslations").EnumerateArray(),
                item => item.GetProperty("languageId").GetGuid() == En).GetProperty("name").GetString());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
            harness.AcceptLanguage = "fa";
        }
    }

    [Fact]
    public async Task Admin_view_only_and_foreign_owner_or_group_requests_are_denied_before_any_mutation()
    {
        using var reader = Harness.Create("pres-http-f", languages:
        [
            Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی"),
        ]);
        var draftId = await reader.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/presentation";

        using var viewOnly = Harness.Create(
            "pres-http-f2",
            adminAllowed: [ShippingAdminPermissions.View],
            languages: [Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی")]);
        var (deniedStatus, _, denied) = await viewOnly.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Fa, "نفوذ"), ""));
        Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, denied.GetProperty("errorCode").GetString());
        Assert.Equal(0, viewOnly.SentCount);
        Assert.Equal(0, viewOnly.HandlerReached);
        Assert.Equal(0, viewOnly.Languages.Calls);

        // Another store resolves neither the draft nor a group of it: the SAME safe not-found answer.
        reader.TenantContext = Harness.Tenant("store-b");
        var (foreignGet, _, foreignGetBody) = await reader.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status404NotFound, foreignGet);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreignGetBody.GetProperty("errorCode").GetString());
        var (foreignPatch, _, foreignPatchBody) = await reader.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Fa, "نفوذ"), ""));
        Assert.Equal(StatusCodes.Status404NotFound, foreignPatch);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreignPatchBody.GetProperty("errorCode").GetString());

        // A foreign/invented group id is refused without disclosing anything and without a partial edit.
        reader.TenantContext = Harness.Tenant("store-a");
        var (foreignGroupStatus, _, foreignGroupBody) = await reader.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Fa, "تغییر"), Title(Guid.NewGuid(), Fa, "x")));
        Assert.Equal(StatusCodes.Status404NotFound, foreignGroupStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionGroupNotFound, foreignGroupBody.GetProperty("errorCode").GetString());
        var (revision, updatedAt) = await reader.ReadDraftStampAsync(draftId);
        Assert.Equal(1, revision);
        Assert.Equal((revision, updatedAt), await reader.ReadDraftStampAsync(draftId));
        var (_, _, after) = await reader.SendAsync(HttpMethod.Get, path);
        Assert.Empty(after.GetProperty("methodTranslations").EnumerateArray());
    }

    [Fact]
    public async Task Marketplace_seller_can_read_and_write_and_single_store_seller_is_unavailable_before_resolution()
    {
        using var seller = Harness.Create(
            "pres-http-g",
            edition: ToobaEdition.Marketplace,
            marketplace: true,
            languages: [Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی")]);
        var draftId = await seller.CreateSellerDraftAsync(Tehran);
        var path = $"{SellerDrafts}/{draftId}/presentation";

        var (patchStatus, _, patched) = await seller.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Fa, "ارسال فروشنده"), ""));
        Assert.Equal(StatusCodes.Status200OK, patchStatus);
        Assert.Equal("ارسال فروشنده", Assert.Single(patched.GetProperty("methodTranslations").EnumerateArray())
            .GetProperty("name").GetString());
        Assert.Equal(StatusCodes.Status200OK, (await seller.SendAsync(HttpMethod.Get, path)).Status);
        Assert.True(seller.SellerCalls > 0);

        // A seller whose effective grant is scoped is refused BEFORE any draft resolution or dispatch.
        using var denied = Harness.Create(
            "pres-http-h",
            edition: ToobaEdition.Marketplace,
            marketplace: true,
            sellerGrants: [new PlatformPermissionGrant(
                ShippingAdminPermissions.Manage, PlatformAccessScopeKind.Category, Guid.NewGuid(), false)],
            languages: [Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی")]);
        var (deniedStatus, _, deniedBody) = await denied.SendAsync(
            HttpMethod.Patch,
            $"{SellerDrafts}/{Guid.NewGuid()}/presentation",
            PatchBody(1, Entry(Fa, "x"), ""));
        Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeDenied, deniedBody.GetProperty("errorCode").GetString());
        Assert.Equal(0, denied.SentCount);
        Assert.True(denied.SellerCalls > 0);

        // SingleStore seller routes stay canonical unavailable BEFORE any seller resolution or dispatch.
        using var singleStore = Harness.Create("pres-http-i", languages:
        [
            Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی"),
        ]);
        var adminDraft = await singleStore.CreateDraftAsync(Tehran);
        var (unavailableGet, _, unavailableGetBody) = await singleStore.SendAsync(
            HttpMethod.Get,
            $"{SellerDrafts}/{adminDraft}/presentation");
        Assert.Equal(StatusCodes.Status404NotFound, unavailableGet);
        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeUnavailable, unavailableGetBody.GetProperty("errorCode").GetString());
        var (unavailablePatch, _, unavailablePatchBody) = await singleStore.SendAsync(
            HttpMethod.Patch,
            $"{SellerDrafts}/{adminDraft}/presentation",
            PatchBody(1, "", ""));
        Assert.Equal(StatusCodes.Status404NotFound, unavailablePatch);
        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeUnavailable, unavailablePatchBody.GetProperty("errorCode").GetString());
        Assert.Equal(0, singleStore.SellerCalls);
        Assert.Equal(0, singleStore.SentCount);
    }

    [Fact]
    public async Task Malformed_presentation_payloads_fail_before_the_handler_and_change_nothing()
    {
        using var harness = Harness.Create("pres-http-j", languages:
        [
            Language(Fa, "fa", "fa-IR", "rtl", isDefault: true, sortOrder: 0, displayName: "Persian", nativeName: "فارسی"),
            Language(En, "en", "en-US", "ltr", isDefault: false, sortOrder: 1, displayName: "English", nativeName: "English"),
        ]);
        var (draftId, firstGroup, _) = await harness.CreateTwoGroupDraftAsync(Tehran, Isfahan);
        var path = $"{AdminDrafts}/{draftId}/presentation";
        var longName = new string('ن', 201);
        var longDescription = new string('d', 2001);
        var longTitle = new string('t', 201);

        var malformed = new (string Json, string Code)[]
        {
            ("{\"expectedRevision\":1,\"groupTitles\":[]}", ShippingAuthoringValidationCodes.MethodTranslationsRequired),
            ("{\"expectedRevision\":1,\"methodTranslations\":null,\"groupTitles\":[]}", ShippingAuthoringValidationCodes.MethodTranslationsRequired),
            ($"{{\"expectedRevision\":1,\"methodTranslations\":[{Entry(Fa, "x")}]}}", ShippingAuthoringValidationCodes.GroupTitlesRequired),
            ($"{{\"expectedRevision\":1,\"methodTranslations\":[],\"groupTitles\":null}}", ShippingAuthoringValidationCodes.GroupTitlesRequired),
            ("{\"expectedRevision\":1,\"methodTranslations\":[null],\"groupTitles\":[]}", ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            ("{\"expectedRevision\":1,\"methodTranslations\":[],\"groupTitles\":[null]}", ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            (PatchBody(1, Entry(Fa, "a") + "," + Entry(Fa, "b"), ""), ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            (PatchBody(1, "", Title(firstGroup, Fa, "a") + "," + Title(firstGroup, Fa, "b")), ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            ("{\"expectedRevision\":1,\"methodTranslations\":[{\"languageId\":\"00000000-0000-0000-0000-000000000000\",\"name\":\"a\"}],\"groupTitles\":[]}", ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            (PatchBody(1, Entry(Fa, "   "), ""), ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            (PatchBody(1, Entry(Fa, longName), ""), ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            (PatchBody(1, Entry(Fa, "a", longDescription), ""), ShippingAuthoringValidationCodes.PresentationEntryInvalid),
            (PatchBody(1, "", Title(firstGroup, Fa, longTitle)), ShippingAuthoringValidationCodes.PresentationEntryInvalid),
        };

        foreach (var (json, code) in malformed)
        {
            var (status, _, body) = await harness.SendAsync(HttpMethod.Patch, path, json);
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("validation.failed", body.GetProperty("errorCode").GetString());
            Assert.Contains(
                body.GetProperty("errors").EnumerateObject().SelectMany(property => property.Value.EnumerateArray()),
                item => item.GetString() == code);
        }

        // Shape failures may dispatch but never reach a handler, never read the registry and never write.
        Assert.True(harness.SentCount > 0);
        Assert.Equal(0, harness.HandlerReached);
        Assert.Equal(0, harness.Languages.Calls);
        Assert.Equal(1, (await harness.ReadDraftStampAsync(draftId)).Revision);

        // An INACTIVE but well-shaped language id is a handler-level rejection with the same no-change rule.
        harness.Languages.Deactivate(En);
        var (inactiveStatus, _, inactiveBody) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(En, "Delivery"), ""));
        Assert.Equal(StatusCodes.Status400BadRequest, inactiveStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringPresentationLanguageInvalid, inactiveBody.GetProperty("errorCode").GetString());

        // A valid FIRST entry followed by a bad LAST group id must not leak the earlier edit: the SAME
        // context instance keeps an empty change tracker and a later SaveChanges cannot publish it.
        var (lastStatus, _, lastBody) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Fa, "نیمه‌کاره"), Title(Guid.NewGuid(), Fa, "x")));
        Assert.Equal(StatusCodes.Status404NotFound, lastStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionGroupNotFound, lastBody.GetProperty("errorCode").GetString());
        await harness.WithContextAsync(async context =>
        {
            Assert.Empty(context.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
            await context.SaveChangesAsync();
            var stored = await context.AuthoringDrafts.AsNoTracking()
                .Include(draft => draft.Translations)
                .Include(draft => draft.RegionGroups)
                .ThenInclude(group => group.Titles)
                .SingleAsync(draft => draft.DraftId == draftId);
            Assert.Empty(stored.Translations);
            Assert.Empty(stored.RegionGroups.SelectMany(group => group.Titles));
            Assert.Equal(1, stored.Revision);
        });

        // Finally the SAME document with an active language persists, proving the refusals were not a
        // blanket refusal of the endpoint.
        var (okStatus, _, ok) = await harness.SendAsync(
            HttpMethod.Patch,
            path,
            PatchBody(1, Entry(Fa, "ارسال"), ""));
        Assert.Equal(StatusCodes.Status200OK, okStatus);
        Assert.Equal(2, ok.GetProperty("revision").GetInt64());
    }

    private static LanguageLookupSnapshot Language(
        Guid id,
        string code,
        string culture,
        string direction,
        bool isDefault,
        int sortOrder,
        string displayName,
        string nativeName)
        => new(id, code, culture, code, isDefault)
        {
            IsActive = true,
            Direction = direction,
            DisplayName = displayName,
            NativeName = nativeName,
            SortOrder = sortOrder,
        };

    private static string Entry(Guid languageId, string name, string? description = null)
        => description is null
            ? $"{{\"languageId\":\"{languageId}\",\"name\":\"{name}\"}}"
            : $"{{\"languageId\":\"{languageId}\",\"name\":\"{name}\",\"description\":\"{description}\"}}";

    private static string ClearDescription(Guid languageId, string name)
        => $"{{\"languageId\":\"{languageId}\",\"name\":\"{name}\",\"description\":null}}";

    private static string Title(Guid groupId, Guid languageId, string? title)
        => title is null
            ? $"{{\"groupId\":\"{groupId}\",\"languageId\":\"{languageId}\",\"title\":null}}"
            : $"{{\"groupId\":\"{groupId}\",\"languageId\":\"{languageId}\",\"title\":\"{title}\"}}";

    private static string ClearTitle(Guid groupId, Guid languageId) => Title(groupId, languageId, null);

    private static string PatchBody(long expectedRevision, string entries, string titles)
        => string.Concat(
            "{\"expectedRevision\":",
            expectedRevision.ToString(CultureInfo.InvariantCulture),
            ",\"methodTranslations\":[",
            entries,
            "],\"groupTitles\":[",
            titles,
            "]}");

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;
        private readonly Counter _counter;
        private readonly StubCurrentTenant _tenant;

        private Harness(
            WebApplication app,
            IServiceScope scope,
            Counter counter,
            StubCurrentTenant tenant,
            MutableLanguages languages)
        {
            _app = app;
            _scope = scope;
            _counter = counter;
            _tenant = tenant;
            Languages = languages;
        }

        public MutableLanguages Languages { get; }

        public string AcceptLanguage { get; set; } = "fa";

        public int SellerCalls { get; private set; }

        public int SentCount => _counter.Sent;

        public int HandlerReached => _counter.Handler;

        public TenantContext? TenantContext
        {
            get => _tenant.Current;
            set => _tenant.Current = value;
        }

        public static Harness Create(
            string database,
            ToobaEdition edition = ToobaEdition.SingleStore,
            IReadOnlyList<string>? adminAllowed = null,
            IReadOnlyList<PlatformPermissionGrant>? sellerGrants = null,
            IReadOnlyList<LanguageLookupSnapshot>? languages = null,
            bool marketplace = false)
        {
            var counter = new Counter();
            var registry = new MutableLanguages(languages ?? []);
            var tenant = new StubCurrentTenant(marketplace ? null : Tenant("store-a"));
            var seller = new RecordingSellerAccess(() => { });

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            // Registered BEFORE the CQRS foundation: outermost, so it observes every real Send.
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SentCountingBehavior<,>));
            builder.Services.AddToobaCqrsFoundation(typeof(CreateShippingAuthoringDraftCommand).Assembly);
            // Registered AFTER the foundation: it runs behind ValidationBehavior, i.e. only a request that
            // passes validation is counted as having reached the handler.
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HandlerReachedBehavior<,>));
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddDbContext<ShippingDbContext>(options => options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
            builder.Services.AddSingleton<IShippingAdminAuthorizer>(
                new RecordingAdminAuthorizer(adminAllowed ?? [ShippingAdminPermissions.View, ShippingAdminPermissions.Manage]));
            builder.Services.AddSingleton<ISellerPanelAccess>(seller);
            builder.Services.AddSingleton<IPlatformEffectiveAccessReader>(
                new RecordingEffectiveAccess(sellerGrants ?? [
                    new PlatformPermissionGrant(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                    new PlatformPermissionGrant(ShippingAdminPermissions.Manage, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                ]));
            builder.Services.AddSingleton<ICurrentEdition>(
                new StubCurrentEdition(new EditionContext(edition, "deployment-test")));
            builder.Services.AddSingleton<ICurrentTenant>(tenant);
            builder.Services.AddSingleton<IShippingMethodOptionValidator>(new FakeMethodValidator());
            builder.Services.AddSingleton<IShippingAuthoringDestinationResolver>(new FakeResolver());
            builder.Services.AddSingleton<ILanguageLookup>(registry);
            builder.Services.AddSingleton<ICurrentStoreCommerceContext>(new FakeStoreCommerce("USD"));
            // The REAL adapter: the controlled Contracts rows travel the production Shipping -> Localization
            // path, so the handler's registry facts are not fabricated inside the handler.
            builder.Services.AddSingleton<IShippingAuthoringContextSource>(provider => new ShippingAuthoringContextSource(
                provider.GetRequiredService<ILanguageLookup>(),
                provider.GetRequiredService<ICurrentStoreCommerceContext>(),
                provider.GetRequiredService<ICurrentEdition>(),
                provider.GetRequiredService<Tooba.BuildingBlocks.Observability.Tracing.IModuleCallTracer>()));
            builder.Services.AddSingleton(counter);

            var app = builder.Build();
            app.MapShippingEndpoints();
            var scope = app.Services.CreateScope();
            var harness = new Harness(app, scope, counter, tenant, registry);
            seller.OnCall = () => harness.SellerCalls++;
            return harness;
        }

        public static TenantContext Tenant(string tenantId) =>
            new(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("store-db"),
                "Test Store",
                null,
                null,
                "store.test",
                null);

        public async Task<Guid> CreateDraftAsync(params Guid[] areas)
        {
            var (status, _, body) = await SendAsync(
                HttpMethod.Post,
                AdminDrafts,
                CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<Guid> CreateSellerDraftAsync(params Guid[] areas)
        {
            var (status, _, body) = await SendAsync(
                HttpMethod.Post,
                SellerDrafts,
                CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<(Guid DraftId, Guid FirstGroup, Guid SecondGroup)> CreateTwoGroupDraftAsync(
            Guid first,
            Guid second)
        {
            var draftId = await CreateDraftAsync(first, second);
            var (_, _, body) = await SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
            var groups = body.GetProperty("regionGroups").EnumerateArray()
                .Select(group => group.GetProperty("groupId").GetGuid())
                .ToArray();
            return (draftId, groups[0], groups[1]);
        }

        public async Task<(long Revision, DateTimeOffset UpdatedAt)> ReadDraftStampAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
                return (draft.Revision, draft.UpdatedAt);
            });

        public async Task WithContextAsync(Func<ShippingDbContext, Task> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<T> WithContextAsync<T>(Func<ShippingDbContext, Task<T>> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<(int Status, string? Location, JsonElement Body)> SendAsync(
            HttpMethod method,
            string path,
            string? json = null)
        {
            var endpoint = FindEndpoint(path, method.Method);
            var context = CreateContext(method, path, json);
            var routeValues = new RouteValueDictionary();
            TemplateMatches(endpoint, path, routeValues);
            context.Request.RouteValues = routeValues;
            try
            {
                await endpoint.RequestDelegate!(context);
            }
            catch (Exception exception)
            {
                await _app.Services.GetRequiredService<IExceptionPresentationService>()
                    .WriteAsync(context, exception, CancellationToken.None);
            }

            return (
                context.Response.StatusCode,
                context.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null,
                ReadBody(context));
        }

        public void Dispose()
        {
            _scope.Dispose();
            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static string CreateBody(IEnumerable<Guid> areas)
            => string.Concat(
                "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":[",
                string.Join(',', areas.Select(area =>
                    $"{{\"groupId\":null,\"areas\":[{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{area}\"}}]}}")),
                "]}");

        private RouteEndpoint FindEndpoint(string path, string method) =>
            ((IEndpointRouteBuilder)_app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Single(endpoint =>
                    TemplateMatches(endpoint, path, new RouteValueDictionary())
                    && (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(method) ?? false));

        private static bool TemplateMatches(RouteEndpoint endpoint, string path, RouteValueDictionary values)
        {
            var template = TemplateParser.Parse(endpoint.RoutePattern.RawText!.TrimStart('/'));
            var matcher = new TemplateMatcher(template, new RouteValueDictionary());
            return matcher.TryMatch(new PathString(path), values);
        }

        private DefaultHttpContext CreateContext(HttpMethod method, string path, string? json)
        {
            var context = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
            context.Request.Method = method.Method;
            context.Request.Path = path;
            context.Request.Headers.AcceptLanguage = AcceptLanguage;
            context.Request.ContentType = "application/json";
            if (json is not null)
            {
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            }

            context.Response.Body = new MemoryStream();
            _app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
            return context;
        }

        private static JsonElement ReadBody(HttpContext context)
        {
            context.Response.Body.Position = 0;
            if (context.Response.Body.Length == 0)
            {
                return default;
            }

            using var document = JsonDocument.Parse(context.Response.Body);
            return document.RootElement.Clone();
        }
    }

    private sealed class Counter
    {
        public int Sent { get; set; }

        public int Handler { get; set; }
    }

    private sealed class SentCountingBehavior<TRequest, TResponse>(Counter counter)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (IsPresentation(request))
            {
                counter.Sent++;
            }

            return await next();
        }
    }

    private sealed class HandlerReachedBehavior<TRequest, TResponse>(Counter counter)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (IsPresentation(request))
            {
                counter.Handler++;
            }

            return await next();
        }
    }

    private static bool IsPresentation(object request) =>
        request is GetShippingAuthoringPresentationQuery or ReplaceShippingAuthoringPresentationCommand;

    /// <summary>Controlled mutable Contracts language rows: test-owned fixtures, not a merchant database.</summary>
    private sealed class MutableLanguages(IReadOnlyList<LanguageLookupSnapshot> initial) : ILanguageLookup
    {
        private readonly List<LanguageLookupSnapshot> _rows = [.. initial];

        public int Calls { get; private set; }

        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>(_rows.ToArray());
        }

        public void Deactivate(Guid languageId) => Replace(languageId, row => With(row, isActive: false));

        public void Activate(Guid languageId) => Replace(languageId, row => With(row, isActive: true));

        public void SetDefault(Guid languageId)
        {
            for (var index = 0; index < _rows.Count; index++)
            {
                _rows[index] = With(_rows[index], isDefault: _rows[index].LanguageId == languageId);
            }
        }

        public void Reorder(params (Guid LanguageId, int SortOrder)[] orders)
        {
            foreach (var (languageId, sortOrder) in orders)
            {
                Replace(languageId, row => With(row, sortOrder: sortOrder));
            }
        }

        private void Replace(Guid languageId, Func<LanguageLookupSnapshot, LanguageLookupSnapshot> change)
        {
            var index = _rows.FindIndex(row => row.LanguageId == languageId);
            _rows[index] = change(_rows[index]);
        }

        // Records expose init-only metadata: a controlled fixture row is replaced, never mutated in place.
        private static LanguageLookupSnapshot With(
            LanguageLookupSnapshot row,
            bool? isActive = null,
            bool? isDefault = null,
            int? sortOrder = null) =>
            new(row.LanguageId, row.Code, row.Culture, row.UrlPrefix, isDefault ?? row.IsDefault)
            {
                IsActive = isActive ?? row.IsActive,
                Direction = row.Direction,
                DisplayName = row.DisplayName,
                NativeName = row.NativeName,
                SortOrder = sortOrder ?? row.SortOrder,
            };
    }

    private sealed class RecordingAdminAuthorizer(IReadOnlyList<string> allowed) : IShippingAdminAuthorizer
    {
        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context,
            string permission,
            CancellationToken cancellationToken)
            => allowed.Contains(permission, StringComparer.Ordinal)
                ? Task.FromResult(Iran)
                : Task.FromException<Guid>(
                    new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)));
    }

    private sealed class RecordingSellerAccess(Action onCall) : ISellerPanelAccess
    {
        public Action? OnCall { get; set; } = onCall;

        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            OnCall?.Invoke();
            return Task.FromResult((Iran, Tehran));
        }
    }

    private sealed class RecordingEffectiveAccess(IReadOnlyList<PlatformPermissionGrant> grants)
        : IPlatformEffectiveAccessReader
    {
        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
            => Task.FromResult(grants);
    }

    private sealed class FakeMethodValidator : IShippingMethodOptionValidator
    {
        public Task<Result<bool>> ValidateAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken)
            => Task.FromResult(string.Equals(serviceCode, "post", StringComparison.Ordinal)
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(new SemanticError(ShippingErrorCodes.ServiceOptionUnknown)));
    }

    private sealed class FakeResolver : IShippingAuthoringDestinationResolver
    {
        public Task<Result<IReadOnlyList<ShippingDestinationSelectionPath>>> ResolveAsync(
            IReadOnlyList<ShippingAuthoringSelectedAreaWrite> selections,
            CancellationToken cancellationToken)
            => Task.FromResult(Result<IReadOnlyList<ShippingDestinationSelectionPath>>.Success(
                selections.Select(selection => ShippingDestinationSelectionPath.FromChain(
                    [
                        new ShippingDestinationNode(ShippingDestinationNodeKind.Country, selection.CountryId),
                        new ShippingDestinationNode(
                            ShippingDestinationNodeKind.AdministrativeArea,
                            selection.AdministrativeAreaId!.Value),
                    ])).ToArray()));
    }

    private sealed class FakeStoreCommerce(string? defaultCurrency) : ICurrentStoreCommerceContext
    {
        public StoreCommerceContext? Current => new("fa-IR", defaultCurrency, "web");
    }

    private sealed class StubCurrentEdition(EditionContext? current) : ICurrentEdition
    {
        public EditionContext? Current { get; } = current;
    }

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; set; } = current;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
