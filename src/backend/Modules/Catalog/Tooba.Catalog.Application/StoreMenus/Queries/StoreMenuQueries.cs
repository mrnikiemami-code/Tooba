using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreMenus.Models;

namespace Tooba.Catalog.Application.StoreMenus.Queries;

/// <summary>فهرست منوهای Admin.</summary>
public sealed record ListStoreMenusQuery : IRequest<Result<IReadOnlyList<StoreMenuListView>>>;

/// <summary>خواندن یک منو Admin.</summary>
public sealed record GetStoreMenuQuery(Guid MenuId) : IRequest<Result<StoreMenuDetailView>>;

/// <summary>ارجاع‌های حذف‌مسدود.</summary>
public sealed record GetStoreMenuUsageQuery(Guid MenuId) : IRequest<Result<IReadOnlyList<StoreMenuUsageView>>>;

/// <summary>انتخاب هدر Admin.</summary>
public sealed record GetStoreHeaderMenuSelectionQuery : IRequest<Result<StoreHeaderMenuSelectionView>>;

/// <summary>هدر عمومی Storefront.</summary>
public sealed record GetStoreHeaderMenuPublicQuery : IRequest<Result<StoreHeaderMenuSelectionView>>;

/// <summary>تصویر عمومی یک منو.</summary>
public sealed record ProjectPublicStoreMenuQuery(Guid MenuId)
    : IRequest<Result<IReadOnlyList<StoreMenuPublicItemView>>>;
