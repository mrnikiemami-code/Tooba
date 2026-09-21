using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Errors;
using Tooba.Settlement.Application.Ports;

namespace Tooba.Settlement.Application.Commands;

/// <summary>درخواست payout فروشنده.</summary>
public sealed record RequestSellerPayoutCommand(
    Guid SellerPartyId,
    Guid ActorUserId,
    decimal Amount,
    string IdempotencyKey) : IRequest<Result<PayoutRequestSnapshot>>;

/// <summary>Handler درخواست payout.</summary>
public sealed class RequestSellerPayoutCommandHandler(ISettlementDirectory settlement)
    : IRequestHandler<RequestSellerPayoutCommand, Result<PayoutRequestSnapshot>>
{
    /// <inheritdoc />
    public async Task<Result<PayoutRequestSnapshot>> Handle(
        RequestSellerPayoutCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await settlement.RequestPayoutAsync(
                new RequestPayoutCommand(
                    request.SellerPartyId,
                    request.Amount,
                    request.IdempotencyKey,
                    request.ActorUserId),
                cancellationToken);
            return Result.Success(snapshot);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PayoutRequestSnapshot>(new SemanticError(Normalize(ex.Message)));
        }
    }

    private static string Normalize(string message) =>
        message switch
        {
            SettlementErrorCodes.AccountMissing => SettlementErrorCodes.AccountMissing,
            SettlementErrorCodes.PayoutInvalidAmount => SettlementErrorCodes.PayoutInvalidAmount,
            SettlementErrorCodes.AmountInvalid => SettlementErrorCodes.PayoutInvalidAmount,
            SettlementErrorCodes.IdempotencyRequired => SettlementErrorCodes.IdempotencyRequired,
            _ when message.StartsWith("settlement.", StringComparison.Ordinal) => message,
            _ => SettlementErrorCodes.PayoutRejected,
        };
}

/// <summary>پردازش payout (admin).</summary>
public sealed record ProcessAdminPayoutCommand(Guid PayoutRequestId, Guid ActorUserId)
    : IRequest<Result<PayoutRequestSnapshot>>;

/// <summary>Handler پردازش payout.</summary>
public sealed class ProcessAdminPayoutCommandHandler(ISettlementDirectory settlement)
    : IRequestHandler<ProcessAdminPayoutCommand, Result<PayoutRequestSnapshot>>
{
    /// <inheritdoc />
    public async Task<Result<PayoutRequestSnapshot>> Handle(
        ProcessAdminPayoutCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await settlement.ProcessPayoutAsync(
                new ProcessPayoutCommand(request.PayoutRequestId, request.ActorUserId),
                cancellationToken);
            return Result.Success(snapshot);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PayoutRequestSnapshot>(new SemanticError(Normalize(ex.Message)));
        }
    }

    private static string Normalize(string message) =>
        message switch
        {
            SettlementErrorCodes.PayoutMissing => SettlementErrorCodes.PayoutMissing,
            SettlementErrorCodes.PayoutInvalidState => SettlementErrorCodes.PayoutInvalidState,
            SettlementErrorCodes.GatewayUnconfigured => SettlementErrorCodes.GatewayUnconfigured,
            _ when message.StartsWith("settlement.", StringComparison.Ordinal)
                || message.StartsWith("payout.", StringComparison.Ordinal) => message,
            _ => SettlementErrorCodes.PayoutRejected,
        };
}

/// <summary>retry payout (admin).</summary>
public sealed record RetryAdminPayoutCommand(Guid PayoutRequestId, Guid ActorUserId)
    : IRequest<Result<PayoutRequestSnapshot>>;

/// <summary>Handler retry payout.</summary>
public sealed class RetryAdminPayoutCommandHandler(ISettlementDirectory settlement)
    : IRequestHandler<RetryAdminPayoutCommand, Result<PayoutRequestSnapshot>>
{
    /// <inheritdoc />
    public async Task<Result<PayoutRequestSnapshot>> Handle(
        RetryAdminPayoutCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await settlement.RetryPayoutAsync(
                new RetryPayoutCommand(request.PayoutRequestId, request.ActorUserId),
                cancellationToken);
            return Result.Success(snapshot);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PayoutRequestSnapshot>(new SemanticError(Normalize(ex.Message)));
        }
    }

    private static string Normalize(string message) =>
        message switch
        {
            SettlementErrorCodes.PayoutMissing => SettlementErrorCodes.PayoutMissing,
            SettlementErrorCodes.PayoutInvalidState => SettlementErrorCodes.PayoutInvalidState,
            SettlementErrorCodes.GatewayUnconfigured => SettlementErrorCodes.GatewayUnconfigured,
            _ when message.StartsWith("settlement.", StringComparison.Ordinal)
                || message.StartsWith("payout.", StringComparison.Ordinal) => message,
            _ => SettlementErrorCodes.PayoutRejected,
        };
}
