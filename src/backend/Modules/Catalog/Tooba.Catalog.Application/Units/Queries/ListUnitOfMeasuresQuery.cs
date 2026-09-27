using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;

namespace Tooba.Catalog.Application.Units.Queries;

/// <summary>GET /v1/admin/catalog/units/</summary>
/// <param name="Language">Optional Code or UrlPrefix for display translation.</param>
public sealed record ListUnitOfMeasuresQuery(string? Language)
    : IRequest<Result<IReadOnlyList<UnitOfMeasureListItem>>>;
