using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Models;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Updates variant physical-specification override (null field inherits product).</summary>
public sealed record UpdateVariantPhysicalSpecificationCommand(
    Guid ProductId,
    Guid VariantId,
    WorkspaceVariantPhysicalSpecificationWriteModel Model) : IRequest<Result>;
