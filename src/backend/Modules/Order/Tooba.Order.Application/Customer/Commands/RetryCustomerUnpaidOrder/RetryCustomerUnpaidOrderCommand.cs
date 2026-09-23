using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Application.Customer.Ports;
using Tooba.Payment.Contracts.Customer;

namespace Tooba.Order.Application.Customer.Commands.RetryCustomerUnpaidOrder;

/// <summary>تلاش مجدد همان سفارش پس از مهلت پرداخت منقضی.</summary>
public sealed record RetryCustomerUnpaidOrderCommand(Guid ActorUserId, Guid CheckoutId)
    : IRequest<Result<CustomerOrderDetailPage>>;

/// <summary>
/// مالکیت Actor را enforce می‌کند، چرخهٔ رزرو را از طریق IReservationCycleCoordinator تضمین می‌کند،
/// و پرداخت منقضی را برای retry باز می‌کند.
/// </summary>
public sealed class RetryCustomerUnpaidOrderHandler(
    ICustomerOrderCheckoutStore store,
    CustomerOrderComposer composer,
    IReservationCycleCoordinator cycles,
    IPaymentCustomerGateway payments)
    : IRequestHandler<RetryCustomerUnpaidOrderCommand, Result<CustomerOrderDetailPage>>
{
    public async Task<Result<CustomerOrderDetailPage>> Handle(
        RetryCustomerUnpaidOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty)
        {
            return Result.Failure<CustomerOrderDetailPage>(
                new SemanticError(CustomerOrderErrors.SessionRequired));
        }

        var group = await store.GetOwnedAsync(request.ActorUserId, request.CheckoutId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<CustomerOrderDetailPage>(
                new SemanticError(CustomerOrderErrors.Missing));
        }

        var current = await composer.ComposeDetailAsync(group, request.ActorUserId, cancellationToken);
        if (current.PaymentId is not { } paymentId || !current.CanRetryUnpaid)
        {
            return Result.Success(current);
        }

        try
        {
            var result = await cycles.EnsureRetryAfterExpiryAsync(request.CheckoutId, cancellationToken);
            if (result.Status is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable
                || result.Outcome is OrderSupplyOutcome.Unavailable or OrderSupplyOutcome.PartiallyUnavailable)
            {
                return Result.Failure<CustomerOrderDetailPage>(
                    new SemanticError(CustomerOrderErrors.SupplyUnavailable));
            }
        }
        catch (ContractOperationException ex) when (
            string.Equals(ex.Code, CustomerOrderErrors.SupplyUnavailable, StringComparison.Ordinal)
            || string.Equals(ex.Code, "inventory.supply.unavailable", StringComparison.Ordinal)
            || string.Equals(ex.Code, ReservationCycleErrors.RetryLimitReached, StringComparison.Ordinal))
        {
            return Result.Failure<CustomerOrderDetailPage>(
                new SemanticError(
                    string.Equals(ex.Code, ReservationCycleErrors.RetryLimitReached, StringComparison.Ordinal)
                        ? ReservationCycleErrors.RetryLimitReached
                        : CustomerOrderErrors.SupplyUnavailable));
        }

        await payments.ReopenExpiredForRetryAsync(paymentId, request.ActorUserId, null, cancellationToken);

        var refreshedGroup = await store.GetOwnedAsync(request.ActorUserId, request.CheckoutId, cancellationToken);
        if (refreshedGroup is null)
        {
            return Result.Failure<CustomerOrderDetailPage>(
                new SemanticError(CustomerOrderErrors.Missing));
        }

        var page = await composer.ComposeDetailAsync(refreshedGroup, request.ActorUserId, cancellationToken);
        return Result.Success(page);
    }
}
