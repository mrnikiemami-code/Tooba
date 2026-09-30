using MediatR;
using Tooba.PageComposition.Application.Presentation;

namespace Tooba.PageComposition.Application.Queries;

/// <summary>ترکیب عمومی خانهٔ فروشگاه.</summary>
public sealed record GetHomeCompositionQuery(Guid TenantId, string? Locale)
    : IRequest<HomeCompositionSnapshot>;

/// <summary>Handler ترکیب عمومی خانه.</summary>
public sealed class GetHomeCompositionQueryHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<GetHomeCompositionQuery, HomeCompositionSnapshot>
{
    /// <inheritdoc />
    public Task<HomeCompositionSnapshot> Handle(GetHomeCompositionQuery request, CancellationToken cancellationToken)
        => composer.GetHomeCompositionAsync(request.TenantId, request.Locale, cancellationToken);
}

/// <summary>کاتالوگ section types.</summary>
public sealed record GetSectionCatalogQuery : IRequest<SectionCatalogSnapshot>;

/// <summary>Handler کاتالوگ.</summary>
public sealed class GetSectionCatalogQueryHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<GetSectionCatalogQuery, SectionCatalogSnapshot>
{
    /// <inheritdoc />
    public Task<SectionCatalogSnapshot> Handle(GetSectionCatalogQuery request, CancellationToken cancellationToken)
        => composer.GetCatalogAsync(cancellationToken);
}

/// <summary>نمای admin خانه.</summary>
public sealed record AdminGetHomeCompositionQuery(Guid TenantId, string? Locale)
    : IRequest<AdminHomeCompositionSnapshot>;

/// <summary>Handler نمای admin خانه.</summary>
public sealed class AdminGetHomeCompositionQueryHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminGetHomeCompositionQuery, AdminHomeCompositionSnapshot>
{
    /// <inheritdoc />
    public Task<AdminHomeCompositionSnapshot> Handle(
        AdminGetHomeCompositionQuery request,
        CancellationToken cancellationToken)
        => composer.AdminGetHomeAsync(request.TenantId, request.Locale, cancellationToken);
}
