using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Grid;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-282-R19 — owned workflow repairs on the REAL owning directory and grid policy:
/// an approved-but-never-published seller story can lawfully return to an editable draft,
/// and the admin story grid accepts the shop/date filters and updatedAt sort the workspace offers.
/// Controlled InMemory harness; no external database and zero committed production rows.
/// </summary>
public sealed class StoryWorkflow282R19Tests
{
    private static readonly Guid Tenant = StoryTenantIds.StoreAlpha;

    [Fact]
    public async Task Approved_draft_story_offers_and_executes_seller_return_to_draft()
    {
        var (db, directory, shop, media, _) = CreateDirectory();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();

        var created = await directory.SellerCreateDraftAsync(
            Tenant, shop, seller,
            new CreateStoryCommand("استوری تأییدشده بدون انتشار", "fa", null, null, null, null, "none", null,
                OccurrenceKey: "r19-approved-create"),
            CancellationToken.None);
        var added = await directory.SellerAddItemAsync(
            Tenant, shop, created.StoryId,
            new AddStoryItemCommand("image", media, null, null, 5000, "none", null, null,
                "r19-approved-item", created.VersionToken),
            CancellationToken.None);
        await directory.SellerSubmitAsync(
            Tenant, shop, created.StoryId, seller, CancellationToken.None,
            added.VersionToken, "r19-approved-submit");

        var submittedView = await directory.AdminGetAsync(Tenant, created.StoryId, CancellationToken.None);
        Assert.NotNull(submittedView);
        var approved = await directory.AdminApproveAsync(
            Tenant, created.StoryId, admin, submittedView!.VersionToken, CancellationToken.None,
            "r19-approved-approve");

        Assert.Equal(StoryReviewStatus.Approved, approved.ReviewStatus);
        Assert.Equal(StoryStatus.Draft, approved.Status);

        var sellerView = await directory.SellerGetAsync(Tenant, shop, created.StoryId, CancellationToken.None);
        Assert.NotNull(sellerView);
        // The repair action must be offered for exactly the state the domain accepts.
        Assert.Contains("return_to_draft", sellerView!.ActionCodes!);
        Assert.DoesNotContain("approve", sellerView.ActionCodes!);
        Assert.DoesNotContain("publish_now", sellerView.ActionCodes!);

        var revised = await directory.SellerReturnToDraftAsync(
            Tenant, shop, created.StoryId, sellerView.VersionToken, CancellationToken.None,
            "r19-approved-return");
        Assert.Equal(StoryStatus.Draft, revised.Status);
        Assert.Equal(sellerView.VersionToken + 1, revised.VersionToken);

        var occurrence = await db.CommandOccurrences.SingleAsync(o => o.OccurrenceKey == "r19-approved-return");
        Assert.Equal("seller_return_to_draft", occurrence.ActionCode);
        Assert.Equal(created.StoryId, occurrence.StoryId);
        Assert.Equal(shop, occurrence.SellerPartyId);
    }

    [Fact]
    public void Admin_grid_policy_accepts_shop_and_date_filters_and_updated_sort()
    {
        var request = new GridQueryRequest(
            1,
            20,
            null,
            [new GridSortRequest("updatedAt", "desc")],
            [
                new GridFilterRequest("shopDisplayName", "contains", "فروشگاه", null, null),
                new GridFilterRequest("startAt", "on", "2026-11-20", null, null),
                new GridFilterRequest("endAt", "between", "2026-11-01", "2026-11-30", null),
                new GridFilterRequest("updatedAt", "after", "2026-11-18", null, null),
            ]);

        var normalized = StoryAdminGridPolicies.Normalize(request);

        Assert.Equal(4, normalized.Filters.Count);
        Assert.Equal("shopDisplayName", normalized.Filters[0].Field);
        var sort = Assert.Single(normalized.Sort);
        Assert.Equal("updatedAt", sort.Field);
        Assert.Equal("desc", sort.Direction);
        _ = Tenant;
    }

