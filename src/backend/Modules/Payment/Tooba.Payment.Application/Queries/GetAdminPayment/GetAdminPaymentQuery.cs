using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Application.Queries.GetAdminPayment;

/// <summary>MediatR admin payment detail query.</summary>
public sealed record GetAdminPaymentQuery(Guid PaymentId)
    : IRequest<Result<PaymentOperationalSnapshot>>;

/// <summary>Gets operational payment snapshot for admin.</summary>
public sealed class GetAdminPaymentHandler(IPaymentAdminDirectory payments)
    : IRequestHandler<GetAdminPaymentQuery, Result<PaymentOperationalSnapshot>>
{
    public async Task<Result<PaymentOperationalSnapshot>> Handle(
        GetAdminPaymentQuery request, CancellationToken cancellationToken)
    {
        var page = await payments.GetOperationalAsync(request.PaymentId, cancellationToken);
        return page is null
            ? Result.Failure<PaymentOperationalSnapshot>(new SemanticError(PaymentErrorCodes.AdminPaymentMissing))
            : Result.Success(page);
    }
}
