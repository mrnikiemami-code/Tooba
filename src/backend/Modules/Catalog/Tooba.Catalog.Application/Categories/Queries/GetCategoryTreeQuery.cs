using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Queries;

/// <summary>Reads the Admin category tree for a locale.</summary>
public sealed record GetCategoryTreeQuery(string Locale, string? Search)
    : IRequest<Result<IReadOnlyList<CategoryTreeNodeDto>>>;
