using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Tooba.BuildingBlocks;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Aggregates;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;
using StoryItem = Tooba.Story.Domain.Aggregates.StoryItem;

namespace Tooba.Story.Infrastructure.Directories;

public sealed partial class StoryDirectory
{
    private static string HashCanonical(params string?[] fields)
    {
        static string F(string? value)
        {
            var text = value ?? string.Empty;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
        }

        var payload = string.Join('\u001f', fields.Select(F));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string HashLifecycleIntent(Guid storyId, int? expectedVersion, string? actionToken) =>
        HashCanonical(
            storyId.ToString("D"),
            expectedVersion?.ToString(CultureInfo.InvariantCulture),
            actionToken);

    private static string HashUpdateStoryIntent(Guid storyId, UpdateStoryCommand command) =>
        HashCanonical(
            storyId.ToString("D"),
            command.ExpectedVersion?.ToString(CultureInfo.InvariantCulture),
            command.Title,
            command.Locale,
            command.Market,
            command.CoverMediaAssetId?.ToString("D"),
            command.CoverMediaUrl,
            command.CtaType,
            command.CtaTarget);

    private static string HashUpdateItemIntent(Guid storyId, Guid itemId, UpdateStoryItemCommand command) =>
        HashCanonical(
            storyId.ToString("D"),
            itemId.ToString("D"),
            command.ExpectedVersion?.ToString(CultureInfo.InvariantCulture),
            command.MediaType,
            command.MediaAssetId?.ToString("D"),
            command.MediaUrl,
            command.Caption,
            command.DurationMs?.ToString(CultureInfo.InvariantCulture),
            command.CtaType,
            command.CtaTarget);

    private static string HashScheduleIntent(Guid storyId, SetStoryScheduleCommand command) =>
        HashCanonical(
            storyId.ToString("D"),
            command.ExpectedVersion?.ToString(CultureInfo.InvariantCulture),
            command.StartAt?.ToString("O", CultureInfo.InvariantCulture),
            command.EndAt?.ToString("O", CultureInfo.InvariantCulture));

    private static string HashRemoveItemIntent(Guid storyId, Guid itemId, int? expectedVersion) =>
        HashCanonical(
            storyId.ToString("D"),
            expectedVersion?.ToString(CultureInfo.InvariantCulture),
            "remove_item",
            itemId.ToString("D"));

    private static string HashReorderItemsIntent(Guid storyId, int? expectedVersion, string orderedIds) =>
        HashCanonical(
            storyId.ToString("D"),
            expectedVersion?.ToString(CultureInfo.InvariantCulture),
            "reorder_items",
            orderedIds);

    private static string HashItemIntent(Guid storyId, AddStoryItemCommand command) =>
        HashCanonical(
            storyId.ToString("D"),
            command.MediaType,
            command.MediaAssetId?.ToString("D"),
            command.MediaUrl,
            command.Caption,
            command.DurationMs?.ToString(CultureInfo.InvariantCulture),
            command.CtaType,
            command.CtaTarget,
            command.DisplayOrder?.ToString(CultureInfo.InvariantCulture),
            command.ExpectedVersion?.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// ذخیرهٔ story و آیتم‌ها و occurrence با داوری برخورد کلید؛ در صورت برخورد PK فقط برنده خوانده می‌شود
    /// و در همزمانی نسخه، VersionStale بدون occurrence اضافی اعلام می‌شود.
    /// </summary>
    private async Task<StoryEntity> SaveStoryAsync(
        StoryEntity story,
        string occurrenceKey,
        string intentHash,
        CancellationToken cancellationToken)
    {
        _db.Stories.Update(story);
        var existingItems = await _db.StoryItems
            .Where(item => item.StoryId == story.StoryId)
            .ToListAsync(cancellationToken);
        var currentIds = story.Items.Select(item => item.StoryItemId).ToHashSet();
        foreach (var removed in existingItems.Where(item => !currentIds.Contains(item.StoryItemId)))
            _db.StoryItems.Remove(removed);
        foreach (var item in story.Items)
        {
            if (existingItems.Any(existing => existing.StoryItemId == item.StoryItemId))
                _db.StoryItems.Update(item);
            else
                _db.StoryItems.Add(item);
        }

        var staged = RequireStagedOccurrence(story.TenantId, occurrenceKey);
        var winnerStoryId = await SaveChangesWithOccurrenceArbitrationAsync(
            story.TenantId,
            occurrenceKey,
            intentHash,
            staged.ActionCode,
            staged.SellerPartyId,
            expectedStoryId: story.StoryId,
            ownedStoryIds: [story.StoryId],
            cancellationToken);
        if (winnerStoryId is null)
            return story;
        return await LoadStoryAsync(story.TenantId, winnerStoryId.Value, track: false, cancellationToken)
            ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
    }

    private StoryCommandOccurrence RequireStagedOccurrence(Guid tenantId, string occurrenceKey)
    {
        var key = occurrenceKey.Trim();
        var staged = _db.ChangeTracker.Entries<StoryCommandOccurrence>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(row =>
                row.TenantId == tenantId
                && string.Equals(row.OccurrenceKey, key, StringComparison.Ordinal));
        return staged
            ?? throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceRequired));
    }

