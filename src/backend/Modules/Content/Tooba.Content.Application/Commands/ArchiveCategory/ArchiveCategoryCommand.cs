using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.ArchiveCategory;

public sealed record ArchiveCategoryCommand(Guid CategoryId) : IRequest<Result>;

public sealed class ArchiveCategoryCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<ArchiveCategoryCommand, Result>
{
    public Task<Result> Handle(ArchiveCategoryCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.ArchiveAsync(request.CategoryId, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
