using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;

namespace Tooba.Catalog.Application.Units.Commands;

/// <summary>PUT /v1/admin/catalog/units/{unitId}</summary>
public sealed record UpdateUnitOfMeasureCommand(Guid UnitId, UnitOfMeasureWriteModel Model)
    : IRequest<Result<UnitOfMeasureIdResult>>;
