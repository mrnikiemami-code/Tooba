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
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
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
/// TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199 — the existing-draft editor document through the REAL
/// HTTP delegates: one consistent read snapshot and ONE composite atomic save with a SINGLE root
/// revision advance.
/// </summary>
/// <remarks>
/// <para>
/// Every fact composes the production entry (<see cref="ShippingEndpointModule"/>), the real
/// <see cref="ShippingAuthoringOwnerAuthorizer"/>, the real body reader, the real MediatR validation
/// pipeline, the real editor-document query/command handlers and the real
/// <see cref="ShippingAuthoringDraftDirectory"/> over the real <see cref="ShippingDbContext"/>. Only the
/// platform authorization edges, the destination/option seams, the language/currency context source and
/// the Fulfillment-owned type catalog are controlled fixtures.
/// </para>
/// <para>
/// <b>Dispatch vs execution.</b> <see cref="Harness.SentCount"/> counts real <c>ISender</c> dispatch and
/// <see cref="Harness.HandlerReached"/> counts arrival past validation, for the editor-document requests
/// only. A shape-rejected document may dispatch but must never reach a handler.
/// </para>
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringEditorDocumentEndpointTests
{
    private const string AdminDrafts = "/v1/admin/shipping-services/authoring-drafts";
    private const string SellerDrafts = "/v1/seller/shipping-services/authoring-drafts";
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PersianLanguage = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EnglishLanguage = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TypeId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public async Task One_composite_put_persists_the_whole_document_with_one_revision_advance_then_noop_and_conflict()
    {
        using var harness = Harness.Create("editor-http-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/editor-document";

        // The initial snapshot is a truthful root: revision 1, one group, unconfigured optional parts.
        var (initialStatus, _, initial) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, initialStatus);
        Assert.Equal(1, initial.GetProperty("revision").GetInt64());
        var initialGroup = Assert.Single(initial.GetProperty("regionGroups").EnumerateArray().ToArray());
        Assert.Equal(0, initialGroup.GetProperty("order").GetInt32());
        Assert.Single(initialGroup.GetProperty("areas").EnumerateArray());
        Assert.Empty(initialGroup.GetProperty("rows").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("settings").ValueKind);
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("weeklySchedule").ValueKind);

        var retainedGroup = initialGroup.GetProperty("groupId").GetGuid();

        // ONE composite PUT: a brand-new group with a nested rate row AND title, a reorder of the
        // retained group, method text, method settings, a weekly schedule and an explicit type selection.
        var (saveStatus, _, saved) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(
                revision: 1,
                groups:
                [
                    Group(null, [Isfahan], [Row(null, 0, 1000, 1, 3)], [(PersianLanguage, "شمال")]),
                    Group(retainedGroup, [Tehran], [], []),
                ],
                translations: [(PersianLanguage, "پست پیشتاز", null)],
                settings: (0, 0),
                schedule: "Asia/Tehran",
                type: "\"shippingTypeId\":\"" + TypeId + "\""));
        Assert.True(saveStatus == StatusCodes.Status200OK, saved.ToString());

        // Exactly ONE revision advance for the whole composite document.
        Assert.Equal(2, saved.GetProperty("revision").GetInt64());
        var groups = saved.GetProperty("regionGroups").EnumerateArray().ToArray();
        Assert.Equal(2, groups.Length);
        Assert.Equal(0, groups[0].GetProperty("order").GetInt32());
        Assert.Equal(1, groups[1].GetProperty("order").GetInt32());
        Assert.Equal(retainedGroup, groups[1].GetProperty("groupId").GetGuid());
        Assert.Equal("IRR", groups[0].GetProperty("currencyCode").GetString());
        Assert.Equal(1000, Assert.Single(groups[0].GetProperty("rows").EnumerateArray()).GetProperty("maxWeightGrams").GetInt64());
        Assert.Equal("شمال", Assert.Single(groups[0].GetProperty("titles").EnumerateArray()).GetProperty("title").GetString());
        Assert.Equal("پست پیشتاز", Assert.Single(
            saved.GetProperty("presentation").GetProperty("methodTranslations").EnumerateArray()).GetProperty("name").GetString());
        Assert.Equal(0, saved.GetProperty("settings").GetProperty("freightPaymentMode").GetInt32());
        Assert.Equal("Asia/Tehran", saved.GetProperty("weeklySchedule").GetProperty("timeZoneId").GetString());
        Assert.Equal(7, saved.GetProperty("weeklySchedule").GetProperty("days").GetArrayLength());
        Assert.Equal(TypeId, saved.GetProperty("type").GetProperty("shippingTypeId").GetGuid());

        // A fresh GET reports the same whole tree and the same single revision.
        var (readStatus, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, readStatus);
        Assert.Equal(2, read.GetProperty("revision").GetInt64());
        Assert.Equal(2, read.GetProperty("regionGroups").GetArrayLength());
        Assert.Equal(TypeId, read.GetProperty("type").GetProperty("shippingTypeId").GetGuid());

        // An identical document is an honest no-op: no write, no revision/timestamp movement.
        var stamp = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(2, stamp.Revision);
        var rowId = Assert.Single(
            saved.GetProperty("regionGroups").EnumerateArray().ToArray()[0].GetProperty("rows").EnumerateArray())
            .GetProperty("rateRowId").GetGuid();
        var (noOpStatus, _, noOp) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(
                revision: 2,
                groups:
                [
                    Group(groups[0].GetProperty("groupId").GetGuid(), [Isfahan], [Row(rowId, 0, 1000, 1, 3)], [(PersianLanguage, "شمال")]),
                    Group(retainedGroup, [Tehran], [], []),
                ],
                translations: [(PersianLanguage, "پست پیشتاز", null)],
                settings: (0, 0),
                schedule: "Asia/Tehran",
                type: "\"shippingTypeId\":\"" + TypeId + "\""));
        Assert.Equal(StatusCodes.Status200OK, noOpStatus);
        Assert.Equal(2, noOp.GetProperty("revision").GetInt64());
        var afterNoOp = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(2, afterNoOp.Revision);
        Assert.Equal(stamp.UpdatedAt, afterNoOp.UpdatedAt);

        // A stale client is the canonical conflict even for an otherwise identical document.
        var (staleStatus, _, stale) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(1, [Group(retainedGroup, [Tehran], [], [])], []));
        Assert.Equal(StatusCodes.Status409Conflict, staleStatus);
        Assert.Equal(
            ShippingErrorCodes.AuthoringDraftRevisionConflict,
            stale.GetProperty("errorCode").GetString());

        // An omitted optional wrapper preserves the stored value; it never clears it.
        var (preserveStatus, _, preserved) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(
                revision: 2,
                groups:
                [
                    Group(groups[0].GetProperty("groupId").GetGuid(), [Isfahan], [Row(rowId, 0, 1000, 1, 3)], []),
                    Group(retainedGroup, [Tehran], [], []),
                ],
                translations: []));
        Assert.Equal(StatusCodes.Status200OK, preserveStatus);
        Assert.Equal(2, preserved.GetProperty("revision").GetInt64());
        Assert.Equal("Asia/Tehran", preserved.GetProperty("weeklySchedule").GetProperty("timeZoneId").GetString());
        Assert.Equal(0, preserved.GetProperty("settings").GetProperty("freightPaymentMode").GetInt32());
        Assert.Equal(TypeId, preserved.GetProperty("type").GetProperty("shippingTypeId").GetGuid());
        Assert.Equal("پست پیشتاز", Assert.Single(
            preserved.GetProperty("presentation").GetProperty("methodTranslations").EnumerateArray()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_late_invalid_child_or_unavailable_language_is_rejected_before_any_write()
    {
        using var harness = Harness.Create("editor-http-b");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/editor-document";
        var before = await harness.ReadDraftStampAsync(draftId);

        // A reversed interval inside a BRAND-NEW group is proven before the first mutation: nothing is
        // added and then rolled back.
        var (reversedStatus, _, reversed) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(1, [Group(null, [Isfahan], [Row(null, 5000, 1000, 3, 1)], [])], []));
        Assert.Equal(StatusCodes.Status400BadRequest, reversedStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringRateConditionInvalid, reversed.GetProperty("errorCode").GetString());
        Assert.Equal(before.Revision, (await harness.ReadDraftStampAsync(draftId)).Revision);

        // A title for a language that is not configured fails closed.
        var (languageStatus, _, language) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(1, [Group(null, [Isfahan], [], [(EnglishLanguage, "north")])], []));
        Assert.Equal(StatusCodes.Status400BadRequest, languageStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringPresentationLanguageInvalid, language.GetProperty("errorCode").GetString());

        // A duplicate node across two groups is the canonical overlap rejection.
        var (overlapStatus, _, overlap) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(1, [Group(null, [Isfahan], [], []), Group(null, [Isfahan], [], [])], []));
        Assert.Equal(StatusCodes.Status400BadRequest, overlapStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionSelectionOverlap, overlap.GetProperty("errorCode").GetString());

        // An empty type id is invalid; an absent wrapper is not a selection and never needs the catalog.
        var (typeStatus, _, type) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(
                revision: 1,
                groups: [Group(null, [Isfahan], [], [])],
                type: "\"shippingTypeId\":\"00000000-0000-0000-0000-000000000000\""));
        Assert.Equal(StatusCodes.Status400BadRequest, typeStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringTypeAssignmentInvalid, type.GetProperty("errorCode").GetString());

        // The whole draft is untouched: same revision, same single group, same timestamp.
        var after = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
        var (_, _, current) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Single(current.GetProperty("regionGroups").EnumerateArray());
        Assert.Empty(current.GetProperty("presentation").GetProperty("methodTranslations").EnumerateArray());
    }

    [Fact]
    public async Task Malformed_documents_never_reach_a_handler_and_unowned_drafts_are_not_found()
    {
        using var harness = Harness.Create("editor-http-c");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/editor-document";
        var before = await harness.ReadDraftStampAsync(draftId);

        var invalid = new[]
        {
            // Required collections are absent: only an explicit [] means "empty".
            "{\"expectedRevision\":1,\"methodTranslations\":[]}",
            "{\"expectedRevision\":1,\"regionGroups\":[]}",
            // A non-positive revision is a shape failure like every other authoring write.
            "{\"expectedRevision\":0,\"regionGroups\":[],\"methodTranslations\":[]}",
        };

        foreach (var json in invalid)
        {
            var reachedBefore = harness.HandlerReached;
            var (status, _, body) = await harness.SendAsync(HttpMethod.Put, path, json);
            Assert.True(status == StatusCodes.Status400BadRequest, json + " -> " + body);
            Assert.Equal(reachedBefore, harness.HandlerReached);
        }

        // Null element collections pass the DTO shape but are refused by the handler before any write.
        var nested = new[]
        {
            "{\"expectedRevision\":1,\"regionGroups\":[{\"groupId\":null,\"areas\":null,\"rows\":[],\"titles\":[]}],\"methodTranslations\":[]}",
            "{\"expectedRevision\":1,\"regionGroups\":[{\"groupId\":null,\"areas\":[],\"rows\":null,\"titles\":[]}],\"methodTranslations\":[]}",
            "{\"expectedRevision\":1,\"regionGroups\":[{\"groupId\":null,\"areas\":[],\"rows\":[],\"titles\":null}],\"methodTranslations\":[]}",
        };

        foreach (var json in nested)
        {
            var (status, _, body) = await harness.SendAsync(HttpMethod.Put, path, json);
            Assert.True(status == StatusCodes.Status400BadRequest, json + " -> " + body);
        }

        var (malformedStatus, _, malformed) = await harness.SendAsync(
            HttpMethod.Put, path, "{\"expectedRevision\":1,\"regionGroups\":");
        Assert.Equal(StatusCodes.Status400BadRequest, malformedStatus);
        Assert.False(string.IsNullOrEmpty(malformed.GetProperty("errorCode").GetString()));

        Assert.True(harness.SentCount > 0);
        Assert.Equal(before.Revision, (await harness.ReadDraftStampAsync(draftId)).Revision);

        // An unknown draft is the same safe not-found for a GET and a PUT.
        var unknown = Guid.NewGuid();
        var (missingGetStatus, _, missingGet) = await harness.SendAsync(
            HttpMethod.Get, $"{AdminDrafts}/{unknown}/editor-document");
        Assert.Equal(StatusCodes.Status404NotFound, missingGetStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, missingGet.GetProperty("errorCode").GetString());

        var (missingPutStatus, _, missingPut) = await harness.SendAsync(
            HttpMethod.Put, $"{AdminDrafts}/{unknown}/editor-document", Document(1, [], []));
        Assert.Equal(StatusCodes.Status404NotFound, missingPutStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, missingPut.GetProperty("errorCode").GetString());

        // A draft of ANOTHER store is not found through this owner scope and is never written.
        var (foreignGetStatus, _, _) = await harness.SendAsync(
            HttpMethod.Get, path, tenantId: "store-b");
        Assert.Equal(StatusCodes.Status404NotFound, foreignGetStatus);
        var (foreignPutStatus, _, _) = await harness.SendAsync(
            HttpMethod.Put, path, Document(1, []), tenantId: "store-b");
        Assert.Equal(StatusCodes.Status404NotFound, foreignPutStatus);
        Assert.Equal(before.Revision, (await harness.ReadDraftStampAsync(draftId)).Revision);
    }

    [Fact]
    public async Task Seller_owner_route_shares_the_same_document_and_advances_one_revision()
    {
        using var harness = Harness.Create("editor-http-d", ToobaEdition.Marketplace, marketplace: true);
        var draftId = await harness.CreateSellerDraftAsync(Tehran);
        var path = $"{SellerDrafts}/{draftId}/editor-document";

        var (initialStatus, _, initial) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, initialStatus);
        Assert.Equal(1, initial.GetProperty("revision").GetInt64());

        var retainedGroup = Assert.Single(initial.GetProperty("regionGroups").EnumerateArray().ToArray())
            .GetProperty("groupId").GetGuid();

        var (saveStatus, _, saved) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(
                revision: 1,
                groups:
                [
                    Group(null, [Isfahan], [Row(null, 0, 2000, 1, 2)], [(PersianLanguage, "جنوب")]),
                    Group(retainedGroup, [Tehran], [], []),
                ],
                translations: [(PersianLanguage, "تیپاکس", null)]));
        Assert.True(saveStatus == StatusCodes.Status200OK, saved.ToString());
        Assert.Equal(2, saved.GetProperty("revision").GetInt64());
        Assert.Equal(2, saved.GetProperty("regionGroups").GetArrayLength());

        Assert.Equal(2, (await harness.ReadDraftStampAsync(draftId)).Revision);
    }

    [Fact]
    public async Task New_editor_routes_deny_before_dispatch_body_reads_or_any_business_lookup()
    {
        // (a) View-only admin: the operator can READ the document but a malformed PUT is denied by the
        // owner gate before the body is read, before ISender, and before any catalog/context lookup.
        using (var admin = Harness.Create("editor-http-e"))
        {
            var draftId = await admin.CreateDraftAsync(Tehran);
            var path = $"{AdminDrafts}/{draftId}/editor-document";
            admin.AllowAdmin(ShippingAdminPermissions.View);

            admin.ResetCounters();
            var (readStatus, _, read) = await admin.SendAsync(HttpMethod.Get, path);
            Assert.Equal(StatusCodes.Status200OK, readStatus);
            Assert.Equal(1, read.GetProperty("revision").GetInt64());

            admin.ResetCounters();
            var (deniedStatus, _, denied) = await admin.SendAsync(
                HttpMethod.Put, path, "{\"expectedRevision\":1,\"regionGroups\":");
            Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
            Assert.Equal(
                FoundationErrorCodes.AdminAuthorizationDenied,
                denied.GetProperty("errorCode").GetString());
            Assert.Equal(0, admin.BodyReads);
            Assert.Equal(0, admin.SentCount);
            Assert.Equal(0, admin.HandlerReached);
            Assert.Equal(0, admin.CatalogReads);
            Assert.Equal(0, admin.ContextReads);
            Assert.Equal(1, (await admin.ReadDraftStampAsync(draftId)).Revision);
        }

        // (b) SingleStore seller: the capability is unavailable (canonical 404) BEFORE seller resolution,
        // dispatch, body, catalog or context.
        using (var single = Harness.Create("editor-http-f"))
        {
            var path = $"{SellerDrafts}/{Guid.NewGuid()}/editor-document";

            single.ResetCounters();
            var (getStatus, _, get) = await single.SendAsync(HttpMethod.Get, path);
            Assert.Equal(StatusCodes.Status404NotFound, getStatus);
            Assert.Equal(
                ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
                get.GetProperty("errorCode").GetString());
            Assert.Equal(0, single.SellerCalls);
            Assert.Equal(0, single.SentCount);
            Assert.Equal(0, single.BodyReads);
            Assert.Equal(0, single.CatalogReads);
            Assert.Equal(0, single.ContextReads);

            single.ResetCounters();
            var (putStatus, _, put) = await single.SendAsync(
                HttpMethod.Put, path, "{\"expectedRevision\":1,\"regionGroups\":");
            Assert.Equal(StatusCodes.Status404NotFound, putStatus);
            Assert.Equal(
                ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
                put.GetProperty("errorCode").GetString());
            Assert.Equal(0, single.SellerCalls);
            Assert.Equal(0, single.SentCount);
            Assert.Equal(0, single.BodyReads);
            Assert.Equal(0, single.CatalogReads);
            Assert.Equal(0, single.ContextReads);
        }

        // (c) Marketplace seller whose manage grant is CATEGORY-scoped: actor resolution happens (it is
        // needed to read grants) but the owner gate answers 403 before dispatch/body/catalog/context.
        using (var scoped = Harness.Create(
                   "editor-http-g", ToobaEdition.Marketplace, marketplace: true, sellerHasGlobalManage: true))
        {
            var draftId = await scoped.CreateSellerDraftAsync(Tehran);
            var path = $"{SellerDrafts}/{draftId}/editor-document";
            scoped.DowngradeSellerToCategoryManage();

            scoped.ResetCounters();
            var (status, _, body) = await scoped.SendAsync(
                HttpMethod.Put, path, "{\"expectedRevision\":1,\"regionGroups\":");
            Assert.Equal(StatusCodes.Status403Forbidden, status);
            Assert.Equal(
                ShippingErrorCodes.AuthoringOwnerScopeDenied,
                body.GetProperty("errorCode").GetString());
            Assert.Equal(1, scoped.SellerCalls);
            Assert.Equal(0, scoped.BodyReads);
            Assert.Equal(0, scoped.SentCount);
            Assert.Equal(0, scoped.HandlerReached);
            Assert.Equal(0, scoped.CatalogReads);
            Assert.Equal(0, scoped.ContextReads);
            Assert.Equal(1, (await scoped.ReadDraftStampAsync(draftId)).Revision);
        }
    }

    [Fact]
    public async Task The_new_composite_handler_reads_the_catalog_once_and_a_later_read_failure_keeps_the_commit()
    {
        using var harness = Harness.Create("editor-http-h");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/editor-document";

        // The catalog answers exactly ONE lookup and throws a named sentinel on ANY second lookup, so an
        // extra lookup introduced into the NEW composite handler cannot pass silently.
        harness.Catalog.ArmSingleLookup();
        harness.ResetCounters();

        var (saveStatus, _, saved) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(
                revision: 1,
                groups: [Group(null, [Isfahan], [], [])],
                type: "\"shippingTypeId\":\"" + TypeId + "\""));
        Assert.True(saveStatus == StatusCodes.Status200OK, saved.ToString());
        Assert.Equal(1, harness.CatalogReads);
        Assert.Equal(1, harness.Catalog.Reads);
        Assert.Equal(draftId, saved.GetProperty("draftId").GetGuid());
        Assert.Equal(2, saved.GetProperty("revision").GetInt64());
        Assert.Equal(TypeId, saved.GetProperty("type").GetProperty("shippingTypeId").GetGuid());

        var committed = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(2, committed.Revision);

        // A subsequent deliberate read genuinely hits the throwing dependency. This is a READ failure,
        // separate from the PUT success, and the already-committed tree/revision/time must not move.
        harness.ResetCounters();
        var (readStatus, _, _) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status500InternalServerError, readStatus);
        Assert.Equal(1, harness.CatalogReads);

        var afterFailedRead = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(committed.Revision, afterFailedRead.Revision);
        Assert.Equal(committed.UpdatedAt, afterFailedRead.UpdatedAt);
        Assert.Equal(
            TypeId,
            await harness.WithContextAsync(async context =>
                (await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId))
                .ShippingTypeId!.Value));

        // The clear path is covered by the SAME new handler and needs no catalog lookup at all.
        harness.ResetCounters();
        var (clearStatus, _, cleared) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Document(
                revision: 2,
                groups: [Group(null, [Isfahan], [], [])],
                type: "\"shippingTypeId\":null"));
        Assert.True(clearStatus == StatusCodes.Status200OK, cleared.ToString());
        Assert.Equal(0, harness.CatalogReads);
        Assert.Equal(3, cleared.GetProperty("revision").GetInt64());
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("type").GetProperty("shippingTypeId").ValueKind);
    }

    private static string CreateBody(IEnumerable<Guid> areas)
        => string.Concat(
            "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":[",
            string.Join(',', areas.Select(area =>
                $"{{\"groupId\":null,\"areas\":[{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{area}\"}}]}}")),
            "]}");

    private static string Group(
        Guid? groupId,
        IReadOnlyList<Guid> areas,
        IReadOnlyList<string> rows,
        IReadOnlyList<(Guid LanguageId, string? Title)> titles)
        => string.Concat(
            "{\"groupId\":",
            groupId is { } id ? $"\"{id}\"" : "null",
            ",\"areas\":[",
            string.Join(',', areas.Select(area =>
                $"{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{area}\"}}")),
            "],\"rows\":[",
            string.Join(',', rows),
            "],\"titles\":[",
            string.Join(',', titles.Select(title =>
                title.Title is null
                    ? $"{{\"languageId\":\"{title.LanguageId}\",\"title\":null}}"
                    : $"{{\"languageId\":\"{title.LanguageId}\",\"title\":\"{title.Title}\"}}")),
            "]}");

    private static string Row(Guid? rateRowId, long minWeight, long maxWeight, int minDays, int maxDays)
        => string.Concat(
            "{\"rateRowId\":",
            rateRowId is { } id ? $"\"{id}\"" : "null",
            $",\"minWeightGrams\":{minWeight},\"maxWeightGrams\":{maxWeight},"
            + "\"minItemCount\":null,\"maxItemCount\":null,"
            + "\"minMerchandiseAmount\":null,\"maxMerchandiseAmount\":null,"
            + "\"freightAmount\":25000,\"packagingAmount\":5000,"
            + $"\"minDeliveryDays\":{minDays},\"maxDeliveryDays\":{maxDays}}}");

    private static string Document(
        long revision,
        IReadOnlyList<string> groups,
        IReadOnlyList<(Guid LanguageId, string Name, string? Description)>? translations = null,
        (int Freight, int Calculation)? settings = null,
        string? schedule = null,
        string? type = null)
    {
        translations ??= [];
        var parts = new List<string>
        {
            $"\"expectedRevision\":{revision}",
            "\"regionGroups\":[" + string.Join(',', groups) + "]",
            "\"methodTranslations\":[" + string.Join(',', translations.Select(translation =>
                translation.Description is null
                    ? $"{{\"languageId\":\"{translation.LanguageId}\",\"name\":\"{translation.Name}\"}}"
                    : $"{{\"languageId\":\"{translation.LanguageId}\",\"name\":\"{translation.Name}\",\"description\":\"{translation.Description}\"}}")) + "]",
        };

        if (settings is { } chosen)
        {
            parts.Add($"\"methodSettings\":{{\"freightPaymentMode\":{chosen.Freight},\"calculationMode\":{chosen.Calculation}}}");
        }

        if (schedule is not null)
        {
            var days = Enumerable.Range(1, 7).Select(weekday => weekday == 6
                ? "{\"weekday\":6,\"isWorking\":true,\"startTime\":\"09:00:00\",\"endTime\":\"13:00:00\"}"
                : $"{{\"weekday\":{weekday},\"isWorking\":false}}");
            parts.Add($"\"weeklySchedule\":{{\"timeZoneId\":\"{schedule}\",\"days\":[" + string.Join(',', days) + "]}");
        }

        if (type is not null)
        {
            parts.Add($"\"typeAssignment\":{{{type}}}");
        }

        return "{" + string.Join(',', parts) + "}";
    }

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;
        private readonly Counter _counter;
        private readonly RecordingAdminAuthorizer _admin;
        private readonly RecordingEffectiveAccess _sellerAccess;

        private Harness(
            WebApplication app,
            IServiceScope scope,
            Counter counter,
            RecordingAdminAuthorizer admin,
            RecordingEffectiveAccess sellerAccess)
        {
            _app = app;
            _scope = scope;
            _counter = counter;
            _admin = admin;
            _sellerAccess = sellerAccess;
        }

        public string AcceptLanguage { get; set; } = "fa";

        public TenantContext? DefaultTenant { get; set; }

        public int SentCount => _counter.Sent;

        public int HandlerReached => _counter.Handler;

        public int SellerCalls => _counter.Seller;

        public int ContextReads => _counter.Context;

        public int CatalogReads => _counter.Catalog;

        public int BodyReads => _counter.BodyReads;

        public FakeTypeCatalog Catalog =>
            (FakeTypeCatalog)_scope.ServiceProvider.GetRequiredService<IShippingTypeCatalogReader>();

        /// <summary>Downgrade the admin edge to a narrower permission without rebuilding the harness.</summary>
        public void AllowAdmin(params string[] permissions)
        {
            _admin.Allowed.Clear();
            _admin.Allowed.AddRange(permissions);
        }

        public void ResetCounters() => _counter.Reset();

        /// <summary>
        /// Replaces the seller's whole-owner manage grant with a CATEGORY-scoped one, which is never
        /// whole-owner permission (the owner gate must answer 403 before dispatch).
        /// </summary>
        public void DowngradeSellerToCategoryManage()
        {
            _sellerAccess.Grants.Clear();
            _sellerAccess.Grants.Add(new PlatformPermissionGrant(
                ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false));
            _sellerAccess.Grants.Add(new PlatformPermissionGrant(
                ShippingAdminPermissions.Manage, PlatformAccessScopeKind.Category, Guid.NewGuid(), false));
        }

        public static Harness Create(
            string database,
            ToobaEdition edition = ToobaEdition.SingleStore,
            bool marketplace = false,
            string? tenantId = "store-a",
            bool sellerHasGlobalManage = true)
        {
            var counter = new Counter();
            var tenant = new StubCurrentTenant(marketplace ? null : Tenant(tenantId ?? "store-a"));
            var admin = new RecordingAdminAuthorizer([ShippingAdminPermissions.View, ShippingAdminPermissions.Manage]);
            var sellerAccess = new RecordingEffectiveAccess(
                sellerHasGlobalManage
                    ? new PlatformPermissionGrant[]
                    {
                        new(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                        new(ShippingAdminPermissions.Manage, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                    }
                    :
                    [
                        new(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                        // A CATEGORY-scoped manage grant is never "whole owner" permission.
                        new(ShippingAdminPermissions.Manage, PlatformAccessScopeKind.Category, Guid.NewGuid(), false),
                    ]);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SentCountingBehavior<,>));
            builder.Services.AddToobaCqrsFoundation(typeof(CreateShippingAuthoringDraftCommand).Assembly);
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HandlerReachedBehavior<,>));
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddDbContext<ShippingDbContext>(options => options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
            builder.Services.AddSingleton<IShippingAdminAuthorizer>(admin);
            builder.Services.AddSingleton<ISellerPanelAccess>(new RecordingSellerAccess(counter));
            builder.Services.AddSingleton<IPlatformEffectiveAccessReader>(sellerAccess);
            builder.Services.AddSingleton<ICurrentEdition>(
                new StubCurrentEdition(new EditionContext(edition, "deployment-test")));
            builder.Services.AddSingleton<ICurrentTenant>(tenant);
            builder.Services.AddSingleton(tenant);
            builder.Services.AddSingleton<IShippingMethodOptionValidator>(new FakeMethodValidator());
            builder.Services.AddSingleton<IShippingAuthoringDestinationResolver>(new FakeResolver());
            builder.Services.AddSingleton<IShippingAuthoringContextSource>(new FakeContextSource(edition, "IRR", counter));
            builder.Services.AddSingleton<IShippingTypeCatalogReader>(new FakeTypeCatalog(counter));
            builder.Services.AddSingleton(counter);

            var app = builder.Build();
            app.MapShippingEndpoints();
            var scope = app.Services.CreateScope();
            return new Harness(app, scope, counter, admin, sellerAccess)
            {
                DefaultTenant = marketplace ? null : Tenant(tenantId ?? "store-a"),
            };
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
            var (status, _, body) = await SendAsync(HttpMethod.Post, AdminDrafts, CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<Guid> CreateSellerDraftAsync(params Guid[] areas)
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, SellerDrafts, CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<(long Revision, DateTimeOffset UpdatedAt)> ReadDraftStampAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
                return (draft.Revision, draft.UpdatedAt);
            });

        public async Task<T> WithContextAsync<T>(Func<ShippingDbContext, Task<T>> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<(int Status, string? Location, JsonElement Body)> SendAsync(
            HttpMethod method,
            string path,
            string? json = null,
            string? tenantId = null)
        {
            var current = _scope.ServiceProvider.GetRequiredService<StubCurrentTenant>();
            current.Current = tenantId is null ? DefaultTenant : Tenant(tenantId);

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
                context.Request.Body = new CountingStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(json)),
                    _counter);
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

        /// <summary>Real seller-panel actor resolution calls (must stay zero before dispatch on denial).</summary>
        public int Seller { get; set; }

        /// <summary>Business context (language/currency) reads.</summary>
        public int Context { get; set; }

        /// <summary>Fulfillment type-catalog lookups.</summary>
        public int Catalog { get; set; }

        /// <summary>Request body stream reads; zero proves the owner gate ran before any body read.</summary>
        public int BodyReads { get; set; }

        public void Reset()
        {
            Sent = 0;
            Handler = 0;
            Seller = 0;
            Context = 0;
            Catalog = 0;
            BodyReads = 0;
        }
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
            if (IsEditorDocument(request))
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
            if (IsEditorDocument(request))
            {
                counter.Handler++;
            }

            return await next();
        }
    }

    private static bool IsEditorDocument(object request) =>
        request is GetShippingAuthoringEditorDocumentQuery or ReplaceShippingAuthoringEditorDocumentCommand;

    private sealed class RecordingAdminAuthorizer(IReadOnlyList<string> allowed) : IShippingAdminAuthorizer
    {
        /// <summary>Mutable so one harness can start fully permitted and then be downgraded to view-only.</summary>
        public List<string> Allowed { get; } = [.. allowed];

        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context,
            string permission,
            CancellationToken cancellationToken)
            => Allowed.Contains(permission, StringComparer.Ordinal)
                ? Task.FromResult(Iran)
                : Task.FromException<Guid>(
                    new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)));
    }

    private sealed class RecordingSellerAccess(Counter counter) : ISellerPanelAccess
    {
        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            counter.Seller++;
            return Task.FromResult((Iran, Tehran));
        }
    }

    private sealed class RecordingEffectiveAccess(IReadOnlyList<PlatformPermissionGrant> grants)
        : IPlatformEffectiveAccessReader
    {
        /// <summary>Mutable so one harness can create a draft with a whole-owner grant and then downgrade.</summary>
        public List<PlatformPermissionGrant> Grants { get; } = [.. grants];

        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PlatformPermissionGrant>>(Grants);
    }

    private sealed class FakeContextSource(ToobaEdition edition, string? currency, Counter counter)
        : IShippingAuthoringContextSource
    {
        public Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken)
        {
            counter.Context++;
            return Task.FromResult(new ShippingAuthoringContextSourceSnapshot(
                edition,
                currency,
                [
                    new ShippingAuthoringLanguageSource(PersianLanguage, "fa", "fa-IR", "fa", true, true, "rtl", "فارسی", "فارسی", 0),
                    new ShippingAuthoringLanguageSource(EnglishLanguage, "en", "en-US", "en", false, false, "ltr", "English", "English", 1),
                ]));
        }
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

    private sealed class FakeTypeCatalog(Counter counter) : IShippingTypeCatalogReader
    {
        private static readonly ShippingTypeFact Fact = new(
            TypeId,
            "parcel",
            true,
            0,
            1,
            [new ShippingTypeFactTranslation(PersianLanguage, "بسته", null)]);

        /// <summary>Named test-only failure identity: the second lookup must be the ONLY allowed failure.</summary>
        public SentinelCatalogException? Sentinel { get; private set; }

        /// <summary>Total lookups since the fixture was created (survives counter resets).</summary>
        public int Reads { get; private set; }

        /// <summary>Supplies one active fact on the FIRST lookup and throws on ANY second lookup.</summary>
        public void ArmSingleLookup()
        {
            Sentinel = new SentinelCatalogException("tb199r1: second type-catalog lookup");
        }

        public Task<IReadOnlyList<ShippingTypeFact>> ListActiveAsync(CancellationToken cancellationToken)
        {
            Count();
            return Task.FromResult<IReadOnlyList<ShippingTypeFact>>([Fact]);
        }

        public Task<IReadOnlyList<ShippingTypeFact>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken)
        {
            Count();
            return Task.FromResult<IReadOnlyList<ShippingTypeFact>>(
                shippingTypeIds.Contains(Fact.ShippingTypeId) ? [Fact] : []);
        }

        private void Count()
        {
            counter.Catalog++;
            Reads++;
            if (Sentinel is not null && Reads > 1)
            {
                throw Sentinel;
            }
        }
    }

    /// <summary>Test-only failure identity for the second catalog lookup.</summary>
    private sealed class SentinelCatalogException(string message) : Exception(message);

    /// <summary>Counts request-body reads so a denial can prove the body was never consumed.</summary>
    private sealed class CountingStream(Stream inner, Counter counter) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            counter.BodyReads++;
            return inner.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            counter.BodyReads++;
            return inner.Read(buffer);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            counter.BodyReads++;
            return inner.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            counter.BodyReads++;
            return inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
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
