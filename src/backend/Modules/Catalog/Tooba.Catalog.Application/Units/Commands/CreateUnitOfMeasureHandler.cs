using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Ports;

namespace Tooba.Catalog.Application.Units.Commands;

/// <summary>Creates a Catalog unit of measure.</summary>
public sealed class CreateUnitOfMeasureHandler
    : IRequestHandler<CreateUnitOfMeasureCommand, Result<UnitOfMeasureIdResult>>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public CreateUnitOfMeasureHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureIdResult>> Handle(
        CreateUnitOfMeasureCommand request,
        CancellationToken cancellationToken)
    {
        var created = await _directory.CreateAsync(request.Model, cancellationToken);
        if (created.IsFailure)
        {
            return Result.Failure<UnitOfMeasureIdResult>(created.Errors);
        }

        return Result.Success(new UnitOfMeasureIdResult(created.Value));
    }
}
