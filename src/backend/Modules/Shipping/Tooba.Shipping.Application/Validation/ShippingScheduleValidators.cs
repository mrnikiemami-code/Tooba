using FluentValidation;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Application.Scheduling.Commands;
using Tooba.Shipping.Contracts.Scheduling;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Domain.Scheduling;

namespace Tooba.Shipping.Application.Validation;

/// <summary>کدهای اعتبارسنجی شکلِ transport برای برنامهٔ عملیاتی Shipping.</summary>
public static class ShippingScheduleValidationCodes
{
    /// <summary>کد خدمت الزامی است.</summary>
    public const string ServiceCodeRequired = "shipping.validation.service_code_required";

    /// <summary>کد خدمت از طول مجاز بیشتر است.</summary>
    public const string ServiceCodeTooLong = "shipping.validation.service_code_too_long";

    /// <summary>کد گزینهٔ روش ارسال از طول مجاز بیشتر است.</summary>
    public const string OptionCodeTooLong = "shipping.validation.option_code_too_long";

    /// <summary>شناسهٔ منطقهٔ زمانی الزامی است.</summary>
    public const string TimeZoneRequired = "shipping.validation.schedule_timezone_required";

    /// <summary>شناسهٔ منطقهٔ زمانی از طول مجاز بیشتر است.</summary>
    public const string TimeZoneTooLong = "shipping.validation.schedule_timezone_too_long";

    /// <summary>فهرست پنجره‌ها غایب است؛ یک برنامه باید مجموعهٔ صریح پنجره داشته باشد.</summary>
    public const string WindowsRequired = "shipping.validation.schedule_windows_required";

    /// <summary>تعداد پنجره‌ها از سقف مجاز بیشتر است.</summary>
    public const string WindowsTooMany = "shipping.validation.schedule_windows_too_many";

    /// <summary>شمارهٔ روز هفته خارج از بازهٔ ISO 1..7 است.</summary>
    public const string WeekdayOutOfRange = "shipping.validation.schedule_weekday_out_of_range";

    /// <summary>بازهٔ پنجره معکوس، صفر یا عبورکننده از نیمه‌شب است.</summary>
    public const string WindowRangeInvalid = "shipping.validation.schedule_window_range_invalid";

    /// <summary>ظرفیت پنجره باید مثبت باشد.</summary>
    public const string WindowCapacityInvalid = "shipping.validation.schedule_window_capacity_invalid";

    /// <summary>کد خدمت بدنه با کد خدمت مسیر یکسان نیست.</summary>
    public const string BodyServiceCodeMismatch = "shipping.validation.body_service_code_mismatch";

    /// <summary>دو پنجره روی یک روز هفته هم‌پوشانی دارند.</summary>
    public const string WindowOverlap = "shipping.validation.schedule_window_overlap";
}

