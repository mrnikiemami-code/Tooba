using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.AddressBook.Application;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Localization.Application;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Host.Storefront;

/// <summary>
/// تصویر یکپارچهٔ مرحلهٔ ارسال فروشگاه: روش‌های Store-enabled، قیمت، حداقل تحویل، پیش‌نویس سبد.
/// </summary>
public sealed class StorefrontShippingComposer
{
    private readonly StorefrontCartComposer _carts;
    private readonly StorefrontCheckoutComposer _checkouts;
    private readonly IAddressBookDirectory _addresses;
    private readonly FulfillmentDbContext _fulfillment;
    private readonly OrderDbContext _orders;
    private readonly ILanguageDirectory _languages;
    private readonly ShippingMethodsOptions _shippingOptions;
    private readonly CurrentAuthenticatedSession _session;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _http;

    /// <summary>سازنده.</summary>
    internal StorefrontShippingComposer(
        StorefrontCartComposer carts,
        StorefrontCheckoutComposer checkouts,
        IAddressBookDirectory addresses,
        FulfillmentDbContext fulfillment,
        OrderDbContext orders,
        ILanguageDirectory languages,
        ShippingMethodsOptions shippingOptions,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IHttpContextAccessor http)
    {
        _carts = carts;
        _checkouts = checkouts;
        _addresses = addresses;
        _fulfillment = fulfillment;
        _orders = orders;
        _languages = languages;
        _shippingOptions = shippingOptions;
        _session = session;
        _environment = environment;
        _http = http;
    }

    /// <summary>تصویر ارسال برای سبد جاری.</summary>
    public async Task<StorefrontShippingProjection> ProjectAsync(
        Guid cartId,
        string? guestSecret,
        string? provinceName,
        string? methodCode,
        string? language,
        CancellationToken cancellationToken)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var sellerIds = cart.Lines.Select(x => x.SellerPartyId).Distinct().ToArray();
        var maxPrep = StorefrontShippingCalculator.MaxSellerPreparationDays(sellerIds, _shippingOptions);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var methods = await LoadEligibleMethodsAsync(cart.SubtotalExclusiveOfTax, provinceName, language, cancellationToken);
        var selectedCode = string.IsNullOrWhiteSpace(methodCode)
            ? null
            : methodCode.Trim().ToLowerInvariant();
        StorefrontShippingMethodView? selected = methods.FirstOrDefault(m => m.MethodCode == selectedCode);
        if (selected is null && selectedCode is not null)
        {
            // روش جعلی/غیرفعال: قیمت و حداقل را از کلاینت قبول نمی‌کنیم.
            selected = null;
        }

