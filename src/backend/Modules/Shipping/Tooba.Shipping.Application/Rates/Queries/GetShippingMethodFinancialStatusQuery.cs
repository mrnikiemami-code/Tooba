using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Application.Rates.ReadModels;
using Tooba.Shipping.Contracts.Admin;

namespace Tooba.Shipping.Application.Rates.Queries;

/// <summary>
/// خواندن فقط-خواندنی وضعیت مالی یک روش canonical با هویت کامل <c>(serviceCode, optionCode)</c>.
/// هویت از پیوند authoritative سرور می‌آید، نه از تقسیم متنی کد روش در مرورگر؛ بنابراین ورودی
/// همان چیزی است که مالک کاتالوگ یک‌سطحی تأیید کرده است.
/// </summary>
/// <param name="ServiceCode">کد سرویس authoritative؛ الزامی.</param>
/// <param name="OptionCode">
/// گزینهٔ authoritative روش؛ تهی یعنی روش در سطح سرویس است و ردیف‌های سطح سرویس همان مالک آن‌اند.
/// </param>
public sealed record GetShippingMethodFinancialStatusQuery(string ServiceCode, string? OptionCode = null)
    : IRequest<Result<ShippingMethodFinancialStatusView>>;

/// <summary>
/// هندلر وضعیت مالی روش — فقط‌خواندنی. از همان درز موجود نرخ/پوشش Shipping می‌خواند و هیچ
/// نرخ‌گذاری مشتری، هیچ نوشتن و هیچ تغییر رفتاری ایجاد نمی‌کند.
/// </summary>
public sealed class GetShippingMethodFinancialStatusHandler(IShippingRateCoverageDirectory directory)
    : IRequestHandler<GetShippingMethodFinancialStatusQuery, Result<ShippingMethodFinancialStatusView>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingMethodFinancialStatusView>> Handle(
        GetShippingMethodFinancialStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var view = await ShippingAdminOperation.ExecuteAsync(
            () => directory.ListAsync(request.ServiceCode, cancellationToken));
        if (view.IsFailure)
        {
            return Result.Failure<ShippingMethodFinancialStatusView>(view.Errors);
        }

        // پوشش‌ها فقط با مجوز فعال دیده می‌شوند، اما مالکیت روش هرگز از شمارش آن‌ها ساخته نمی‌شود.
        return Result.Success(ShippingMethodFinancialStatusEvaluator.Evaluate(
            request.ServiceCode,
            request.OptionCode,
            view.Value.RatePolicies,
            view.Value.CoverageGrants));
    }
}
