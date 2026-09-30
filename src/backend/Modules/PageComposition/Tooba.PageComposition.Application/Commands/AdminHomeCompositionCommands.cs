using MediatR;
using Tooba.PageComposition.Application.Presentation;

namespace Tooba.PageComposition.Application.Commands;

/// <summary>مرتب‌سازی sectionهای خانه.</summary>
public sealed record AdminReorderHomeSectionsCommand(
    Guid TenantId,
    string? Locale,
    IReadOnlyList<Guid> SectionIds) : IRequest<AdminHomeCompositionSnapshot>;

/// <summary>Handler مرتب‌سازی.</summary>
public sealed class AdminReorderHomeSectionsCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminReorderHomeSectionsCommand, AdminHomeCompositionSnapshot>
{
    /// <inheritdoc />
    public Task<AdminHomeCompositionSnapshot> Handle(
        AdminReorderHomeSectionsCommand request,
        CancellationToken cancellationToken)
        => composer.AdminReorderHomeAsync(
            request.TenantId, request.Locale, request.SectionIds, cancellationToken);
}

/// <summary>به‌روزرسانی یک section.</summary>
public sealed record AdminUpdateHomeSectionCommand(
    Guid TenantId,
    string? Locale,
    Guid SectionId,
    UpdateHomeSectionCommand Input) : IRequest<AdminHomeCompositionSnapshot>;

/// <summary>Handler به‌روزرسانی section.</summary>
public sealed class AdminUpdateHomeSectionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminUpdateHomeSectionCommand, AdminHomeCompositionSnapshot>
{
    /// <inheritdoc />
    public Task<AdminHomeCompositionSnapshot> Handle(
        AdminUpdateHomeSectionCommand request,
        CancellationToken cancellationToken)
        => composer.AdminUpdateSectionAsync(
            request.TenantId, request.Locale, request.SectionId, request.Input, cancellationToken);
}

/// <summary>افزودن section.</summary>
public sealed record AdminAddHomeSectionCommand(
    Guid TenantId,
    string? Locale,
    AddHomeSectionCommand Input) : IRequest<AdminHomeCompositionSnapshot>;

/// <summary>Handler افزودن section.</summary>
public sealed class AdminAddHomeSectionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminAddHomeSectionCommand, AdminHomeCompositionSnapshot>
{
    /// <inheritdoc />
    public Task<AdminHomeCompositionSnapshot> Handle(
        AdminAddHomeSectionCommand request,
        CancellationToken cancellationToken)
        => composer.AdminAddSectionAsync(request.TenantId, request.Locale, request.Input, cancellationToken);
}

/// <summary>حذف section.</summary>
public sealed record AdminRemoveHomeSectionCommand(
    Guid TenantId,
    string? Locale,
    Guid SectionId) : IRequest<AdminHomeCompositionSnapshot>;

/// <summary>Handler حذف section.</summary>
public sealed class AdminRemoveHomeSectionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminRemoveHomeSectionCommand, AdminHomeCompositionSnapshot>
{
    /// <inheritdoc />
    public Task<AdminHomeCompositionSnapshot> Handle(
        AdminRemoveHomeSectionCommand request,
        CancellationToken cancellationToken)
        => composer.AdminRemoveSectionAsync(
            request.TenantId, request.Locale, request.SectionId, cancellationToken);
}

/// <summary>بازگردانی ترکیب پیش‌فرض خانه.</summary>
public sealed record AdminRestoreDefaultHomeCompositionCommand(Guid TenantId, string? Locale)
    : IRequest<AdminHomeCompositionSnapshot>;

/// <summary>Handler بازگردانی پیش‌فرض.</summary>
public sealed class AdminRestoreDefaultHomeCompositionCommandHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminRestoreDefaultHomeCompositionCommand, AdminHomeCompositionSnapshot>
{
    /// <inheritdoc />
    public Task<AdminHomeCompositionSnapshot> Handle(
        AdminRestoreDefaultHomeCompositionCommand request,
        CancellationToken cancellationToken)
        => composer.AdminRestoreDefaultHomeAsync(request.TenantId, request.Locale, cancellationToken);
}
