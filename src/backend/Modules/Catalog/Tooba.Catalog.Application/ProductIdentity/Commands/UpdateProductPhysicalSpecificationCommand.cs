using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Models;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Updates product physical specification (grams + centimetres).</summary>
public sealed record UpdateProductPhysicalSpecificationCommand(
    Guid ProductId,
    WorkspaceProductPhysicalSpecificationWriteModel Model) : IRequest<Result>;
