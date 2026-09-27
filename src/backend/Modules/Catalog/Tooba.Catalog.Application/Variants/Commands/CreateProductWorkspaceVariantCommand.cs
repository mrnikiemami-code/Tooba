using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Variants.Models;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Creates one product variant from workspace Admin HTTP.</summary>
public sealed record CreateProductWorkspaceVariantCommand(
    Guid ProductId,
    WorkspaceVariantCreateWriteModel Model) : IRequest<Result>;
