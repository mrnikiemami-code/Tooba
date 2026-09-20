using MediatR;
using FluentValidation;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application;

/// <summary>نوشتن منوی فروشگاه روی مالک فعلی Catalog (orchestration موقت تا جابه‌جایی BC).</summary>
public interface IStoreMenuDirectory
{
    /// <summary>منوی جدید.</summary>
    Task<StoreMenu> CreateAsync(StoreMenuWriteModel model, CancellationToken cancellationToken);

    /// <summary>به‌روزرسانی منو.</summary>
    Task<StoreMenu> UpdateAsync(Guid menuId, StoreMenuWriteModel model, CancellationToken cancellationToken);

    /// <summary>فعال/غیرفعال منو.</summary>
    Task<StoreMenu> SetEnabledAsync(Guid menuId, bool enabled, CancellationToken cancellationToken);

    /// <summary>حذف منو.</summary>
    Task DeleteAsync(Guid menuId, CancellationToken cancellationToken);

    /// <summary>افزودن آیتم.</summary>
    Task<StoreMenuItem> AddItemAsync(Guid menuId, StoreMenuItemWriteModel model, CancellationToken cancellationToken);

    /// <summary>ویرایش آیتم.</summary>
    Task<StoreMenuItem> UpdateItemAsync(Guid menuId, Guid menuItemId, StoreMenuItemWriteModel model, CancellationToken cancellationToken);

    /// <summary>فعال/غیرفعال آیتم.</summary>
    Task<StoreMenuItem> SetItemEnabledAsync(Guid menuId, Guid menuItemId, bool enabled, CancellationToken cancellationToken);

    /// <summary>حذف آیتم و زیرشاخه.</summary>
    Task DeleteItemAsync(Guid menuId, Guid menuItemId, CancellationToken cancellationToken);

    /// <summary>ترتیب آیتم‌ها.</summary>
    Task<StoreMenu> ReorderItemsAsync(Guid menuId, IReadOnlyList<Guid> orderedIds, CancellationToken cancellationToken);

    /// <summary>ارجاع منوی هدر.</summary>
    Task SetHeaderAsync(Guid? headerMenuId, CancellationToken cancellationToken);
}

/// <summary>مدل نوشتن منو.</summary>
/// <param name="Title">عنوان.</param>
/// <param name="Locale">لوکیل.</param>
/// <param name="MenuKey">کلید.</param>
/// <param name="IsEnabled">فعال؟</param>
public sealed record StoreMenuWriteModel(string? Title, string? Locale, string? MenuKey, bool? IsEnabled);

/// <summary>مدل نوشتن آیتم.</summary>
/// <param name="Label">برچسب.</param>
/// <param name="LinkType">نوع پیوند.</param>
/// <param name="ParentMenuItemId">والد.</param>
/// <param name="TargetId">مقصد.</param>
/// <param name="ExternalUrl">URL خارجی.</param>
/// <param name="SortOrder">ترتیب.</param>
/// <param name="IsEnabled">فعال؟</param>
public sealed record StoreMenuItemWriteModel(
    string? Label,
    string? LinkType,
    Guid? ParentMenuItemId,
    Guid? TargetId,
    string? ExternalUrl,
    int? SortOrder,
    bool? IsEnabled);

/// <summary>فرمان ایجاد منو.</summary>
/// <param name="Model">مدل.</param>
public sealed record CreateStoreMenuCommand(StoreMenuWriteModel Model) : IRequest<StoreMenu>;

/// <summary>فرمان به‌روزرسانی منو.</summary>
/// <param name="MenuId">شناسه.</param>
/// <param name="Model">مدل.</param>
public sealed record UpdateStoreMenuCommand(Guid MenuId, StoreMenuWriteModel Model) : IRequest<StoreMenu>;

/// <summary>فرمان فعال‌سازی منو.</summary>
/// <param name="MenuId">شناسه.</param>
/// <param name="Enabled">فعال؟</param>
public sealed record SetStoreMenuEnabledCommand(Guid MenuId, bool Enabled) : IRequest<StoreMenu>;

/// <summary>فرمان حذف منو.</summary>
/// <param name="MenuId">شناسه.</param>
public sealed record DeleteStoreMenuCommand(Guid MenuId) : IRequest<Unit>;

/// <summary>فرمان افزودن آیتم.</summary>
/// <param name="MenuId">منو.</param>
/// <param name="Model">مدل.</param>
public sealed record AddStoreMenuItemCommand(Guid MenuId, StoreMenuItemWriteModel Model) : IRequest<StoreMenuItem>;

/// <summary>فرمان ویرایش آیتم.</summary>
/// <param name="MenuId">منو.</param>
/// <param name="MenuItemId">آیتم.</param>
/// <param name="Model">مدل.</param>
public sealed record UpdateStoreMenuItemCommand(Guid MenuId, Guid MenuItemId, StoreMenuItemWriteModel Model) : IRequest<StoreMenuItem>;

/// <summary>فرمان فعال‌سازی آیتم.</summary>
/// <param name="MenuId">منو.</param>
/// <param name="MenuItemId">آیتم.</param>
/// <param name="Enabled">فعال؟</param>
public sealed record SetStoreMenuItemEnabledCommand(Guid MenuId, Guid MenuItemId, bool Enabled) : IRequest<StoreMenuItem>;

/// <summary>فرمان حذف آیتم.</summary>
/// <param name="MenuId">منو.</param>
/// <param name="MenuItemId">آیتم.</param>
public sealed record DeleteStoreMenuItemCommand(Guid MenuId, Guid MenuItemId) : IRequest<Unit>;

/// <summary>فرمان ترتیب آیتم‌ها.</summary>
/// <param name="MenuId">منو.</param>
/// <param name="OrderedIds">ترتیب.</param>
public sealed record ReorderStoreMenuItemsCommand(Guid MenuId, IReadOnlyList<Guid> OrderedIds) : IRequest<StoreMenu>;

/// <summary>فرمان ارجاع هدر.</summary>
/// <param name="HeaderMenuId">منوی هدر یا null.</param>
public sealed record SetStoreHeaderMenuCommand(Guid? HeaderMenuId) : IRequest<Unit>;

/// <summary>اعتبارسنج ایجاد منو.</summary>
public sealed class CreateStoreMenuCommandValidator : AbstractValidator<CreateStoreMenuCommand>
{
    /// <summary>عنوان و کلید الزامی.</summary>
    public CreateStoreMenuCommandValidator()
    {
        RuleFor(x => x.Model.Title).NotEmpty().WithErrorCode("menu.title.required");
        RuleFor(x => x.Model.MenuKey).NotEmpty().WithErrorCode("menu.key.required");
    }
}
