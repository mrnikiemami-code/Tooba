using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Models;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Updates product core identity and locale translations.</summary>
public sealed record UpdateProductCoreCommand(
    Guid ProductId,
    WorkspaceProductCoreUpdateWriteModel Model) : IRequest<Result>;
