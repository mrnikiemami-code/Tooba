using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Ports;

namespace Tooba.Catalog.Application.Units.Commands;

/// <summary>Updates a Catalog unit of measure.</summary>
public sealed class UpdateUnitOfMeasureHandler
    : IRequestHandler<UpdateUnitOfMeasureCommand, Result<UnitOfMeasureIdResult>>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public UpdateUnitOfMeasureHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureIdResult>> Handle(
        UpdateUnitOfMeasureCommand request,
        CancellationToken cancellationToken)
    {
        var updated = await _directory.UpdateAsync(request.UnitId, request.Model, cancellationToken);
        if (updated.IsFailure)
        {
            return Result.Failure<UnitOfMeasureIdResult>(updated.Errors);
        }

        return Result.Success(new UnitOfMeasureIdResult(updated.Value));
    }
}
