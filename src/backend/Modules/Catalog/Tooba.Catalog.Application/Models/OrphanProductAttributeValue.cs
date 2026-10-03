using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// مقدار ویژگی محصول که در schema جدید جایی ندارد.
/// </summary>
public sealed record OrphanProductAttributeValue(Guid DefinitionId, string CanonicalValue);
