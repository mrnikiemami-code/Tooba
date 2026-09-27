using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Archives a catalog product.</summary>
public sealed record ArchiveProductCommand(Guid ProductId) : IRequest<Result>;
