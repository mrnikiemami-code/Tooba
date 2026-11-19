using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Policies;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Story.Infrastructure.Directories;

/// <summary>Admin-for-shop, version-fenced lifecycle, occurrence replay and seller stop/reopen.</summary>
public sealed partial class StoryDirectory
{
    /// <summary>Creates a shop-owned draft for an authorized admin actor.</summary>
    public async Task<AdminStorySnapshot> AdminCreateForShopAsync(
        Guid tenantId,
        Guid adminActorUserId,
        Guid sellerPartyId,
        CreateStoryCommand command,
        CancellationToken cancellationToken)
    {
        await EnsureShopEligibleAsync(sellerPartyId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(command.OccurrenceKey))
        {
            var replay = await TryReplayAsync(
                tenantId,
                sellerPartyId,
                "admin_create_for_shop",
                command.OccurrenceKey!,
                HashIntent(command),
                cancellationToken);
            if (replay is not null)
                return replay;
        }

        var now = DateTimeOffset.UtcNow;
        var order = command.DisplayOrder ?? await NextDisplayOrderAsync(tenantId, cancellationToken);
        var story = StoryEntity.CreateShopOwnedDraft(
            tenantId,
            sellerPartyId,
            adminActorUserId,
            command.Title,
            order,
            now,
            command.Locale,
            command.Market,
            command.CoverMediaAssetId,
            command.CoverMediaUrl,
            command.CtaType,
            command.CtaTarget);
        _db.Stories.Add(story);
        await RecordOccurrenceAsync(
            tenantId,
            sellerPartyId,
            "admin_create_for_shop",
            command.OccurrenceKey,
            HashIntent(command),
            story.StoryId,
            adminActorUserId,
            now,
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <summary>Approves the reviewed version and publishes immediately.</summary>
    public async Task<AdminStorySnapshot> AdminApproveAndPublishAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        story.Approve(adminActorUserId, DateTimeOffset.UtcNow);
        story.Activate(DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <summary>Approves the reviewed version and applies the schedule in one owning transition.</summary>
    public async Task<AdminStorySnapshot> AdminApproveAndScheduleAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        int? expectedVersion,
        SetStoryScheduleCommand schedule,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        story.Approve(adminActorUserId, DateTimeOffset.UtcNow);
        story.SetSchedule(schedule.StartAt, schedule.EndAt, DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <summary>Seller returns an approved story to draft and stops publication.</summary>
    public async Task<AdminStorySnapshot> SellerReturnToDraftAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var story = await RequireSellerStoryAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        story.ReturnToDraftForEdit(DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <summary>Seller stops its own live/scheduled publication.</summary>
    public async Task<AdminStorySnapshot> SellerStopAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var story = await RequireSellerStoryAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        story.StopOwnPublication(DateTimeOffset.UtcNow);
        await SaveStoryAsync(story, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <summary>Attaches shop display names from Party Contracts.</summary>
    public async Task<AdminStorySnapshot> EnrichAsync(
        AdminStorySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot.SellerPartyId is not Guid shop || _partySellers is null)
            return snapshot;

        var rows = await _partySellers.GetStatusProjectionsAsync([shop], cancellationToken);
        var name = rows.FirstOrDefault(row => row.PartyId == shop)?.DisplayName;
        return snapshot with { ShopDisplayName = name };
    }

    private async Task EnsureShopEligibleAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        if (sellerPartyId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.ShopInvalid));
        if (_partySellers is null)
            throw new SemanticException(new SemanticError(StoryErrorCodes.ShopAuthorityUnavailable));

        var rows = await _partySellers.GetStatusProjectionsAsync([sellerPartyId], cancellationToken);
        var row = rows.FirstOrDefault(item => item.PartyId == sellerPartyId);
        if (row is null || !string.Equals(row.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new SemanticException(new SemanticError(StoryErrorCodes.ShopInvalid));
    }

    private async Task<AdminStorySnapshot?> TryReplayAsync(
        Guid tenantId,
        Guid? sellerPartyId,
        string actionCode,
        string occurrenceKey,
        string intentHash,
        CancellationToken cancellationToken)
    {
        var key = occurrenceKey.Trim();
        var existing = await _db.CommandOccurrences.AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.OccurrenceKey == key && row.TenantId == tenantId,
                cancellationToken);
        if (existing is null)
            return null;
        if (!string.Equals(existing.ActionCode, actionCode, StringComparison.Ordinal)
            || !string.Equals(existing.IntentHash, intentHash, StringComparison.Ordinal)
            || existing.SellerPartyId != sellerPartyId)
        {
            throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceConflict));
        }

        var story = await LoadStoryAsync(tenantId, existing.StoryId, track: false, cancellationToken)
            ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    private async Task RecordOccurrenceAsync(
        Guid tenantId,
        Guid? sellerPartyId,
        string actionCode,
        string? occurrenceKey,
        string intentHash,
        Guid storyId,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(occurrenceKey))
            return;

        _db.CommandOccurrences.Add(new StoryCommandOccurrence
        {
            OccurrenceKey = occurrenceKey.Trim(),
            TenantId = tenantId,
            SellerPartyId = sellerPartyId,
            ActionCode = actionCode,
            IntentHash = intentHash,
            StoryId = storyId,
            ActorUserId = actorUserId,
            CreatedAt = now,
        });
        await Task.CompletedTask;
    }

    private static string HashIntent(CreateStoryCommand command)
    {
        // Length-prefixed fields keep values that contain '|' unambiguous.
        static string F(string? value)
        {
            var text = value ?? string.Empty;
            return text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + text;
        }

        var payload = string.Join('\u001f',
            F(command.Title),
            F(command.Locale),
            F(command.Market),
            F(command.CoverMediaAssetId?.ToString("D")),
            F(command.CoverMediaUrl),
            F(command.DisplayOrder?.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            F(command.CtaType),
            F(command.CtaTarget),
            F(command.SellerPartyId?.ToString("D")));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
