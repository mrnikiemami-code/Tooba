using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>چرخه رزرو در schema order؛ ردیف Inventory را زنده نمی‌کند.</summary>
public sealed class ReservationCycleDirectory : IReservationCycleDirectory
{
    private readonly OrderDbContext _db;

    /// <summary>دایرکتوری را به OrderDbContext وصل می‌کند.</summary>
    public ReservationCycleDirectory(OrderDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public ReservationCycle PrepareStart(
        Guid checkoutId,
        ReservationCycleReason reason,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        ReservationCyclePolicySnapshot policy,
        IReadOnlyList<Guid> reservationIds,
        string? actor,
        string? correlationId,
        Guid? paymentAttemptId)
    {
        var existing = _db.ReservationCycles.Local
            .FirstOrDefault(x => x.CheckoutId == checkoutId && x.Status == ReservationCycleStatus.Active);
        existing ??= _db.ReservationCycles
            .FirstOrDefault(x => x.CheckoutId == checkoutId && x.Status == ReservationCycleStatus.Active);
        if (existing is not null)
        {
            return existing;
        }

        var next = NextNumber(checkoutId);
        var cycle = ReservationCycle.Start(
            checkoutId,
            next,
            reason,
            startedAt,
            expiresAt,
            reason is ReservationCycleReason.RetryAfterExpiry or ReservationCycleReason.LatePaymentRecovery
                ? policy.RetryHoldMinutes
                : policy.InitialHoldMinutes,
            policy.MaxCycles,
            policy.Source,
            reservationIds,
            actor,
            correlationId ?? $"cycle:{checkoutId:N}:{next}",
            paymentAttemptId);
        _db.ReservationCycles.Add(cycle);
        _db.ReservationCycleEvents.Add(ReservationCycleEvent.Record(
            checkoutId,
            cycle.CycleId,
            reason == ReservationCycleReason.RetryAfterExpiry
                ? ReservationCycleEventKind.StartedAfterRetry
                : ReservationCycleEventKind.Started,
            startedAt,
            $"cycle={next};reason={reason}"));
        return cycle;
    }

    /// <inheritdoc />
    public async Task<ReservationCycleSnapshot> StartAsync(
        Guid checkoutId,
        ReservationCycleReason reason,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        ReservationCyclePolicySnapshot policy,
        IReadOnlyList<Guid> reservationIds,
        string? actor,
        string? correlationId,
        Guid? paymentAttemptId,
        CancellationToken cancellationToken)
    {
        var cycle = PrepareStart(
            checkoutId,
            reason,
            startedAt,
            expiresAt,
            policy,
            reservationIds,
            actor,
            correlationId,
            paymentAttemptId);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var winner = await _db.ReservationCycles
                .AsNoTracking()
                .Where(x => x.CheckoutId == checkoutId && x.Status == ReservationCycleStatus.Active)
                .OrderByDescending(x => x.CycleNumber)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("چرخه رزرو تکراری ذخیره شد ولی خوانده نشد.");
            return ToSnapshot(winner);
        }

        return ToSnapshot(cycle);
    }

