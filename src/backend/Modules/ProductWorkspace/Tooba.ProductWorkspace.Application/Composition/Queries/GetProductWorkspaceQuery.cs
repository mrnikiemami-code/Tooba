using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.ProductWorkspace.Application.Composition.Queries;

/// <summary>Admin aggregate GET composition query. Route Guid + parsed workspace scope only.</summary>
public sealed record GetProductWorkspaceQuery(
    Guid ProductId,
    ProductWorkspacePermissions Permissions) : IRequest<Result<ProductWorkspaceView>>;
