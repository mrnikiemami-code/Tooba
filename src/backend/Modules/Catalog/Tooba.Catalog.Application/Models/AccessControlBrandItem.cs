using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// برند برای انتخابگر scope Access Control.
/// </summary>
/// <param name="BrandId">شناسه.</param>
/// <param name="Name">نام محلی.</param>
/// <param name="Status">وضعیت.</param>
public sealed record AccessControlBrandItem(Guid BrandId, string Name, string Status);
