using Tooba.Order.Contracts.Reservation;

namespace Tooba.Catalog.Application.Settings.HoldPolicy.Models;

/// <summary>یک مقدار مهلت با منبع و برچسب FA/EN.</summary>
public sealed record HoldPolicyDurationView(
    int? Hours,
    int EffectiveHours,
    string Source,
    string LabelFa,
    string LabelEn,
    string HelperFa,
    string HelperEn);

/// <summary>override روش پرداخت.</summary>
public sealed record PaymentMethodHoldView(
    string ProviderCode,
    string LabelFa,
    string LabelEn,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours);

/// <summary>تصویر تنظیم مهلت فروشگاه و روش پرداخت.</summary>
public sealed record HoldPolicySettingsView(
    HoldPolicyDurationView CartPersistence,
    HoldPolicyDurationView OnlinePaymentHold,
    HoldPolicyDurationView ManualInitialHold,
    HoldPolicyDurationView ManualReviewHold,
    IReadOnlyList<PaymentMethodHoldView> Methods,
    ReservationPolicyEditorContract ReservationCycle);
