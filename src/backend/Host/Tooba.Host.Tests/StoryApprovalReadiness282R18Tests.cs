using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Rules;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;
using StoryItemEntity = Tooba.Story.Domain.Aggregates.StoryItem;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-282-R18 — owner approval path on the REAL owning directory method (controlled InMemory harness, no
/// external database and zero committed production rows): lawful submitted content approves, legacy URL-only
/// content is not offered «approve» and the owning command refuses it with the concrete content-not-ready code.
/// </summary>
public sealed class StoryApprovalReadiness282R18Tests
{
    private static readonly Guid Tenant = StoryTenantIds.StoreAlpha;

    [Fact]
    public async Task Lawful_media_submitted_story_offers_approve_and_real_command_approves()
    {
        var (db, directory, shop, media, _) = CreateDirectory();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();

        var created = await directory.SellerCreateDraftAsync(
            Tenant, shop, seller,
            new CreateStoryCommand("استوری معتبر", "fa", null, null, null, null, "none", null,
                OccurrenceKey: "r18-lawful-create"),
            CancellationToken.None);
        var added = await directory.SellerAddItemAsync(
            Tenant, shop, created.StoryId,
            new AddStoryItemCommand("image", media, null, null, 5000, "none", null, null,
                "r18-lawful-item", created.VersionToken),
            CancellationToken.None);
        var submitted = await directory.SellerSubmitAsync(
            Tenant, shop, created.StoryId, seller, CancellationToken.None,
            added.VersionToken, "r18-lawful-submit");

        Assert.Equal(StoryReviewStatus.Submitted, submitted.ReviewStatus);
        Assert.True(submitted.ContentReady);

        var adminView = await directory.AdminGetAsync(Tenant, created.StoryId, CancellationToken.None);
        Assert.NotNull(adminView);
        Assert.Contains("approve", adminView!.ActionCodes!);
        Assert.Contains("reject", adminView.ActionCodes!);

        var reviewedVersion = adminView.VersionToken;
        var approved = await directory.AdminApproveAsync(
            Tenant, created.StoryId, admin, reviewedVersion, CancellationToken.None, "r18-lawful-approve");

        Assert.Equal(StoryReviewStatus.Approved, approved.ReviewStatus);
        Assert.Equal(reviewedVersion + 1, approved.VersionToken);
        Assert.Equal(admin, approved.ReviewedByActorUserId);
        Assert.DoesNotContain("approve", approved.ActionCodes!);
        Assert.DoesNotContain("reject", approved.ActionCodes!);

        var occurrence = await db.CommandOccurrences.SingleAsync(o => o.OccurrenceKey == "r18-lawful-approve");
        Assert.Equal("admin_approve", occurrence.ActionCode);
        Assert.Equal(created.StoryId, occurrence.StoryId);
        Assert.Equal(shop, occurrence.SellerPartyId);
    }

    [Fact]
    public async Task Legacy_url_only_submitted_story_is_not_offered_and_real_approve_refuses_content_not_ready()
    {
        var (db, directory, shop, media, _) = CreateDirectory();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();

        var created = await directory.SellerCreateDraftAsync(
            Tenant, shop, seller,
            new CreateStoryCommand("میراث بدون دارایی", "fa", null, null, null, null, "none", null,
                OccurrenceKey: "r18-legacy-create"),
            CancellationToken.None);
        var added = await directory.SellerAddItemAsync(
            Tenant, shop, created.StoryId,
            new AddStoryItemCommand("image", media, null, null, 5000, "none", null, null,
                "r18-legacy-item", created.VersionToken),
            CancellationToken.None);
        var submitted = await directory.SellerSubmitAsync(
            Tenant, shop, created.StoryId, seller, CancellationToken.None,
            added.VersionToken, "r18-legacy-submit");

        // Simulated legacy stored row (the shape reported by the owner): item keeps only a raw URL and the
        // owning media asset reference is gone. Current commands cannot produce this shape, so the fixture
        // is written directly into the controlled store instead of faking an approval response.
        var itemRow = await db.StoryItems.SingleAsync(item => item.StoryId == created.StoryId);
        SetPrivate(itemRow, nameof(StoryItemEntity.MediaAssetId), null);
        SetPrivate(itemRow, nameof(StoryItemEntity.MediaUrl), "/images/stories/2.jpg");
        await db.SaveChangesAsync();

        var snapshot = await directory.AdminGetAsync(Tenant, created.StoryId, CancellationToken.None);
        Assert.NotNull(snapshot);
        Assert.Equal(StoryReviewStatus.Submitted, snapshot!.ReviewStatus);
        Assert.False(snapshot.ContentReady);
        Assert.DoesNotContain("approve", snapshot.ActionCodes!);
        Assert.DoesNotContain("approve_and_publish", snapshot.ActionCodes!);
        Assert.DoesNotContain("approve_and_schedule", snapshot.ActionCodes!);
        // Rejection has no readiness gate and must stay available for exactly this case.
        Assert.Contains("reject", snapshot.ActionCodes!);

        var versionBefore = snapshot.VersionToken;
        var approve = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminApproveAsync(
                Tenant, created.StoryId, admin, versionBefore, CancellationToken.None, "r18-legacy-approve"));
        Assert.Equal(StoryErrorCodes.ContentNotReady, approve.Error.Code);

        var publish = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminApproveAndPublishAsync(
                Tenant, created.StoryId, admin, versionBefore, CancellationToken.None, "r18-legacy-publish"));
        Assert.Equal(StoryErrorCodes.ContentNotReady, publish.Error.Code);

        Assert.Equal(0, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey.StartsWith("r18-legacy-approve")));
        Assert.Equal(0, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey.StartsWith("r18-legacy-publish")));

        var after = await directory.AdminGetAsync(Tenant, created.StoryId, CancellationToken.None);
        Assert.Equal(StoryReviewStatus.Submitted, after!.ReviewStatus);
        Assert.Equal(versionBefore, after.VersionToken);

        // Lawful repair path: reject (no readiness gate) → seller replaces the slide with an owned asset →
        // resubmit → the very same owning approve command succeeds. No data is silently rewritten.
        var rejected = await directory.AdminRejectAsync(
            Tenant, created.StoryId, admin, "اسلاید رسانهٔ معتبر ندارد", after.VersionToken,
            CancellationToken.None, "r18-legacy-reject");
        Assert.Equal(StoryReviewStatus.Rejected, rejected.ReviewStatus);

        var repaired = await directory.SellerUpdateItemAsync(
            Tenant, shop, created.StoryId, itemRow.StoryItemId,
            new UpdateStoryItemCommand("image", media, null, null, 5000, "none", null,
                rejected.VersionToken, "r18-legacy-repair"),
            CancellationToken.None);
        Assert.True(repaired.ContentReady);

        var resubmitted = await directory.SellerSubmitAsync(
            Tenant, shop, created.StoryId, seller, CancellationToken.None,
            repaired.VersionToken, "r18-legacy-resubmit");
        var resubmittedAdminView = await directory.AdminGetAsync(Tenant, created.StoryId, CancellationToken.None);
        Assert.Contains("approve", resubmittedAdminView!.ActionCodes!);
        var approvedAfterRepair = await directory.AdminApproveAsync(
            Tenant, created.StoryId, admin, resubmittedAdminView.VersionToken, CancellationToken.None,
            "r18-legacy-approve-after-repair");
        Assert.Equal(StoryReviewStatus.Approved, approvedAfterRepair.ReviewStatus);
        Assert.Equal(resubmitted.VersionToken, approvedAfterRepair.VersionToken - 1);
    }

    [Fact]
    public async Task Seller_and_admin_can_edit_legacy_caption_cta_without_changing_legacy_media()
    {
        var (db, directory, shop, media, _) = CreateDirectory();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();

        var created = await directory.SellerCreateDraftAsync(
            Tenant, shop, seller,
            new CreateStoryCommand("ویرایش اسلاید قدیمی", "fa", null, null, null, null, "none", null,
                OccurrenceKey: "cta-legacy-create"),
            CancellationToken.None);
        var added = await directory.SellerAddItemAsync(
            Tenant, shop, created.StoryId,
            new AddStoryItemCommand("image", media, null, null, 5000, "none", null, null,
                "cta-legacy-item", created.VersionToken),
            CancellationToken.None);

        var itemRow = await db.StoryItems.SingleAsync(item => item.StoryId == created.StoryId);
        SetPrivate(itemRow, nameof(StoryItemEntity.MediaAssetId), null);
        SetPrivate(itemRow, nameof(StoryItemEntity.MediaUrl), "/images/stories/2.jpg");
        await db.SaveChangesAsync();

        var beforeSeller = await directory.SellerGetAsync(Tenant, shop, created.StoryId, CancellationToken.None);
        Assert.NotNull(beforeSeller);
        var sellerUpdated = await directory.SellerUpdateItemAsync(
            Tenant, shop, created.StoryId, itemRow.StoryItemId,
            new UpdateStoryItemCommand(
                "image", null, "/images/stories/2.jpg", "زیرنویس تازه", 5000,
                "external", "https://example.com/story",
                beforeSeller!.VersionToken, "cta-legacy-seller-edit"),
            CancellationToken.None);

        var sellerItem = Assert.Single(sellerUpdated.Items);
        Assert.Null(sellerItem.MediaAssetId);
        Assert.Equal("/images/stories/2.jpg", sellerItem.MediaUrl);
        Assert.Equal("زیرنویس تازه", sellerItem.Caption);
        Assert.Equal("external", sellerItem.CtaType);
        Assert.Equal("https://example.com/story", sellerItem.CtaTarget);

        var beforeAdmin = await directory.AdminGetAsync(Tenant, created.StoryId, CancellationToken.None);
        var adminUpdated = await directory.AdminUpdateItemAsync(
            Tenant, created.StoryId, itemRow.StoryItemId,
            new UpdateStoryItemCommand(
                "image", null, "/images/stories/2.jpg", "زیرنویس ادمین", 5000,
                "internal", "/products",
                beforeAdmin!.VersionToken, "cta-legacy-admin-edit"),
            CancellationToken.None);
        var adminItem = Assert.Single(adminUpdated.Items);
        Assert.Equal("/images/stories/2.jpg", adminItem.MediaUrl);
        Assert.Equal("زیرنویس ادمین", adminItem.Caption);
        Assert.Equal("internal", adminItem.CtaType);
        Assert.Equal("/products", adminItem.CtaTarget);

        var beforeBad = await directory.SellerGetAsync(Tenant, shop, created.StoryId, CancellationToken.None);
        var changedUrl = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerUpdateItemAsync(
                Tenant, shop, created.StoryId, itemRow.StoryItemId,
                new UpdateStoryItemCommand(
                    "image", null, "/images/stories/3.jpg", "نباید بنشیند", 5000,
                    "none", null,
                    beforeBad!.VersionToken, "cta-legacy-bad-url"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.MediaForbidden, changedUrl.Error.Code);
        var afterBad = await directory.SellerGetAsync(Tenant, shop, created.StoryId, CancellationToken.None);
        Assert.Equal(beforeBad.VersionToken, afterBad!.VersionToken);
        _ = admin;
    }

    [Fact]
    public async Task Every_supported_cta_type_validates_and_resolves_a_public_href()
    {
        var (db, directory, shop, media, product) = CreateDirectory();
        _ = shop;
        _ = media;

        var created = await directory.AdminCreateAsync(
            Tenant,
            new CreateStoryCommand("CTA matrix", "fa", null, null, null, null, "none", null,
                OccurrenceKey: "cta-matrix-create"),
            CancellationToken.None);
        var version = created.VersionToken;
        var cases = new (string Type, string? Target)[]
        {
            ("none", null),
            ("external", "https://example.com/story"),
            ("internal", "/products"),
            ("category", "/offers"),
            ("article", "/blog/story"),
            ("product", product.ToString("D")),
        };
        var index = 0;
        foreach (var cta in cases)
        {
            var next = await directory.AdminAddItemAsync(
                Tenant, created.StoryId,
                new AddStoryItemCommand(
                    "image", null, "/images/stories/matrix.jpg", null, 5000,
                    cta.Type, cta.Target, index, $"cta-matrix-item-{cta.Type}", version),
                CancellationToken.None);
            version = next.VersionToken;
            index += 1;
        }

        var active = await directory.AdminSetStatusAsync(
            Tenant, created.StoryId, StoryStatus.Active, version, CancellationToken.None, "cta-matrix-activate");
        Assert.Equal(StoryStatus.Active, active.Status);
        Assert.Contains("publish_now", active.ActionCodes!);
        Assert.Contains("schedule", active.ActionCodes!);

        var cards = await directory.GetPublicStoriesAsync(
            Tenant, "fa", null, DateTimeOffset.UtcNow, CancellationToken.None);
        var card = Assert.Single(cards, row => row.StoryId == created.StoryId);
        Assert.Equal("https://example.com/story", card.Items.Single(row => row.CtaType == "external").CtaHref);
        Assert.Equal("/products", card.Items.Single(row => row.CtaType == "internal").CtaHref);
        Assert.Equal("/offers", card.Items.Single(row => row.CtaType == "category").CtaHref);
        Assert.Equal("/blog/story", card.Items.Single(row => row.CtaType == "article").CtaHref);
        Assert.Equal($"/fa/products/{product:D}", card.Items.Single(row => row.CtaType == "product").CtaHref);
        Assert.Null(card.Items.Single(row => row.CtaType == "none").CtaHref);

        Assert.Equal(("external", "https://example.com"), StoryRules.ValidateCta("external", "https://example.com"));
        Assert.Equal(("internal", "/products"), StoryRules.ValidateCta("internal", "/products"));
        Assert.Equal(("category", "/offers"), StoryRules.ValidateCta("category", "/offers"));
        Assert.Equal(("article", "/blog/story"), StoryRules.ValidateCta("article", "/blog/story"));
        Assert.Throws<SemanticException>(() => StoryRules.ValidateCta("external", "ftp://example.com"));
        Assert.Throws<SemanticException>(() => StoryRules.ValidateCta("external", "/relative"));
        Assert.Throws<SemanticException>(() => StoryRules.ValidateCta("internal", "https://example.com"));
        Assert.Throws<SemanticException>(() => StoryRules.ValidateCta("internal", "//evil.example"));
        Assert.Throws<SemanticException>(() => StoryRules.ValidateCta("internal", "bad"));
        Assert.Throws<SemanticException>(() => StoryRules.ValidateCta("category", "bad"));
        Assert.Throws<SemanticException>(() => StoryRules.ValidateCta("article", "//evil.example"));
    }
    private static void SetPrivate(object target, string propertyName, object? value)
    {
        var setter = target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(nonPublic: true)!;
        setter.Invoke(target, [value]);
    }

    private static (StoryDbContext db, StoryDirectory directory, Guid shop, Guid media, Guid product) CreateDirectory()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r18-approval-" + Guid.NewGuid().ToString("N"))
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
