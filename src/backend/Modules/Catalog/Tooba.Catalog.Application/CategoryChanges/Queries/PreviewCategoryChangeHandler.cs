using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.CategoryChanges.Ports;

namespace Tooba.Catalog.Application.CategoryChanges.Queries;

/// <summary>Handles PreviewCategoryChangeQuery.</summary>
public sealed class PreviewCategoryChangeHandler
    : IRequestHandler<PreviewCategoryChangeQuery, Result<CategoryChangeImpactReport>>
{
    private readonly ICategoryChangeDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public PreviewCategoryChangeHandler(ICategoryChangeDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public Task<Result<CategoryChangeImpactReport>> Handle(
        PreviewCategoryChangeQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Model.Locale)
            ? "fa-IR"
            : request.Model.Locale.Trim();
        return _directory.PreviewReportAsync(
            request.ProductId,
            request.Model.NewCategoryId,
            locale,
            cancellationToken);
    }
}
