using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Ports;

namespace Tooba.Catalog.Application.Units.Queries;

/// <summary>Gets Catalog unit detail for Admin.</summary>
public sealed class GetUnitOfMeasureHandler
    : IRequestHandler<GetUnitOfMeasureQuery, Result<UnitOfMeasureDetail>>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetUnitOfMeasureHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<UnitOfMeasureDetail>> Handle(
        GetUnitOfMeasureQuery request,
        CancellationToken cancellationToken)
        => _directory.GetAsync(request.UnitId, cancellationToken);
}