    [Fact]
    public async Task Admin_grid_query_applies_nullable_start_date_filter_and_updated_sort()
    {
        var (db, directory, shop, media, _) = CreateDirectory();
        var seller = Guid.NewGuid();

        async Task<Guid> SeedAsync(string title, string occurrence, DateTimeOffset? startAt)
        {
            var created = await directory.SellerCreateDraftAsync(
                Tenant, shop, seller,
                new CreateStoryCommand(title, "fa", null, null, null, null, "none", null,
                    OccurrenceKey: occurrence, StartAt: startAt),
                CancellationToken.None);
            await directory.SellerAddItemAsync(
                Tenant, shop, created.StoryId,
                new AddStoryItemCommand("image", media, null, null, 5000, "none", null, null,
                    occurrence + "-item", created.VersionToken),
                CancellationToken.None);
            return created.StoryId;
        }

        var withStart = await SeedAsync("با شروع", "r19-grid-start", new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero));
        var withoutStart = await SeedAsync("بدون شروع", "r19-grid-none", null);

        var engine = new AdminStoryGridQueryEngine(db);
        var request = StoryAdminGridPolicies.Normalize(new GridQueryRequest(
            1,
            20,
            null,
            [new GridSortRequest("updatedAt", "desc")],
            [new GridFilterRequest("startAt", "notBlank", null, null, null)]));

        var page = await engine.QueryAsync(Tenant, null, request, CancellationToken.None);

        Assert.Contains(page.Items, row => row.StoryId == withStart);
        Assert.DoesNotContain(page.Items, row => row.StoryId == withoutStart);
    }

    private static (StoryDbContext db, StoryDirectory directory, Guid shop, Guid media, Guid product) CreateDirectory()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r19-workflow-" + Guid.NewGuid().ToString("N"))
            .Options;
        var db = new StoryDbContext(options);
        var shop = Guid.NewGuid();
        var media = Guid.NewGuid();
        var product = Guid.NewGuid();
        var directory = new StoryDirectory(
            db,
            new StubParty(shop),
            new StubMedia(media),
            new StubProducts(shop, product));
        return (db, directory, shop, media, product);
    }

    private sealed class StubParty(params Guid[] eligible) : IPartyAdminSellerReadGateway
    {
        private readonly HashSet<Guid> _ok = eligible.ToHashSet();

        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<PartyStatusProjection> rows = partyIds
                .Where(_ok.Contains)
                .Select(id => new PartyStatusProjection(id, "Shop", "Active"))
                .ToList();
            return Task.FromResult(rows);
        }
    }

    private sealed class StubMedia(params Guid[] ok) : IStoryMediaAssetPort
    {
        private readonly HashSet<Guid> _ok = ok.ToHashSet();

        public Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
            Guid mediaAssetId, Guid tenantId, Guid? sellerPartyId, bool allowUnscopedAdminAssets,
            CancellationToken cancellationToken)
        {
            if (!_ok.Contains(mediaAssetId))
                throw new ContractOperationException("media.owner.forbidden");
            return Task.FromResult(new StoryMediaAssetAuthorization(
                mediaAssetId, "image/jpeg", $"/v1/storefront/media/{mediaAssetId:D}", false, sellerPartyId));
        }

        public Task<StoryMediaAssetAuthorization> UploadForSellerAsync(
            Stream stream, string fileName, string contentType, Guid tenantId, Guid sellerPartyId,
            Guid actorUserId, string? occurrenceKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StoryMediaAssetAuthorization> UploadForPlatformStoryAsync(
            Stream stream, string fileName, string contentType, Guid tenantId, Guid actorUserId,
            string? occurrenceKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
            Guid tenantId, Guid sellerPartyId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoryMediaAssetAuthorization>>([]);
    }

    private sealed class StubProducts(Guid shop, Guid owned) : IStoryProductTargetPort
    {
        public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken)
        {
            if (sellerPartyId != shop || productId != owned)
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<string?> TryResolvePublicProductPathAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(productId == owned ? $"/fa/products/{productId:D}" : null);
    }
}
