using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Application.Queries.QueryAdminPaymentsGrid;

/// <summary>MediatR admin payments grid query (Payment owns orchestration; enrichment via port).</summary>
public sealed record QueryAdminPaymentsGridQuery(AdminPaymentGridQueryInput Input)
    : IRequest<Result<AdminPaymentGridPageDto>>;

/// <summary>Handles QueryAdminPaymentsGrid.</summary>
public sealed class QueryAdminPaymentsGridHandler(
    IPaymentQueryDirectory payments,
    IPaymentAdminOrderEnrichmentPort enrichment)
    : IRequestHandler<QueryAdminPaymentsGridQuery, Result<AdminPaymentGridPageDto>>
{
    public async Task<Result<AdminPaymentGridPageDto>> Handle(
        QueryAdminPaymentsGridQuery request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        IReadOnlyList<Guid>? restrict = null;
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            restrict = await enrichment.ResolveSearchCheckoutIdsAsync(input.Search.Trim(), cancellationToken);
        }

        foreach (var filter in input.Filters.Where(f => f.Field is "supply" or "reservation"))
        {
            var wanted = (filter.Values ?? [])
                .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();
            IReadOnlyList<Guid> checkoutIds = filter.Field == "supply"
                ? await enrichment.ResolveSupplyFilterCheckoutIdsAsync(wanted, cancellationToken)
                : await enrichment.ResolveReservationFilterCheckoutIdsAsync(wanted, cancellationToken);
            restrict = restrict is null ? checkoutIds : restrict.Intersect(checkoutIds).ToList();
        }

        var page = await payments.QueryAdminGridAsync(
            new PaymentAdminGridQueryDto(
                input.Search,
                input.Filters
                    .Where(f => f.Field is not ("supply" or "reservation"))
                    .Select(f => new PaymentAdminGridFilterDto(f.Field, f.Operator, f.Value, f.ValueTo, f.Values))
                    .ToList(),
                input.SortField,
                input.SortDirection,
                input.Page,
                input.PageSize,
                restrict),
            cancellationToken);

        if (page.Items.Count == 0)
        {
            return Result.Success(new AdminPaymentGridPageDto([], input.Page, input.PageSize, page.Total));
        }

        var enriched = await enrichment.EnrichAsync(
            page.Items.Select(x => x.CheckoutId).Distinct().ToList(),
            cancellationToken);
        var byCheckout = enriched.ToDictionary(x => x.CheckoutId);

        var items = page.Items.Select(payment =>
        {
            byCheckout.TryGetValue(payment.CheckoutId, out var order);
            return new AdminPaymentGridItemDto(
                payment.PaymentId,
                payment.CheckoutId,
                order?.OrderReference ?? payment.CheckoutId.ToString("N")[..12],
                order?.CustomerDisplayName ?? "مشتری توبا",
                payment.Amount,
                payment.Currency,
                payment.Status,
                payment.ProviderCode,
                payment.CreatedAt,
                payment.CompletedAt,
                order?.SupplyStatus ?? "NotApplicable",
                order?.ReservationLabel ?? "—",
                order?.ReservationLabelEn ?? "—",
                order?.ReservationState ?? "none",
                order?.ReservationCycleNumber,
                order?.ReservationRetryPossible ?? false,
                order?.ReservationNeedsReacquire ?? false,
                order?.ReservationRetryLimitReached ?? false);
        }).ToList();

        return Result.Success(new AdminPaymentGridPageDto(items, input.Page, input.PageSize, page.Total));
    }
}
