using Microsoft.EntityFrameworkCore;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>پرس‌وجوی خواندنی پرداخت برای Host بدون نشت PaymentDbContext.</summary>
public sealed class PaymentQueryDirectory : IPaymentQueryDirectory
{
    private readonly PaymentDbContext _db;

    /// <summary>پرس‌وجوی خواندنی را به schema payment وصل می‌کند.</summary>
    public PaymentQueryDirectory(PaymentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentCheckoutRowDto>> GetLatestByCheckoutIdsAsync(
        IReadOnlyCollection<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
        {
            return [];
        }

        var ids = checkoutIds.Distinct().ToArray();
        var paymentRows = await _db.Payments.AsNoTracking()
            .Where(x => ids.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var latest = paymentRows
            .GroupBy(x => x.CheckoutId)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToList();
        if (latest.Count == 0)
        {
            return [];
        }

        var paymentIds = latest.Select(x => x.PaymentId).ToArray();
        var evidence = await _db.Attempts.AsNoTracking()
            .Where(x => paymentIds.Contains(x.PaymentId))
            .Select(x => new { x.PaymentId, x.EvidenceSubmittedAt, x.CreatedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var evidenceByPayment = evidence
            .GroupBy(x => x.PaymentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CreatedAt).First().EvidenceSubmittedAt);

        return latest.Select(row => new PaymentCheckoutRowDto(
            row.PaymentId,
            row.CheckoutId,
            row.Amount,
            row.Currency,
            row.Status.ToString(),
            row.ProviderCode,
            row.CreatedAt,
            row.CompletedAt,
            evidenceByPayment.TryGetValue(row.PaymentId, out var ev) ? ev : null)).ToList();
    }

    /// <inheritdoc />
    public async Task<PaymentAdminGridPageDto> QueryAdminGridAsync(
        PaymentAdminGridQueryDto query,
        CancellationToken cancellationToken)
    {
        IQueryable<CustomerPayment> q = _db.Payments.AsNoTracking();

        if (query.RestrictCheckoutIds is { Count: > 0 } restrict)
        {
            q = q.Where(p => restrict.Contains(p.CheckoutId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(p =>
                p.PaymentId.ToString().ToLower().Contains(term)
                || p.CheckoutId.ToString().ToLower().Contains(term)
                || p.ProviderCode.ToLower().Contains(term));
        }

        foreach (var filter in query.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var asc = query.SortDirection == "asc";
        q = query.SortField switch
        {
            "reference" => asc
                ? q.OrderBy(x => x.CheckoutId).ThenBy(x => x.PaymentId)
                : q.OrderByDescending(x => x.CheckoutId).ThenBy(x => x.PaymentId),
            "amount" => asc
                ? q.OrderBy(x => x.Amount).ThenBy(x => x.PaymentId)
                : q.OrderByDescending(x => x.Amount).ThenBy(x => x.PaymentId),
            "status" => asc
                ? q.OrderBy(x => x.Status).ThenBy(x => x.PaymentId)
                : q.OrderByDescending(x => x.Status).ThenBy(x => x.PaymentId),
            "provider" => asc
                ? q.OrderBy(x => x.ProviderCode).ThenBy(x => x.PaymentId)
                : q.OrderByDescending(x => x.ProviderCode).ThenBy(x => x.PaymentId),
            "completed" => asc
                ? q.OrderBy(x => x.CompletedAt ?? x.CreatedAt).ThenBy(x => x.PaymentId)
                : q.OrderByDescending(x => x.CompletedAt ?? x.CreatedAt).ThenBy(x => x.PaymentId),
            _ => asc
                ? q.OrderBy(x => x.CreatedAt).ThenBy(x => x.PaymentId)
                : q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.PaymentId),
        };

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, 200);
        var rows = await q.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken).ConfigureAwait(false);
        var items = rows.Select(row => new PaymentCheckoutRowDto(
            row.PaymentId,
            row.CheckoutId,
            row.Amount,
            row.Currency,
            row.Status.ToString(),
            row.ProviderCode,
            row.CreatedAt,
            row.CompletedAt,
            null)).ToList();
        return new PaymentAdminGridPageDto(items, total);
    }

    private static IQueryable<CustomerPayment> ApplyFilter(IQueryable<CustomerPayment> source, PaymentAdminGridFilterDto filter)
    {
        if (filter.Field == "amount" && decimal.TryParse(filter.Value, out var amount))
        {
            return filter.Operator switch
            {
                "eq" => source.Where(x => x.Amount == amount),
                "gte" => source.Where(x => x.Amount >= amount),
                "lte" => source.Where(x => x.Amount <= amount),
                _ => source,
            };
        }

        if (filter.Field == "status" && Enum.TryParse<PaymentStatus>(filter.Value, true, out var status))
        {
            return source.Where(x => x.Status == status);
        }

        if (filter.Field == "provider" && !string.IsNullOrWhiteSpace(filter.Value))
        {
            var v = filter.Value.Trim().ToLower();
            return source.Where(x => x.ProviderCode.ToLower().Contains(v));
        }

        return source;
    }
}
