using Tooba.BuildingBlocks;

namespace Tooba.Wishlist.Domain;

/// <summary>قصد خصوصی یک کاربر برای نگهداری مرجع یک محصول؛ دادهٔ قیمت و موجودی را مالک نمی‌شود.</summary>
public sealed class WishlistItem
{
    private WishlistItem() { }

    /// <summary>شناسهٔ پایدار ردیف علاقه‌مندی.</summary>
    public Guid WishlistItemId { get; init; }
    /// <summary>شناسهٔ کاربر مالک که فقط از نشست سرور تعیین می‌شود.</summary>
    public Guid OwnerUserId { get; init; }
    /// <summary>مرجع opaque محصول در Catalog.</summary>
    public Guid ProductId { get; init; }
    /// <summary>زمان UTC افزودن محصول.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>ردیف معتبر را برای مالک و محصول مشخص می‌سازد.</summary>
    public static WishlistItem Create(Guid ownerUserId, Guid productId, DateTimeOffset createdAt)
    {
        if (ownerUserId == Guid.Empty || productId == Guid.Empty)
            throw new InvalidOperationException("شناسهٔ مالک و محصول الزامی است.");
        return new WishlistItem
        {
            WishlistItemId = UuidV7.New(),
            OwnerUserId = ownerUserId,
            ProductId = productId,
            CreatedAt = createdAt,
        };
    }
}
