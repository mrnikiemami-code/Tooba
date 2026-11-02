using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Carriers.Ports;

/// <summary>درز مالک‌محور حساب/مرجع/نگاشت/اتصال CarrierApi.</summary>
public interface IShippingCarrierAccountDirectory
{
    Task<Result<IReadOnlyList<ShippingCarrierAccountView>>> ListAccountsAsync(
        ShippingAuthoringOwnerScope owner,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierAccountView>> GetAccountAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierAccountView>> UpsertAccountAsync(
        ShippingAuthoringOwnerScope owner,
        Guid? accountId,
        ShippingCarrierAccountUpsertRequest request,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierConnectionTestOutcome>> TestConnectionAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> SyncReferencesAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> ListReferencesAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>> ListDestinationMappingsAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierDestinationMappingView>> UpsertDestinationMappingAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        ShippingCarrierDestinationMappingUpsertRequest request,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierMethodBindingView?>> GetMethodBindingAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        CancellationToken cancellationToken);

    Task<Result<ShippingCarrierMethodBindingView>> ReplaceMethodBindingAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        ShippingCarrierMethodBindingReplaceRequest request,
        CancellationToken cancellationToken);

    Task<ShippingCarrierAccountExecutionContext?> TryResolveExecutionContextAsync(
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<string?> TryResolveDestinationExternalCodeAsync(
        Guid accountId,
        Guid geographyCityId,
        CancellationToken cancellationToken);

    /// <summary>
    /// بارگذاری اتصال CarrierApi یک پیش‌نویس همراه با زمینهٔ اجرای حساب برای قیمت‌گیری capture‌شده.
    /// مالکیت باید قبلاً با snapshot ثابت شده باشد.
    /// </summary>
    Task<CarrierBindingExecution?> GetMethodBindingForDraftAsync(
        Guid draftId,
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        CancellationToken cancellationToken);
}

/// <summary>اتصال CarrierApi به‌همراه زمینهٔ اجرای حساب برای Quote.</summary>
public sealed record CarrierBindingExecution(
    Guid BindingId,
    Guid AccountId,
    string ProviderCode,
    string ProviderServiceId,
    string OriginExternalCityCode,
    decimal DefaultLengthCentimeters,
    decimal DefaultWidthCentimeters,
    decimal DefaultHeightCentimeters,
    bool IsActiveAccount,
    CarrierAccountExecution Execution);

/// <summary>زمینهٔ اجرای حساب برای adapter؛ شامل راز و نسخهٔ پیکربندی.</summary>
public sealed record ShippingCarrierAccountExecutionContext(
    Guid AccountId,
    string ProviderCode,
    string DisplayName,
    bool IsActive,
    int ConfigurationVersion,
    string? ApiBaseUrl,
    string? TokenUrl,
    string CurrencyCode,
    string Username,
    string Password,
    ShippingCarrierMethodBindingView? MethodBinding);
