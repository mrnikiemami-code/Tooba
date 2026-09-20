namespace Tooba.Offer.Contracts;

/// <summary>کدهای پایدار خطای معنایی ماژول Offer (بدون متن محلی).</summary>
public static class OfferErrorCodes
{
    /// <summary>حداقل مقدار خرید نامعتبر.</summary>
    public const string MinQuantityInvalid = "offer.min_quantity.invalid";

    /// <summary>حداکثر مقدار خرید نامعتبر.</summary>
    public const string MaxQuantityInvalid = "offer.max_quantity.invalid";

    /// <summary>حداقل از حداکثر بیشتر است.</summary>
    public const string MinQuantityExceedsMax = "offer.min_quantity.exceeds_max";

    /// <summary>Offer بایگانی‌شده دوباره فعال نمی‌شود.</summary>
    public const string ArchivedCannotActivate = "offer.archived.cannot_activate";

    /// <summary>Variant Catalog پیدا نشد.</summary>
    public const string CatalogVariantMissing = "offer.catalog_variant.missing";

    /// <summary>فروشنده پیدا نشد.</summary>
    public const string SellerMissing = "offer.seller.missing";

    /// <summary>فروشنده Organization نیست.</summary>
    public const string SellerNotOrganization = "offer.seller.not_organization";

    /// <summary>listing فعال تکراری.</summary>
    public const string DuplicateActiveListing = "offer.listing.duplicate_active";

    /// <summary>SKU فروشنده تکراری.</summary>
    public const string DuplicateSellerSku = "offer.seller_sku.duplicate";

    /// <summary>تغییر سیاست مرجوعی مجاز نیست.</summary>
    public const string ReturnPolicyOverrideDenied = "offer.return_policy.override_denied";

    /// <summary>غیرقابل مرجوعی مجاز نیست.</summary>
    public const string NonReturnableDenied = "offer.return_policy.non_returnable_denied";

    /// <summary>مهلت اختصاصی الزامی است.</summary>
    public const string CustomReturnWindowRequired = "offer.return_policy.custom_window_required";

    /// <summary>مهلت خارج از بازه است.</summary>
    public const string CustomReturnWindowOutOfRange = "offer.return_policy.custom_window_out_of_range";
}
