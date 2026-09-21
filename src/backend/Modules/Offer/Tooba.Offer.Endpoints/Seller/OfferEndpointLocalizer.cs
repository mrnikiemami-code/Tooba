using System.Globalization;
using Tooba.BuildingBlocks.Localization;
using Tooba.Offer.Contracts;

namespace Tooba.Offer.Endpoints.Seller;

/// <summary>
/// مشارکت‌کنندهٔ محلی‌سازی عنوان خطای Offer — بدون Accept-Language ad-hoc.
/// Residual R2/R3: move catalog into Localization SSOT resources when available.
/// </summary>
public sealed class OfferErrorMessageContributor : IErrorMessageContributor
{
    /// <inheritdoc />
    public bool TryLocalize(
        string localizationKey,
        CultureInfo culture,
        IReadOnlyDictionary<string, string?> arguments,
        out string title)
    {
        if (!localizationKey.StartsWith("offer.", StringComparison.OrdinalIgnoreCase))
        {
            title = string.Empty;
            return false;
        }

        title = OfferEndpointLocalizer.Title(
            new Tooba.BuildingBlocks.SemanticError(localizationKey, arguments),
            culture);
        return true;
    }
}

/// <summary>محلی‌سازی عنوان خطا در مرز Endpoint Offer بر پایهٔ CultureInfo مرکزی.</summary>
internal static class OfferEndpointLocalizer
{
    /// <summary>عنوان محلی‌سازی‌شده برای خطای معنایی Offer.</summary>
    public static string Title(Tooba.BuildingBlocks.SemanticError error, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(culture);
        var en = IsEnglishPreferred(culture);
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

    private static bool IsEnglishPreferred(CultureInfo culture)
    {
        var name = culture.Name;
        if (name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Non-en (including fa and unlimited other locales) use Persian catalog for Offer R1 residual.
        // Central locale resolver still picks the culture; catalog coverage expands in R2/R3.
        return false;
    }

    private static string Arg(Tooba.BuildingBlocks.SemanticError error, string key) =>
        error.Arguments.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : "?";
}
