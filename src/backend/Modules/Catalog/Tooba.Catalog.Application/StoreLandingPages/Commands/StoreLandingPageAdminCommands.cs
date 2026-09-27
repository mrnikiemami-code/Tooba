using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreLandingPages.Models;

namespace Tooba.Catalog.Application.StoreLandingPages.Commands;

/// <summary>ایجاد صفحه Landing (Admin facade → Result).</summary>
public sealed record CreateStoreLandingPageAdminCommand(StoreLandingPageWriteRequest Body)
    : IRequest<Result<StoreLandingPageAdminView>>;

/// <summary>به‌روزرسانی صفحه.</summary>
public sealed record UpdateStoreLandingPageAdminCommand(Guid PageId, StoreLandingPageWriteRequest Body)
    : IRequest<Result<StoreLandingPageAdminView>>;

/// <summary>تغییر وضعیت انتشار.</summary>
public sealed record SetStoreLandingPageStatusAdminCommand(Guid PageId, string? Status)
    : IRequest<Result<StoreLandingPageAdminView>>;

/// <summary>حذف صفحه.</summary>
public sealed record DeleteStoreLandingPageAdminCommand(Guid PageId)
    : IRequest<Result<StoreLandingOkView>>;

/// <summary>تنظیم خانه.</summary>
public sealed record SetStoreHomePageAdminCommand(Guid? HomePageId)
    : IRequest<Result<StoreHomeSelectionView>>;

/// <summary>افزودن بخش.</summary>
public sealed record AddStoreLandingPageSectionAdminCommand(Guid PageId, StoreLandingPageSectionWriteRequest Body)
    : IRequest<Result<StoreLandingPageSectionAdminView>>;

/// <summary>جایگزینی ترکیب.</summary>
public sealed record ReplaceStoreLandingPageCompositionAdminCommand(
    Guid PageId,
    IReadOnlyList<StoreLandingPageSectionWriteRequest>? Sections)
    : IRequest<Result<IReadOnlyList<StoreLandingPageSectionAdminView>>>;

/// <summary>به‌روزرسانی بخش.</summary>
public sealed record UpdateStoreLandingPageSectionAdminCommand(
    Guid PageId,
    Guid SectionId,
    StoreLandingPageSectionWriteRequest Body)
    : IRequest<Result<StoreLandingPageSectionAdminView>>;

/// <summary>فعال/غیرفعال بخش.</summary>
public sealed record SetStoreLandingPageSectionEnabledAdminCommand(Guid PageId, Guid SectionId, bool Enabled)
    : IRequest<Result<StoreLandingPageSectionAdminView>>;

/// <summary>ترتیب بخش‌ها.</summary>
public sealed record ReorderStoreLandingPageSectionsAdminCommand(Guid PageId, IReadOnlyList<Guid>? SectionIds)
    : IRequest<Result<IReadOnlyList<StoreLandingPageSectionAdminView>>>;

/// <summary>حذف بخش.</summary>
public sealed record DeleteStoreLandingPageSectionAdminCommand(Guid PageId, Guid SectionId)
    : IRequest<Result<StoreLandingOkView>>;
