using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;

namespace Tooba.Catalog.Application.Units.Commands;

/// <summary>POST /v1/admin/catalog/units/{unitId}/deactivate</summary>
public sealed record DeactivateUnitOfMeasureCommand(Guid UnitId)
    : IRequest<Result<UnitOfMeasureDeactivateResult>>;
