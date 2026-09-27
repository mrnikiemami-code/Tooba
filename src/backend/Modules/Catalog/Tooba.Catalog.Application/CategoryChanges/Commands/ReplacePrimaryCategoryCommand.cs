using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.CategoryChanges.Models;

namespace Tooba.Catalog.Application.CategoryChanges.Commands;

/// <summary>Replace product primary category with migration semantics.</summary>
public sealed record ReplacePrimaryCategoryCommand(Guid ProductId, CategoryChangeWriteModel Model)
    : IRequest<Result<CategoryChangeImpact>>;
