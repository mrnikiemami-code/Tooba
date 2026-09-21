using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>پیاده‌سازی سیاست احراز order.handle برای جهش‌های فروشنده.</summary>
public sealed class SellerFulfillmentAuthorizer : ISellerFulfillmentAuthorizer
{
    private readonly ISellerOrderAuthReader _orders;

    /// <summary>Authorizer را به Order.Contracts وصل می‌کند.</summary>
    public SellerFulfillmentAuthorizer(ISellerOrderAuthReader orders) => _orders = orders;

    /// <inheritdoc />
    public async Task<Result> EnsureCanMutateAsync(
        Guid sellerPartyId,
        Guid sellerOrderId,
        SellerOrderHandlePermissionSnapshot permission,
        CancellationToken cancellationToken)
    {
        if (!permission.HasGlobalWithinOwner && permission.AllowedCategoryIds.Count == 0)
        {
            return Result.Failure(new SemanticError(FulfillmentErrorCodes.SellerOrderHandleDenied));
        }

        if (permission.HasGlobalWithinOwner)
        {
            return Result.Success();
        }

        var order = await _orders.GetForSellerAsync(sellerOrderId, sellerPartyId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new SemanticError(FulfillmentErrorCodes.SellerOrderMissing));
        }

        var allowed = permission.AllowedCategoryIds.ToHashSet();
        var allAuthorized = order.Lines.All(line =>
            line.CategoryIdSnapshot is Guid cid && allowed.Contains(cid));
        if (!allAuthorized)
        {
            return Result.Failure(new SemanticError(FulfillmentErrorCodes.SellerOrderHandleScopeDenied));
        }

        return Result.Success();
    }
}
