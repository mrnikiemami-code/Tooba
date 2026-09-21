using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Queries;

/// <summary>فهرست fulfillment فروشنده.</summary>
public sealed record ListSellerFulfillmentsQuery(Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<FulfillmentSnapshot>>>;

/// <summary>Handler فهرست فروشنده.</summary>
public sealed class ListSellerFulfillmentsHandler
    : IRequestHandler<ListSellerFulfillmentsQuery, Result<IReadOnlyList<FulfillmentSnapshot>>>
{
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>Handler را می‌سازد.</summary>
    public ListSellerFulfillmentsHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FulfillmentSnapshot>>> Handle(
        ListSellerFulfillmentsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await _fulfillment.ListForSellerAsync(request.SellerPartyId, cancellationToken));
}

/// <summary>خواندن fulfillment فروشنده.</summary>
public sealed record GetSellerFulfillmentQuery(Guid SellerPartyId, Guid FulfillmentId)
    : IRequest<Result<FulfillmentSnapshot>>;

/// <summary>Handler خواندن فروشنده.</summary>
public sealed class GetSellerFulfillmentHandler
    : IRequestHandler<GetSellerFulfillmentQuery, Result<FulfillmentSnapshot>>
{
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>Handler را می‌سازد.</summary>
    public GetSellerFulfillmentHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    /// <inheritdoc />
    public async Task<Result<FulfillmentSnapshot>> Handle(
        GetSellerFulfillmentQuery request,
        CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetAsync(request.FulfillmentId, cancellationToken);
        if (snapshot is null || snapshot.SellerPartyId != request.SellerPartyId)
        {
            return Result.Failure<FulfillmentSnapshot>(new SemanticError(FulfillmentErrorCodes.Missing));
        }

        return Result.Success(snapshot);
    }
}

/// <summary>فهرست admin.</summary>
public sealed record ListAdminFulfillmentsQuery : IRequest<Result<IReadOnlyList<FulfillmentSnapshot>>>;

/// <summary>Handler فهرست admin.</summary>
public sealed class ListAdminFulfillmentsHandler
    : IRequestHandler<ListAdminFulfillmentsQuery, Result<IReadOnlyList<FulfillmentSnapshot>>>
{
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>Handler را می‌سازد.</summary>
    public ListAdminFulfillmentsHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FulfillmentSnapshot>>> Handle(
        ListAdminFulfillmentsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await _fulfillment.ListAllAsync(cancellationToken));
}

/// <summary>خواندن admin.</summary>
public sealed record GetAdminFulfillmentQuery(Guid FulfillmentId)
    : IRequest<Result<FulfillmentSnapshot>>;

/// <summary>Handler خواندن admin.</summary>
public sealed class GetAdminFulfillmentHandler
    : IRequestHandler<GetAdminFulfillmentQuery, Result<FulfillmentSnapshot>>
{
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>Handler را می‌سازد.</summary>
    public GetAdminFulfillmentHandler(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    /// <inheritdoc />
    public async Task<Result<FulfillmentSnapshot>> Handle(
        GetAdminFulfillmentQuery request,
        CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetAsync(request.FulfillmentId, cancellationToken);
        return snapshot is null
            ? Result.Failure<FulfillmentSnapshot>(new SemanticError(FulfillmentErrorCodes.Missing))
            : Result.Success(snapshot);
    }
}

/// <summary>گرید صف کار admin.</summary>
public sealed record QueryAdminFulfillmentWorkQueueQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminFulfillmentWorkQueueRow>>>;

/// <summary>Handler گرید.</summary>
public sealed class QueryAdminFulfillmentWorkQueueHandler
    : IRequestHandler<QueryAdminFulfillmentWorkQueueQuery, Result<GridPageResponse<AdminFulfillmentWorkQueueRow>>>
{
    private readonly IAdminFulfillmentWorkQueueQuery _query;

    /// <summary>Handler را می‌سازد.</summary>
    public QueryAdminFulfillmentWorkQueueHandler(IAdminFulfillmentWorkQueueQuery query) => _query = query;

    /// <inheritdoc />
    public async Task<Result<GridPageResponse<AdminFulfillmentWorkQueueRow>>> Handle(
        QueryAdminFulfillmentWorkQueueQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await _query.QueryAsync(request.Request, cancellationToken));
}
