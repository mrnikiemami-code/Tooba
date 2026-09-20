using System.Globalization;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts;

namespace Tooba.Offer.Endpoints.Seller;

/// <summary>محلی‌سازی عنوان خطا در مرز Endpoint Offer.</summary>
internal static class OfferEndpointLocalizer
{
    public static string Title(SemanticError error, string? acceptLanguage)
    {
        ArgumentNullException.ThrowIfNull(error);
        var en = !string.IsNullOrWhiteSpace(acceptLanguage)
            && acceptLanguage.Contains("en", StringComparison.OrdinalIgnoreCase)
            && !acceptLanguage.TrimStart().StartsWith("fa", StringComparison.OrdinalIgnoreCase);
        return error.Code switch
        {
            OfferErrorCodes.MinQuantityInvalid => en
                ? "Minimum order quantity must be greater than zero."
                : "حداقل مقدار خرید باید بزرگ‌تر از صفر باشد.",
            OfferErrorCodes.MaxQuantityInvalid => en
                ? "Maximum order quantity must be greater than zero."
                : "حداکثر مقدار خرید باید بزرگ‌تر از صفر باشد.",
            OfferErrorCodes.MinQuantityExceedsMax => en
                ? "Minimum order quantity cannot exceed maximum."
                : "حداقل مقدار خرید نمی‌تواند از حداکثر بیشتر باشد.",
            OfferErrorCodes.ArchivedCannotActivate => en
                ? "An archived offer cannot be reactivated; create a new listing."
                : "پیشنهاد بایگانی‌شده دوباره فعال نمی‌شود؛ listing جدید بسازید.",
            OfferErrorCodes.CatalogVariantMissing => en
                ? "Catalog variant was not found."
                : "گونهٔ Catalog پیدا نشد.",
            OfferErrorCodes.SellerMissing => en
                ? "Seller was not found."
                : "فروشنده پیدا نشد.",
            OfferErrorCodes.SellerNotOrganization => en
                ? "Seller must be an Organization party."
                : "فروشنده باید Organization باشد.",
            OfferErrorCodes.DuplicateActiveListing => en
                ? "An active offer already exists for this seller, variant, and channel."
                : "برای این فروشنده و گونه و کانال یک پیشنهاد فعال وجود دارد.",
            OfferErrorCodes.DuplicateSellerSku => en
                ? "Seller SKU is already used by this seller."
                : "SKU فروشنده داخل همین فروشنده تکراری است.",
            OfferErrorCodes.ReturnPolicyOverrideDenied => en
                ? "Seller cannot override return policy."
                : "تغییر سیاست مرجوعی برای فروشنده مجاز نیست.",
            OfferErrorCodes.NonReturnableDenied => en
                ? "Non-returnable offers are not allowed."
                : "ثبت پیشنهاد غیرقابل مرجوعی مجاز نیست.",
            OfferErrorCodes.CustomReturnWindowRequired => en
                ? "Custom return window days are required."
                : "مهلت اختصاصی مرجوعی الزامی است.",
            OfferErrorCodes.CustomReturnWindowOutOfRange => en
                ? $"Return window must be between {Arg(error, "min")} and {Arg(error, "max")} days."
                : $"مهلت مرجوعی باید بین {Arg(error, "min")} و {Arg(error, "max")} روز باشد.",
            _ => en ? "Offer request rejected." : "درخواست پیشنهاد رد شد.",
        };
    }

    private static string Arg(SemanticError error, string key) =>
        error.Arguments.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : "?";
}
