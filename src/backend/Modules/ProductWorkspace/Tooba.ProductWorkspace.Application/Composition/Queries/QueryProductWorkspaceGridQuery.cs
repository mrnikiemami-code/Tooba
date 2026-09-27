using MediatR;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.ProductWorkspace.Application.Composition.Queries;

/// <summary>Server-side Admin product grid query for ProductWorkspace.</summary>
public sealed record QueryProductWorkspaceGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminProductListItem>>>;
