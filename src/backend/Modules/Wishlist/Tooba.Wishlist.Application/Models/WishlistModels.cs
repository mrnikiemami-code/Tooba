using Tooba.Catalog.Contracts;

namespace Tooba.Wishlist.Application.Models;

/// <summary>نمای خصوصی یک ردیف علاقه‌مندی متعلق به کاربر جاری.</summary>
public sealed record WishlistEntry(Guid WishlistItemId, Guid ProductId, DateTimeOffset CreatedAt);

/// <summary>صفحهٔ خصوصی Wishlist بدون افشای شناسهٔ مالک.</summary>
public sealed record WishlistPage(IReadOnlyList<WishlistPageItem> Items);

/// <summary>ردیف Wishlist همراه کارت زنده یا دلیل صادقانهٔ عدم امکان ترکیب.</summary>
public sealed record WishlistPageItem(
    Guid WishlistItemId,
    Guid ProductId,
    DateTimeOffset CreatedAt,
    CatalogStorefrontProductCardDto? Product,
    string? UnavailableReason);

/// <summary>پاسخ مجموعه‌ای عضویت برای شناسه‌های درخواستی.</summary>
public sealed record WishlistMembershipResult(IReadOnlySet<Guid> ProductIds);
