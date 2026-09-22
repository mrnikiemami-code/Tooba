using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Queries.ListCustomerCheckoutFulfillments;

/// <summary>پاسخ HTTP پایدار لیست fulfillment مشتری برای یک checkout.</summary>
public sealed record CustomerCheckoutFulfillmentsResponse(
    IReadOnlyList<FulfillmentSnapshot> Fulfillments,
    string? PreferredCustomerTrackingReference,
    string? PreferredCustomerTrackingPackageNumber,
    string? PreferredCustomerPackageStatus);

/// <summary>ترجیح بسته فعال برای رهگیری اصلی مشتری — منطق سابق Host composer.</summary>
public static class CustomerFulfillmentPackageSelector
{
    public static ConsolidatedPackageSnapshot? SelectPreferred(IReadOnlyList<ConsolidatedPackageSnapshot> packages)
    {
        var active = packages
            .Where(p => p.Status is ConsolidatedPackageStatus.Created
                or ConsolidatedPackageStatus.Dispatched
                or ConsolidatedPackageStatus.Delivered)
            .OrderByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.CreatedAt)
            .ToList();
        if (active.Count == 0) return null;
        return active.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.TrackingReference)) ?? active[0];
    }
}

public sealed record ListCustomerCheckoutFulfillmentsQuery(Guid CheckoutId)
    : IRequest<Result<CustomerCheckoutFulfillmentsResponse>>;

public sealed class ListCustomerCheckoutFulfillmentsHandler
    : IRequestHandler<ListCustomerCheckoutFulfillmentsQuery, Result<CustomerCheckoutFulfillmentsResponse>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    public ListCustomerCheckoutFulfillmentsHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    public async Task<Result<CustomerCheckoutFulfillmentsResponse>> Handle(
        ListCustomerCheckoutFulfillmentsQuery request, CancellationToken cancellationToken)
    {
        var list = await _fulfillment.ListForCheckoutAsync(request.CheckoutId, cancellationToken);
        var packages = await _fulfillment.GetPackagesForCheckoutAsync(request.CheckoutId, cancellationToken);
        var preferred = CustomerFulfillmentPackageSelector.SelectPreferred(packages);
        var preferredTracking = preferred?.TrackingReference;
        if (!string.IsNullOrWhiteSpace(preferredTracking))
        {
            list = list.Select(s => s with { PreferredTrackingReference = preferredTracking }).ToList();
        }

        return Result.Success(new CustomerCheckoutFulfillmentsResponse(
            list,
            preferred?.TrackingReference,
            preferred?.PackageNumber,
            preferred?.Status.ToString()));
    }
}
