using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Models;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Updates product quantity policy.</summary>
public sealed record UpdateProductQuantityPolicyCommand(
    Guid ProductId,
    WorkspaceProductQuantityPolicyWriteModel Model) : IRequest<Result>;
