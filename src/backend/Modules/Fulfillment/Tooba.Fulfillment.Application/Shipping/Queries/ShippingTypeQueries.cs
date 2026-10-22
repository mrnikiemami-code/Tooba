using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Application.Composition;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Shipping.Queries;

/// <summary>
/// فهرست کامل کاتالوگ نوع ارسال برای مدیریت (شامل غیرفعال‌ها).
/// </summary>
/// <remarks>
/// این کوئری عمداً بدون پارامتر است و پرووِننس آن <c>NO_VALIDATOR_REQUIRED</c> است: هیچ ورودی
/// caller-controlled (فیلتر زبان، locale، فرهنگ یا صفحه‌بندی) ندارد، زبان محتوا از هیچ query/culture
/// استنتاج نمی‌شود و کاتالوگ کامل خروجی است. یک پارامتر بلااستفاده که نادیده گرفته شود مجاز نیست.
/// </remarks>
public sealed record ListShippingTypesQuery : IRequest<Result<IReadOnlyList<ShippingTypeSummaryView>>>;

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
/// <param name="ShippingTypeId">شناسهٔ غیرتهی هدف.</param>
public sealed record GetShippingTypeQuery(Guid ShippingTypeId) : IRequest<Result<ShippingTypeView>>;

/// <summary>مسیر جزئیات: نبودن ردیف یک خطای پایدار و صریح است، نه فهرست خالی.</summary>
public sealed class GetShippingTypeHandler : IRequestHandler<GetShippingTypeQuery, Result<ShippingTypeView>>
{
    private readonly IShippingTypeStore _store;
    private readonly IShippingTypeLanguageFacts _languageFacts;

    /// <summary>سازنده.</summary>
    public GetShippingTypeHandler(IShippingTypeStore store, IShippingTypeLanguageFacts languageFacts)
    {
        _store = store;
        _languageFacts = languageFacts;
    }

    /// <inheritdoc />
    public Task<Result<ShippingTypeView>> Handle(
        GetShippingTypeQuery request,
        CancellationToken cancellationToken) =>
        FulfillmentOperation.ExecuteAsync(async () =>
        {
            var languages = (await _languageFacts.ReadAsync(cancellationToken)).Resolve();

            return await _store.GetAsync(
                       request.ShippingTypeId,
                       languages.ActiveDefaultLanguageId,
                       cancellationToken)
                   ?? throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeNotFound));
        });
}
