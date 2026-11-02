namespace Tooba.Shipping.Contracts.Carriers;

/// <summary>وضعیت پایدار عملیات حاملی؛ جدا از شاهد مالی Order.</summary>
public enum ShippingCarrierShipmentStateView
{
    /// <summary>ثبت شده و در انتظار پاسخ provider.</summary>
    Pending = 0,

    /// <summary>provider پاسخ موفق داده است.</summary>
    Succeeded = 1,

    /// <summary>provider رد کرده یا درخواست نامعتبر بوده است.</summary>
    Failed = 2,

    /// <summary>نتیجه مبهم است و بدون تصمیم اپراتور تکرار نمی‌شود.</summary>
    Unknown = 3
}

/// <summary>گونهٔ عملیات حاملی پایدار.</summary>
public enum ShippingCarrierShipmentKindView
{
    /// <summary>ایجاد محموله.</summary>
    Create = 0,

    /// <summary>رهگیری محموله.</summary>
    Track = 1,

    /// <summary>لغو محموله.</summary>
    Cancel = 2
}

/// <summary>نمایهٔ عملیات حاملی بدون راز و بدون پول سفارش.</summary>
/// <param name="OperationId">شناسهٔ پایدار عملیات.</param>
/// <param name="OperationKey">کلید پایدار برای replay.</param>
/// <param name="Kind">گونهٔ عملیات.</param>
/// <param name="State">وضعیت پایدار.</param>
/// <param name="CheckoutId">سفارش متعهد.</param>
/// <param name="ShippingGroupId">گروه ارسال متعهد.</param>
/// <param name="ProviderCode">کد provider خنثی.</param>
/// <param name="AccountId">حساب حامل مالک.</param>
/// <param name="ConfigurationVersion">نسخهٔ پیکربندی حساب در زمان عملیات.</param>
/// <param name="ProviderTrackingCode">کد رهگیری صادرشده یا تهی.</param>
/// <param name="ProviderOrderId">شناسهٔ سفارش provider یا تهی.</param>
/// <param name="ProviderStatus">وضعیت خام provider یا تهی.</param>
/// <param name="LastErrorCode">آخرین کد خطای پایدار یا تهی.</param>
/// <param name="CanCreate">آیا adapter ایجاد را پشتیبانی می‌کند.</param>
/// <param name="CanTrack">آیا adapter رهگیری را پشتیبانی می‌کند.</param>
/// <param name="CanCancel">آیا adapter لغو را پشتیبانی می‌کند.</param>
/// <param name="CanLabel">آیا adapter برچسب را پشتیبانی می‌کند.</param>
/// <param name="AttemptedAt">زمان تلاش.</param>
/// <param name="CompletedAt">زمان اتمام یا تهی.</param>
public sealed record ShippingCarrierShipmentOperationView(
    Guid OperationId,
    string OperationKey,
    ShippingCarrierShipmentKindView Kind,
    ShippingCarrierShipmentStateView State,
    Guid CheckoutId,
    Guid ShippingGroupId,
    string ProviderCode,
    Guid AccountId,
    int ConfigurationVersion,
    string? ProviderTrackingCode,
    string? ProviderOrderId,
    string? ProviderStatus,
    string? LastErrorCode,
    bool CanCreate,
    bool CanTrack,
    bool CanCancel,
    bool CanLabel,
    DateTimeOffset AttemptedAt,
    DateTimeOffset? CompletedAt);

/// <summary>طرف فرستنده یا گیرنده برای ایجاد محموله.</summary>
/// <param name="FullName">نام کامل.</param>
/// <param name="Mobile">شمارهٔ تماس.</param>
/// <param name="AddressLine">نشانی.</param>
/// <param name="LocationCode">کد شهر provider.</param>
/// <param name="PostalCode">کدپستی یا تهی.</param>
public sealed record ShippingCarrierShipmentPartyInput(
    string FullName,
    string Mobile,
    string AddressLine,
    string LocationCode,
    string? PostalCode = null);

/// <summary>بستهٔ فیزیکی برای ایجاد محموله؛ مبلغ collect جدا از کرایهٔ تسویه است.</summary>
/// <param name="WeightGrams">وزن به گرم.</param>
/// <param name="DeclaredValue">ارزش اظهارشده.</param>
/// <param name="LengthCentimeters">طول.</param>
/// <param name="WidthCentimeters">عرض.</param>
/// <param name="HeightCentimeters">ارتفاع.</param>
/// <param name="CollectAmount">مبلغ جمع‌آوری در محل؛ جدا از مبلغ تسویه.</param>
public sealed record ShippingCarrierShipmentPackageInput(
    decimal WeightGrams,
    decimal DeclaredValue,
    decimal LengthCentimeters,
    decimal WidthCentimeters,
    decimal HeightCentimeters,
    decimal CollectAmount);

/// <summary>ایجاد محموله فقط پس از سفارش متعهد و با فکت‌های پذیرفته‌شده.</summary>
/// <param name="CheckoutId">سفارش متعهد.</param>
/// <param name="ShippingGroupId">گروه ارسال متعهد.</param>
/// <param name="CarrierAccountId">حساب حامل؛ با شاهد سفارش overlay می‌شود.</param>
/// <param name="ProviderServiceId">کد خدمت provider.</param>
/// <param name="Sender">فرستنده.</param>
/// <param name="Recipient">گیرنده؛ فیلدهای خالی از Order پر می‌شوند.</param>
/// <param name="Package">بسته؛ CollectAmount از شاهد سفارش overlay می‌شود.</param>
public sealed record ShippingCarrierShipmentCreateRequest(
    Guid CheckoutId,
    Guid ShippingGroupId,
    Guid CarrierAccountId,
    string ProviderServiceId,
    ShippingCarrierShipmentPartyInput Sender,
    ShippingCarrierShipmentPartyInput Recipient,
    ShippingCarrierShipmentPackageInput Package);
