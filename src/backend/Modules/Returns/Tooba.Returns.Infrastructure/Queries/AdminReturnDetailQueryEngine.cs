using Tooba.Catalog.Contracts.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Infrastructure.Queries;

/// <summary>
/// TB-276: the admin detail read of one return case.
/// <para>
/// The stored snapshot (from <see cref="IReturnDirectory"/>) owns every case/line/money/physical fact. This engine adds
/// ONLY the operator display facts the owning modules answer through their Contracts — the Catalog variant title and
/// the Order line's stored unit code — resolved in the same batched way the work-queue engine already uses, so the
/// served detail shows the same product/quantity/unit as the queue row instead of a bare quantity.
/// </para>
/// <para>
/// It is read-only: no tracking change, no write, no money/stock/decision/provider call, and an unanswered display fact
/// stays null (an unknown product/unit is never invented and a missing unit is never defaulted to «واحد» here).
/// </para>
/// </summary>
public sealed class AdminReturnDetailQueryEngine(
    IReturnDirectory returns,
    IOrderGridEnrichmentReader orders,
    ICatalogVariantLookup catalog) : IAdminReturnDetailQuery
{
    /// <inheritdoc />
    public async Task<ReturnSnapshot?> QueryAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var snapshot = await returns.GetAsync(returnRequestId, cancellationToken);
        if (snapshot is null || snapshot.Items.Count == 0)
        {
            return snapshot;
        }

        var lineIds = snapshot.Items.Select(item => item.OrderLineId).Distinct().ToList();
        var lineBy = await orders.GetLinesAsync(lineIds, cancellationToken);
        var variantIds = lineBy.Values
            .Select(line => line.CatalogVariantId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var titles = variantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await catalog.GetVariantTitlesAsync(variantIds, cancellationToken);

        var items = snapshot.Items
            .Select(item =>
            {
                lineBy.TryGetValue(item.OrderLineId, out var line);
                var productLabel = string.Empty;
                if (line is not null
                    && titles.TryGetValue(line.CatalogVariantId, out var title)
                    && !string.IsNullOrWhiteSpace(title))
                {
                    productLabel = title.Trim();
                }

                var unitLabel = string.IsNullOrWhiteSpace(line?.UnitCodeSnapshot)
                    ? null
                    : line!.UnitCodeSnapshot!.Trim();

                return item with
                {
                    ProductLabel = productLabel.Length == 0 ? null : productLabel,
                    UnitLabel = unitLabel,
                    // TB-277: the OWNING accepted line snapshot is the only unit-precision authority. An unanswered
                    // line leaves it null (honest unavailable) instead of guessing whole pieces.
                    UnitDecimalPlaces = line is null ? null : Math.Clamp(line.QuantityDecimalPlacesSnapshot, 0, 6),
                };
            })
            .ToArray();

        return snapshot with { Items = items };
    }
}
