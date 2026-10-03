using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;

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

/// <summary>نتیجهٔ افزودن idempotent که مشخص می‌کند ردیف تازه ساخته شده است یا خیر.</summary>
public sealed record WishlistAddResult(Guid WishlistItemId, bool Created);
