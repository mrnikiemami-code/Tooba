using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Variants.Models;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Patches one product variant from workspace Admin HTTP.</summary>
public sealed record PatchProductWorkspaceVariantCommand(
    Guid ProductId,
    Guid VariantId,
    WorkspaceVariantPatchWriteModel Model) : IRequest<Result>;
