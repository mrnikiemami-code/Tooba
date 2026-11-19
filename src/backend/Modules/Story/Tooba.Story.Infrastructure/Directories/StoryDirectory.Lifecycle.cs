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

/// <summary>چرخهٔ عمر ادمین-برای-فروشگاه با حصار نسخه، بازپخش رخداد و توقف/بازگشت فروشنده.</summary>
public sealed partial class StoryDirectory
{
    /// <summary>پیش‌نویس متعلق به فروشگاه را برای بازیگر ادمین مجاز می‌سازد.</summary>
    public async Task<AdminStorySnapshot> AdminCreateForShopAsync(
        Guid tenantId,
        Guid adminActorUserId,
        Guid sellerPartyId,
        CreateStoryCommand command,
        CancellationToken cancellationToken)
    {
        await EnsureShopEligibleAsync(sellerPartyId, cancellationToken);
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var createIntent = HashIntent(command);
        {
            var replay = await TryReplayAsync(
                tenantId,
                sellerPartyId,
                "admin_create_for_shop",
                key,
                createIntent,
                cancellationToken);
            if (replay is not null)
                return replay;
        }

        await EnsureProductCtaAsync(sellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var cover = await BindCoverAsync(
            tenantId, sellerPartyId, command.CoverMediaAssetId, command.CoverMediaUrl,
            allowUnscopedAdminAssets: false, cancellationToken);
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
            cover.AssetId,
            cover.DisplayUrl,
            command.CtaType,
            command.CtaTarget);
        _db.Stories.Add(story);
        await RecordOccurrenceAsync(
            tenantId,
            sellerPartyId,
            "admin_create_for_shop",
            key,
            createIntent,
            story.StoryId,
            adminActorUserId,
            now,
            cancellationToken);
        var foldedShopCreate = await SaveChangesWithOccurrenceArbitrationAsync(
            tenantId, key, createIntent, "admin_create_for_shop", sellerPartyId, expectedStoryId: null, attemptStoryId: story.StoryId, cancellationToken);
        if (foldedShopCreate is Guid foldedShopStoryId)
        {
            story = await LoadStoryAsync(tenantId, foldedShopStoryId, track: false, cancellationToken)
                ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        }
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <summary>نسخهٔ بازبینی‌شده را تأیید و فوراً منتشر می‌کند.</summary>
    public async Task<AdminStorySnapshot> AdminApproveAndPublishAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashLifecycleIntent(storyId, expectedVersion, "approve_and_publish");
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_approve_and_publish", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        await EnsureStoredContentAuthorizedAsync(story, cancellationToken);
        story.Approve(adminActorUserId, DateTimeOffset.UtcNow);
        story.Activate(DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_approve_and_publish", key, intent,
            story.StoryId, adminActorUserId, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <summary>نسخهٔ بازبینی‌شده را تأیید و زمان‌بندی را در یک انتقال مالک اعمال می‌کند.</summary>
    public async Task<AdminStorySnapshot> AdminApproveAndScheduleAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        int? expectedVersion,
        SetStoryScheduleCommand schedule,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey ?? schedule.OccurrenceKey);
        var intent = HashCanonical(
            storyId.ToString("D"),
            expectedVersion?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "approve_and_schedule",
            schedule.StartAt?.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            schedule.EndAt?.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_approve_and_schedule", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        await EnsureStoredContentAuthorizedAsync(story, cancellationToken);
        story.Approve(adminActorUserId, DateTimeOffset.UtcNow);
        story.SetSchedule(schedule.StartAt, schedule.EndAt, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_approve_and_schedule", key, intent,
            story.StoryId, adminActorUserId, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <summary>فروشنده استوری تأییدشده را به پیش‌نویس برمی‌گرداند و انتشار را متوقف می‌کند.</summary>
    public async Task<AdminStorySnapshot> SellerReturnToDraftAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashLifecycleIntent(storyId, expectedVersion, "return_to_draft");
        var replay = await TryReplayLifecycleAsync(
            tenantId, sellerPartyId, "seller_return_to_draft", key, intent, isAdmin: false, cancellationToken);
        if (replay is not null)
            return replay;

        var story = await RequireSellerStoryAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        var now = DateTimeOffset.UtcNow;
        story.ReturnToDraftForEdit(now);
        await RecordOccurrenceAsync(
            tenantId, sellerPartyId, "seller_return_to_draft", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, now, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <summary>فروشنده انتشار زنده/زمان‌بندی‌شدهٔ خود را متوقف می‌کند.</summary>
    public async Task<AdminStorySnapshot> SellerStopAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashLifecycleIntent(storyId, expectedVersion, "stop");
        var replay = await TryReplayLifecycleAsync(
            tenantId, sellerPartyId, "seller_stop", key, intent, isAdmin: false, cancellationToken);
        if (replay is not null)
            return replay;

        var story = await RequireSellerStoryAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        var now = DateTimeOffset.UtcNow;
        story.StopOwnPublication(now);
        await RecordOccurrenceAsync(
            tenantId, sellerPartyId, "seller_stop", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, now, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <summary>نام نمایشی فروشگاه را از Party Contracts می‌چسباند.</summary>
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
        var normalized = RequireOccurrenceKey(occurrenceKey);

        _db.CommandOccurrences.Add(new StoryCommandOccurrence
        {
            OccurrenceKey = normalized,
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
