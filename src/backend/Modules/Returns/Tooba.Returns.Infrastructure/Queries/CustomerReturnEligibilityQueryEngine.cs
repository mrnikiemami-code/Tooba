using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Evaluators;

namespace Tooba.Returns.Infrastructure.Queries;

/// <summary>
/// TB-278: the customer-facing eligibility read.
/// <para>
/// It answers with the SAME authority the create command uses (<see cref="IReturnEligibilityEvaluator"/>) and adds
/// only the owning display/unit facts of each line through Contracts reads:
/// </para>
/// <list type="bullet">
/// <item><description>the seller order's owning customer is settled BEFORE any fact is returned, so a foreign seller
/// order can never leak a product, quantity or blocker;</description></item>
/// <item><description>the quantity facts come from the evaluator (delivered slices + prior occupied quantity), never
/// from the shipped count or an aggregate delivered label;</description></item>
/// <item><description>product title, stored unit code and the accepted unit decimal places come from the Order line
/// and Catalog variant; an unanswered unit precision stays <c>null</c> and blocks the line instead of guessing six
/// decimals.</description></item>
/// </list>
/// <para>
/// Nothing here writes, reserves or grants permission: the write path re-reads the same authority under the owning
/// per-seller-order serialization and remains final.
/// </para>
/// </summary>
public sealed class CustomerReturnEligibilityQueryEngine(
    IOrderReturnReader orders,
    IReturnEligibilityEvaluator eligibility,
    IOrderGridEnrichmentReader enrichment,
    ICatalogVariantLookup catalog) : ICustomerReturnEligibilityQuery
{
    private static readonly int FallbackWindowDays = (int)ReturnEligibilityEvaluator.ReturnWindow.TotalDays;

    /// <inheritdoc />
    public async Task<CustomerReturnEligibilitySnapshot> QueryAsync(
        Guid customerUserId,
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        // Binding FIRST: the order context carries the owning customer of this seller order. A foreign scope is a
        // typed owner refusal and no line fact is read from it.
        var orderContext = await orders.GetReturnContextAsync(sellerOrderId, cancellationToken);
        if (orderContext is null)
        {
            throw new ContractOperationException(ReturnsErrorCodes.Missing);
        }

        if (orderContext.PlacedByUserId != customerUserId)
        {
            throw new ContractOperationException(ReturnsErrorCodes.NotOwner);
        }

        var result = await eligibility.EvaluateAsync(sellerOrderId, cancellationToken);
        if (result.Lines.Count == 0)
        {
            return new CustomerReturnEligibilitySnapshot(
                result.SellerOrderId,
                result.CheckoutId,
                orderContext.Currency,
                result.Eligible,
                result.ReasonCode,
                result.EligibleUntil,
                result.LastDeliveredAt,
                []);
        }

        var lineIds = result.Lines.Select(line => line.OrderLineId).Distinct().ToList();
        var linesBy = await enrichment.GetLinesAsync(lineIds, cancellationToken);
        var variantIds = linesBy.Values
            .Select(line => line.CatalogVariantId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var titles = variantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await catalog.GetVariantTitlesAsync(variantIds, cancellationToken);

        var windowDaysBy = orderContext.Lines.ToDictionary(
            line => line.OrderLineId,
            line => line.ReturnWindowDaysSnapshot > 0 ? line.ReturnWindowDaysSnapshot : FallbackWindowDays);

        var lines = result.Lines
            .Select(line =>
            {
                linesBy.TryGetValue(line.OrderLineId, out var owning);
                var productLabel = string.Empty;
                if (owning is not null
                    && titles.TryGetValue(owning.CatalogVariantId, out var title)
                    && !string.IsNullOrWhiteSpace(title))
                {
                    productLabel = title.Trim();
                }

                var unitLabel = string.IsNullOrWhiteSpace(owning?.UnitCodeSnapshot)
                    ? null
                    : owning!.UnitCodeSnapshot!.Trim();
                // TB-277-R1: the accepted line snapshot is the only unit-precision authority. An unanswered line
                // leaves it null (honest unavailable) instead of guessing whole pieces.
                int? unitDecimalPlaces = owning is null
                    ? null
                    : Math.Clamp(owning.QuantityDecimalPlacesSnapshot, 0, OwningUnitQuantity.MaxDecimalPlaces);

                return new CustomerReturnEligibilityLine(
                    line.OrderLineId,
                    productLabel.Length == 0 ? null : productLabel,
                    unitLabel,
                    unitDecimalPlaces,
                    line.DeliveredQuantity,
                    line.AlreadyReturnedQuantity,
                    line.RemainingReturnableQuantity,
                    orderContext.Lines
                        .SingleOrDefault(x => x.OrderLineId == line.OrderLineId)?.IsReturnableSnapshot ?? false,
                    windowDaysBy.TryGetValue(line.OrderLineId, out var windowDays) ? windowDays : FallbackWindowDays,
                    line.ReturnableUntil,
                    // An owning read that did not answer a unit quantum blocks the line honestly: the customer can
                    // never be offered a fractional quantity nobody can receive.
                    unitDecimalPlaces is null ? ReturnEligibilityReasonCodes.UnitPrecisionUnavailable : line.BlockerCode);
            })
            .ToArray();

        return new CustomerReturnEligibilitySnapshot(
            result.SellerOrderId,
            result.CheckoutId,
            orderContext.Currency,
            result.Eligible,
            result.ReasonCode,
            result.EligibleUntil,
            result.LastDeliveredAt,
            lines);
    }
}
