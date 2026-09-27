using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreMenus.Models;

namespace Tooba.Catalog.Application.StoreMenus.Commands;

/// <summary>ایجاد منو (Admin facade → Result).</summary>
public sealed record CreateStoreMenuAdminCommand(StoreMenuWriteRequest Body)
    : IRequest<Result<StoreMenuDetailView>>;

/// <summary>به‌روزرسانی منو.</summary>
public sealed record UpdateStoreMenuAdminCommand(Guid MenuId, StoreMenuWriteRequest Body)
    : IRequest<Result<StoreMenuDetailView>>;

/// <summary>فعال/غیرفعال منو.</summary>
public sealed record SetStoreMenuEnabledAdminCommand(Guid MenuId, bool Enabled)
    : IRequest<Result<StoreMenuDetailView>>;

/// <summary>حذف منو.</summary>
public sealed record DeleteStoreMenuAdminCommand(Guid MenuId)
    : IRequest<Result<StoreMenuDeletedView>>;

/// <summary>افزودن آیتم.</summary>
public sealed record AddStoreMenuItemAdminCommand(Guid MenuId, StoreMenuItemWriteRequest Body)
    : IRequest<Result<StoreMenuItemAdminView>>;

/// <summary>ویرایش آیتم.</summary>
public sealed record UpdateStoreMenuItemAdminCommand(Guid MenuId, Guid ItemId, StoreMenuItemWriteRequest Body)
    : IRequest<Result<StoreMenuItemAdminView>>;

/// <summary>فعال/غیرفعال آیتم.</summary>
public sealed record SetStoreMenuItemEnabledAdminCommand(Guid MenuId, Guid ItemId, bool Enabled)
    : IRequest<Result<StoreMenuItemAdminView>>;

/// <summary>حذف آیتم.</summary>
public sealed record DeleteStoreMenuItemAdminCommand(Guid MenuId, Guid ItemId)
    : IRequest<Result<StoreMenuDeletedView>>;

/// <summary>ترتیب آیتم‌ها.</summary>
public sealed record ReorderStoreMenuItemsAdminCommand(Guid MenuId, IReadOnlyList<Guid>? ItemIds)
    : IRequest<Result<StoreMenuDetailView>>;

/// <summary>تنظیم منوی هدر.</summary>
public sealed record SetStoreHeaderMenuAdminCommand(Guid? HeaderMenuId)
    : IRequest<Result<StoreHeaderMenuSelectionView>>;
