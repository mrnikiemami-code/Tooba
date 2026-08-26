using Microsoft.Extensions.Logging;
using Tooba.Returns.Application;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// restock از طریق قرارداد Inventory؛ پیاده‌سازی فعلی no-op log است.
/// </summary>
public sealed class ReturnInventoryGateway : IReturnInventoryGateway
{
    private readonly ILogger<ReturnInventoryGateway> _logger;

    /// <summary>
    /// gateway را به logger وصل می‌کند.
    /// </summary>
    public ReturnInventoryGateway(ILogger<ReturnInventoryGateway> logger) => _logger = logger;

    /// <inheritdoc />
    public Task RestockConsumedReservationAsync(Guid reservationId, int quantity, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Return restock no-op for reservation {ReservationId} quantity {Quantity}",
            reservationId,
            quantity);
        return Task.CompletedTask;
    }
}
