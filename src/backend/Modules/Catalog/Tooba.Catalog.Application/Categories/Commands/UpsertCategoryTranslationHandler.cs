using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Upserts a category translation.</summary>
public sealed class UpsertCategoryTranslationHandler
    : IRequestHandler<UpsertCategoryTranslationCommand, Result<CategoryTranslationDto>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public UpsertCategoryTranslationHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public Task<Result<CategoryTranslationDto>> Handle(
        UpsertCategoryTranslationCommand request,
        CancellationToken cancellationToken) =>
        _categories.UpsertTranslationAsync(
            request.CategoryId,
            new CategoryTranslationUpsertRequest(
                request.Locale,
                request.Name,
                request.Slug,
                request.ShortDescription,
                request.Description,
                request.SeoTitle,
                request.SeoDescription,
                request.MetaKeywords),
            cancellationToken);
}
