using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Tooba.Returns.Application;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Host.Returns;

/// <summary>
/// خط مرجوعی در درخواست HTTP.
/// </summary>
public sealed record ReturnLineRequest(Guid OrderLineId, int Quantity);

/// <summary>
/// درخواست ایجاد مرجوعی.
/// </summary>
public sealed record CreateReturnRequest(
    Guid SellerOrderId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineRequest> Items,
    string? RefundDestination = null,
    string? Destination = null)
{
    /// <summary>مقصد بازپرداخت از فیلدهای هم‌نام FE/Host.</summary>
    public string? EffectiveRefundDestination => RefundDestination ?? Destination;
}

/// <summary>
/// درخواست تأیید مرجوعی فروشنده (مقصد اختیاری).
/// </summary>
public sealed record ApproveReturnRequest(string? RefundDestination = null, string? Destination = null)
{
    /// <summary>مقصد بازپرداخت از فیلدهای هم‌نام FE/Host.</summary>
    public string? EffectiveRefundDestination => RefundDestination ?? Destination;
}

/// <summary>
/// درخواست رد مرجوعی.
/// </summary>
public sealed record RejectReturnRequest(string? Reason);

/// <summary>
/// ترکیب HTTP مرجوعی برای customer/seller/admin.
/// </summary>
public sealed class ReturnPanelComposer
{
    private readonly IReturnDirectory _returns;
    private readonly AdminReturnGridQueryEngine _grid;

    /// <summary>
    /// سازندهٔ ترکیب مرجوعی.
    /// </summary>
    public ReturnPanelComposer(IReturnDirectory returns, ReturnsDbContext db)
    {
        _returns = returns;
        _grid = new AdminReturnGridQueryEngine(db);
    }

    /// <summary>
    /// درخواست را می‌خواند.
    /// </summary>
    public Task<ReturnSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken) =>
        _returns.GetAsync(returnRequestId, cancellationToken);

    /// <summary>
    /// فهرست درخواست‌های یک مشتری.
    /// </summary>
    public Task<IReadOnlyList<ReturnSnapshot>> ListForCustomerAsync(Guid customerUserId, CancellationToken cancellationToken) =>
        _returns.ListForCustomerAsync(customerUserId, cancellationToken);

    /// <summary>
    /// درخواست را برای همان فروشنده می‌خواند؛ در صورت عدم تطابق null برمی‌گرداند.
    /// </summary>
    public async Task<ReturnSnapshot?> GetForSellerAsync(Guid sellerPartyId, Guid returnRequestId, CancellationToken cancellationToken)
    {
        var snapshot = await _returns.GetAsync(returnRequestId, cancellationToken);
        return snapshot is null || snapshot.SellerPartyId != sellerPartyId ? null : snapshot;
    }

    /// <summary>
    /// فهرست درخواست‌های یک فروشنده.
    /// </summary>
    public Task<IReadOnlyList<ReturnSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
        _returns.ListForSellerAsync(sellerPartyId, cancellationToken);

    /// <summary>
    /// فهرست همه درخواست‌ها برای admin.
    /// </summary>
    public Task<IReadOnlyList<ReturnSnapshot>> ListAllAsync(CancellationToken cancellationToken) =>
        _returns.ListAllAsync(cancellationToken);

    /// <summary>صفحه‌بندی server-side گرید مرجوعی Admin (DB-native).</summary>
    public Task<GridPageResponse<ReturnSnapshot>> QueryGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Returns.Normalize(request);
        return _grid.QueryAsync(q, cancellationToken);
    }

    /// <summary>
    /// درخواست مرجوعی می‌سازد.
    /// </summary>
    public Task<ReturnSnapshot> CreateAsync(Guid actorUserId, CreateReturnRequest request, CancellationToken cancellationToken) =>
        _returns.CreateAsync(
            new CreateReturnCommand(
                request.SellerOrderId,
                actorUserId,
                request.IdempotencyKey,
                request.Reason,
                request.Items.Select(x => new ReturnLineCommand(x.OrderLineId, x.Quantity)).ToArray(),
                ParseDestination(request.EffectiveRefundDestination)),
            cancellationToken);

    /// <summary>
    /// درخواست را تأیید می‌کند.
    /// </summary>
    public Task<ReturnSnapshot> ApproveAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? refundDestination,
        CancellationToken cancellationToken) =>
        _returns.ApproveAsync(
            new ApproveReturnCommand(
                returnRequestId,
                actorUserId,
                string.IsNullOrWhiteSpace(refundDestination) ? null : ParseDestination(refundDestination)),
            cancellationToken);

    /// <summary>
    /// درخواست را رد می‌کند.
    /// </summary>
    public Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken) =>
        _returns.RejectAsync(new RejectReturnCommand(returnRequestId, actorUserId, reason), cancellationToken);

    /// <summary>
    /// refund را دوباره تلاش می‌کند (admin).
    /// </summary>
    public Task<ReturnSnapshot> RetryRefundAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken) =>
        _returns.RetryRefundAsync(new RetryRefundCommand(returnRequestId, actorUserId), cancellationToken);

    private static Tooba.Returns.Domain.RefundDestination ParseDestination(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Tooba.Returns.Domain.RefundDestination.OriginalPayment;
        return Enum.TryParse<Tooba.Returns.Domain.RefundDestination>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("مقصد بازگشت وجه نامعتبر است.");
    }
}
