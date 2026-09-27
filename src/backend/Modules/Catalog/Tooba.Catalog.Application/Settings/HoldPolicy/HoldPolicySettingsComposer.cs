using Tooba.Cart.Contracts.Lifetime;
using Tooba.Catalog.Application.Settings.HoldPolicy.Models;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Order.Contracts.Reservation;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Catalog.Application.Settings.HoldPolicy;

/// <summary>Composes hold-policy Admin view from Catalog/Payment/Cart/Order Contracts ports.</summary>
internal static class HoldPolicySettingsComposer
{
    /// <summary>Builds the aggregate view (JSON shape parity with Host HoldPolicySettingsView).</summary>
    public static async Task<HoldPolicySettingsView> BuildAsync(
        IStoreHoldPolicySettingsPort storeHours,
        IPaymentHoldSettingsGateway paymentHolds,
        IReservationCyclePolicyPreviewPort reservationPreview,
        ICartPersistenceHoursSource cartPersistence,
        CancellationToken cancellationToken)
    {
        var store = await storeHours.GetHoursAsync(cancellationToken);
        var methods = await paymentHolds.ListMethodOverridesAsync(cancellationToken);
        var reservation = await reservationPreview.PreviewStoreEditorAsync(cancellationToken);
        var platformCart = await cartPersistence.ResolvePersistenceHoursAsync(cancellationToken);
        var platform = paymentHolds.GetPlatformDefaults();
        return new HoldPolicySettingsView(
            Duration(
                store.CartPersistenceHours,
                store.CartPersistenceHours ?? platformCart,
                store.CartPersistenceHours is null ? "platform" : "store",
                "مدت نگهداری سبد خرید",
                "Cart persistence",
                "فقط وضعیت سبد را نگه می‌دارد و موجودی را رزرو نمی‌کند.",
                "Retains cart state only. This does not reserve inventory."),
            Duration(
                store.OnlinePaymentHoldHours,
                store.OnlinePaymentHoldHours ?? platform.OnlinePaymentHoldHours,
                store.OnlinePaymentHoldHours is null ? "platform" : "store",
                "مهلت پرداخت آنلاین",
                "Online payment deadline",
                "پس از ثبت سفارش، مشتری تا این مدت فرصت پرداخت آنلاین دارد.",
                "How long a committed order waits for online payment."),
            Duration(
                store.ManualPaymentInitialHoldHours,
                store.ManualPaymentInitialHoldHours ?? platform.ManualPaymentInitialHoldHours,
                store.ManualPaymentInitialHoldHours is null ? "platform" : "store",
                "مهلت ثبت اطلاعات پرداخت کارت‌به‌کارت",
                "Card-to-card submission deadline",
                "تا این مدت مشتری می‌تواند شماره پیگیری یا مدرک واریز را ثبت کند.",
                "How long a manual-payment order waits for proof."),
            Duration(
                store.ManualPaymentReviewHoldHours,
                store.ManualPaymentReviewHoldHours ?? platform.ManualPaymentReviewHoldHours,
                store.ManualPaymentReviewHoldHours is null ? "platform" : "store",
                "مهلت بررسی پرداخت کارت‌به‌کارت",
                "Card-to-card review hold",
                "پس از ثبت مدرک، موجودی تا بررسی فروشگاه نگه داشته می‌شود.",
                "How long submitted evidence is held for admin review."),
            new[]
            {
                MethodView("fake", "درگاه آنلاین", "Online gateway", methods),
                MethodView("manual", "کارت به کارت", "Card-to-card", methods),
            },
            reservation);
    }

    private static PaymentMethodHoldView MethodView(
        string code,
        string fa,
        string en,
        IReadOnlyList<PaymentMethodHoldOverride> rows)
    {
        var row = rows.FirstOrDefault(x => string.Equals(x.ProviderCode, code, StringComparison.OrdinalIgnoreCase));
        return new PaymentMethodHoldView(
            code,
            fa,
            en,
            row?.OnlinePaymentHoldHours,
            row?.ManualPaymentInitialHoldHours,
            row?.ManualPaymentReviewHoldHours);
    }

    private static HoldPolicyDurationView Duration(
        int? hours,
        int effective,
        string source,
        string fa,
        string en,
        string helperFa,
        string helperEn) =>
        new(hours, effective, source, fa, en, helperFa, helperEn);
}
