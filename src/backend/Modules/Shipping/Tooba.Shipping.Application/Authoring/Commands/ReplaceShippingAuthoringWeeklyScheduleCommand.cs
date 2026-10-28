using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// جایگزینی کامل برنامهٔ عملیاتی هفتگی یک پیش‌نویس مالک با بازبینی ریشهٔ مورد انتظار.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">بدنهٔ منطقهٔ زمانی و هفت روز.</param>
public sealed record ReplaceShippingAuthoringWeeklyScheduleCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringWeeklyScheduleReplaceRequest Request)
    : IRequest<Result<ShippingAuthoringWeeklyScheduleItem>>;

/// <summary>
/// هندلر PUT برنامهٔ هفتگی. کل هفته پیش از هر نوشتن در دامنه پیش‌سنجی می‌شود و سپس برنامه + ردیف‌های
/// روز + بازبینی ریشه در <b>یک</b> SaveChanges ضمنی نوشته می‌شوند.
/// </summary>
/// <remarks>
/// هیچ تراکنش دست‌سندی و هیچ نوشتن تدریجی روز‌به‌روز وجود ندارد: یک PUT یک عمل اتمیک است. پرچم کاری
/// در لایهٔ دامنه الزامی است (<c>bool</c>)، پس نبود آن در transport هرگز به «تعطیل» تبدیل نمی‌شود.
/// </remarks>
public sealed class ReplaceShippingAuthoringWeeklyScheduleHandler(IShippingAuthoringDraftDirectory directory)
    : IRequestHandler<ReplaceShippingAuthoringWeeklyScheduleCommand, Result<ShippingAuthoringWeeklyScheduleItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringWeeklyScheduleItem>> Handle(
        ReplaceShippingAuthoringWeeklyScheduleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        // Defense in depth for direct (non-HTTP) invocation: an absent week is never "all closed".
        if (request.Request.Days is null)
        {
            return Result.Failure<ShippingAuthoringWeeklyScheduleItem>(
                new SemanticError(ShippingErrorCodes.AuthoringScheduleWeekdaySetInvalid));
        }

        IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> days;
        try
        {
            days = ShippingAuthoringWeeklyScheduleWriteWorkflow.ToDomainDays(request.Request.Days);
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringWeeklyScheduleItem>(exception.Error);
        }

        try
        {
            var replaced = await directory.ReplaceWeeklyScheduleAsync(
                request.Owner,
                request.DraftId,
                request.Request.ExpectedRevision,
                request.Request.TimeZoneId,
                days,
                cancellationToken,
                request.Request.SlotDurationMinutes);
            return replaced.IsFailure
                ? Result.Failure<ShippingAuthoringWeeklyScheduleItem>(replaced.Errors)
                : Result.Success(ShippingAuthoringWeeklyScheduleWorkflow.Map(replaced.Value));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringWeeklyScheduleItem>(exception.Error);
        }
    }
}

/// <summary>
/// نگاشت شکل انتقال به شکل دامنه. هیچ قاعدهٔ تجاری اینجا نیست؛ عنصر null و پرچم کاری غایب پیش از هر
/// نوشتن با کد پایدار رد می‌شوند.
/// </summary>
internal static class ShippingAuthoringWeeklyScheduleWriteWorkflow
{
    /// <summary>روزهای بدنه را به ورودی دامنه تبدیل می‌کند.</summary>
    /// <param name="writes">روزهای بدنه.</param>
    public static IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> ToDomainDays(
        IReadOnlyList<ShippingAuthoringScheduleDayWrite> writes)
    {
        ArgumentNullException.ThrowIfNull(writes);
        var result = new List<ShippingAuthoringDraft.ScheduleDayInput>(writes.Count);
        foreach (var write in writes)
        {
            if (write is null)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
            }

            if (write.IsWorking is not { } isWorking)
            {
                // A missing flag must never deserialize into an implicit closed day.
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
            }

            result.Add(new ShippingAuthoringDraft.ScheduleDayInput(
                write.Weekday,
                isWorking,
                write.StartTime,
                write.EndTime));
        }

        return result;
    }
}
