using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>
/// درز گونهٔ محصول برای schema ویژگی‌های بعدی. نوع تجاری Offer نیست.
/// </summary>
public enum CatalogProductKind
{
    /// <summary>
    /// کالای فیزیکی توصیفی. حمل و موجودی اینجا مدل نمی‌شود.
    /// </summary>
    PhysicalGood = 0,

    /// <summary>
    /// خدمت توصیفی. قیمت خدمت در Pricing است.
    /// </summary>
    Service = 1,
}
