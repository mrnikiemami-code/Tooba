using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Models;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Attributes.Definitions.Ports;

/// <summary>Catalog-owned port for Attribute Definition Admin operations.</summary>
public interface IAttributeDefinitionDirectory
{
    /// <summary>Lists definitions ordered by DisplayOrder then Code.</summary>
    Task<Result<IReadOnlyList<AttributeDefinitionView>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Reads one definition; missing → catalog.attribute.missing.</summary>
    Task<Result<AttributeDefinitionView>> GetAsync(Guid definitionId, CancellationToken cancellationToken);

    /// <summary>Creates a definition and optionally applies metadata in one SaveChanges.</summary>
    Task<Result<AttributeDefinitionCreatedResult>> CreateAsync(
        string code,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxisAllowed,
        IReadOnlyDictionary<string, string> localizedNames,
        AttributeDefinitionMetadataWriteModel? metadata,
        CancellationToken cancellationToken);

    /// <summary>Updates definition metadata fields (not Code/ValueKind/capability).</summary>
    Task<Result<AttributeDefinitionView>> UpdateMetadataAsync(
        Guid definitionId,
        AttributeDefinitionMetadataWriteModel metadata,
        CancellationToken cancellationToken);

    /// <summary>Non-destructive preview of disabling variant-axis capability.</summary>
    Task<Result<VariantAxisCapabilityDisableImpactView>> PreviewVariantAxisCapabilityDisableAsync(
        Guid definitionId,
        CancellationToken cancellationToken);

    /// <summary>Enables or disables variant-axis capability on the definition.</summary>
    Task<Result<AttributeDefinitionView>> SetVariantAxisCapabilityAsync(
        Guid definitionId,
        bool isVariantAxisAllowed,
        CancellationToken cancellationToken);

    /// <summary>Adds an enumeration option under a definition.</summary>
    Task<Result<AttributeOptionCreatedResult>> AddOptionAsync(
        Guid definitionId,
        string code,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken);
}
