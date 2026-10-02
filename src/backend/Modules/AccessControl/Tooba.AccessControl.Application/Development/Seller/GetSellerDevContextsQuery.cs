using MediatR;
using Tooba.AccessControl.Contracts.Errors;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AccessControl.Application.Development.Seller;

/// <summary>
/// خواندن نگاشت Actor↔Seller توسعهٔ فروشنده برای مسیر <c>GET /v1/seller/dev-contexts</c>.
/// </summary>
public sealed record GetSellerDevContextsQuery : IRequest<Result<SellerDevContextsView>>;

/// <summary>
/// هندلر خواندن dev-contexts فروشنده. هیچ وضعیت کسب‌وکاری ندارد؛ فقط snapshot توسعه را پروجکت می‌کند.
/// </summary>
public sealed class GetSellerDevContextsQueryHandler(ISellerDevContextStore store)
    : IRequestHandler<GetSellerDevContextsQuery, Result<SellerDevContextsView>>
{
    /// <inheritdoc />
    public Task<Result<SellerDevContextsView>> Handle(
        GetSellerDevContextsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = store.Current;
        if (snapshot is null)
        {
            return Task.FromResult(
                Result.Failure<SellerDevContextsView>(new SemanticError(AccessControlErrorCodes.SellerDevNotReady)));
        }

        var rows = new List<SellerDevContextActorView>
        {
            new(
                snapshot.ActorA.ActorUserId,
                snapshot.ActorA.ActorLabel,
                snapshot.ActorA.SellerPartyId,
                snapshot.ActorA.SellerLabel,
                "seller-owner"),
            new(
                snapshot.ActorB.ActorUserId,
                snapshot.ActorB.ActorLabel,
                snapshot.ActorB.SellerPartyId,
                snapshot.ActorB.SellerLabel,
                "seller-owner-alt"),
        };

        if (snapshot.ScopedEmployee is { } employee)
        {
            rows.Add(new SellerDevContextActorView(
                employee.ActorUserId,
                employee.ActorLabel,
                employee.SellerPartyId,
                employee.SellerLabel,
                "scoped-employee"));
        }

        return Task.FromResult(Result.Success(new SellerDevContextsView(rows)));
    }
}
