using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>برچسب خوانا یک مقدار محور.</summary>
public sealed record ProductVariantAxisLabel(string DefinitionName, string ValueLabel);
