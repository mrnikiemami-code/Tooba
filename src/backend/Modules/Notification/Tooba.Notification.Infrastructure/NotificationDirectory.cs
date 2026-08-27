using Microsoft.EntityFrameworkCore;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;
using Tooba.Notification.Infrastructure.Persistence;

namespace Tooba.Notification.Infrastructure;

/// <summary>
/// پیاده‌سازی دایرکتوری اعلان با idempotency روی SourceEventId.
/// </summary>
public sealed class NotificationDirectory : INotificationDirectory
{
    private readonly NotificationDbContext _db;
    private readonly NotificationInstrumentation _telemetry;

    /// <summary>دایرکتوری را به schema notification وصل می‌کند.</summary>
    public NotificationDirectory(NotificationDbContext db, NotificationInstrumentation telemetry)
    {
        _db = db;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<UserNotification?> CreateIfAbsentAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sourceEventId = command.SourceEventId.Trim();
        var existing = await _db.Notifications.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.RecipientKind == command.RecipientKind
                    && x.RecipientPartyId == command.RecipientPartyId
                    && x.SourceEventId == sourceEventId,
                cancellationToken);
        if (existing is not null)
        {
            _telemetry.RecordDuplicateSuppressed(command.SourceType);
            return null;
        }

        var target = NotificationTargetRoutes.RequireAllowed(command.TargetRoute);
        var entity = UserNotification.Create(
            command.RecipientKind,
            command.RecipientPartyId,
            command.RecipientActorUserId,
            command.Type,
            NotificationCopy.ToPayloadJson(command.Payload),
            target,
            sourceEventId,
            command.SourceType,
            DateTimeOffset.UtcNow);

        _db.Notifications.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            _telemetry.RecordCreated(command.SourceType, command.RecipientKind.ToString());
            return entity;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var raced = await _db.Notifications.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.RecipientKind == command.RecipientKind
                        && x.RecipientPartyId == command.RecipientPartyId
                        && x.SourceEventId == sourceEventId,
                    cancellationToken);
            if (raced is null)
            {
                throw;
            }

            _telemetry.RecordDuplicateSuppressed(command.SourceType);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<NotificationListPage> ListAsync(
        NotificationRecipientQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var take = Math.Clamp(query.Take <= 0 ? 20 : query.Take, 1, 100);
        var skip = Math.Max(0, query.Skip);
        var baseQuery = RecipientFilter(
                _db.Notifications.AsNoTracking(),
                query.RecipientKind,
                query.RecipientPartyId,
                query.RecipientActorUserId)
            .Where(x => !x.IsDeleted);
        var total = await baseQuery.LongCountAsync(cancellationToken);
        var unread = await baseQuery.LongCountAsync(x => !x.IsRead, cancellationToken);
        var rows = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.NotificationId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        var items = rows.Select(x =>
        {
            var (title, body) = NotificationCopy.Resolve(x.Type, x.PayloadJson, query.Locale);
            return new NotificationListItemDto(
                x.NotificationId,
                x.Type,
                NotificationCopy.CategoryOf(x.Type),
                title,
                body,
                x.PayloadJson,
                x.TargetRoute,
                x.IsRead,
                x.ReadAt,
                x.CreatedAt,
                x.SourceType);
        }).ToList();
        return new NotificationListPage(items, skip, take, total, unread);
    }

    /// <inheritdoc />
    public Task<long> UnreadCountAsync(
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken) =>
        RecipientFilter(_db.Notifications.AsNoTracking(), recipientKind, recipientPartyId, recipientActorUserId)
            .Where(x => !x.IsDeleted && !x.IsRead)
            .LongCountAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> MarkReadAsync(
        Guid notificationId,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await RecipientFilter(_db.Notifications, recipientKind, recipientPartyId, recipientActorUserId)
            .SingleOrDefaultAsync(x => x.NotificationId == notificationId && !x.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (!entity.MarkRead(DateTimeOffset.UtcNow))
        {
            return true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordReadTransition();
        return true;
    }

    /// <inheritdoc />
    public async Task<int> MarkAllReadAsync(
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken)
    {
        var unread = await RecipientFilter(_db.Notifications, recipientKind, recipientPartyId, recipientActorUserId)
            .Where(x => !x.IsDeleted && !x.IsRead)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var changed = 0;
        foreach (var row in unread)
        {
            if (row.MarkRead(now))
            {
                changed++;
            }
        }

        if (changed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            for (var i = 0; i < changed; i++)
            {
                _telemetry.RecordReadTransition();
            }
        }

        return changed;
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(
        Guid notificationId,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await RecipientFilter(_db.Notifications, recipientKind, recipientPartyId, recipientActorUserId)
            .SingleOrDefaultAsync(x => x.NotificationId == notificationId && !x.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (!entity.SoftDelete(DateTimeOffset.UtcNow))
        {
            return true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IQueryable<UserNotification> RecipientFilter(
        IQueryable<UserNotification> source,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId)
    {
        if (recipientKind == NotificationRecipientKind.Customer)
        {
            var actor = recipientActorUserId ?? recipientPartyId;
            return source.Where(x =>
                x.RecipientKind == NotificationRecipientKind.Customer
                && x.RecipientActorUserId == actor);
        }

        return source.Where(x =>
            x.RecipientKind == NotificationRecipientKind.Seller
            && x.RecipientPartyId == recipientPartyId);
    }
}
