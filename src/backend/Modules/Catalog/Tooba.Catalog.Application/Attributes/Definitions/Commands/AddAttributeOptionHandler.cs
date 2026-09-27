using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Definitions.Models;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Adds an option under an Attribute Definition.</summary>
public sealed class AddAttributeOptionHandler
    : IRequestHandler<AddAttributeOptionCommand, Result<AttributeOptionCreatedResult>>
{
    private readonly IAttributeDefinitionDirectory _definitions;

    /// <summary>Creates the handler.</summary>
    public AddAttributeOptionHandler(IAttributeDefinitionDirectory definitions) =>
        _definitions = definitions;

    /// <inheritdoc />
    public Task<Result<AttributeOptionCreatedResult>> Handle(
        AddAttributeOptionCommand request,
        CancellationToken cancellationToken) =>
        _definitions.AddOptionAsync(
            request.DefinitionId,
            request.Code,
            request.LocalizedNames ?? new Dictionary<string, string>(),
            cancellationToken);
}
