using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Ports;

namespace Tooba.Catalog.Application.Units.Commands;

/// <summary>Soft-deactivates a Catalog unit of measure.</summary>
public sealed class DeactivateUnitOfMeasureHandler
    : IRequestHandler<DeactivateUnitOfMeasureCommand, Result<UnitOfMeasureDeactivateResult>>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public DeactivateUnitOfMeasureHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<UnitOfMeasureDeactivateResult>> Handle(
        DeactivateUnitOfMeasureCommand request,
        CancellationToken cancellationToken)
        => _directory.DeactivateAsync(request.UnitId, cancellationToken);
}
