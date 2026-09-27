using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Commands.CreateCategory;

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
            () => categories.CreateAsync(new CreateContentCategoryCommand(
                request.LanguageCode, request.ParentCategoryId, request.Name, request.Slug,
                request.ShortDescription, request.Description, request.SortOrder), cancellationToken),
            ContentErrorCodes.CreateRejected);
}
