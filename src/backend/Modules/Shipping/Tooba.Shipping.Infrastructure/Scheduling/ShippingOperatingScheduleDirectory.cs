using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Scheduling.Ports;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Scheduling;
using Tooba.Shipping.Domain.Scheduling;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Scheduling;

/// <summary>
/// پیاده‌سازی نویسندگی برنامهٔ عملیاتی روی <see cref="ShippingDbContext"/> خودِ همین ماژول. هیچ
/// DbContext ماژول دیگری خوانده یا نوشته نمی‌شود؛ کد خدمت فقط یک کلید مات Contracts-سازگار است.
/// </summary>
/// <remarks>
/// <para>
/// یکتایی هویت (خدمت + گزینه) از دو لایه محافظت می‌شود: upsert همین‌جا (تا یک برنامهٔ فعال در هر
/// زمان بماند) و ایندکس یکتای <c>NULLS NOT DISTINCT</c> در دیتابیس (تا هیچ نویسندهٔ دیگری نتواند
/// ردیف هم‌هویت ثبت کند). هیچ‌کدام جای دیگری را نمی‌گیرد.
/// </para>
/// <para>
/// نوشتن پنجره‌ها «جایگزینی کامل» است: مجموعهٔ قبلی حذف و مجموعهٔ جدید درج می‌شود، در یک
/// <c>SaveChangesAsync</c> واحد. این کار بازپخش همان بدنه را idempotent نگه می‌دارد.
/// </para>
/// </remarks>
public sealed class ShippingOperatingScheduleDirectory(ShippingDbContext context)
    : IShippingOperatingScheduleDirectory
{
    /// <inheritdoc />
    public async Task<Result<ShippingOperatingSchedulesView>> ListAsync(
        string serviceCode,
        CancellationToken cancellationToken)
    {
        var code = Normalize(serviceCode);
        if (code.Length == 0)
        {
            return Result.Failure<ShippingOperatingSchedulesView>(
                new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var schedules = await context.OperatingSchedules
            .AsNoTracking()
            .Include(x => x.Windows)
            .Where(x => x.ServiceCode == code)
            .OrderBy(x => x.OptionCode)
            .ToListAsync(cancellationToken);

        // An empty set is a real answer: NO explicit schedule, never an implicit default.
        return Result.Success(new ShippingOperatingSchedulesView(code, schedules.Select(Map).ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingOperatingScheduleAdminItem>> UpsertAsync(
        string serviceCode,
        ShippingOperatingScheduleWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = Normalize(serviceCode);
        if (code.Length == 0)
        {
            return Result.Failure<ShippingOperatingScheduleAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var optionCode = NormalizeOption(request.OptionCode);
        var existing = await context.OperatingSchedules
            .Include(x => x.Windows)
            .FirstOrDefaultAsync(
                x => x.ServiceCode == code
                     && (x.OptionCode == optionCode || (x.OptionCode == null && optionCode == null)),
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var definitions = ToDefinitions(request.Windows);

        // The domain guards throw SemanticException; they are mapped to a typed failure here as well as
        // in the handler, so the seam stays fail-closed for any caller (handler, test, future job) and
        // never leaks a raw exception out of a data operation.
        try
        {
            if (existing is null)
            {
                var created = ShippingOperatingSchedule.Create(
                    code,
                    request.TimeZoneId,
                    definitions,
                    now,
                    optionCode);

                context.OperatingSchedules.Add(created);
                await context.SaveChangesAsync(cancellationToken);
                return Result.Success(Map(created));
            }

            // Cross-identity mutation guard: the route service code plus the resolved option identity
            // decide which row is edited, so a body cannot silently re-point a schedule to another method.
            if (!existing.BelongsTo(code, optionCode))
            {
                return Result.Failure<ShippingOperatingScheduleAdminItem>(
                    new SemanticError(ShippingErrorCodes.ScheduleOptionMismatch));
            }

            existing.ChangeTimeZone(request.TimeZoneId, now);
            existing.ReplaceWindows(definitions, now);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(Map(existing));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<ShippingOperatingScheduleAdminItem>(ex.Error);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ShippingOperatingScheduleAdminItem>> SetActiveAsync(
        Guid scheduleId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var schedule = await context.OperatingSchedules
            .Include(x => x.Windows)
            .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result.Failure<ShippingOperatingScheduleAdminItem>(
                new SemanticError(ShippingErrorCodes.ScheduleNotFound));
        }

        // The route service code is authoritative: reaching the same id through another service path must
        // fail instead of toggling a row that belongs to a different service.
        if (!BelongsTo(schedule.ServiceCode, serviceCode))
        {
            return Result.Failure<ShippingOperatingScheduleAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceScopeMismatch));
        }

        var now = DateTimeOffset.UtcNow;
        if (isActive)
        {
            schedule.Activate(now);
        }
        else
        {
            schedule.Deactivate(now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(schedule));
    }

    private static IReadOnlyList<ShippingOperatingWindowDefinition> ToDefinitions(
        IReadOnlyList<ShippingOperatingWindowWrite>? windows)
        => windows is null
            ? []
            : windows
                .Select(x => new ShippingOperatingWindowDefinition(x.Weekday, x.StartTime, x.EndTime, x.Capacity))
                .ToList();

    private static bool BelongsTo(string storedServiceCode, string? routeServiceCode) =>
        string.Equals(
            storedServiceCode?.Trim() ?? string.Empty,
            routeServiceCode?.Trim() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? serviceCode) => serviceCode?.Trim() ?? string.Empty;

    /// <summary>کد گزینه را به شکل متعارف می‌برد؛ تهی/فاصله یعنی برنامه در سطح خدمت است.</summary>
    private static string? NormalizeOption(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static ShippingOperatingScheduleAdminItem Map(ShippingOperatingSchedule schedule) =>
        new(
            schedule.ScheduleId,
            schedule.ServiceCode,
            schedule.OptionCode,
            schedule.TimeZoneId,
            schedule.IsActive,
            schedule.Windows
                .OrderBy(x => x.Weekday)
                .ThenBy(x => x.StartTime)
                .Select(x => new ShippingOperatingWindowAdminItem(
                    x.WindowId,
                    x.Weekday,
                    x.StartTime,
                    x.EndTime,
                    x.Capacity))
                .ToList(),
            schedule.ConfiguredCapacityTotal,
            schedule.CreatedAt,
            schedule.UpdatedAt);
}
