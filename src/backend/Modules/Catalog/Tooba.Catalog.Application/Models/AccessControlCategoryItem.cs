using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// ردهٔ Catalog برای انتخابگر scope در Access Control (نام محلی؛ نه JOIN از Order).
/// </summary>
/// <param name="CategoryId">شناسهٔ رده.</param>
/// <param name="ParentCategoryId">والد اختیاری.</param>
/// <param name="Name">نام محلی (اولویت fa سپس en).</param>
/// <param name="Status">وضعیت انتشار.</param>
public sealed record AccessControlCategoryItem(
    Guid CategoryId,
    Guid? ParentCategoryId,
    string Name,
    string Status);