/// <summary>
/// اعتبارسنج شکلِ transport برنامهٔ عملیاتی. فقط شکل بررسی می‌شود؛ ناورداهای تجاری (یکتایی هویت،
/// وجود خدمت/گزینه) در Domain و زیرساخت می‌مانند و اینجا تکرار نمی‌شوند.
/// </summary>
public sealed class ShippingOperatingScheduleWriteRequestValidator
    : AbstractValidator<ShippingOperatingScheduleWriteRequest>
{
    /// <summary>قواعد شکل درخواست برنامه را تعریف می‌کند.</summary>
    public ShippingOperatingScheduleWriteRequestValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingScheduleValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingScheduleValidationCodes.ServiceCodeTooLong);

        RuleFor(x => x.OptionCode)
            .MaximumLength(ShippingRatePolicy.OptionCodeMaxLength)
            .WithErrorCode(ShippingScheduleValidationCodes.OptionCodeTooLong);

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithErrorCode(ShippingScheduleValidationCodes.TimeZoneRequired)
            .MaximumLength(ShippingRatePolicyLimits.TimeZoneIdMaxLength)
            .WithErrorCode(ShippingScheduleValidationCodes.TimeZoneTooLong);

        // یک برنامه باید مجموعهٔ صریح پنجره داشته باشد: «null» و «خالی» دو معنای متفاوت‌اند و
        // تنها دومی («هیچ بازه‌ای») یک تصمیم قابل‌بازبینی است. بدنهٔ غایب پنجره یک ورودی بدشکل است.
        RuleFor(x => x.Windows)
            .NotNull().WithErrorCode(ShippingScheduleValidationCodes.WindowsRequired)
            .Must(x => x is null || x.Count <= ShippingOperatingSchedule.MaxWindows)
            .WithErrorCode(ShippingScheduleValidationCodes.WindowsTooMany);

        When(x => x.Windows is not null, () =>
        {
            RuleForEach(x => x.Windows).ChildRules(window =>
            {
                window.RuleFor(w => w.Weekday)
                    .InclusiveBetween(ShippingOperatingWindow.MinWeekday, ShippingOperatingWindow.MaxWeekday)
                    .WithErrorCode(ShippingScheduleValidationCodes.WeekdayOutOfRange);

                // end > start is a domain invariant mirrored here so a reversed or zero-length window is
                // refused as a declared 400 rather than reaching persistence.
                window.RuleFor(w => w.EndTime)
                    .GreaterThan(w => w.StartTime)
                    .WithErrorCode(ShippingScheduleValidationCodes.WindowRangeInvalid);

                window.RuleFor(w => w.Capacity)
                    .GreaterThan(0)
                    .When(w => w.Capacity is not null)
                    .WithErrorCode(ShippingScheduleValidationCodes.WindowCapacityInvalid);

                window.RuleFor(w => w.Capacity)
                    .LessThanOrEqualTo(ShippingOperatingWindow.CapacityMaxValue)
                    .When(w => w.Capacity is not null)
                    .WithErrorCode(ShippingScheduleValidationCodes.WindowCapacityInvalid);
            });

            // Overlap is a cross-item rule, so it cannot live on a single child: it is evaluated on the
            // whole list and reported once with a stable code.
            RuleFor(x => x.Windows)
                .Must(HaveNoOverlap)
                .WithErrorCode(ShippingScheduleValidationCodes.WindowOverlap);
        });
    }

    private static bool HaveNoOverlap(IReadOnlyList<ShippingOperatingWindowWrite>? windows)
    {
        if (windows is null)
        {
            return true;
        }

        for (var i = 0; i < windows.Count; i++)
        {
            for (var j = i + 1; j < windows.Count; j++)
            {
                var left = windows[i];
                var right = windows[j];
                if (left.Weekday == right.Weekday
                    && left.StartTime < right.EndTime
                    && right.StartTime < left.EndTime)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

/// <summary>اعتبارسنج درخواست ایجاد/جایگزینی برنامهٔ عملیاتی.</summary>
public sealed class UpsertShippingOperatingScheduleCommandValidator
    : AbstractValidator<UpsertShippingOperatingScheduleCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public UpsertShippingOperatingScheduleCommandValidator()
    {
        ShippingCommandScopeRules.Apply(this, x => x.ServiceCode, x => x.Request?.ServiceCode);
        RuleFor(x => x.Request).NotNull()
            .SetValidator(new ShippingOperatingScheduleWriteRequestValidator()!);
    }
}

/// <summary>اعتبارسنج درخواست تغییر وضعیت برنامهٔ عملیاتی.</summary>
public sealed class SetShippingOperatingScheduleActiveCommandValidator
    : AbstractValidator<SetShippingOperatingScheduleActiveCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public SetShippingOperatingScheduleActiveCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingScheduleValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingScheduleValidationCodes.ServiceCodeTooLong);
    }
}

/// <summary>اعتبارسنج درخواست خواندن برنامه‌های عملیاتی.</summary>
public sealed class GetShippingOperatingSchedulesQueryValidator
    : AbstractValidator<GetShippingOperatingSchedulesQuery>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public GetShippingOperatingSchedulesQueryValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingScheduleValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingScheduleValidationCodes.ServiceCodeTooLong);
    }
}
