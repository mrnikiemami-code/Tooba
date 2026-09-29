using MediatR;

namespace Tooba.AccessControl.Application.Development.Seller;

/// <summary>
/// هندلر خواندن dev-contexts فروشنده. هیچ وضعیت کسب‌وکاری ندارد؛ فقط snapshot توسعه را پروجکت می‌کند
/// و <see langword="null"/> برمی‌گرداند تا Endpoint بتواند not-ready را با کد پایدار برگرداند.
/// </summary>
public sealed class GetSellerDevContextsQueryHandler(ISellerDevContextStore store)
    : IRequestHandler<GetSellerDevContextsQuery, SellerDevContextsView?>
{
    /// <inheritdoc />
    public Task<SellerDevContextsView?> Handle(
        GetSellerDevContextsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = store.Current;
        if (snapshot is null)
        {
            return Task.FromResult<SellerDevContextsView?>(null);
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

        return Task.FromResult<SellerDevContextsView?>(new SellerDevContextsView(rows));
    }
}
