using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Ports;

namespace Tooba.Catalog.Application.Units.Queries;

/// <summary>Lists Catalog units for Admin grid.</summary>
public sealed class ListUnitOfMeasuresHandler
    : IRequestHandler<ListUnitOfMeasuresQuery, Result<IReadOnlyList<UnitOfMeasureListItem>>>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public ListUnitOfMeasuresHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnitOfMeasureListItem>>> Handle(
        ListUnitOfMeasuresQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _directory.ListAsync(request.Language, cancellationToken);
        return Result.Success(items);
    }
}