    /// <inheritdoc />
    public async Task CorrelatePaymentAttemptAsync(
        Guid checkoutId,
        Guid? paymentAttemptId,
        CancellationToken cancellationToken)
    {
        var cycle = await LoadActiveAsync(checkoutId, cancellationToken);
        if (cycle is null)
        {
            return;
        }

        var expires = cycle.ExpiresAt;
        cycle.CorrelatePaymentAttempt(paymentAttemptId);
        if (cycle.ExpiresAt != expires)
        {
            throw new InvalidOperationException("تلاش پرداخت نباید مهلت چرخه را تغییر دهد.");
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task TransitionManualReviewAsync(
        Guid checkoutId,
        DateTimeOffset reviewExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cycle = await LoadActiveAsync(checkoutId, cancellationToken);
        if (cycle is null)
        {
            return;
        }

        cycle.TransitionManualReview(reviewExpiresAt, now);
        _db.ReservationCycleEvents.Add(ReservationCycleEvent.Record(
            checkoutId,
            cycle.CycleId,
            ReservationCycleEventKind.ManualReviewTransitioned,
            now,
            $"expires={reviewExpiresAt:O}"));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CloseExpiredDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = await _db.ReservationCycles
            .Where(x => x.Status == ReservationCycleStatus.Active && x.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
        foreach (var cycle in due)
        {
            cycle.Close(ReservationCycleStatus.Expired, now);
            _db.ReservationCycleEvents.Add(ReservationCycleEvent.Record(
                cycle.CheckoutId,
                cycle.CycleId,
                ReservationCycleEventKind.Expired,
                now,
                $"cycle={cycle.CycleNumber}"));
        }

        if (due.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }

    /// <inheritdoc />
    public async Task CloseActiveAsync(
        Guid checkoutId,
        ReservationCycleStatus status,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken)
    {
        var cycle = await LoadActiveAsync(checkoutId, cancellationToken);
        if (cycle is null)
        {
            return;
        }

        cycle.Close(status, endedAt);
        var kind = status switch
        {
            ReservationCycleStatus.ReleasedByCancel => ReservationCycleEventKind.ReleasedByCancel,
            ReservationCycleStatus.ReleasedByPolicy => ReservationCycleEventKind.ReleasedByPolicy,
            ReservationCycleStatus.CommittedPaid => ReservationCycleEventKind.CommittedPaid,
            ReservationCycleStatus.Expired => ReservationCycleEventKind.Expired,
            _ => throw new InvalidOperationException("وضعیت پایانی چرخه نامعتبر است."),
        };
        _db.ReservationCycleEvents.Add(ReservationCycleEvent.Record(
            checkoutId,
            cycle.CycleId,
            kind,
            endedAt,
            $"cycle={cycle.CycleNumber}"));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordReacquireRequestedAsync(
        Guid checkoutId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        _db.ReservationCycleEvents.Add(ReservationCycleEvent.Record(
            checkoutId,
            null,
            ReservationCycleEventKind.ReacquireRequested,
            now,
            string.Empty));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordReacquireFailedAsync(
        Guid checkoutId,
        DateTimeOffset now,
        string detail,
        CancellationToken cancellationToken)
    {
        _db.ReservationCycleEvents.Add(ReservationCycleEvent.Record(
            checkoutId,
            null,
            ReservationCycleEventKind.ReacquireFailed,
            now,
            detail));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordRetryLimitReachedAsync(
        Guid checkoutId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        _db.ReservationCycleEvents.Add(ReservationCycleEvent.Record(
            checkoutId,
            null,
            ReservationCycleEventKind.RetryLimitReached,
            now,
            ReservationCycleErrors.RetryLimitReached));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReservationCycleSnapshot?> GetActiveAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var cycle = await _db.ReservationCycles.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId && x.Status == ReservationCycleStatus.Active)
            .OrderByDescending(x => x.CycleNumber)
            .FirstOrDefaultAsync(cancellationToken);
        return cycle is null ? null : ToSnapshot(cycle);
    }

    /// <inheritdoc />
    public Task<int> CountCreatedAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        _db.ReservationCycles.AsNoTracking().CountAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    /// <inheritdoc />
    public async Task<ReservationCycleProjection> GetProjectionAsync(
        Guid checkoutId,
        DateTimeOffset serverNow,
        string? supplyStatus,
        CancellationToken cancellationToken)
    {
        var map = await GetProjectionsAsync(
            [checkoutId],
            serverNow,
            supplyStatus is null ? null : new Dictionary<Guid, string?> { [checkoutId] = supplyStatus },
            cancellationToken);
        return map[checkoutId];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ReservationCycleProjection>> GetProjectionsAsync(
        IReadOnlyList<Guid> checkoutIds,
        DateTimeOffset serverNow,
        IReadOnlyDictionary<Guid, string?>? supplyByCheckout,
        CancellationToken cancellationToken)
    {
        var ids = checkoutIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, ReservationCycleProjection>();
        }

        var history = await _db.ReservationCycles.AsNoTracking()
            .Where(x => ids.Contains(x.CheckoutId))
            .OrderBy(x => x.CheckoutId)
            .ThenBy(x => x.CycleNumber)
            .ToListAsync(cancellationToken);
        var grouped = history.GroupBy(x => x.CheckoutId).ToDictionary(g => g.Key, g => g.ToList());
        var result = new Dictionary<Guid, ReservationCycleProjection>(ids.Length);
        foreach (var id in ids)
        {
            grouped.TryGetValue(id, out var rows);
            result[id] = ProjectRows(id, rows ?? [], serverNow, supplyByCheckout?.GetValueOrDefault(id));
        }

        return result;
    }

    private static ReservationCycleProjection ProjectRows(
        Guid checkoutId,
        IReadOnlyList<ReservationCycle> history,
        DateTimeOffset serverNow,
        string? supplyStatus)
    {
        var current = history.LastOrDefault(x => x.Status == ReservationCycleStatus.Active)
            ?? history.LastOrDefault();
        var created = history.Count;
        var max = current?.EffectiveMaxCycles
            ?? history.LastOrDefault()?.EffectiveMaxCycles
            ?? 3;
        var remaining = Math.Max(0, max - created);
        var seconds = 0;
        if (current is { Status: ReservationCycleStatus.Active })
        {
            seconds = (int)Math.Max(0, Math.Floor((current.ExpiresAt - serverNow).TotalSeconds));
        }

        return new ReservationCycleProjection(
            checkoutId,
            current?.CycleNumber,
            current?.Status,
            current?.StartedAt,
            current?.ExpiresAt,
            serverNow,
            seconds,
            created,
            max,
            remaining,
            history.Select(ToSnapshot).ToArray(),
            supplyStatus);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReservationCycleEventSnapshot>> ListEventsAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ReservationCycleEvents.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.EventId)
            .ToListAsync(cancellationToken);
        return rows.Select(x => new ReservationCycleEventSnapshot(
            x.EventId,
            x.CheckoutId,
            x.CycleId,
            x.Kind,
            x.OccurredAt,
            x.Detail)).ToArray();
    }

    private int NextNumber(Guid checkoutId)
    {
        var localMax = _db.ReservationCycles.Local
            .Where(x => x.CheckoutId == checkoutId)
            .Select(x => x.CycleNumber)
            .DefaultIfEmpty(0)
            .Max();
        var storedMax = _db.ReservationCycles
            .Where(x => x.CheckoutId == checkoutId)
            .Select(x => (int?)x.CycleNumber)
            .Max() ?? 0;
        return Math.Max(localMax, storedMax) + 1;
    }

    private async Task<ReservationCycle?> LoadActiveAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        await _db.ReservationCycles
            .Where(x => x.CheckoutId == checkoutId && x.Status == ReservationCycleStatus.Active)
            .OrderByDescending(x => x.CycleNumber)
            .FirstOrDefaultAsync(cancellationToken);

    private static ReservationCycleSnapshot ToSnapshot(ReservationCycle cycle) =>
        new(
            cycle.CycleId,
            cycle.CheckoutId,
            cycle.CycleNumber,
            cycle.Reason,
            cycle.Status,
            cycle.StartedAt,
            cycle.ExpiresAt,
            cycle.EndedAt,
            cycle.EffectiveHoldMinutes,
            cycle.EffectiveMaxCycles,
            cycle.PolicySource,
            cycle.ParseReservationIds(),
            cycle.PaymentAttemptId,
            cycle.CorrelationId);
}
