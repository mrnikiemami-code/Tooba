using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Definitions.Models;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Creates an Attribute Definition.</summary>
public sealed class CreateAttributeDefinitionHandler
    : IRequestHandler<CreateAttributeDefinitionCommand, Result<AttributeDefinitionCreatedResult>>
{
    private readonly IAttributeDefinitionDirectory _definitions;

    /// <summary>Creates the handler.</summary>
    public CreateAttributeDefinitionHandler(IAttributeDefinitionDirectory definitions) =>
        _definitions = definitions;

    /// <inheritdoc />
    public Task<Result<AttributeDefinitionCreatedResult>> Handle(
        CreateAttributeDefinitionCommand request,
        CancellationToken cancellationToken) =>
        _definitions.CreateAsync(
            request.Model.Code,
            request.Model.ValueKind,
            request.Model.IsVariantAxisAllowed,
            request.Model.LocalizedNames ?? new Dictionary<string, string>(),
            request.Model.Metadata,
            cancellationToken);
}
