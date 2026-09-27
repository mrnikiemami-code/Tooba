namespace Tooba.Catalog.Application.Brands.Models;

/// <summary>Admin brand option row for product brand pickers.</summary>
public sealed record BrandOptionView(Guid BrandId, string Name, string Status);
