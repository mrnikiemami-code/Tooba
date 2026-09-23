using System.Text.RegularExpressions;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Detail;
using Tooba.Order.Application.Admin.Detail.Models;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class AdminReservationCycleAuditTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-09-13T08:00:00Z");

    [Fact]
    public void Active_cycle_one_uses_human_labels_and_server_remaining()
    {
        var audit = Audit(Proj([Active(1, ReservationCycleReason.InitialPayment)], 1, ReservationCycleStatus.Active), []);
        Assert.Equal("رزرو فعال", audit.StatusLabelFa);
        Assert.Equal("Active reservation", audit.StatusLabelEn);
        Assert.Equal("رزرو اولیه پرداخت", audit.ReasonLabelFa);
        Assert.Equal(1, audit.CurrentCycleNumber);
        Assert.Equal(600, audit.SecondsRemaining);
        Assert.False(audit.CanRetryReservation);
        Assert.False(audit.CanExtendTimer);
        Assert.DoesNotContain("Active", audit.StatusLabelFa, StringComparison.Ordinal);
    }

    [Fact]
    public void Expired_cycle_one_and_committed_and_cancel_are_distinct()
    {
        Assert.Equal("مهلت رزرو پایان یافته", Audit(Proj([Closed(1, ReservationCycleStatus.Expired)], 1, ReservationCycleStatus.Expired, remaining: 2), []).StatusLabelFa);
        Assert.Equal("رزرو پس از پرداخت نهایی شد", Audit(Proj([Closed(1, ReservationCycleStatus.CommittedPaid)], 1, ReservationCycleStatus.CommittedPaid, remaining: 2), []).StatusLabelFa);
        Assert.Equal("رزرو با لغو سفارش آزاد شد", Audit(Proj([Closed(1, ReservationCycleStatus.ReleasedByCancel)], 1, ReservationCycleStatus.ReleasedByCancel, remaining: 2), []).StatusLabelFa);
    }

    [Fact]
    public void Active_cycle_two_keeps_both_history_rows_and_stored_policy()
    {
        var first = Closed(1, ReservationCycleStatus.Expired, hold: 30, max: 2, source: "offer");
        var second = Active(2, ReservationCycleReason.RetryAfterExpiry, hold: 15, max: 2, source: "offer");
        var audit = Audit(Proj([first, second], 2, ReservationCycleStatus.Active, remaining: 0), []);
        Assert.Equal(2, audit.CurrentCycleNumber);
        Assert.Equal(2, audit.History.Count);
        Assert.Equal(30, audit.History[0].EffectiveHoldMinutes);
        Assert.Equal("offer", audit.History[0].PolicySource);
        Assert.Equal("پیشنهاد", audit.History[0].PolicySourceLabelFa);
        Assert.Equal("رزرو مجدد پس از پایان مهلت", audit.ReasonLabelFa);
    }

    [Fact]
    public void Reacquire_failed_shows_business_copy_and_all_shortage_lines()
    {
        var expired = Closed(1, ReservationCycleStatus.Expired);
        var events = new[]
        {
            Ev(ReservationCycleEventKind.Expired),
            Ev(ReservationCycleEventKind.ReacquireRequested),
            Ev(ReservationCycleEventKind.ReacquireFailed),
        };
        var supply = new OrderSupplyStatus(Guid.NewGuid(), OrderSupplyStatusKind.Unavailable,
        [
            new(Guid.NewGuid(), "سیب", "kg", 2, 0.5m, 1.5m, OrderSupplyStatusKind.Unavailable, null),
            new(Guid.NewGuid(), "گلابی", "kg", 1, 0, 1, OrderSupplyStatusKind.Unavailable, null),
        ]);
        var audit = AdminOrderReservationCycleMapper.ToAudit(
            Proj([expired], 1, ReservationCycleStatus.Expired, remaining: 2, supply: "Unavailable"),
            events,
            supply);
        Assert.Equal("رزرو مجدد ناموفق", audit.StatusLabelFa);
        Assert.Contains(audit.Events, e => e.DetailFa == "رزرو مجدد به دلیل کمبود موجودی انجام نشد.");
        Assert.Equal(2, audit.Shortages.Count);
        Assert.Equal(1.5m, audit.Shortages[0].Shortage);
        Assert.Equal(1, audit.Shortages[1].Shortage);
    }

    [Fact]
    public void Retry_limit_copy_and_manual_review_reason()
    {
        var expired = Closed(3, ReservationCycleStatus.Expired, max: 3);
        var events = new[] { Ev(ReservationCycleEventKind.RetryLimitReached) };
        var audit = Audit(Proj([expired], 3, ReservationCycleStatus.Expired, remaining: 0, max: 3), events);
        Assert.True(audit.RetryLimitReached);
        Assert.Contains(audit.Events, e => e.DetailFa == "حداکثر دفعات رزرو این سفارش استفاده شده است.");
        Assert.Equal("در انتظار بررسی پرداخت", AdminOrderReservationCycleMapper.ReasonFa(ReservationCycleReason.ManualReview));
        Assert.Equal("در انتظار ثبت اطلاعات پرداخت", AdminOrderReservationCycleMapper.ReasonFa(ReservationCycleReason.ManualInitial));
        Assert.Equal("بازیابی سفارش", AdminOrderReservationCycleMapper.ReasonFa(ReservationCycleReason.Restore));
        Assert.Equal("بازیابی تاریخی", AdminOrderReservationCycleMapper.ReasonFa(ReservationCycleReason.HistoricalRecovery));
        Assert.Equal("بازیابی پرداخت دیرهنگام", AdminOrderReservationCycleMapper.ReasonFa(ReservationCycleReason.LatePaymentRecovery));
    }

    [Fact]
    public void Grid_compact_labels_and_summary_capabilities()
    {
        Assert.Equal("فعال #1", AdminOrderReservationCycleMapper.CompactFa(ReservationCycleStatus.Active, 1));
        Assert.Equal("پایان‌یافته #1", AdminOrderReservationCycleMapper.CompactFa(ReservationCycleStatus.Expired, 1));
        Assert.Equal("فعال #2", AdminOrderReservationCycleMapper.CompactFa(ReservationCycleStatus.Active, 2));
        Assert.Equal("نهایی‌شده", AdminOrderReservationCycleMapper.CompactFa(ReservationCycleStatus.CommittedPaid, 1));
        Assert.Equal("—", AdminOrderReservationCycleMapper.CompactFa(null, null));
        var expired = OrderReservationCycleSummaryMapper.ToSummary(
            Proj([Closed(1, ReservationCycleStatus.Expired)], 1, ReservationCycleStatus.Expired, remaining: 2, supply: "AvailableForReacquire"));
        Assert.True(expired.RetryPossible);
        Assert.True(expired.NeedsReacquire);
        Assert.False(expired.RetryLimitReached);
    }

    [Fact]
    public void Historical_policy_is_not_current_settings()
    {
        var mapper = File.ReadAllText(OrderModule(
            "Tooba.Order.Application/Admin/Detail/AdminOrderReservationCycleMapper.cs"));
        Assert.Contains("PolicySourceFa", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("IOptions<ReservationCycleOptions>", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("InitialReservationHoldMinutes", mapper, StringComparison.Ordinal);
        Assert.False(File.Exists(Host("Admin/AdminReservationCycleMapper.cs")));
    }

    [Fact]
    public void Order_detail_includes_one_projection_and_grids_batch()
    {
        var detail = File.ReadAllText(OrderModule(
            "Tooba.Order.Application/Admin/Detail/AdminOrderDetailComposer.cs"));
        Assert.Contains("GetProjectionAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ListEventsAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ToAudit", detail, StringComparison.Ordinal);
        var hostComposer = File.ReadAllText(Host("Admin/AdminPanelComposer.cs"));
        Assert.DoesNotContain("GetProjectionAsync", hostComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("ToAudit", hostComposer, StringComparison.Ordinal);
        var orders = File.ReadAllText(OrderModule(
            "Tooba.Order.Infrastructure/Admin/OrdersGrid/AdminOrdersGridReader.cs"));
        var payments = File.ReadAllText(Module(
            "Payment/Tooba.Payment.Application/Queries/QueryAdminPaymentsGrid/QueryAdminPaymentsGridQuery.cs"));
        Assert.Contains("GetProjectionsAsync", orders, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(payments, @"EnrichAsync\(").Count);
        Assert.DoesNotContain("GetProjectionAsync(", orders, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProjectionAsync(", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("ListEventsAsync", orders, StringComparison.Ordinal);
        Assert.DoesNotContain("تمدید رزرو", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpiresAt editor", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Locks_and_no_t005()
    {
        var locks = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "TOOBA-LOCKS.md"));
        Assert.Contains("LOCK-SF-091", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-096", locks, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "TB-P10-T005",
            File.ReadAllText(OrderModule("Tooba.Order.Application/Admin/Detail/AdminOrderReservationCycleMapper.cs")),
            StringComparison.Ordinal);
    }

    private static AdminReservationCycleAuditView Audit(
        ReservationCycleProjection projection,
        IReadOnlyList<ReservationCycleEventSnapshot> events) =>
        AdminOrderReservationCycleMapper.ToAudit(projection, events, null);

    private static ReservationCycleSnapshot Active(
        int n,
        ReservationCycleReason reason,
        int hold = 10,
        int max = 3,
        string source = "platform") =>
        Snap(n, reason, ReservationCycleStatus.Active, hold, max, source, T0.AddMinutes(hold), null);

    private static ReservationCycleSnapshot Closed(
        int n,
        ReservationCycleStatus status,
        int hold = 10,
        int max = 3,
        string source = "platform") =>
        Snap(n, ReservationCycleReason.InitialPayment, status, hold, max, source, T0.AddMinutes(hold), T0.AddMinutes(hold));

    private static ReservationCycleSnapshot Snap(
        int n,
        ReservationCycleReason reason,
        ReservationCycleStatus status,
        int hold,
        int max,
        string source,
        DateTimeOffset expires,
        DateTimeOffset? ended) =>
        new(
            Guid.NewGuid(),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
            n,
            reason,
            status,
            T0,
            expires,
            ended,
            hold,
            max,
            source,
            Array.Empty<Guid>(),
            null,
            null);

    private static ReservationCycleProjection Proj(
        IReadOnlyList<ReservationCycleSnapshot> history,
        int current,
        ReservationCycleStatus status,
        int remaining = 2,
        int max = 3,
        string? supply = "Reserved") =>
        new(
            history[0].CheckoutId,
            current,
            status,
            history[^1].StartedAt,
            history[^1].ExpiresAt,
            T0,
            status == ReservationCycleStatus.Active
                ? (int)Math.Max(0, Math.Floor((history[^1].ExpiresAt - T0).TotalSeconds))
                : 0,
            history.Count,
            max,
            remaining,
            history,
            supply);

    private static ReservationCycleEventSnapshot Ev(ReservationCycleEventKind kind) =>
        new(Guid.NewGuid(), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), null, kind, T0, kind.ToString());

    private static string Host(string relative) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Tooba.Host", relative.Replace('/', Path.DirectorySeparatorChar));

    private static string OrderModule(string relative) => Module("Order/" + relative);

    private static string Module(string relative) =>
        Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Modules",
            relative.Replace('/', Path.DirectorySeparatorChar));

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".."));
}
