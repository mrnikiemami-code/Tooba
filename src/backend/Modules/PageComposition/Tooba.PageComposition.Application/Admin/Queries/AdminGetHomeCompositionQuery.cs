using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.PageComposition.Application.Composition;
using Tooba.PageComposition.Application.Models;

namespace Tooba.PageComposition.Application.Admin.Queries;

/// <summary>نمای admin خانه.</summary>
public sealed record AdminGetHomeCompositionQuery(Guid TenantId, string? Locale)
    : IRequest<Result<AdminHomeCompositionSnapshot>>;

/// <summary>Handler نمای admin خانه.</summary>
public sealed class AdminGetHomeCompositionQueryHandler(PageCompositionPresentationComposer composer)
    : IRequestHandler<AdminGetHomeCompositionQuery, Result<AdminHomeCompositionSnapshot>>
{
    /// <inheritdoc />
    public Task<Result<AdminHomeCompositionSnapshot>> Handle(
        AdminGetHomeCompositionQuery request,
        CancellationToken cancellationToken) =>
        PageCompositionOperation.ExecuteAsync(() =>
            composer.AdminGetHomeAsync(request.TenantId, request.Locale, cancellationToken));
}
