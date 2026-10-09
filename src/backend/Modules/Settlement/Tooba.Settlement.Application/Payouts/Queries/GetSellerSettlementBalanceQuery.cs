using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Payouts.Ports;
using Tooba.Settlement.Contracts.Errors;

namespace Tooba.Settlement.Application.Payouts.Queries;

/// <summary>مانده فروشنده.</summary>
public sealed record GetSellerSettlementBalanceQuery(Guid SellerPartyId)
    : IRequest<Result<SettlementBalanceSnapshot>>;

/// <summary>Handler مانده فروشنده.</summary>
public sealed class GetSellerSettlementBalanceQueryHandler(ISettlementDirectory settlement)
    : IRequestHandler<GetSellerSettlementBalanceQuery, Result<SettlementBalanceSnapshot>>
{
    /// <inheritdoc />
    public async Task<Result<SettlementBalanceSnapshot>> Handle(
        GetSellerSettlementBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var balance = await settlement.GetBalanceAsync(request.SellerPartyId, cancellationToken);
        return balance is null
            ? Result.Failure<SettlementBalanceSnapshot>(new SemanticError(SettlementErrorCodes.AccountMissing))
            : Result.Success(balance);
    }
}
