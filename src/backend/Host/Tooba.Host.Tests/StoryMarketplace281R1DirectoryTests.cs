using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-281-R1 focused Directory proofs (fresh materialization + role actions).</summary>
public sealed class StoryMarketplace281R1DirectoryTests
{
    [Fact]
    public async Task Fresh_materialized_active_and_scheduled_stories_are_public_with_items()
    {
        var dbName = "tb281r1-public-" + Guid.NewGuid().ToString("N");
        var shop = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var seller = Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0");
        var admin = Guid.Parse("01a036c2-970e-7000-8eb7-94bf5cc2d8db");
        var mediaId = Guid.Parse("01a0aaaa-0001-7000-8000-000000000001");
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");

        await using (var write = CreateDb(dbName))
        {
            var directory = new StoryDirectory(write, new StubParty(shop), new StubMedia(mediaId));
            var draft = await directory.SellerCreateDraftAsync(
                StoryTenantIds.StoreAlpha, shop, seller,
                new CreateStoryCommand("ready", null, null, null, null, null, null, null),
                CancellationToken.None);
            await directory.SellerAddItemAsync(
                StoryTenantIds.StoreAlpha, shop, draft.StoryId,
                new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, ExpectedVersion: draft.VersionToken),
                CancellationToken.None);
            var afterAdd = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        await directory.SellerSubmitAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None, afterAdd!.VersionToken, "submit-" + draft.StoryId.ToString("N"));
            var submitted = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
            await directory.AdminApproveAndPublishAsync(
                StoryTenantIds.StoreAlpha, draft.StoryId, admin, submitted!.VersionToken, CancellationToken.None);

            var scheduledDraft = await directory.SellerCreateDraftAsync(
                StoryTenantIds.StoreAlpha, shop, seller,
                new CreateStoryCommand("later", null, null, null, null, null, null, null),
                CancellationToken.None);
            await directory.SellerAddItemAsync(
                StoryTenantIds.StoreAlpha, shop, scheduledDraft.StoryId,
                new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, ExpectedVersion: scheduledDraft.VersionToken),
                CancellationToken.None);
            var afterSchedAdd = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, scheduledDraft.StoryId, CancellationToken.None);
            await directory.SellerSubmitAsync(StoryTenantIds.StoreAlpha, shop, scheduledDraft.StoryId, seller, CancellationToken.None, afterSchedAdd!.VersionToken, "submit-" + scheduledDraft.StoryId.ToString("N"));
            var scheduled = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, scheduledDraft.StoryId, CancellationToken.None);
            await directory.AdminApproveAndScheduleAsync(
                StoryTenantIds.StoreAlpha,
                scheduledDraft.StoryId,
                admin,
                scheduled!.VersionToken,
                new SetStoryScheduleCommand(now.AddMinutes(-1), now.AddHours(2)),
                CancellationToken.None);
        }

        await using var read = CreateDb(dbName);
        var reader = new StoryDirectory(read, new StubParty(shop), new StubMedia(mediaId));
        var publicNow = await reader.GetPublicStoriesAsync(StoryTenantIds.StoreAlpha, null, null, now, CancellationToken.None);
        Assert.Equal(2, publicNow.Count);
        Assert.All(publicNow, card => Assert.NotEmpty(card.Items));

        var before = await reader.GetPublicStoriesAsync(
            StoryTenantIds.StoreAlpha, null, null, now.AddMinutes(-30), CancellationToken.None);
        Assert.Single(before);
        Assert.Equal("ready", before[0].Title);

        var afterEnd = await reader.GetPublicStoriesAsync(
            StoryTenantIds.StoreAlpha, null, null, now.AddHours(3), CancellationToken.None);
        Assert.Single(afterEnd);
        Assert.Equal("ready", afterEnd[0].Title);
    }

    [Fact]
    public async Task Seller_actions_exclude_admin_codes_and_include_stop_return()
    {
        await using var db = CreateDb("tb281r1-actions-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(mediaId));
        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("a", null, null, null, null, null, null, null),
            CancellationToken.None);
        await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, ExpectedVersion: draft.VersionToken),
            CancellationToken.None);
        var afterAdd = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        await directory.SellerSubmitAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None, afterAdd!.VersionToken, "submit-" + draft.StoryId.ToString("N"));
        var submitted = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        await directory.AdminApproveAndPublishAsync(
            StoryTenantIds.StoreAlpha, draft.StoryId, admin, submitted!.VersionToken, CancellationToken.None);
        var sellerView = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        Assert.NotNull(sellerView);
        Assert.Contains("stop", sellerView!.ActionCodes!);
        Assert.Contains("return_to_draft", sellerView.ActionCodes!);
        Assert.DoesNotContain("approve", sellerView.ActionCodes!);
        Assert.DoesNotContain("approve_and_publish", sellerView.ActionCodes!);

        var adminView = await directory.AdminGetAsync(StoryTenantIds.StoreAlpha, draft.StoryId, CancellationToken.None);
        Assert.Contains("stop", adminView!.ActionCodes!);
        Assert.DoesNotContain("return_to_draft", adminView.ActionCodes!);
    }

    [Fact]
    public async Task Missing_media_port_refuses_seller_item_bind()
    {
        await using var db = CreateDb("tb281r1-media-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), media: null);
        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("a", null, null, null, null, null, null, null),
            CancellationToken.None);
        var ex = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerAddItemAsync(
                StoryTenantIds.StoreAlpha, shop, draft.StoryId,
                new AddStoryItemCommand("image", Guid.NewGuid(), null, null, null, null, null, null, ExpectedVersion: draft.VersionToken),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.MediaUnavailable, ex.Error.Code);
    }

    private static StoryDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new StoryDbContext(options);
    }

    private sealed class StubParty(Guid eligible) : IPartyAdminSellerReadGateway
    {
        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<PartyStatusProjection> rows = partyIds
                .Where(id => id == eligible)
                .Select(id => new PartyStatusProjection(id, "Shop " + id.ToString("N")[..8], "Active"))
                .ToList();
            return Task.FromResult(rows);
        }
    }

    private sealed class StubMedia(Guid readyId) : IStoryMediaAssetPort
    {
        public Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
            Guid mediaAssetId, Guid tenantId, Guid? sellerPartyId, bool allowUnscopedAdminAssets,
            CancellationToken cancellationToken)
        {
            if (mediaAssetId != readyId)
                throw new ContractOperationException("media.owner.forbidden");
            return Task.FromResult(new StoryMediaAssetAuthorization(
                mediaAssetId, "image/jpeg", $"/v1/storefront/media/{mediaAssetId:D}", false, sellerPartyId));
        }

        public Task<StoryMediaAssetAuthorization> UploadForSellerAsync(
            Stream stream, string fileName, string contentType, Guid tenantId, Guid sellerPartyId,
            Guid actorUserId, string? occurrenceKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
            Guid tenantId, Guid sellerPartyId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoryMediaAssetAuthorization>>([]);
    }
}