        DateOnly? minDate = selected is null
            ? null
            : StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, maxPrep, selected.LeadDays);
        var dates = minDate is DateOnly min
            ? StorefrontShippingCalculator.BuildDeliveryDates(min, _shippingOptions.DeliveryHorizonDays)
                .Select(d => new StorefrontDeliveryDateOption(
                    d.ToString("yyyy-MM-dd"),
                    FormatFaDateLabel(d, today),
                    FormatFaDateSub(d),
                    d == min))
                .ToList()
            : [];
        var timeWindows = StorefrontShippingCalculator.DayTimeWindows
            .Select(t => new StorefrontDeliveryTimeOption(t.Value, t.LabelFa))
            .ToList();

        var draft = await LoadDraftAsync(cart.CartId, guestSecret, cancellationToken);
        var revalidationMessage = (string?)null;
        if (draft is not null)
        {
            var stillValid = methods.Any(m => m.MethodCode == draft.ShippingMethodCode)
                && (string.IsNullOrWhiteSpace(provinceName)
                    || string.Equals(provinceName, draft.ProvinceName, StringComparison.OrdinalIgnoreCase)
                    || StorefrontShippingCalculator.IsDestinationAllowed(
                        StorefrontShippingCalculator.ResolveRate(draft.ShippingMethodCode, _shippingOptions),
                        draft.ProvinceName));
            if (!stillValid)
            {
                revalidationMessage = "انتخاب ارسال دیگر معتبر نیست؛ لطفاً دوباره انتخاب کنید.";
                draft = null;
            }
        }

        return new StorefrontShippingProjection(
            cart.CartId,
            cart.Version,
            cart.Currency,
            cart.ItemCount,
            cart.SubtotalExclusiveOfTax,
            sellerIds.Length,
            maxPrep,
            methods,
            selected?.MethodCode,
            selected?.PriceAmount ?? 0m,
            minDate?.ToString("yyyy-MM-dd"),
            dates,
            timeWindows,
            draft is null ? null : MapDraft(draft),
            revalidationMessage,
            StorefrontIranGeography.Provinces);
    }

    /// <summary>پیش‌نویس ارسال را ذخیره و اعتبارسنجی می‌کند.</summary>
    public async Task<StorefrontShippingDraftView> SaveSelectionAsync(
        StorefrontShippingSelectionRequest request,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var cart = await RequireCartAsync(request.CartId, guestSecret, cancellationToken);
        if (cart.Version != request.ExpectedCartVersion)
        {
            throw new InvalidOperationException("shipping.cart.stale");
        }

        var prepared = await _checkouts.PrepareShippingAsync(
            new StorefrontCheckoutShippingInput(
                request.RecipientName,
                request.ContactMobile,
                request.ProvinceName,
                request.CityName,
                request.PostalAddress,
                request.PostalCode,
                request.SavedAddressId),
            cancellationToken);

        var methods = await LoadEligibleMethodsAsync(
            cart.SubtotalExclusiveOfTax,
            prepared.Shipping.ProvinceName,
            null,
            cancellationToken);
        var method = methods.FirstOrDefault(m =>
            string.Equals(m.MethodCode, request.ShippingMethodCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("shipping.method.unavailable");

        var sellerIds = cart.Lines.Select(x => x.SellerPartyId).Distinct();
        var maxPrep = StorefrontShippingCalculator.MaxSellerPreparationDays(sellerIds, _shippingOptions);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var minimum = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, maxPrep, method.LeadDays);
        if (!DateOnly.TryParse(request.SelectedDeliveryDate, out var selectedDate))
        {
            throw new InvalidOperationException("shipping.delivery.invalid");
        }

        StorefrontShippingCalculator.EnsureDeliveryNotEarlier(selectedDate, minimum);
        var windows = StorefrontShippingCalculator.ValidTimeWindows(selectedDate, minimum);
        if (string.IsNullOrWhiteSpace(request.SelectedDeliveryTimeWindow)
            || windows.All(w => w.Value != request.SelectedDeliveryTimeWindow.Trim()))
        {
            throw new InvalidOperationException("shipping.delivery.slot_unavailable");
        }

        if (request.CustomerNote is { Length: > CartShippingDraft.CustomerNoteMaxLength })
        {
            throw new InvalidOperationException("shipping.note.too_long");
        }

        // کلاینت نمی‌تواند قیمت را جعل کند.
        var amount = method.PriceAmount;
        var now = DateTimeOffset.UtcNow;
        var hash = HashGuestSecret(guestSecret);
        var existing = await _orders.ShippingDrafts.FirstOrDefaultAsync(x => x.CartId == cart.CartId, cancellationToken);
        if (existing is null)
        {
            existing = CartShippingDraft.Create(
                cart.CartId,
                hash,
                cart.Version,
                prepared.Shipping.RecipientName,
                prepared.Shipping.ContactMobile,
                prepared.Shipping.ProvinceName,
                prepared.Shipping.CityName,
                prepared.Shipping.PostalAddress,
                prepared.Shipping.PostalCode,
                prepared.Shipping.SavedAddressId ?? request.SavedAddressId,
                method.MethodCode,
                method.Label,
                amount,
                minimum,
                selectedDate,
                request.SelectedDeliveryTimeWindow,
                request.CustomerNote,
                now);
            _orders.ShippingDrafts.Add(existing);
        }
        else
        {
            EnsureDraftOwnership(existing, guestSecret);
            existing.Replace(
                hash,
                cart.Version,
                prepared.Shipping.RecipientName,
                prepared.Shipping.ContactMobile,
                prepared.Shipping.ProvinceName,
                prepared.Shipping.CityName,
                prepared.Shipping.PostalAddress,
                prepared.Shipping.PostalCode,
                prepared.Shipping.SavedAddressId ?? request.SavedAddressId,
                method.MethodCode,
                method.Label,
                amount,
                minimum,
                selectedDate,
                request.SelectedDeliveryTimeWindow,
                request.CustomerNote,
                now);
        }

        await _orders.SaveChangesAsync(cancellationToken);
        return MapDraft(existing);
    }

    /// <summary>انتخاب معتبر را به Checkout ثبت و برای /payment برمی‌گرداند.</summary>
    public async Task<StorefrontCheckoutPage> CommitAsync(
        Guid cartId,
        string? guestSecret,
        int expectedCartVersion,
        string idempotencyKey,
        string? couponCode,
        CancellationToken cancellationToken)
    {
        var draft = await _orders.ShippingDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CartId == cartId, cancellationToken)
            ?? throw new InvalidOperationException("shipping.selection.required");
        EnsureDraftOwnership(draft, guestSecret);

        // بازاعتبارسنجی قبل از ثبت سفارش.
        await SaveSelectionAsync(
            new StorefrontShippingSelectionRequest(
                cartId,
                expectedCartVersion,
                draft.RecipientName,
                draft.ContactMobile,
                draft.ProvinceName,
                draft.CityName,
                draft.PostalAddress,
                draft.PostalCode,
                draft.SavedAddressId,
                draft.ShippingMethodCode,
                draft.SelectedDeliveryDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                draft.SelectedDeliveryTimeWindow ?? string.Empty,
                draft.CustomerNote),
            guestSecret,
            cancellationToken);

        draft = await _orders.ShippingDrafts.AsNoTracking()
            .FirstAsync(x => x.CartId == cartId, cancellationToken);

        return await _checkouts.SubmitAsync(
            cartId,
            guestSecret,
            expectedCartVersion,
            idempotencyKey,
            new StorefrontCheckoutShippingInput(
                draft.RecipientName,
                draft.ContactMobile,
                draft.ProvinceName,
                draft.CityName,
                draft.PostalAddress,
                draft.PostalCode,
                draft.SavedAddressId),
            couponCode,
            cancellationToken,
            draft.ShippingMethodCode,
            draft.ShippingMethodLabel,
            draft.ShippingAmount,
            draft.MinimumDeliveryDate,
            draft.SelectedDeliveryDate,
            draft.SelectedDeliveryTimeWindow,
            draft.CustomerNote);
    }

    private async Task<IReadOnlyList<StorefrontShippingMethodView>> LoadEligibleMethodsAsync(
        decimal cartSubtotal,
        string? provinceName,
        string? language,
        CancellationToken cancellationToken)
    {
        await ShippingServiceEndpoints.EnsureSeedAsync(_fulfillment, _languages, cancellationToken);
        var enabled = ShippingMethodRegistry.Enabled(_shippingOptions)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var langId = await ResolveLanguageIdAsync(language, cancellationToken);
        var services = await _fulfillment.ShippingServices.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var serviceIds = services.Select(x => x.ShippingServiceId).ToArray();
        var translations = await _fulfillment.ShippingServiceTranslations.AsNoTracking()
            .Where(x => serviceIds.Contains(x.ShippingServiceId))
            .ToListAsync(cancellationToken);
        var options = await _fulfillment.ShippingServiceOptions.AsNoTracking()
            .Where(x => serviceIds.Contains(x.ShippingServiceId) && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var optionIds = options.Select(x => x.ShippingServiceOptionId).ToArray();
        var optionTranslations = await _fulfillment.ShippingServiceOptionTranslations.AsNoTracking()
            .Where(x => optionIds.Contains(x.ShippingServiceOptionId))
            .ToListAsync(cancellationToken);

        var result = new List<StorefrontShippingMethodView>();
        foreach (var service in services)
        {
            if (!enabled.Contains(service.Code))
            {
                continue;
            }

            var serviceName = translations
                .Where(t => t.ShippingServiceId == service.ShippingServiceId)
                .OrderByDescending(t => t.LanguageId == langId)
                .Select(t => t.Name)
                .FirstOrDefault()
                ?? ShippingMethodRegistry.ResolveLabel(service.Code, service.Code);
            var serviceOptions = options.Where(o => o.ShippingServiceId == service.ShippingServiceId).ToList();
            if (serviceOptions.Count == 0)
            {
                TryAddMethod(result, service.Code, serviceName, null, service.IconKey, cartSubtotal, provinceName);
                continue;
            }

            foreach (var option in serviceOptions)
            {
                var optionName = optionTranslations
                    .Where(t => t.ShippingServiceOptionId == option.ShippingServiceOptionId)
                    .OrderByDescending(t => t.LanguageId == langId)
                    .Select(t => t.Name)
                    .FirstOrDefault()
                    ?? option.Code;
                var code = $"{service.Code}:{option.Code}";
                var label = $"{serviceName} — {optionName}";
                TryAddMethod(result, code, label, service.Code, service.IconKey, cartSubtotal, provinceName);
            }
        }

        return result;
    }

    private void TryAddMethod(
        List<StorefrontShippingMethodView> result,
        string methodCode,
        string label,
        string? serviceCode,
        string iconKey,
        decimal cartSubtotal,
        string? provinceName)
    {
        var rate = StorefrontShippingCalculator.ResolveRate(methodCode, _shippingOptions);
        if (!StorefrontShippingCalculator.IsDestinationAllowed(rate, provinceName)
            && !string.IsNullOrWhiteSpace(provinceName))
        {
            return;
        }

        var price = StorefrontShippingCalculator.QuotePrice(rate, cartSubtotal);
        result.Add(new StorefrontShippingMethodView(
            methodCode,
            label,
            serviceCode ?? methodCode.Split(':')[0],
            iconKey,
            price,
            rate.LeadDays,
            price == 0m,
            rate.LeadDays == 0 ? "آماده امروز" : $"حدود {ToPersianDigits(rate.LeadDays)} روز"));
    }

    private async Task<StorefrontCartPage> RequireCartAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException("shipping.cart.missing");
        if (cart.Lines.Count == 0)
        {
            throw new InvalidOperationException("shipping.cart.empty");
        }

        return cart;
    }

    private async Task<CartShippingDraft?> LoadDraftAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var draft = await _orders.ShippingDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CartId == cartId, cancellationToken);
        if (draft is null)
        {
            return null;
        }

        try
        {
            EnsureDraftOwnership(draft, guestSecret);
            return draft;
        }
        catch
        {
            return null;
        }
    }

    private void EnsureDraftOwnership(CartShippingDraft draft, string? guestSecret)
    {
        var hash = HashGuestSecret(guestSecret);
        if (!string.Equals(draft.GuestSecretHash, hash, StringComparison.Ordinal))
        {
            // سبد احرازشده با draft مهمان یا برعکس
            if (!(string.IsNullOrEmpty(draft.GuestSecretHash) && string.IsNullOrEmpty(hash) && _session.IsAuthenticated))
            {
                throw new InvalidOperationException("shipping.cart.forbidden");
            }
        }
    }

    private async Task<Guid> ResolveLanguageIdAsync(string? language, CancellationToken cancellationToken)
    {
        var langs = await _languages.ListAsync(cancellationToken);
        if (langs.Count == 0)
        {
            return Guid.Empty;
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            var match = langs.FirstOrDefault(l =>
                l.Code.StartsWith(language, StringComparison.OrdinalIgnoreCase)
                || l.Culture.StartsWith(language, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match.LanguageId;
            }
        }

        return (langs.FirstOrDefault(l => l.IsDefault) ?? langs[0]).LanguageId;
    }

    private static string HashGuestSecret(string? guestSecret)
    {
        if (string.IsNullOrWhiteSpace(guestSecret))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(guestSecret.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static StorefrontShippingDraftView MapDraft(CartShippingDraft draft) =>
        new(
            draft.CartId,
            draft.CartVersion,
            draft.RecipientName,
            draft.ContactMobile,
            draft.ProvinceName,
            draft.CityName,
            draft.PostalAddress,
            draft.PostalCode,
            draft.SavedAddressId,
            draft.ShippingMethodCode,
            draft.ShippingMethodLabel,
            draft.ShippingAmount,
            draft.MinimumDeliveryDate.ToString("yyyy-MM-dd"),
            draft.SelectedDeliveryDate?.ToString("yyyy-MM-dd"),
            draft.SelectedDeliveryTimeWindow,
            draft.CustomerNote);

    private static string FormatFaDateLabel(DateOnly date, DateOnly today)
    {
        var delta = date.DayNumber - today.DayNumber;
        return delta switch
        {
            0 => "امروز",
            1 => "فردا",
            2 => "پس‌فردا",
            _ => FormatFaDateSub(date),
        };
    }

    private static string FormatFaDateSub(DateOnly date) =>
        ToPersianDigits($"{date.Month}/{date.Day}");

    private static string ToPersianDigits(int n) => ToPersianDigits(n.ToString());

    private static string ToPersianDigits(string input)
    {
        var chars = input.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c).ToArray();
        return new string(chars);
    }
}