    /// <returns>شناسهٔ story برنده در fold؛ null اگر همین attempt ذخیره شده باشد.</returns>
    private async Task<Guid?> SaveChangesWithOccurrenceArbitrationAsync(
        Guid tenantId,
        string occurrenceKey,
        string intentHash,
        string actionCode,
        Guid? sellerPartyId,
        Guid? expectedStoryId,
        IReadOnlyCollection<Guid> ownedStoryIds,
        CancellationToken cancellationToken)
    {
        // Capture exact command-owned identities BEFORE SaveChanges mutates tracker state.
        var owned = ownedStoryIds is null || ownedStoryIds.Count == 0
            ? Array.Empty<Guid>()
            : ownedStoryIds.Distinct().ToArray();
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            DetachAttemptOwned(occurrenceKey, tenantId, owned);
            throw new SemanticException(new SemanticError(StoryErrorCodes.VersionStale));
        }
        catch (DbUpdateException ex) when (IsCommandOccurrenceUniqueViolation(ex))
        {
            DetachAttemptOwned(occurrenceKey, tenantId, owned);
            var winner = await _db.CommandOccurrences.AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.OccurrenceKey == occurrenceKey && row.TenantId == tenantId,
                    cancellationToken);
            if (winner is null)
                throw;
            // Same full identity as TryReplayMappedAsync — hash alone must never bind a foreign shop/action/target.
            if (!AnswersOccurrenceIdentity(winner, actionCode, intentHash, sellerPartyId, expectedStoryId))
                throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceConflict));
            return winner.StoryId;
        }
    }

    private static bool AnswersOccurrenceIdentity(
        StoryCommandOccurrence existing,
        string actionCode,
        string intentHash,
        Guid? sellerPartyId,
        Guid? expectedStoryId) =>
        string.Equals(existing.ActionCode, actionCode, StringComparison.Ordinal)
        && string.Equals(existing.IntentHash, intentHash, StringComparison.Ordinal)
        && existing.SellerPartyId == sellerPartyId
        && (expectedStoryId is not Guid requiredStory || existing.StoryId == requiredStory);

    /// <summary>
    /// فقط occurrence و story/آیتم‌های همین command (ownedStoryIds صریح) را detach می‌کند.
    /// Guid.Empty/فهرست خالی هرگز به معنی «همهٔ Modifiedها» نیست؛ ChangeTracker.Clear ممنوع.
    /// </summary>
    private void DetachAttemptOwned(string occurrenceKey, Guid tenantId, IReadOnlyCollection<Guid> ownedStoryIds)
    {
        var owned = ownedStoryIds as HashSet<Guid> ?? ownedStoryIds.ToHashSet();
        foreach (var entry in _db.ChangeTracker.Entries<StoryCommandOccurrence>().ToList())
        {
            if (entry.State == EntityState.Detached)
                continue;
            if (!string.Equals(entry.Entity.OccurrenceKey, occurrenceKey, StringComparison.Ordinal)
                || entry.Entity.TenantId != tenantId)
                continue;
            entry.State = EntityState.Detached;
        }

        foreach (var entry in _db.ChangeTracker.Entries<StoryEntity>().ToList())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;
            if (!owned.Contains(entry.Entity.StoryId))
                continue;
            entry.State = EntityState.Detached;
        }

        foreach (var entry in _db.ChangeTracker.Entries<StoryItem>().ToList())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;
            if (!owned.Contains(entry.Entity.StoryId))
                continue;
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsCommandOccurrenceUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgres
        && string.Equals(postgres.SchemaName, "story", StringComparison.Ordinal)
        && string.Equals(postgres.TableName, "command_occurrences", StringComparison.Ordinal)
        && string.Equals(postgres.ConstraintName, "pk_command_occurrences", StringComparison.Ordinal);
}
