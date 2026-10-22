using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Endpoints.Errors;

/// <summary>
/// کاتالوگ صریح خطاهای معنایی Shipping — مالک یگانهٔ فضای کلید <c>shipping.*</c>.
/// <para>
/// این کاتالوگ <b>معنای</b> کد را صریح می‌کند (طبقه‌بندی، وضعیت HTTP، کلید منبع، عنوان امن)؛
/// هیچ استنتاجی از پسوند نام کد انجام نمی‌شود.
/// </para>
/// <para>
/// سازگاری HTTP قدیمی حفظ می‌شود: پیش از ثبت، هر کد ثبت‌نشده در
/// <see cref="SafeErrorMapper"/> به ۴۰۰ / <see cref="ErrorClassification.Business"/> /
/// <see cref="ErrorSeverity.Warning"/> نگاشت می‌شد. بنابراین همان مقدار برای همهٔ کدهای
/// این ماژول ثبت می‌شود؛ تبدیل خودکار <c>*.not_found</c> به ۴۰۴ یا <c>*duplicate*</c> به ۴۰۹
/// در این برش مجاز نیست و باید در تسک جداگانه تصمیم‌گیری شود.
/// </para>
/// <para>
/// کدهای مشترک پلتفرم (<c>admin.*</c>, <c>validation.*</c>, <c>platform.*</c>) مالکیت
/// <c>FoundationErrorCatalogContributor</c> هستند و اینجا ثبت نمی‌شوند.
/// </para>
/// </summary>
public sealed class ShippingErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(ShippingErrorCodes.ServiceCodeRequired, "A shipping service code is required."),
        D(ShippingErrorCodes.ServiceCodeInvalid, "The shipping service code is invalid."),
        D(ShippingErrorCodes.DestinationCountryIdRequired, "A destination country is required."),
        D(ShippingErrorCodes.ChargeAmountNegative, "The shipping charge amount cannot be negative."),
        D(ShippingErrorCodes.ChargeAmountOutOfRange, "The shipping charge amount is out of range."),
        D(ShippingErrorCodes.CurrencyCodeInvalid, "The shipping currency code is invalid."),
        D(ShippingErrorCodes.PreparationDaysNegative, "Preparation days cannot be negative."),
        D(ShippingErrorCodes.LeadDaysNegative, "Lead days cannot be negative."),
        D(ShippingErrorCodes.FreeAboveThresholdNegative, "The free-shipping threshold cannot be negative."),
        D(ShippingErrorCodes.CoverageScopeInvalid, "The shipping coverage scope is invalid."),
        D(ShippingErrorCodes.RatePolicyNotFound, "The shipping rate policy was not found."),
        D(ShippingErrorCodes.CoverageGrantNotFound, "The shipping coverage grant was not found."),
        D(ShippingErrorCodes.RatePolicyScopeDuplicate, "A shipping rate policy already exists for this scope."),
        D(ShippingErrorCodes.CoverageGrantScopeDuplicate, "A shipping coverage grant already exists for this scope."),
        D(ShippingErrorCodes.ServiceCodeUnknown, "The shipping service code is unknown."),
        D(ShippingErrorCodes.ServiceScopeMismatch, "The shipping service scope does not match the request."),
        D(ShippingErrorCodes.DestinationScopeInvalid, "The shipping destination scope is invalid."),
        D(ShippingErrorCodes.OptionCodeInvalid, "The shipping option code is invalid."),
        D(ShippingErrorCodes.ServiceOptionUnknown, "The shipping service option is unknown."),
        D(ShippingErrorCodes.RatePolicyOptionMismatch, "The shipping rate policy does not match the requested method."),
        D(ShippingErrorCodes.ScheduleTimeZoneRequired, "A schedule time zone is required."),
        D(ShippingErrorCodes.ScheduleTimeZoneInvalid, "The schedule time zone cannot be resolved on this runtime."),
        D(ShippingErrorCodes.ScheduleWeekdayInvalid, "The schedule weekday is invalid."),
        D(ShippingErrorCodes.ScheduleWindowRangeInvalid, "The operating window range is invalid."),
        D(ShippingErrorCodes.ScheduleWindowOverlap, "Operating windows overlap on the same weekday."),
        D(ShippingErrorCodes.ScheduleWindowCapacityInvalid, "The operating window capacity must be positive."),
        D(ShippingErrorCodes.ScheduleWindowsTooMany, "Too many operating windows were provided."),
        D(ShippingErrorCodes.ScheduleNotFound, "The shipping schedule was not found."),
        D(ShippingErrorCodes.ScheduleDuplicate, "A shipping schedule already exists for this method."),
        D(ShippingErrorCodes.ScheduleOptionMismatch, "The shipping schedule does not match the requested method."),
        D(ShippingErrorCodes.WeightTierInvalid, "The weight tier range is invalid."),
        D(ShippingErrorCodes.WeightTierOutOfRange, "The weight tier is out of range."),
        D(ShippingErrorCodes.CountTierInvalid, "The item-count tier range is invalid."),
        D(ShippingErrorCodes.CountTierOutOfRange, "The item-count tier is out of range."),
        D(ShippingErrorCodes.RatePolicyTierOverlap, "Active shipping rate tiers overlap."),
        D(ShippingErrorCodes.RatePolicyConditionNotFound, "The shipping rate policy condition was not found."),
        D(ShippingErrorCodes.PackagingFeeNegative, "The packaging fee cannot be negative."),
        D(ShippingErrorCodes.PackagingFeeOutOfRange, "The packaging fee is out of range."),
        D(ShippingErrorCodes.PackagingCurrencyInvalid, "The packaging currency code is invalid."),
        D(ShippingErrorCodes.PackagingPolicyNotFound, "The packaging policy was not found."),
        D(ShippingErrorCodes.PackagingPolicyScopeDuplicate, "A packaging policy already exists for this scope."),
        D(ShippingErrorCodes.PackagingPolicyOptionMismatch, "The packaging policy does not match the requested method."),
        D(ShippingErrorCodes.PackagingPolicyAmbiguous, "Multiple active packaging policies make this method ambiguous."),
        D(ShippingErrorCodes.PackagingFreightPayerModeInvalid, "The freight payer mode is invalid."),
        // TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188: the ONE explicitly classified availability
        // code of this module. Every legacy shipping.* code above keeps its historical
        // 400/Business/Warning contract unchanged; this one is a consumer-readiness failure and is
        // declared Platform/503/Error instead of being inferred from its name.
        new(
            Code: ShippingErrorCodes.AuthoringConfigurationUnavailable,
            Classification: ErrorClassification.Platform,
            HttpStatus: StatusCodes.Status503ServiceUnavailable,
            LocalizationKey: ShippingErrorCodes.AuthoringConfigurationUnavailable,
            Severity: ErrorSeverity.Error,
            SafeTitleFallback: "Shipping authoring configuration is unavailable."),
        // TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189: the owner-scope gate is a Shipping policy
        // failure, not a Foundation panel/party gate. 403 says "you are not permitted this owner
        // scope at whole-owner level"; 404 says "this owner scope does not exist in this
        // installation". Neither reveals whether a seller or owner row exists.
        new(
            Code: ShippingErrorCodes.AuthoringOwnerScopeDenied,
            Classification: ErrorClassification.Forbidden,
            HttpStatus: StatusCodes.Status403Forbidden,
            LocalizationKey: ShippingErrorCodes.AuthoringOwnerScopeDenied,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "You are not permitted to author shipping rules for this owner scope."),
        new(
            Code: ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
            Classification: ErrorClassification.NotFound,
            HttpStatus: StatusCodes.Status404NotFound,
            LocalizationKey: ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Shipping rule ownership is unavailable in this installation."),
        new(
            Code: ShippingErrorCodes.AuthoringDraftNotFound,
            Classification: ErrorClassification.NotFound,
            HttpStatus: StatusCodes.Status404NotFound,
            LocalizationKey: ShippingErrorCodes.AuthoringDraftNotFound,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "The shipping authoring draft was not found."),
        new(
            Code: ShippingErrorCodes.AuthoringDraftRevisionConflict,
            Classification: ErrorClassification.Conflict,
            HttpStatus: StatusCodes.Status409Conflict,
            LocalizationKey: ShippingErrorCodes.AuthoringDraftRevisionConflict,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "The shipping authoring draft has changed; reload it and try again."),
        D(ShippingErrorCodes.AuthoringDraftBindingDuplicate, "A draft already exists for this owner and shipping method."),
        D(ShippingErrorCodes.AuthoringRegionSelectionOverlap, "The selected destinations overlap or repeat."),
        D(ShippingErrorCodes.AuthoringRegionPayloadInvalid, "The draft region tree is not a valid configuration."),
    ];

    private static ErrorDescriptor D(string code, string fallback) =>
        new(
            Code: code,
            Classification: ErrorClassification.Business,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
