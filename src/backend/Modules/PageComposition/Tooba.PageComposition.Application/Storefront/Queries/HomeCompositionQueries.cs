using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.PageComposition.Application.Composition;
using Tooba.PageComposition.Application.Models;

namespace Tooba.PageComposition.Application.Storefront.Queries;

/// <summary>ترکیب عمومی خانهٔ فروشگاه.</summary>
public sealed record GetHomeCompositionQuery(Guid TenantId, string? Locale)
    : IRequest<Result<HomeCompositionSnapshot>>;

/// <summary>Handler ترکیب عمومی خانه.</summary>
public sealed class GetHomeCompositionQueryHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<GetHomeCompositionQuery, Result<HomeCompositionSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<HomeCompositionSnapshot>> Handle(
        GetHomeCompositionQuery request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() =>
            composer.GetHomeCompositionAsync(request.TenantId, request.Locale, cancellationToken));
}

/// <summary>کاتالوگ section types.</summary>
public sealed record GetSectionCatalogQuery : IRequest<Result<SectionCatalogSnapshot>>;

/// <summary>Handler کاتالوگ.</summary>
public sealed class GetSectionCatalogQueryHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<GetSectionCatalogQuery, Result<SectionCatalogSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<SectionCatalogSnapshot>> Handle(
        GetSectionCatalogQuery request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() => composer.GetCatalogAsync(cancellationToken));
}
