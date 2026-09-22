using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Queries.GetCustomerTicket;

/// <summary>MediatR get customer ticket use case.</summary>
public sealed record GetCustomerTicketQuery(Guid ActorUserId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Gets one customer ticket; missing → SemanticError.</summary>
public sealed class GetCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<GetCustomerTicketQuery, Result<TicketSnapshotDto>>
{
    public async Task<Result<TicketSnapshotDto>> Handle(GetCustomerTicketQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await directory.GetForCustomerAsync(request.ActorUserId, request.TicketId, cancellationToken);
        return snapshot is null
            ? Result.Failure<TicketSnapshotDto>(new SemanticError(SupportErrorCodes.Missing))
            : Result.Success(snapshot);
    }
}
