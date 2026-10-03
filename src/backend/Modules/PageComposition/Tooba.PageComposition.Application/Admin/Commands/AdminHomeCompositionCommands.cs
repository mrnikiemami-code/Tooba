using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.PageComposition.Application.Composition;
using Tooba.PageComposition.Application.Models;

namespace Tooba.PageComposition.Application.Admin.Commands;

/// <summary>مرتب‌سازی sectionهای خانه.</summary>
public sealed record AdminReorderHomeSectionsCommand(
    Guid TenantId,
    string? Locale,
    IReadOnlyList<Guid> SectionIds) : IRequest<Result<AdminHomeCompositionSnapshot>>;

/// <summary>Handler مرتب‌سازی.</summary>
public sealed class AdminReorderHomeSectionsCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminReorderHomeSectionsCommand, Result<AdminHomeCompositionSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<AdminHomeCompositionSnapshot>> Handle(
        AdminReorderHomeSectionsCommand request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() =>
            composer.AdminReorderHomeAsync(
                request.TenantId, request.Locale, request.SectionIds, cancellationToken));
}

/// <summary>به‌روزرسانی یک section.</summary>
public sealed record AdminUpdateHomeSectionCommand(
    Guid TenantId,
    string? Locale,
    Guid SectionId,
    UpdateHomeSectionCommand Input) : IRequest<Result<AdminHomeCompositionSnapshot>>;

/// <summary>Handler به‌روزرسانی section.</summary>
public sealed class AdminUpdateHomeSectionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminUpdateHomeSectionCommand, Result<AdminHomeCompositionSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<AdminHomeCompositionSnapshot>> Handle(
        AdminUpdateHomeSectionCommand request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() =>
            composer.AdminUpdateSectionAsync(
                request.TenantId, request.Locale, request.SectionId, request.Input, cancellationToken));
}

/// <summary>افزودن section.</summary>
public sealed record AdminAddHomeSectionCommand(
    Guid TenantId,
    string? Locale,
    AddHomeSectionCommand Input) : IRequest<Result<AdminHomeCompositionSnapshot>>;

/// <summary>Handler افزودن section.</summary>
public sealed class AdminAddHomeSectionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminAddHomeSectionCommand, Result<AdminHomeCompositionSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<AdminHomeCompositionSnapshot>> Handle(
        AdminAddHomeSectionCommand request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() =>
            composer.AdminAddSectionAsync(request.TenantId, request.Locale, request.Input, cancellationToken));
}

/// <summary>حذف section.</summary>
public sealed record AdminRemoveHomeSectionCommand(
    Guid TenantId,
    string? Locale,
    Guid SectionId) : IRequest<Result<AdminHomeCompositionSnapshot>>;

/// <summary>Handler حذف section.</summary>
public sealed class AdminRemoveHomeSectionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminRemoveHomeSectionCommand, Result<AdminHomeCompositionSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<AdminHomeCompositionSnapshot>> Handle(
        AdminRemoveHomeSectionCommand request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() =>
            composer.AdminRemoveSectionAsync(
                request.TenantId, request.Locale, request.SectionId, cancellationToken));
}

/// <summary>بازگردانی ترکیب پیش‌فرض خانه.</summary>
public sealed record AdminRestoreDefaultHomeCompositionCommand(Guid TenantId, string? Locale)
    : IRequest<Result<AdminHomeCompositionSnapshot>>;

/// <summary>Handler بازگردانی پیش‌فرض.</summary>
public sealed class AdminRestoreDefaultHomeCompositionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminRestoreDefaultHomeCompositionCommand, Result<AdminHomeCompositionSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<AdminHomeCompositionSnapshot>> Handle(
        AdminRestoreDefaultHomeCompositionCommand request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() =>
            composer.AdminRestoreDefaultHomeAsync(request.TenantId, request.Locale, cancellationToken));
}
