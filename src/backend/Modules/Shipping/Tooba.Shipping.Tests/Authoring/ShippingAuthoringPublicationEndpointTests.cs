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
using Tooba.Shipping.Application.Authoring.Validation;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.StoreContext.Contracts.Current;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220 — the authorized publication API for an EXISTING draft,
/// executed through the REAL admin/seller route delegates.
/// </summary>
/// <remarks>
/// <para>
/// Every fact composes the production <see cref="ShippingEndpointModule"/>, the real
/// <see cref="Tooba.Shipping.Endpoints.Admin.ShippingAuthoringOwnerAuthorizer"/>, the real body reader, the
/// real MediatR validation pipeline, the real publication query/command handlers, the real
/// <see cref="ShippingAuthoringDraftDirectory"/> (TB-214 capture) and the real
/// <see cref="ShippingSelectedCapturedRevisionDirectory"/> (TB-219 pointer) over a real
/// <see cref="ShippingDbContext"/> on the task-owned disposable PostgreSQL target. Only the platform
/// authorization edges, the destination/option seams and the commerce context source are controlled.
/// </para>
/// <para>
/// <b>Publication is selection intent.</b> No fact asserts customer eligibility, readiness, pricing or a
/// Storefront connection; a selected captured revision is only the stored DATA the next slice will consume.
/// </para>
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringPublicationEndpointTests
{
    private const string AdminDrafts = "/v1/admin/shipping-services/authoring-drafts";
    private const string SellerDrafts = "/v1/seller/shipping-services/authoring-drafts";
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PersianLanguage = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _schemaReady;

    [Fact]
    public async Task View_only_admin_reads_an_explicit_absent_state_and_never_publishes()
    {
        using var harness = Harness.Create("pub-view-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/publication";

        var (status, _, body) = await harness.SendAsync(HttpMethod.Get, path);

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(draftId, body.GetProperty("draftId").GetGuid());
        Assert.Equal(1, body.GetProperty("currentDraftRevision").GetInt64());
        Assert.Equal("post", body.GetProperty("serviceCode").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("optionCode").ValueKind);
        // Absent is 0 and is the ONLY state where the version is 0.
        Assert.Equal((int)ShippingAuthoringPublicationState.Absent, body.GetProperty("state").GetInt32());
        Assert.Equal(0, body.GetProperty("selectionVersion").GetInt64());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("selectedSnapshotId").ValueKind);
        Assert.False(body.GetProperty("selectedMatchesCurrentDraftRevision").GetBoolean());

        // A read-only administrator is denied BEFORE the body is read, before dispatch and before any write.
        using var readOnly = Harness.Create("pub-view-a", adminAllowed: [ShippingAdminPermissions.View]);
        var (deniedStatus, _, _) = await readOnly.SendAsync(
            HttpMethod.Post,
            path,
            "{ not json ]");
        Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
        Assert.Equal(0, readOnly.SentCount);
        Assert.Equal(0, await readOnly.CountSnapshotsAsync(draftId));
        Assert.Equal(0, await readOnly.CountPointersAsync(draftId));
    }

    [Fact]
    public async Task Malformed_publication_payloads_are_rejected_before_any_capture_or_pointer_write()
    {
        using var harness = Harness.Create("pub-shape-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/publication";

        // Missing publication body fields: the [JsonRequired] contract rejects the payload before ANY
        // handler, and an explicit null is a shape failure with the stable selection-version code. An
        // omitted expectedSelectionVersion is never "version 0" in either case.
        var (missingStatus, _, missing) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1}");
        Assert.Equal(StatusCodes.Status400BadRequest, missingStatus);
        Assert.False(string.IsNullOrEmpty(missing.GetProperty("errorCode").GetString()));

        var (nullVersionStatus, _, nullVersion) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":null}");
        Assert.Equal(StatusCodes.Status400BadRequest, nullVersionStatus);
        Assert.Contains(
            ShippingAuthoringValidationCodes.ExpectedSelectionVersionInvalid,
            nullVersion.GetRawText(),
            StringComparison.Ordinal);

        // Non-positive saved draft revision and negative selection version are shape failures.
        var (revisionStatus, _, revision) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":0,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status400BadRequest, revisionStatus);
        Assert.Contains(
            ShippingAuthoringValidationCodes.ExpectedRevisionInvalid,
            revision.GetRawText(),
            StringComparison.Ordinal);

        var (negativeStatus, _, negative) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":-1}");
        Assert.Equal(StatusCodes.Status400BadRequest, negativeStatus);
        Assert.Contains(
            ShippingAuthoringValidationCodes.ExpectedSelectionVersionInvalid,
            negative.GetRawText(),
            StringComparison.Ordinal);

        var (withdrawStatus, _, withdraw) = await harness.SendAsync(
            HttpMethod.Post,
            $"{path}/withdraw",
            "{}");
        Assert.Equal(StatusCodes.Status400BadRequest, withdrawStatus);
        Assert.False(string.IsNullOrEmpty(withdraw.GetProperty("errorCode").GetString()));

        var (withdrawNullStatus, _, withdrawNull) = await harness.SendAsync(
            HttpMethod.Post,
            $"{path}/withdraw",
            "{\"expectedSelectionVersion\":null}");
        Assert.Equal(StatusCodes.Status400BadRequest, withdrawNullStatus);
        Assert.Contains(
            ShippingAuthoringValidationCodes.ExpectedSelectionVersionInvalid,
            withdrawNull.GetRawText(),
            StringComparison.Ordinal);

        // Nothing was captured and no pointer row exists.
        Assert.Equal(0, await harness.CountSnapshotsAsync(draftId));
        Assert.Equal(0, await harness.CountPointersAsync(draftId));
        Assert.Equal(1, (await harness.ReadDraftStampAsync(draftId)).Revision);
    }

    [Fact]
    public async Task Seller_publication_needs_marketplace_and_a_legitimate_seller_keeps_its_own_owner()
    {
        // SingleStore: the seller family is closed (404) before any dispatch or write.
        using var store = Harness.Create("pub-seller-store-a", edition: ToobaEdition.SingleStore);
        var storeDraftId = await store.CreateDraftAsync(Tehran);
        var (closedStatus, _, _) = await store.SendAsync(
            HttpMethod.Post,
            $"{SellerDrafts}/{storeDraftId}/publication",
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status404NotFound, closedStatus);
        Assert.Equal(0, store.SentCount);
        Assert.Equal(0, await store.CountSnapshotsAsync(storeDraftId));

        // Marketplace without a whole-owner Manage grant: the SAME seller can create its draft with Manage,
        // but a View-only publication is denied with zero mutation.
        var deniedSeller = Guid.NewGuid();
        Guid deniedDraftId;
        using (var deniedOwner = Harness.Create(
                   "pub-seller-denied-a",
                   edition: ToobaEdition.Marketplace,
                   marketplace: true,
                   sellerPartyId: deniedSeller))
        {
            deniedDraftId = await deniedOwner.CreateSellerDraftAsync(Tehran);
            Assert.Equal(0, await deniedOwner.CountSnapshotsAsync(deniedDraftId));
        }

        using var denied = Harness.Create(
            "pub-seller-denied-b",
            edition: ToobaEdition.Marketplace,
            marketplace: true,
            sellerGrants: [new PlatformPermissionGrant(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false)],
            sellerPartyId: deniedSeller);
        var (deniedStatus, _, _) = await denied.SendAsync(
            HttpMethod.Post,
            $"{SellerDrafts}/{deniedDraftId}/publication",
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
        Assert.Equal(0, denied.SentCount);
        Assert.Equal(0, await denied.CountSnapshotsAsync(deniedDraftId));

        // A legitimate Marketplace seller publishes under ITS OWN owner and reads the same provenance.
        var okSeller = Guid.NewGuid();
        using var seller = Harness.Create(
            "pub-seller-ok-a",
            edition: ToobaEdition.Marketplace,
            marketplace: true,
            sellerPartyId: okSeller);
        var draftId = await seller.CreateSellerDraftAsync(Tehran);
        var (publishedStatus, _, published) = await seller.SendAsync(
            HttpMethod.Post,
            $"{SellerDrafts}/{draftId}/publication",
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status200OK, publishedStatus);
        var snapshotId = published.GetProperty("selectedSnapshotId").GetGuid();

        var sellerRow = await seller.WithContextAsync(context => context.SelectedCapturedRevisions
            .AsNoTracking()
            .SingleAsync(row => row.OwnerSellerPartyId == okSeller && row.BindingServiceKey == "post"));
        Assert.Equal(ShippingDraftOwnerKind.Seller, sellerRow.OwnerKind);
        Assert.Equal(okSeller, sellerRow.OwnerSellerPartyId);

        // The store owner can never see the seller's draft/selection: another owner is a separate scope.
        using var storeView = Harness.Create("pub-seller-store-b", edition: ToobaEdition.SingleStore);
        var (storeAbsent, _, _) = await storeView.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}/publication");
        Assert.Equal(StatusCodes.Status404NotFound, storeAbsent);
    }

    [Fact]
    public async Task Real_publication_captures_the_exact_revision_and_a_repeat_is_an_honest_no_op()
    {
        using var harness = Harness.Create("pub-ok-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var before = await harness.ReadDraftStampAsync(draftId);
        var path = $"{AdminDrafts}/{draftId}/publication";

        var (status, _, body) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");

        Assert.Equal(StatusCodes.Status200OK, status);
        var snapshotId = body.GetProperty("selectedSnapshotId").GetGuid();
        Assert.Equal((int)ShippingAuthoringPublicationState.Selected, body.GetProperty("state").GetInt32());
        Assert.Equal(1, body.GetProperty("selectionVersion").GetInt64());
        Assert.Equal(draftId, body.GetProperty("selectedDraftId").GetGuid());
        Assert.Equal(1, body.GetProperty("selectedSourceRevision").GetInt64());
        Assert.Equal(1, body.GetProperty("selectedFormatVersion").GetInt32());
        Assert.True(body.GetProperty("selectedMatchesCurrentDraftRevision").GetBoolean());

        // Publication never mutates the draft: revision and UpdatedAt are byte-identical.
        var after = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(before, after);
        Assert.Equal(1, await harness.CountSnapshotsAsync(draftId));

        // GET exposes exactly the same provenance and version.
        var (readStatus, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, readStatus);
        Assert.Equal(snapshotId, read.GetProperty("selectedSnapshotId").GetGuid());
        Assert.Equal(1, read.GetProperty("selectionVersion").GetInt64());
        Assert.Equal(1, read.GetProperty("selectedSourceRevision").GetInt64());

        // Same revision + same current pointer version: honest no-op, version and snapshot unchanged.
        var (noopStatus, _, noop) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":1}");
        Assert.Equal(StatusCodes.Status200OK, noopStatus);
        Assert.Equal(1, noop.GetProperty("selectionVersion").GetInt64());
        Assert.Equal(snapshotId, noop.GetProperty("selectedSnapshotId").GetGuid());
        Assert.Equal(1, await harness.CountSnapshotsAsync(draftId));

        // A stale pointer token is a 409 conflict and the stored pointer is untouched.
        var (staleStatus, _, stale) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status409Conflict, staleStatus);
        Assert.Contains(ShippingErrorCodes.SelectedCaptureConflict, stale.GetRawText(), StringComparison.Ordinal);
        var (stillVersion, stillSnapshot) = await harness.ReadPointerAsync(draftId);
        Assert.Equal(1, stillVersion);
        Assert.Equal(snapshotId, stillSnapshot);
    }

    [Fact]
    public async Task A_later_draft_edit_keeps_the_old_selection_and_the_next_publish_selects_the_new_capture()
    {
        using var harness = Harness.Create("pub-next-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/publication";

        var (_, _, first) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        var firstSnapshot = first.GetProperty("selectedSnapshotId").GetGuid();

        // An ORDINARY draft edit must never publish implicitly.
        var (editStatus, _, _) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}",
            $"{{\"expectedRevision\":1,\"regionGroups\":[{{\"groupId\":null,\"areas\":["
            + $"{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{Isfahan}\"}}]}}]}}");
        Assert.Equal(StatusCodes.Status200OK, editStatus);

        var (readStatus, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, readStatus);
        Assert.Equal(2, read.GetProperty("currentDraftRevision").GetInt64());
        Assert.Equal(firstSnapshot, read.GetProperty("selectedSnapshotId").GetGuid());
        Assert.Equal(1, read.GetProperty("selectedSourceRevision").GetInt64());
        Assert.False(read.GetProperty("selectedMatchesCurrentDraftRevision").GetBoolean());

        // A stale SAVED draft revision is rejected before any capture.
        var (staleDraftStatus, _, staleDraft) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":1}");
        Assert.Equal(StatusCodes.Status409Conflict, staleDraftStatus);
        Assert.Contains(ShippingErrorCodes.AuthoringDraftRevisionConflict, staleDraft.GetRawText(), StringComparison.Ordinal);
        Assert.Equal(1, await harness.CountSnapshotsAsync(draftId));

        // The explicit next publication selects the NEW capture at the expected pointer version.
        var (nextStatus, _, next) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":2,\"expectedSelectionVersion\":1}");
        Assert.Equal(StatusCodes.Status200OK, nextStatus);
        Assert.NotEqual(firstSnapshot, next.GetProperty("selectedSnapshotId").GetGuid());
        Assert.Equal(2, next.GetProperty("selectedSourceRevision").GetInt64());
        Assert.Equal(2, next.GetProperty("selectionVersion").GetInt64());
        Assert.True(next.GetProperty("selectedMatchesCurrentDraftRevision").GetBoolean());
    }

    [Fact]
    public async Task Withdraw_increments_the_token_and_a_stale_token_can_never_resurrect_a_selection()
    {
        using var harness = Harness.Create("pub-withdraw-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/publication";

        await harness.SendAsync(HttpMethod.Post, path, "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");

        var (clearStatus, _, cleared) = await harness.SendAsync(
            HttpMethod.Post,
            $"{path}/withdraw",
            "{\"expectedSelectionVersion\":1}");
        Assert.Equal(StatusCodes.Status200OK, clearStatus);
        Assert.Equal((int)ShippingAuthoringPublicationState.Cleared, cleared.GetProperty("state").GetInt32());
        Assert.Equal(2, cleared.GetProperty("selectionVersion").GetInt64());
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("selectedSnapshotId").ValueKind);

        // Clearing again with the CURRENT version is a no-op that keeps row and version.
        var (noopStatus, _, noop) = await harness.SendAsync(
            HttpMethod.Post,
            $"{path}/withdraw",
            "{\"expectedSelectionVersion\":2}");
        Assert.Equal(StatusCodes.Status200OK, noopStatus);
        Assert.Equal(2, noop.GetProperty("selectionVersion").GetInt64());
        Assert.Equal((int)ShippingAuthoringPublicationState.Cleared, noop.GetProperty("state").GetInt32());

        // A stale token cannot resurrect the previous selection.
        var (staleStatus, _, stale) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status409Conflict, staleStatus);
        Assert.Contains(ShippingErrorCodes.SelectedCaptureConflict, stale.GetRawText(), StringComparison.Ordinal);

        var (version, snapshot) = await harness.ReadPointerAsync(draftId);
        Assert.Equal(2, version);
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task An_unknown_or_foreign_draft_is_a_safe_not_found_with_no_pointer_or_capture()
    {
        using var harness = Harness.Create("pub-foreign-a");
        var foreignDraftId = await harness.CreateDraftAsync(Tehran, tenantId: $"foreign-{harness.TenantId}");

        // The same harness reads as the DEFAULT tenant, so the draft belongs to another owner.
        var (getStatus, _, _) = await harness.SendAsync(
            HttpMethod.Get,
            $"{AdminDrafts}/{foreignDraftId}/publication");
        Assert.Equal(StatusCodes.Status404NotFound, getStatus);

        var (postStatus, _, post) = await harness.SendAsync(
            HttpMethod.Post,
            $"{AdminDrafts}/{foreignDraftId}/publication",
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status404NotFound, postStatus);
        Assert.Contains(ShippingErrorCodes.AuthoringDraftNotFound, post.GetRawText(), StringComparison.Ordinal);

        var (withdrawStatus, _, _) = await harness.SendAsync(
            HttpMethod.Post,
            $"{AdminDrafts}/{foreignDraftId}/publication/withdraw",
            "{\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status404NotFound, withdrawStatus);

        // An unknown id is the same safe answer, with zero rows created anywhere.
        var unknown = Guid.Parse("9f9f9f9f-1111-2222-3333-444444444444");
        var (unknownStatus, _, _) = await harness.SendAsync(
            HttpMethod.Post,
            $"{AdminDrafts}/{unknown}/publication",
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        Assert.Equal(StatusCodes.Status404NotFound, unknownStatus);
        Assert.Equal(0, await harness.CountSnapshotsAsync(foreignDraftId));
        Assert.Equal(0, await harness.CountPointersAsync(foreignDraftId));
    }

    [Fact]
    public async Task A_pointer_failure_after_a_real_capture_reports_a_refusal_and_keeps_the_old_selection()
    {
        using var harness = Harness.Create("pub-seam-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/publication";

        var (_, _, first) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":1,\"expectedSelectionVersion\":0}");
        var firstSnapshot = first.GetProperty("selectedSnapshotId").GetGuid();

        // A real ordinary edit advances the draft so the next publication must capture a NEW revision.
        await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}",
            $"{{\"expectedRevision\":1,\"regionGroups\":[{{\"groupId\":null,\"areas\":["
            + $"{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{Isfahan}\"}}]}}]}}");

        // NARROW SCRIPTED SEAM (explicitly labelled, not a real race): ONLY SelectAsync refuses once; the
        // real capture underneath still succeeds, so a staging capture MAY remain while the pointer keeps
        // its old selected value. No combined atomicity is claimed anywhere.
        harness.ArmPointerFailure();

        var (failedStatus, _, failed) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":2,\"expectedSelectionVersion\":1}");
        Assert.Equal(StatusCodes.Status409Conflict, failedStatus);
        Assert.Contains(ShippingErrorCodes.SelectedCaptureConflict, failed.GetRawText(), StringComparison.Ordinal);

        var (version, snapshot) = await harness.ReadPointerAsync(draftId);
        Assert.Equal(1, version);
        Assert.Equal(firstSnapshot, snapshot);

        // The staging capture of revision 2 exists (explicitly asserted: TWO captures, ONE selection).
        Assert.Equal(2, await harness.CountSnapshotsAsync(draftId));

        // After the seam is disarmed the same publication succeeds without any automatic retry.
        var (retryStatus, _, retry) = await harness.SendAsync(
            HttpMethod.Post,
            path,
            "{\"expectedRevision\":2,\"expectedSelectionVersion\":1}");
        Assert.Equal(StatusCodes.Status200OK, retryStatus);
        Assert.Equal(2, retry.GetProperty("selectionVersion").GetInt64());
        Assert.Equal(2, retry.GetProperty("selectedSourceRevision").GetInt64());
        Assert.Equal(2, await harness.CountSnapshotsAsync(draftId));
    }

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;
        private readonly Counter _counter;
        private readonly ScriptedPointerDirectory _pointer;

        private Harness(
            WebApplication app,
            IServiceScope scope,
            Counter counter,
            ScriptedPointerDirectory pointer,
            string tenantId)
        {
            _app = app;
            _scope = scope;
            _counter = counter;
            _pointer = pointer;
            TenantId = tenantId;
        }

        /// <summary>
        /// The owner tenant of this fact. Every fact gets its OWN store tenant, so the single
        /// pointer-per-owner binding of TB-219 can never collide across facts on the shared disposable
        /// database.
        /// </summary>
        public string TenantId { get; }

        public string AcceptLanguage { get; set; } = "fa";

        public TenantContext? DefaultTenant { get; set; }

        public int SentCount => _counter.Sent;

        public static Harness Create(
            string database,
            ToobaEdition edition = ToobaEdition.SingleStore,
            IReadOnlyList<string>? adminAllowed = null,
            IReadOnlyList<PlatformPermissionGrant>? sellerGrants = null,
            bool marketplace = false,
            string? tenantId = null,
            Guid? sellerPartyId = null)
        {
            // Every harness gets its OWN owner identity, so the single draft/pointer per owner+binding of
            // TB-214/TB-219 can never collide with a fact from this or a previous run on the disposable DB.
            var ownedTenant = tenantId ?? $"{database}-{Guid.NewGuid():N}"[..24];
            var ownedSeller = sellerPartyId ?? Tehran;
            EnsureSchemaAsync().GetAwaiter().GetResult();

            var counter = new Counter();
            var tenant = new StubCurrentTenant(marketplace ? null : Tenant(ownedTenant));
            var seller = new RecordingSellerAccess(ownedSeller);
            var commerce = new FakeContextSource(edition, "USD");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SentCountingBehavior<,>));
            builder.Services.AddToobaCqrsFoundation(typeof(CreateShippingAuthoringDraftCommand).Assembly);
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();

            var target = ShippingCaptureRelationalTestTarget.FromEnvironment();
            var options = new DbContextOptionsBuilder<ShippingDbContext>();
            ToobaNpgsql.ConfigureModuleContext(
                options,
                target.ConnectionString,
                ShippingDbContext.Schema,
                typeof(ShippingDbContext));
            builder.Services.AddScoped(_ => new ShippingDbContext(options.Options));

            builder.Services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
            builder.Services.AddScoped<ShippingSelectedCapturedRevisionDirectory>();
            builder.Services.AddScoped<IShippingSelectedCapturedRevisionDirectory>(
                services => services.GetRequiredService<ScriptedPointerDirectory>());
            builder.Services.AddScoped<IShippingSelectedCapturedRevisionLookup>(
                services => services.GetRequiredService<ScriptedPointerDirectory>());
            builder.Services.AddScoped<ScriptedPointerDirectory>();
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
            builder.Services.AddSingleton(tenant);
            builder.Services.AddSingleton<IShippingMethodOptionValidator>(new FakeMethodValidator());
            builder.Services.AddSingleton<IShippingAuthoringDestinationResolver>(new FakeResolver());
            builder.Services.AddSingleton<IShippingAuthoringContextSource>(commerce);
            builder.Services.AddSingleton(counter);

            var app = builder.Build();
            app.MapShippingEndpoints();
            var scope = app.Services.CreateScope();
            var harness = new Harness(
                app,
                scope,
                counter,
                scope.ServiceProvider.GetRequiredService<ScriptedPointerDirectory>(),
                ownedTenant)
            {
                DefaultTenant = marketplace ? null : Tenant(ownedTenant),
            };
            seller.OnCall = () => { };
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

        public void ArmPointerFailure() => _pointer.FailSelect = true;

        public async Task<Guid> CreateDraftAsync(Guid area, string? tenantId = null)
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, AdminDrafts, CreateBody(area), tenantId ?? TenantId);
            Assert.True(status == StatusCodes.Status201Created, body.GetRawText());
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<Guid> CreateSellerDraftAsync(Guid area)
        {
            // A Marketplace seller draft is created with NO store tenant: that is the trusted scope.
            var (status, _, body) = await SendAsync(HttpMethod.Post, SellerDrafts, CreateBody(area));
            Assert.True(status == StatusCodes.Status201Created, body.GetRawText());
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<(long Revision, DateTimeOffset UpdatedAt)> ReadDraftStampAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
                return (draft.Revision, draft.UpdatedAt);
            });

        public async Task<int> CountSnapshotsAsync(Guid draftId)
            => await WithContextAsync(context => context.AuthoringRevisionSnapshots
                .AsNoTracking()
                .CountAsync(row => row.DraftId == draftId));

        public async Task<int> CountPointersAsync(Guid draftId)
            => await WithContextAsync(context => context.SelectedCapturedRevisions
                .AsNoTracking()
                .CountAsync(row => row.SelectedSnapshotId != null
                    && context.AuthoringRevisionSnapshots.Any(snapshot =>
                        snapshot.SnapshotId == row.SelectedSnapshotId && snapshot.DraftId == draftId)));

        public async Task<(long Version, Guid? SnapshotId)> ReadPointerAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                _ = draftId;
                var row = await context.SelectedCapturedRevisions
                    .AsNoTracking()
                    .SingleAsync(item => item.OwnerTenantId == TenantId
                        && item.OwnerKind == ShippingDraftOwnerKind.Store
                        && item.BindingServiceKey == "post");
                return (row.SelectionVersion, row.SelectedSnapshotId);
            });

        public async Task WithContextAsync(Func<ShippingDbContext, Task> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<T> WithContextAsync<T>(Func<ShippingDbContext, Task<T>> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<(int Status, string? Location, JsonElement Body)> SendAsync(
            HttpMethod method,
            string path,
            string? json = null,
            string? tenantId = null)
        {
            // The owner scope is resolved from the trusted per-request tenant: a caller can only switch it
            // in this fixture to model a DIFFERENT store, never through the HTTP payload.
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

        private static string CreateBody(Guid area)
            => "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":["
               + $"{{\"groupId\":null,\"areas\":[{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{area}\"}}]}}]}}";

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

        private static async Task EnsureSchemaAsync()
        {
            await MigrationGate.WaitAsync();
            try
            {
                if (_schemaReady)
                {
                    return;
                }

                var target = ShippingCaptureRelationalTestTarget.FromEnvironment();
                var options = new DbContextOptionsBuilder<ShippingDbContext>();
                ToobaNpgsql.ConfigureModuleContext(
                    options,
                    target.ConnectionString,
                    ShippingDbContext.Schema,
                    typeof(ShippingDbContext));
                await using var context = new ShippingDbContext(options.Options);

                // TCP (not a socket) plus the exact identity this task owns: never the shared database.
                var facts = await context.Database
                    .SqlQueryRaw<string>(
                        "SELECT (current_database() || '|' || current_user || '|' || coalesce(host(inet_server_addr()), 'socket')) AS \"Value\"")
                    .SingleAsync();
                var parts = facts.Split('|');
                Assert.Equal(ShippingCaptureRelationalTestTarget.ExpectedDatabase, parts[0]);
                Assert.Equal(ShippingCaptureRelationalTestTarget.ExpectedUser, parts[1]);
                Assert.NotEqual("socket", parts[2]);

                await context.Database.MigrateAsync();
                _schemaReady = true;
            }
            finally
            {
                MigrationGate.Release();
            }
        }
    }

    /// <summary>
    /// NARROW SCRIPTED SEAM used by ONE fact only: the real TB-219 adapter underneath is unchanged and both
    /// ports delegate to it; only <see cref="SelectAsync"/> can be made to refuse once, to model a lost
    /// pointer CAS AFTER a real, successful capture. This is explicitly NOT a real concurrency race.
    /// </summary>
    private sealed class ScriptedPointerDirectory(ShippingSelectedCapturedRevisionDirectory inner)
        : IShippingSelectedCapturedRevisionDirectory, IShippingSelectedCapturedRevisionLookup
    {
        public bool FailSelect { get; set; }

        public Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
            ShippingSelectedCapturedRevisionRequest request,
            CancellationToken cancellationToken) =>
            inner.ReadAsync(request, cancellationToken);

        public Task<Result<ShippingSelectedCaptureSelection>> SelectAsync(
            ShippingAuthoringOwnerScope owner,
            string serviceCode,
            string? optionCode,
            Guid draftId,
            long sourceRevision,
            long expectedSelectionVersion,
            CancellationToken cancellationToken)
        {
            if (FailSelect)
            {
                FailSelect = false;
                return Task.FromResult(Result.Failure<ShippingSelectedCaptureSelection>(
                    new SemanticError(ShippingErrorCodes.SelectedCaptureConflict)));
            }

            return inner.SelectAsync(
                owner,
                serviceCode,
                optionCode,
                draftId,
                sourceRevision,
                expectedSelectionVersion,
                cancellationToken);
        }

        public Task<Result<ShippingSelectedCaptureSelection>> ClearAsync(
            ShippingAuthoringOwnerScope owner,
            string serviceCode,
            string? optionCode,
            long expectedSelectionVersion,
            CancellationToken cancellationToken) =>
            inner.ClearAsync(owner, serviceCode, optionCode, expectedSelectionVersion, cancellationToken);
    }

    private sealed class Counter
    {
        public int Sent { get; set; }
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
            if (IsPublication(request))
            {
                counter.Sent++;
            }

            return await next();
        }
    }

    private static bool IsPublication(object request) =>
        request is GetShippingAuthoringPublicationQuery
            or PublishShippingAuthoringDraftCommand
            or WithdrawShippingAuthoringPublicationCommand;

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

    private sealed class RecordingSellerAccess(Guid sellerPartyId) : ISellerPanelAccess
    {
        public Action? OnCall { get; set; }

        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            OnCall?.Invoke();
            return Task.FromResult((Iran, sellerPartyId));
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

    private sealed class FakeContextSource(ToobaEdition edition, string? currency) : IShippingAuthoringContextSource
    {
        public Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ShippingAuthoringContextSourceSnapshot(
                edition,
                currency,
                [new ShippingAuthoringLanguageSource(PersianLanguage, "fa", "fa-IR", "fa", true, true, "rtl", "فارسی", "فارسی", 0)]));
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
