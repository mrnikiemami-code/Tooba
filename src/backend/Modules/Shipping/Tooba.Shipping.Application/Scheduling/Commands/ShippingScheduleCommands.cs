using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Application.Scheduling.Ports;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Shipping.Application.Scheduling.Commands;

/// <summary>
/// ایجاد یا جایگزینی کامل برنامهٔ عملیاتی هفتگی یک هویت روش (خدمت + گزینهٔ اختیاری).
/// </summary>
/// <remarks>
/// <b>چرا upsert.</b> برنامهٔ عملیاتی یک «سیاست» است، نه یک ردیف انباشتی: نگه‌داشتن چند برنامهٔ
/// فعال برای یک هویت، پرسش «کدام بازه واقعاً برقرار است» را بی‌پاسخ می‌گذارد. upsert با کلید
/// هویت، یک برنامهٔ فعال در هر زمان را تضمین می‌کند و بازپخش همان بدنه را idempotent می‌سازد.
/// </remarks>
/// <param name="ServiceCode">کد خدمت مسیر؛ مرجع قطعی.</param>
/// <param name="Request">بدنهٔ درخواست.</param>
public sealed record UpsertShippingOperatingScheduleCommand(
    string ServiceCode,
    ShippingOperatingScheduleWriteRequest Request)
    : IRequest<Result<ShippingOperatingScheduleAdminItem>>;

/// <summary>هندلر ایجاد/جایگزینی برنامهٔ عملیاتی.</summary>
public sealed class UpsertShippingOperatingScheduleHandler(
    IShippingOperatingScheduleDirectory directory,
    IShippingMethodOptionValidator optionValidator)
    : IRequestHandler<UpsertShippingOperatingScheduleCommand, Result<ShippingOperatingScheduleAdminItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingOperatingScheduleAdminItem>> Handle(
        UpsertShippingOperatingScheduleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Method identity is validated against the real Fulfillment catalog before anything is written:
        // an invented service or option must never become a schedule key.
        var methodValid = await optionValidator.ValidateAsync(
            request.ServiceCode,
            request.Request.OptionCode,
            cancellationToken);
        if (methodValid.IsFailure)
        {
            return Result.Failure<ShippingOperatingScheduleAdminItem>(methodValid.Errors);
        }

        return await ShippingScheduleOperation.ExecuteAsync(
            () => directory.UpsertAsync(request.ServiceCode, request.Request, cancellationToken));
    }
}

/// <summary>فعال/غیرفعال کردن برنامهٔ عملیاتی.</summary>
/// <param name="ScheduleId">شناسهٔ برنامه.</param>
/// <param name="ServiceCode">کد خدمت مسیر؛ مرجع قطعی.</param>
/// <param name="IsActive">وضعیت هدف.</param>
public sealed record SetShippingOperatingScheduleActiveCommand(Guid ScheduleId, string ServiceCode, bool IsActive)
    : IRequest<Result<ShippingOperatingScheduleAdminItem>>;

/// <summary>هندلر تغییر وضعیت برنامهٔ عملیاتی.</summary>
public sealed class SetShippingOperatingScheduleActiveHandler(IShippingOperatingScheduleDirectory directory)
    : IRequestHandler<SetShippingOperatingScheduleActiveCommand, Result<ShippingOperatingScheduleAdminItem>>
{
    /// <inheritdoc />
    public Task<Result<ShippingOperatingScheduleAdminItem>> Handle(
        SetShippingOperatingScheduleActiveCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingScheduleOperation.ExecuteAsync(
            () => directory.SetActiveAsync(
                request.ScheduleId,
                request.ServiceCode,
                request.IsActive,
                cancellationToken));
    }
}

/// <summary>خواندن برنامه‌های عملیاتی یک کد خدمت برای نویسندگی ادمین.</summary>
/// <param name="ServiceCode">کد خدمت Fulfillment.</param>
public sealed record GetShippingOperatingSchedulesQuery(string ServiceCode)
    : IRequest<Result<ShippingOperatingSchedulesView>>;

/// <summary>هندلر خواندن برنامه‌های عملیاتی یک خدمت.</summary>
public sealed class GetShippingOperatingSchedulesHandler(IShippingOperatingScheduleDirectory directory)
    : IRequestHandler<GetShippingOperatingSchedulesQuery, Result<ShippingOperatingSchedulesView>>
{
    /// <inheritdoc />
    public Task<Result<ShippingOperatingSchedulesView>> Handle(
        GetShippingOperatingSchedulesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingScheduleOperation.ExecuteAsync(
            () => directory.ListAsync(request.ServiceCode, cancellationToken));
    }
}
