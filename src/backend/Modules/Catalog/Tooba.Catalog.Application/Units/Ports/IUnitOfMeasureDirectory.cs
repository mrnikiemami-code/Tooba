using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;

namespace Tooba.Catalog.Application.Units.Ports;

/// <summary>Catalog-owned port for UnitOfMeasure admin read/write.</summary>
public interface IUnitOfMeasureDirectory
{
    /// <summary>Lists units ordered by SortOrder then Code, with language-resolved display names.</summary>
    Task<IReadOnlyList<UnitOfMeasureListItem>> ListAsync(string? language, CancellationToken cancellationToken);

    /// <summary>Gets unit detail with all translations; missing → unit.missing.</summary>
    Task<Result<UnitOfMeasureDetail>> GetAsync(Guid unitId, CancellationToken cancellationToken);

    /// <summary>Creates a unit and translations.</summary>
    Task<Result<Guid>> CreateAsync(UnitOfMeasureWriteModel model, CancellationToken cancellationToken);

    /// <summary>Updates a unit and translations.</summary>
    Task<Result<Guid>> UpdateAsync(Guid unitId, UnitOfMeasureWriteModel model, CancellationToken cancellationToken);

    /// <summary>Soft-deactivates a unit.</summary>
    Task<Result<UnitOfMeasureDeactivateResult>> DeactivateAsync(Guid unitId, CancellationToken cancellationToken);
}
