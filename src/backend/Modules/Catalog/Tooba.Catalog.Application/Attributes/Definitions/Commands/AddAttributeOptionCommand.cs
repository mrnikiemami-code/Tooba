using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Definitions.Models;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Adds an option under an Attribute Definition.</summary>
public sealed record AddAttributeOptionCommand(
    Guid DefinitionId,
    string Code,
    Dictionary<string, string>? LocalizedNames)
    : IRequest<Result<AttributeOptionCreatedResult>>;
