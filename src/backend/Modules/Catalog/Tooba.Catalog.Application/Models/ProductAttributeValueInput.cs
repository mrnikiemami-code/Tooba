using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// ورودی تنظیم/پاک‌سازی یک مقدار ویژگی محصول.
/// برای Enumeration چندمقداری: RawValue = شناسه‌های گزینه با کاما (N یا D)؛ EnumOptionId برای تک‌گزینه.
/// </summary>
public sealed record ProductAttributeValueInput(
    Guid DefinitionId,
    string? RawValue,
    Guid? EnumOptionId,
    bool Clear);
