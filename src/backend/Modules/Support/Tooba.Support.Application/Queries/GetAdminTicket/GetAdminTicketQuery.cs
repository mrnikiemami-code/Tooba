using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Queries.GetAdminTicket;

/// <summary>MediatR get admin ticket use case.</summary>
public sealed record GetAdminTicketQuery(Guid TicketId) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Gets one admin ticket including internal notes; missing → SemanticError.</summary>
public sealed class GetAdminTicketHandler(ISupportDirectory directory)
    : IRequestHandler<GetAdminTicketQuery, Result<TicketSnapshotDto>>
{
    public async Task<Result<TicketSnapshotDto>> Handle(GetAdminTicketQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await directory.GetForAdminAsync(request.TicketId, cancellationToken);
        return snapshot is null
            ? Result.Failure<TicketSnapshotDto>(new SemanticError(SupportErrorCodes.Missing))
            : Result.Success(snapshot);
    }
}
