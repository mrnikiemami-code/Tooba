using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Application.Rendering;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Ports;
using Tooba.Notification.Contracts.Routes;
using Tooba.Notification.Domain.Aggregates;
using Tooba.Notification.Infrastructure.Observability;
using Tooba.Notification.Infrastructure.Persistence;
using DomainKind = Tooba.Notification.Domain.ValueObjects.NotificationRecipientKind;

namespace Tooba.Notification.Infrastructure.Directories;

/// <summary>
/// پیاده‌سازی دایرکتوری اعلان با idempotency روی SourceEventId.
/// </summary>
public sealed class NotificationDirectory : INotificationDirectory, INotificationCreationPort
{
    private readonly NotificationDbContext _db;
    private readonly NotificationInstrumentation _telemetry;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>دایرکتوری را به schema notification وصل می‌کند.</summary>
    public NotificationDirectory(
        NotificationDbContext db,
        NotificationInstrumentation telemetry,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        _telemetry = telemetry;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    async Task<bool> INotificationCreationPort.CreateIfAbsentAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var created = await CreateIfAbsentAsync(command, cancellationToken);
        return created is not null;
    }

    /// <inheritdoc />
    public async Task<UserNotification?> CreateIfAbsentAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sourceEventId = command.SourceEventId.Trim();
        var domainKind = NotificationRecipientKindMapping.ToDomain(command.RecipientKind);
        var existing = await _db.Notifications.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.RecipientKind == domainKind
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
            _ids.NewId(),
            domainKind,
            command.RecipientPartyId,
            command.RecipientActorUserId,
            command.Type,
            NotificationCopy.ToPayloadJson(command.Payload),
            target,
            sourceEventId,
            command.SourceType,
            _clock.UtcNow);

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
                    x => x.RecipientKind == domainKind
                        && x.RecipientPartyId == command.RecipientPartyId
                        && x.SourceEventId == sourceEventId,
                    cancellationToken);
            if (raced is null)
                throw;

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
            return false;

        if (!entity.MarkRead(_clock.UtcNow))
            return true;

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
        var now = _clock.UtcNow;
        var changed = 0;
        foreach (var row in unread)
        {
            if (row.MarkRead(now))
                changed++;
        }

        if (changed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            for (var i = 0; i < changed; i++)
                _telemetry.RecordReadTransition();
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
        var entity = await _db.Notifications
            .SingleOrDefaultAsync(x => x.NotificationId == notificationId, cancellationToken);
        if (entity is null)
            return false;

        var domainKind = NotificationRecipientKindMapping.ToDomain(recipientKind);
        if (domainKind == DomainKind.Customer)
        {
            var actor = recipientActorUserId ?? recipientPartyId;
            if (entity.RecipientKind != DomainKind.Customer || entity.RecipientActorUserId != actor)
                return false;
        }
        else if (entity.RecipientKind != DomainKind.Seller || entity.RecipientPartyId != recipientPartyId)
        {
            return false;
        }

        if (entity.IsDeleted)
            return true;

        if (!entity.SoftDelete(_clock.UtcNow))
            return true;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IQueryable<UserNotification> RecipientFilter(
        IQueryable<UserNotification> source,
        NotificationRecipientKind recipientKind,
        Guid recipientPartyId,
        Guid? recipientActorUserId)
    {
        var domainKind = NotificationRecipientKindMapping.ToDomain(recipientKind);
        if (domainKind == DomainKind.Customer)
        {
            var actor = recipientActorUserId ?? recipientPartyId;
            return source.Where(x =>
                x.RecipientKind == DomainKind.Customer
                && x.RecipientActorUserId == actor);
        }

        return source.Where(x =>
            x.RecipientKind == DomainKind.Seller
            && x.RecipientPartyId == recipientPartyId);
    }
}
