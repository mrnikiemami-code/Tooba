using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Detail.Models;
using Tooba.Order.Application.Admin.Detail.Ports;

namespace Tooba.Order.Application.Admin.Detail.Queries.GetAdminOrderDetail;

/// <summary>جزئیات سفارش مدیر برای یک Checkout.</summary>
public sealed record GetAdminOrderDetailQuery(Guid CheckoutId, Guid ViewerUserId)
    : IRequest<Result<AdminOrderDetailPage>>;

/// <summary>Checkout را می‌خواند، AdminViewAck را با IClock ثبت می‌کند، و صفحه را ترکیب می‌کند.</summary>
public sealed class GetAdminOrderDetailHandler(
    IAdminOrderDetailCheckoutStore store,
    AdminOrderDetailComposer composer,
    IClock clock)
    : IRequestHandler<GetAdminOrderDetailQuery, Result<AdminOrderDetailPage>>
{
    public const string MissingErrorCode = "admin.order.missing";

    public async Task<Result<AdminOrderDetailPage>> Handle(
        GetAdminOrderDetailQuery request,
        CancellationToken cancellationToken)
    {
        var group = await store.GetCheckoutAsync(request.CheckoutId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<AdminOrderDetailPage>(new SemanticError(MissingErrorCode));
        }

        if (request.ViewerUserId != Guid.Empty)
        {
            await store.RecordAdminViewAckAsync(
                request.CheckoutId,
                request.ViewerUserId,
                clock.UtcNow,
                cancellationToken);
        }

        return await composer.ComposeAsync(group, cancellationToken);
    }
}
