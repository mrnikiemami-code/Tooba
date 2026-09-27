using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.ProductWorkspace.Application.Composition.Queries;

/// <summary>Lists recent Admin products for ProductWorkspace entry.</summary>
public sealed record ListProductWorkspaceQuery
    : IRequest<Result<IReadOnlyList<AdminProductListItem>>>;
