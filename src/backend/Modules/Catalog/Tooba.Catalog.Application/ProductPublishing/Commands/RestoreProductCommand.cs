using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Restores an archived catalog product to draft.</summary>
public sealed record RestoreProductCommand(Guid ProductId) : IRequest<Result>;
