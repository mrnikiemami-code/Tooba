using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Upserts a category translation for a locale.</summary>
public sealed record UpsertCategoryTranslationCommand(
    Guid CategoryId,
    string Locale,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? MetaKeywords)
    : IRequest<Result<CategoryTranslationDto>>;
