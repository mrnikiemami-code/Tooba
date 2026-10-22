using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// درز مجوزدهی مالک نویسندگی Shipping. هر مسیر با مجوزدهندهٔ متناظر خودش شروع می‌شود و scope
/// <b>سرورساخته</b> را برمی‌گرداند؛ هیچ مالک/حالت/Tenant/فروشنده‌ای از transport خوانده نمی‌شود.
/// </summary>
/// <remarks>
/// متدها <c>permissionId</c> می‌گیرند تا نوشتن‌های آینده بتوانند جداگانه
/// <c>fulfillment.manage</c> بخواهند؛ این تسک فقط خواندن را expose می‌کند و مجوز خواندن، مجوز
/// نوشتن نیست.
/// </remarks>
public interface IShippingAuthoringOwnerAuthorizer
{
    /// <summary>
    /// مسیر ادمین: همان <see cref="IShippingAdminAuthorizer"/> و همان مجوز موجود، سپس scope مالک
    /// نصب (Store در SingleStore با Tenant پایدار، Marketplace بدون Tenant).
    /// </summary>
    /// <param name="context">زمینهٔ HTTP همین درخواست.</param>
    /// <param name="permissionId">شناسهٔ مجوز موجود پلتفرم.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<ShippingAuthoringOwnerScope> RequireAdminOwnerAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// مسیر فروشنده: ابتدا نصب Marketplace و زمینهٔ consistent، سپس مجوز پنل فروشنده برای همین
    /// actor و SellerPartyId، سپس مجوز مؤثر همان عملیات در سطح کل مالک.
    /// </summary>
    /// <param name="context">زمینهٔ HTTP همین درخواست.</param>
    /// <param name="permissionId">شناسهٔ مجوز موجود پلتفرم برای همین عملیات.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<ShippingAuthoringOwnerScope> RequireSellerOwnerAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken);
}

/// <summary>
/// پیاده‌سازی مالک‌محور روی درزهای موجود پلتفرم. هیچ سیاست تازه‌ای در پلتفرم ساخته نمی‌شود و هیچ
/// مجوزی اضافه/تغییر نام نمی‌دهد.
/// </summary>
/// <remarks>
/// <para>
/// منبع حقیقت: <see cref="IShippingAdminAuthorizer"/> برای ادمین،
/// <see cref="ISellerPanelAccess"/> برای actor→SellerPartyId، و
/// <see cref="IPlatformEffectiveAccessReader"/> برای grant مؤثر همان actor در همان مالک.
/// </para>
/// <para>
/// فقط grant <see cref="PlatformAccessScopeKind.GlobalWithinOwner"/> و بدون
/// <see cref="PlatformPermissionGrant.DeniedByCeiling"/> مجوز کل مالک است؛ grant محدود به
/// دسته/محصول/برند/انبار یا ردشده با سقف هرگز «مجوز کل مالک» نمی‌شود. هدر فروشنده فقط کاندید
/// ورودی درز پلتفرم است و خودش مجوز نیست.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringOwnerAuthorizer(
    IShippingAdminAuthorizer adminAuthorizer,
    ISellerPanelAccess sellerAccess,
    IPlatformEffectiveAccessReader effectiveAccess,
    ICurrentEdition edition,
    ICurrentTenant tenant,
    IModuleCallTracer moduleCallTracer) : IShippingAuthoringOwnerAuthorizer
{
    private const string SourceModule = "Shipping";
    private const string AccessControlModule = "AccessControl";
    private const string EffectivePermissionsOperation = "GetEffectivePermissions";

    /// <inheritdoc />
    public async Task<ShippingAuthoringOwnerScope> RequireAdminOwnerAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // همان گیت ادمین و همان مجوز موجود؛ رد/نبود مجوز پیش از هر خواندن business بالا می‌آید.
        await adminAuthorizer.RequireAuthorizedAsync(context, permissionId, cancellationToken);

        // همان زمینهٔ trusted همین درخواست؛ این بار برای شکل scope، نه یک مرجع حالت دوم.
        var installation = ShippingInstallationContext.Resolve(edition, tenant);
        return installation.Edition == ToobaEdition.SingleStore
            ? ShippingAuthoringOwnerScope.ForStore(installation.TenantId!)
            : ShippingAuthoringOwnerScope.ForMarketplace();
    }

    /// <inheritdoc />
    public async Task<ShippingAuthoringOwnerScope> RequireSellerOwnerAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1) نصب/زمینه: فروشنده فقط در Marketplace معنا دارد. SingleStore پیش از هر resolve فروشنده
        //    و پیش از هر ISender با ناتوانی قابلیت 404 بسته می‌شود؛ Marketplace جعل نمی‌شود.
        var installation = ShippingInstallationContext.Resolve(edition, tenant);
        if (installation.Edition != ToobaEdition.Marketplace)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringOwnerScopeUnavailable));
        }

        // 2) actor و SellerPartyId مجاز از درز canonical پلتفرم؛ هدر فقط کاندید است و اینجا
        //    هیچ هدری parse یا به پاسخ کپی نمی‌شود.
        var (actorUserId, sellerPartyId) =
            await sellerAccess.RequireAuthorizedAsync(context.Request, cancellationToken);

        // 3) grant مؤثر همان actor در همان مالک؛ این خواندن ماژول خارجی با span canonical پوشیده
        //    می‌شود و هیچ شناسهٔ actor/فروشنده در tag نمی‌رود.
        IReadOnlyList<PlatformPermissionGrant> grants;
        using (var moduleCall = moduleCallTracer.Begin(
                   SourceModule,
                   AccessControlModule,
                   EffectivePermissionsOperation))
        {
            try
            {
                grants = await effectiveAccess.GetEffectivePermissionsAsync(
                    actorUserId,
                    PlatformAccessOwnerKind.Seller,
                    sellerPartyId,
                    cancellationToken);
                moduleCall.SetOk();
            }
            catch (Exception exception)
            {
                moduleCall.SetError(exception);
                throw;
            }
        }

        var permitted = grants.Any(grant =>
            string.Equals(grant.PermissionId, permissionId, StringComparison.Ordinal)
            && !grant.DeniedByCeiling
            && grant.ScopeKind == PlatformAccessScopeKind.GlobalWithinOwner);
        if (!permitted)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringOwnerScopeDenied));
        }

        return ShippingAuthoringOwnerScope.ForSeller(sellerPartyId);
    }
}
