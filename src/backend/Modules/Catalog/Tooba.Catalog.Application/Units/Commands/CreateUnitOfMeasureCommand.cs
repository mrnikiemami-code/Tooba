using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;

namespace Tooba.Catalog.Application.Units.Commands;

/// <summary>POST /v1/admin/catalog/units/</summary>
public sealed record CreateUnitOfMeasureCommand(UnitOfMeasureWriteModel Model)
    : IRequest<Result<UnitOfMeasureIdResult>>;
