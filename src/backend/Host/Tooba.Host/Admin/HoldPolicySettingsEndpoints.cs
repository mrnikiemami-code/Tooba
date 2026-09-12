using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

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
    IReadOnlyList<PaymentMethodHoldView> Methods);

/// <summary>بدنهٔ ذخیره.</summary>
public sealed record HoldPolicySettingsWriteRequest(
    int? CartPersistenceHours,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours,
    IReadOnlyList<PaymentMethodHoldView>? Methods);

/// <summary>تنظیم مهلت سبد/پرداخت روی معماری Settings موجود.</summary>
public static class HoldPolicySettingsEndpoints
{
    /// <summary>مسیرهای Admin تنظیم مهلت را ثبت می‌کند.</summary>
    public static void MapHoldPolicySettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/settings/hold-policy");
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        CatalogDbContext catalog,
        PaymentDbContext payments,
        Microsoft.Extensions.Options.IOptions<PaymentGatewayOptions> gateway,
        Microsoft.Extensions.Options.IOptions<CartLifetimeOptions> cart,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await BuildViewAsync(catalog, payments, gateway.Value, cart.Value, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutAsync(
        HoldPolicySettingsWriteRequest body,
        CatalogDbContext catalog,
        PaymentDbContext payments,
        Microsoft.Extensions.Options.IOptions<PaymentGatewayOptions> gateway,
        Microsoft.Extensions.Options.IOptions<CartLifetimeOptions> cart,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            ValidateHours(body.CartPersistenceHours, 1, 24 * 90, "cart.persistence.invalid");
            ValidateHours(body.OnlinePaymentHoldHours, 1, 24 * 30, "hold.online.invalid");
            ValidateHours(body.ManualPaymentInitialHoldHours, 1, 24 * 30, "hold.manual_initial.invalid");
            ValidateHours(body.ManualPaymentReviewHoldHours, 1, 24 * 30, "hold.manual_review.invalid");
            var now = DateTimeOffset.UtcNow;
            var store = await catalog.StoreHoldPolicySettings
                .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
            if (store is null)
            {
                store = StoreHoldPolicySettings.CreateDefault(now);
                catalog.StoreHoldPolicySettings.Add(store);
            }

            store.Replace(
                body.CartPersistenceHours,
                body.OnlinePaymentHoldHours,
                body.ManualPaymentInitialHoldHours,
                body.ManualPaymentReviewHoldHours,
                now);

            foreach (var method in body.Methods ?? [])
            {
                ValidateHours(method.OnlinePaymentHoldHours, 1, 24 * 30, "hold.method.invalid");
                ValidateHours(method.ManualPaymentInitialHoldHours, 1, 24 * 30, "hold.method.invalid");
                ValidateHours(method.ManualPaymentReviewHoldHours, 1, 24 * 30, "hold.method.invalid");
                var code = (method.ProviderCode ?? string.Empty).Trim().ToLowerInvariant();
                if (code.Length == 0)
                {
                    continue;
                }

                var row = await payments.MethodHoldOverrides
                    .SingleOrDefaultAsync(x => x.ProviderCode == code, cancellationToken);
                if (row is null)
                {
                    row = PaymentMethodHoldOverride.Create(code, now);
                    payments.MethodHoldOverrides.Add(row);
                }

                row.Replace(
                    method.OnlinePaymentHoldHours,
                    method.ManualPaymentInitialHoldHours,
                    method.ManualPaymentReviewHoldHours,
                    now);
            }

            await catalog.SaveChangesAsync(cancellationToken);
            await payments.SaveChangesAsync(cancellationToken);
            return Results.Json(await BuildViewAsync(catalog, payments, gateway.Value, cart.Value, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<HoldPolicySettingsView> BuildViewAsync(
        CatalogDbContext catalog,
        PaymentDbContext payments,
        PaymentGatewayOptions gateway,
        CartLifetimeOptions cart,
        CancellationToken cancellationToken)
    {
        var store = await catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
        var methods = await payments.MethodHoldOverrides.AsNoTracking().ToListAsync(cancellationToken);
        var platformCart = Math.Clamp(cart.PersistenceHours <= 0 ? 168 : cart.PersistenceHours, 1, 24 * 90);
        var platformOnline = Math.Clamp(gateway.OnlinePaymentHoldHours <= 0 ? 2 : gateway.OnlinePaymentHoldHours, 1, 24 * 30);
        var platformManual = Math.Clamp(gateway.ManualPaymentInitialHoldHours <= 0 ? 2 : gateway.ManualPaymentInitialHoldHours, 1, 24 * 30);
        var platformReview = Math.Clamp(gateway.ManualPaymentReviewHoldHours <= 0 ? 24 : gateway.ManualPaymentReviewHoldHours, 1, 24 * 30);
        return new HoldPolicySettingsView(
            Duration(
                store?.CartPersistenceHours,
                store?.CartPersistenceHours ?? platformCart,
                store?.CartPersistenceHours is null ? "platform" : "store",
                "مدت نگهداری سبد خرید",
                "Cart persistence",
                "فقط وضعیت سبد را نگه می‌دارد و موجودی را رزرو نمی‌کند.",
                "Retains cart state only. This does not reserve inventory."),
            Duration(
                store?.OnlinePaymentHoldHours,
                store?.OnlinePaymentHoldHours ?? platformOnline,
                store?.OnlinePaymentHoldHours is null ? "platform" : "store",
                "مهلت پرداخت آنلاین",
                "Online payment deadline",
                "پس از ثبت سفارش، مشتری تا این مدت فرصت پرداخت آنلاین دارد.",
                "How long a committed order waits for online payment."),
            Duration(
                store?.ManualPaymentInitialHoldHours,
                store?.ManualPaymentInitialHoldHours ?? platformManual,
                store?.ManualPaymentInitialHoldHours is null ? "platform" : "store",
                "مهلت ثبت اطلاعات پرداخت کارت‌به‌کارت",
                "Card-to-card submission deadline",
                "تا این مدت مشتری می‌تواند شماره پیگیری یا مدرک واریز را ثبت کند.",
                "How long a manual-payment order waits for proof."),
            Duration(
                store?.ManualPaymentReviewHoldHours,
                store?.ManualPaymentReviewHoldHours ?? platformReview,
                store?.ManualPaymentReviewHoldHours is null ? "platform" : "store",
                "مهلت بررسی پرداخت کارت‌به‌کارت",
                "Card-to-card review hold",
                "پس از ثبت مدرک، موجودی تا بررسی فروشگاه نگه داشته می‌شود.",
                "How long submitted evidence is held for admin review."),
            new[]
            {
                MethodView("fake", "درگاه آنلاین", "Online gateway", methods),
                MethodView("manual", "کارت به کارت", "Card-to-card", methods),
            });
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

    private static void ValidateHours(int? value, int min, int max, string error)
    {
        if (value is null)
        {
            return;
        }

        if (value < min || value > max)
        {
            throw new PlatformHttpException(400, "مقدار مهلت معتبر نیست.", error);
        }
    }
}
