using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>MediatR create-return use case (single authoritative request shape).</summary>
public sealed record CreateReturnCommand(
    Guid SellerOrderId,
    Guid ActorUserId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineCommand> Items,
    RefundDestination RefundDestination = RefundDestination.OriginalPayment) : IRequest<Result<ReturnSnapshot>>;

/// <summary>Handler — Result via the canonical typed-fault seam.</summary>
public sealed class CreateReturnHandler(IReturnDirectory returns)
    : IRequestHandler<CreateReturnCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(CreateReturnCommand request, CancellationToken cancellationToken) =>
        ReturnsOperation.ExecuteAsync(() => returns.CreateAsync(request, cancellationToken));
}
