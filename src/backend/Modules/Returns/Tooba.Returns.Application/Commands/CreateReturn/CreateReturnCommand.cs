using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Commands.CreateReturn;

/// <summary>MediatR create-return use case.</summary>
public sealed record CreateReturnCommand(
    Guid SellerOrderId,
    Guid ActorUserId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineCommand> Items,
    RefundDestination RefundDestination) : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — Result via ReturnsExceptionMapper.</summary>
public sealed class CreateReturnHandler(IReturnDirectory returns)
    : IRequestHandler<CreateReturnCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(CreateReturnCommand request, CancellationToken cancellationToken) =>
        ReturnsExceptionMapper.TryAsync(() => returns.CreateAsync(
            new Models.CreateReturnCommand(
                request.SellerOrderId,
                request.ActorUserId,
                request.IdempotencyKey,
                request.Reason,
                request.Items,
                request.RefundDestination),
            cancellationToken));
}
