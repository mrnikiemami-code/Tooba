using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy.Queries;

/// <summary>Admin GET category reservation-policy preview.</summary>
public sealed record GetCategoryReservationPolicyQuery(Guid CategoryId)
    : IRequest<Result<ReservationPolicyEditorView>>;

/// <summary>Handler for <see cref="GetCategoryReservationPolicyQuery"/>.</summary>
public sealed class GetCategoryReservationPolicyHandler
    : IRequestHandler<GetCategoryReservationPolicyQuery, Result<ReservationPolicyEditorView>>
{
    private readonly IReservationCyclePolicyResolver _resolver;

    /// <summary>Creates the handler.</summary>
    public GetCategoryReservationPolicyHandler(IReservationCyclePolicyResolver resolver) => _resolver = resolver;

    /// <inheritdoc />
    public async Task<Result<ReservationPolicyEditorView>> Handle(
        GetCategoryReservationPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var preview = await _resolver.PreviewAsync(null, request.CategoryId, cancellationToken);
        return Result.Success(ReservationPolicyComposer.ForCategory(request.CategoryId, preview, true));
    }
}
