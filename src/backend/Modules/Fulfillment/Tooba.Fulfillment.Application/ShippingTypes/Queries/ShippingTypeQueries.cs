using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Composition;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.ShippingTypes;

/// <summary>فهرست کامل کاتالوگ نوع ارسال برای مدیریت (شامل غیرفعال‌ها).</summary>
/// <param name="LanguageId">فیلتر اختیاری زبان برای نمایش؛ فقط زبان شناخته‌شده پذیرفته می‌شود.</param>
public sealed record ListShippingTypesQuery(Guid? LanguageId)
    : IRequest<Result<IReadOnlyList<ShippingTypeSummaryView>>>;

/// <summary>مسیر فهرست: کاتالوگ خالی معتبر است، شکست زیرساخت استثنا می‌ماند.</summary>
public sealed class ListShippingTypesHandler
    : IRequestHandler<ListShippingTypesQuery, Result<IReadOnlyList<ShippingTypeSummaryView>>>
{
    private readonly IShippingTypeStore _store;

    /// <summary>سازنده.</summary>
    public ListShippingTypesHandler(IShippingTypeStore store) => _store = store;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ShippingTypeSummaryView>>> Handle(
        ListShippingTypesQuery request,
        CancellationToken cancellationToken) =>
        FulfillmentOperation.ExecuteAsync(() => _store.ListAsync(cancellationToken));
}

/// <summary>جزئیات یک تعریف نوع ارسال.</summary>
/// <param name="ShippingTypeId">شناسه.</param>
public sealed record GetShippingTypeQuery(Guid ShippingTypeId) : IRequest<Result<ShippingTypeView>>;

/// <summary>مسیر جزئیات: نبودن ردیف یک خطای پایدار و صریح است، نه فهرست خالی.</summary>
public sealed class GetShippingTypeHandler : IRequestHandler<GetShippingTypeQuery, Result<ShippingTypeView>>
{
    private readonly IShippingTypeStore _store;

    /// <summary>سازنده.</summary>
    public GetShippingTypeHandler(IShippingTypeStore store) => _store = store;

    /// <inheritdoc />
    public Task<Result<ShippingTypeView>> Handle(
        GetShippingTypeQuery request,
        CancellationToken cancellationToken) =>
        FulfillmentOperation.ExecuteAsync(async () =>
            await _store.GetAsync(request.ShippingTypeId, cancellationToken)
            ?? throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeNotFound)));
}
