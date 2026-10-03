using MediatR;
using Tooba.PageComposition.Application.Composition;
using Tooba.PageComposition.Application.Models;

namespace Tooba.PageComposition.Application.Admin.Queries;

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
