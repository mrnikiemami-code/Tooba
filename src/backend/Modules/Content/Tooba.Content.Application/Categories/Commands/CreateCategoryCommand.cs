using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Commands;

public sealed record CreateCategoryCommand(
    string LanguageCode, Guid? ParentCategoryId, string Name, string Slug,
    string? ShortDescription, string? Description, int SortOrder)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class CreateCategoryCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<CreateCategoryCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        CreateCategoryCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.CreateAsync(request, cancellationToken),
            ContentErrorCodes.CreateRejected);
}
