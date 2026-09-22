using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Queries.GetSellerTicket;

/// <summary>MediatR get seller ticket use case.</summary>
public sealed record GetSellerTicketQuery(Guid SellerPartyId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Gets one seller ticket; missing → SemanticError.</summary>
public sealed class GetSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<GetSellerTicketQuery, Result<TicketSnapshotDto>>
{
    public async Task<Result<TicketSnapshotDto>> Handle(GetSellerTicketQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await directory.GetForSellerAsync(request.SellerPartyId, request.TicketId, cancellationToken);
        return snapshot is null
            ? Result.Failure<TicketSnapshotDto>(new SemanticError(SupportErrorCodes.Missing))
            : Result.Success(snapshot);
    }
}
