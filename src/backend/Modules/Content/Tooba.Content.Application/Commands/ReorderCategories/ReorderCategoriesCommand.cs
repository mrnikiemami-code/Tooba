using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.ReorderCategories;

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
