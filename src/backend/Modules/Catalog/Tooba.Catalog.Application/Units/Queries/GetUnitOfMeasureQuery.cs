using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;

namespace Tooba.Catalog.Application.Units.Queries;

/// <summary>GET /v1/admin/catalog/units/{unitId}</summary>
public sealed record GetUnitOfMeasureQuery(Guid UnitId)
    : IRequest<Result<UnitOfMeasureDetail>>;
