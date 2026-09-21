using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Domain.ValueObjects;

/// <summary>
/// Domain type.
/// </summary>
public enum CartAccessKind
{
    /// <summary>
    /// مالک با هویت پایدار User.
    /// </summary>
    Authenticated = 0,

    /// <summary>
    /// مهمان با راز پرمخاطره؛ فقط هش ذخیره می‌شود.
    /// </summary>
    Guest = 1,
}
