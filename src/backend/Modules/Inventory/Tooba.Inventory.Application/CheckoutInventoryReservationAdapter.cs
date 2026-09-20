using Tooba.Inventory.Contracts;
using Tooba.Inventory.Domain;

namespace Tooba.Inventory.Application;

/// <summary>آداپتر در-فرآیند؛ بعداً می‌تواند HTTP/gRPC/message شود بدون تغییر Order.</summary>
public sealed class CheckoutInventoryReservationAdapter : ICheckoutInventoryReservationPort
{
    private readonly IInventoryDirectory _inventory;
    private readonly IInventoryAvailabilityGateway _availability;

    /// <summary>آداپتر را به directory و gateway موجود وصل می‌کند.</summary>
    public CheckoutInventoryReservationAdapter(
        IInventoryDirectory inventory,
        IInventoryAvailabilityGateway availability)
    {
        _inventory = inventory;
        _availability = availability;
    }

    /// <inheritdoc />
    public async Task<CheckoutInventoryReservationResult> ReserveForCheckoutAsync(
        CheckoutInventoryReservationRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _availability.GetAvailabilityBatchAsync(
            request.Lines.Select(x => x.OfferId).Distinct().ToArray(),
            cancellationToken);
        var acquired = new List<Guid>();
        var lines = new List<CheckoutInventoryLineReservation>();
        try
        {
            foreach (var cartLine in request.Lines)
            {
                if (cartLine.ExistingReservationId is { } existingId)
                {
                    var existing = await _inventory.FindReservationAsync(existingId, cancellationToken);
                    if (existing is { Status: StockReservationStatus.Held }
                        && existing.Quantity >= cartLine.Quantity
                        && (existing.ExpiresAt is null || existing.ExpiresAt > request.Now))
                    {
                        lines.Add(new CheckoutInventoryLineReservation(cartLine.CartLineId, existingId));
                        continue;
                    }
                }

                if (!stock.TryGetValue(cartLine.OfferId, out var availability))
                {
                    throw new InvalidOperationException("inventory.supply.unavailable");
                }

                var location = availability.Locations
                    .Where(x => x.Available >= cartLine.Quantity)
                    .OrderByDescending(x => x.Available)
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("inventory.supply.unavailable");

                var correlation = string.IsNullOrWhiteSpace(request.CorrelationId)
                    ? $"checkout:{request.CartId:N}"
                    : request.CorrelationId.Trim();
                var externalReference = request.ProcessId is { } processId
                    ? $"order-commit:{request.CartId:N}:process:{processId:N}"
                    : $"order-commit:{request.CartId:N}";
                var idempotencyKey = $"{correlation}:line:{cartLine.CartLineId:N}";

                var receipt = await _inventory.ReserveAsync(
                    location.StockItemId,
                    cartLine.Quantity,
                    externalReference,
                    idempotencyKey,
                    request.ExpiresAt,
                    cancellationToken);
                acquired.Add(receipt.ReservationId);
                lines.Add(new CheckoutInventoryLineReservation(cartLine.CartLineId, receipt.ReservationId));
            }

            return new CheckoutInventoryReservationResult(lines);
        }
        catch
        {
            await ReleaseAsync(acquired, cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(IEnumerable<Guid> reservationIds, CancellationToken cancellationToken)
    {
        foreach (var reservationId in reservationIds.Distinct())
        {
            try
            {
                await _inventory.ReleaseAsync(reservationId, cancellationToken);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
