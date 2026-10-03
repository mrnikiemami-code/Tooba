using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// محصول منتشرشده برای انتخابگر scope Access Control.
/// </summary>
/// <param name="ProductId">شناسه.</param>
/// <param name="Title">عنوان محلی.</param>
/// <param name="Status">وضعیت.</param>
public sealed record AccessControlProductItem(Guid ProductId, string Title, string Status);
