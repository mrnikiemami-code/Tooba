using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Models;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Creates a draft workspace product; returns the new product id.</summary>
public sealed record CreateWorkspaceProductCommand(WorkspaceProductCreateWriteModel Model)
    : IRequest<Result<Guid>>;
