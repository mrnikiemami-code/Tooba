using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.CategoryChanges.Ports;

namespace Tooba.Catalog.Application.CategoryChanges.Commands;

/// <summary>Handles ReplacePrimaryCategoryCommand.</summary>
public sealed class ReplacePrimaryCategoryHandler
    : IRequestHandler<ReplacePrimaryCategoryCommand, Result<CategoryChangeImpact>>
{
    private readonly ICategoryChangeDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public ReplacePrimaryCategoryHandler(ICategoryChangeDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public Task<Result<CategoryChangeImpact>> Handle(
        ReplacePrimaryCategoryCommand request,
        CancellationToken cancellationToken) =>
        _directory.ReplacePrimaryAsync(
            request.ProductId,
            request.Model.NewCategoryId,
            cancellationToken);
}
