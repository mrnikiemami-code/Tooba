using Microsoft.EntityFrameworkCore;
using Tooba.Returns.Application;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// snapshot refund برای Settlement بدون cross-DbContext.
/// </summary>
public sealed class ReturnSettlementBridge : IReturnSettlementReader
{
    private readonly ReturnsDbContext _db;

    /// <summary>پل settlement را به schema returns وصل می‌کند.</summary>
    public ReturnSettlementBridge(ReturnsDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<ReturnSettlementSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var request = await _db.ReturnRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken);
        return request is null
            ? null
            : new ReturnSettlementSnapshot(
                request.ReturnRequestId,
                request.SellerOrderId,
                request.SellerPartyId,
                request.RefundAmount,
                request.Currency);
    }
}
