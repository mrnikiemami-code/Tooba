using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Ports;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Handles <see cref="CreateWorkspaceProductCommand"/>.</summary>
public sealed class CreateWorkspaceProductHandler
    : IRequestHandler<CreateWorkspaceProductCommand, Result<Guid>>
{
    private readonly IProductIdentityDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public CreateWorkspaceProductHandler(IProductIdentityDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<Guid>> Handle(
        CreateWorkspaceProductCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.CreateWorkspaceProductAsync(request.Model, cancellationToken);
    }
}
