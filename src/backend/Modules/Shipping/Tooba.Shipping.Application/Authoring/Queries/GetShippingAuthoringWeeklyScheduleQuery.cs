using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن برنامهٔ عملیاتی هفتگی یک پیش‌نویس مالک: وضعیت پیکربندی‌شده/نشده، منطقهٔ زمانی و روزها.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
public sealed record GetShippingAuthoringWeeklyScheduleQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId)
    : IRequest<Result<ShippingAuthoringWeeklyScheduleItem>>;

/// <summary>
/// هندلر خواندن برنامهٔ هفتگی. برنامه فقط داخل محدودهٔ کامل مالک مجاز خوانده می‌شود؛ پیش‌نویس غایب و
/// پیش‌نویس مالک دیگر همان پاسخ ایمن «پیدا نشد» را می‌گیرند. زبان/منطقهٔ زمانی رابط هیچ نقشی در
/// هویت روزها یا ساعت‌های ذخیره‌شده ندارد.
/// </summary>
public sealed class GetShippingAuthoringWeeklyScheduleHandler(IShippingAuthoringDraftDirectory directory)
    : IRequestHandler<GetShippingAuthoringWeeklyScheduleQuery, Result<ShippingAuthoringWeeklyScheduleItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringWeeklyScheduleItem>> Handle(
        GetShippingAuthoringWeeklyScheduleQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        return draft.IsFailure
            ? Result.Failure<ShippingAuthoringWeeklyScheduleItem>(draft.Errors)
            : Result.Success(ShippingAuthoringWeeklyScheduleWorkflow.Map(draft.Value));
    }
}

/// <summary>
/// نگاشت پیش‌نویس به سند برنامهٔ هفتگی. هیچ پیکربندی‌نشده‌ای «هفت روز کاری» یا منطقهٔ زمانی پیش‌فرض
/// نمی‌گیرد و ساعت‌های روز تعطیل صادقانه (همان‌طور که ذخیره شده‌اند) برگردانده می‌شوند.
/// </summary>
internal static class ShippingAuthoringWeeklyScheduleWorkflow
{
    /// <summary>پیش‌نویس را به پاسخ برنامهٔ هفتگی نگاشت می‌کند.</summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    public static ShippingAuthoringWeeklyScheduleItem Map(Domain.Authoring.ShippingAuthoringDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var schedule = draft.WeeklySchedule;
        return new ShippingAuthoringWeeklyScheduleItem(
            draft.DraftId,
            draft.Revision,
            schedule is not null,
            schedule?.TimeZoneId,
            schedule is null
                ? []
                : schedule.Days
                    .OrderBy(day => day.Weekday)
                    .Select(day => new ShippingAuthoringScheduleDayItem(
                        day.Weekday,
                        day.IsWorking,
                        day.StartTime,
                        day.EndTime))
                    .ToArray(),
            schedule?.SlotDurationMinutes);
    }
}
