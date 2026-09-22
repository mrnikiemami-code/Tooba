using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Detail;
using Tooba.Order.Application.Admin.Detail.Models;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// Facade Host برای برچسب‌های چرخه رزرو؛ ممیزی جزئیات در Order.Application.Admin.Detail است.
/// </summary>
public static class AdminReservationCycleMapper
{
    /// <summary>وضعیت کسب‌وکاری فارسی.</summary>
    public static string StatusFa(ReservationCycleStatus? status) =>
        AdminOrderReservationCycleMapper.StatusFa(status);

    /// <summary>وضعیت کسب‌وکاری انگلیسی.</summary>
    public static string StatusEn(ReservationCycleStatus? status) =>
        AdminOrderReservationCycleMapper.StatusEn(status);

    /// <summary>دلیل شروع چرخه فارسی.</summary>
    public static string ReasonFa(ReservationCycleReason? reason) =>
        AdminOrderReservationCycleMapper.ReasonFa(reason);

    /// <summary>دلیل شروع چرخه انگلیسی.</summary>
    public static string ReasonEn(ReservationCycleReason? reason) =>
        AdminOrderReservationCycleMapper.ReasonEn(reason);

    /// <summary>برچسب فشرده گرید فارسی.</summary>
    public static string CompactFa(ReservationCycleStatus? status, int? cycleNumber) =>
        AdminOrderReservationCycleMapper.CompactFa(status, cycleNumber);

    /// <summary>برچسب فشرده گرید انگلیسی.</summary>
    public static string CompactEn(ReservationCycleStatus? status, int? cycleNumber) =>
        AdminOrderReservationCycleMapper.CompactEn(status, cycleNumber);

    /// <summary>کلید فیلتر پایدار.</summary>
    public static string StateKey(ReservationCycleStatus? status) =>
        OrderReservationCycleSummaryMapper.StateKey(status);

    /// <summary>منبع سیاست فارسی.</summary>
    public static string PolicySourceFa(string? source) =>
        AdminOrderReservationCycleMapper.PolicySourceFa(source);

    /// <summary>منبع سیاست انگلیسی.</summary>
    public static string PolicySourceEn(string? source) =>
        AdminOrderReservationCycleMapper.PolicySourceEn(source);

    /// <summary>وضعیت نمایشی با رویدادهای ممیزی.</summary>
    public static ReservationCycleStatus? EffectiveStatus(
        ReservationCycleProjection projection,
        IReadOnlyList<ReservationCycleEventSnapshot>? events) =>
        AdminOrderReservationCycleMapper.EffectiveStatus(projection, events);

    /// <summary>خلاصهٔ فشرده برای گرید سفارش/پرداخت.</summary>
    public static AdminReservationCycleSummary ToSummary(ReservationCycleProjection? projection) =>
        Map(OrderReservationCycleSummaryMapper.ToSummary(projection));

    /// <summary>ممیزی کامل جزئیات سفارش (مالک Order Detail).</summary>
    public static AdminReservationCycleAuditView ToAudit(
        ReservationCycleProjection projection,
        IReadOnlyList<ReservationCycleEventSnapshot> events,
        OrderSupplyStatus? supply) =>
        AdminOrderReservationCycleMapper.ToAudit(projection, events, supply);

    /// <summary>خلاصهٔ خالی گرید.</summary>
    public static AdminReservationCycleSummary EmptySummary() =>
        Map(OrderReservationCycleSummaryMapper.EmptySummary());

    private static AdminReservationCycleSummary Map(OrderReservationCycleSummary summary) =>
        new(
            summary.CompactLabelFa,
            summary.CompactLabelEn,
            summary.State,
            summary.CycleNumber,
            summary.RetryPossible,
            summary.NeedsReacquire,
            summary.RetryLimitReached);
}
