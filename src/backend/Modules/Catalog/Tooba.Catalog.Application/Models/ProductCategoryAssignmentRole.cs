using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>نقش پیوند محصول↔رده در لایهٔ Application.</summary>
public enum ProductCategoryAssignmentRole
{
    /// <summary>دسته اصلی.</summary>
    Primary = 0,

    /// <summary>دسته اضافی.</summary>
    Additional = 1,
}
