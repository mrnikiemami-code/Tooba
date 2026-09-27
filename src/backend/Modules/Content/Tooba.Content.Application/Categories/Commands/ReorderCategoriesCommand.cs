using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Commands;

public sealed record ReorderCategoriesCommand(IReadOnlyList<ReorderContentCategoryItem> Items)
    : IRequest<Result>;

public sealed class ReorderCategoriesCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<ReorderCategoriesCommand, Result>
{
    public Task<Result> Handle(ReorderCategoriesCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.ReorderAsync(request.Items, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
