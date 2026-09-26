using System.Security.Cryptography;
using System.Text;
using Tooba.AddressBook.Contracts.Customer;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>
/// Order-owned storefront shipping projection/selection/commit.
/// </summary>
public sealed class StorefrontShippingService
{
    private readonly ICartPresentationGateway _carts;
    private readonly StorefrontCheckoutService _checkouts;
    private readonly IAddressBookCheckoutLookup _addresses;
    private readonly IShippingCatalogReader _shippingCatalog;
    private readonly IStorefrontShippingDraftStore _drafts;
    private readonly ILanguageLookup _languages;
    private readonly ShippingMethodsOptions _shippingOptions;
    private readonly IShippingCatalogSeedPort _shippingSeed;
    private readonly IOrderStorefrontActor _actor;
    private readonly IClock _clock;

    public StorefrontShippingService(
        ICartPresentationGateway carts,
        StorefrontCheckoutService checkouts,
        Tooba.AddressBook.Contracts.Customer.IAddressBookCheckoutLookup addresses,
        IShippingCatalogReader shippingCatalog,
        IStorefrontShippingDraftStore drafts,
        ILanguageLookup languages,
        ShippingMethodsOptions shippingOptions,
        IShippingCatalogSeedPort shippingSeed,
        IOrderStorefrontActor actor,
        IClock clock)
    {
        _carts = carts;
        _checkouts = checkouts;
        _addresses = addresses;
        _shippingCatalog = shippingCatalog;
        _drafts = drafts;
        _languages = languages;
        _shippingOptions = shippingOptions;
        _shippingSeed = shippingSeed;
        _actor = actor;
        _clock = clock;
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
        var (effectiveCurrency, cartSubtotal) = StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal(cart);
        var sellerIds = cart.Lines.Select(x => x.SellerPartyId).Distinct().ToArray();
        var maxPrep = StorefrontShippingCalculator.MaxSellerPreparationDays(sellerIds, _shippingOptions);
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var methods = await LoadEligibleMethodsAsync(cartSubtotal, provinceName, language, cancellationToken);

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

        var selectedCode = !string.IsNullOrWhiteSpace(methodCode)
            ? methodCode.Trim().ToLowerInvariant()
            : draft?.ShippingMethodCode?.Trim().ToLowerInvariant();
        StorefrontShippingMethodView? selected = methods.FirstOrDefault(m =>
            selectedCode is not null
            && string.Equals(m.MethodCode, selectedCode, StringComparison.OrdinalIgnoreCase));

        DateOnly? minDate = selected is null
            ? null
            : StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, maxPrep, selected.LeadDays);
        var dates = minDate is DateOnly min
            ? StorefrontShippingCalculator.BuildDeliveryDates(min, _shippingOptions.DeliveryHorizonDays)
                .Select(d => new StorefrontDeliveryDateOption(
                    d.ToString("yyyy-MM-dd"),
                    StorefrontShippingCalculator.FormatDeliveryDateLabelFa(d, today),
                    StorefrontShippingCalculator.FormatDeliveryDateSubLabelFa(d),
                    d == min))
                .ToList()
            : [];
        var timeWindows = StorefrontShippingCalculator.DayTimeWindows
            .Select(t => new StorefrontDeliveryTimeOption(t.Value, t.LabelFa))
            .ToList();

        return new StorefrontShippingProjection(
            cart.CartId,
            cart.Version,
            effectiveCurrency,
            cart.ItemCount,
            cartSubtotal,
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
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingCartStale);
        }

        var names = StorefrontRecipientNames.Resolve(request.FirstName, request.LastName, request.RecipientName);
        if (request.SavedAddressId is null || request.SavedAddressId == Guid.Empty)
        {
            StorefrontRecipientNames.EnsureNewAddressNames(names.First, names.Last);
        }
        else if (names.First.Length == 0 && names.Last.Length == 0 && names.Recipient.Length == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingFirstNameRequired);
        }

        var prepared = await _checkouts.PrepareShippingAsync(
            new StorefrontCheckoutShippingInput(
                names.Recipient,
                request.ContactMobile,
                request.ProvinceName,
                request.CityName,
                request.PostalAddress,
                request.PostalCode,
                request.SavedAddressId,
                names.First,
                names.Last),
            cancellationToken);

        var methods = await LoadEligibleMethodsAsync(
            StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal(cart).SubtotalExclusiveOfTax,
            prepared.Shipping.ProvinceName,
            null,
            cancellationToken);
        var method = methods.FirstOrDefault(m =>
            string.Equals(m.MethodCode, request.ShippingMethodCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.ShippingMethodUnavailable);

        var sellerIds = cart.Lines.Select(x => x.SellerPartyId).Distinct();
        var maxPrep = StorefrontShippingCalculator.MaxSellerPreparationDays(sellerIds, _shippingOptions);
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var minimum = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, maxPrep, method.LeadDays);
        if (!DateOnly.TryParse(request.SelectedDeliveryDate, out var selectedDate))
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingDeliveryInvalid);
        }

        StorefrontShippingCalculator.EnsureDeliveryNotEarlier(selectedDate, minimum);
        var windows = StorefrontShippingCalculator.ValidTimeWindows(selectedDate, minimum);
        if (string.IsNullOrWhiteSpace(request.SelectedDeliveryTimeWindow)
            || windows.All(w => w.Value != request.SelectedDeliveryTimeWindow.Trim()))
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingDeliverySlotUnavailable);
        }

        if (request.CustomerNote is { Length: > CartShippingDraft.CustomerNoteMaxLength })
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingNoteTooLong);
        }

        // کلاینت نمی‌تواند قیمت را جعل کند.
        var amount = method.PriceAmount;
        var now = _clock.UtcNow;
        var hash = HashGuestSecret(guestSecret);
        var existing = await _drafts.GetByCartIdAsync(cart.CartId, cancellationToken);
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
            existing.ApplyRecipientNames(names.First, names.Last);
            if (_actor.IsAuthenticated)
            {
                existing.ClearGuestSecret();
            }

            await _drafts.SaveAsync(existing, isNew: true, cancellationToken);
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
            existing.ApplyRecipientNames(names.First, names.Last);
            if (_actor.IsAuthenticated)
            {
                existing.ClearGuestSecret();
            }

            await _drafts.SaveAsync(existing, isNew: false, cancellationToken);
        }

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
        var draft = await _drafts.GetByCartIdAsync(cartId, cancellationToken)
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.ShippingSelectionRequired);
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
                draft.CustomerNote,
                draft.FirstName,
                draft.LastName),
            guestSecret,
            cancellationToken);

        draft = (await _drafts.GetByCartIdAsync(cartId, cancellationToken))!;

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
                draft.SavedAddressId,
                draft.FirstName,
                draft.LastName),
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
        await _shippingSeed.EnsureSeedAsync(cancellationToken);
        var enabled = ShippingMethodRegistry.Enabled(_shippingOptions)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var langId = await ResolveLanguageIdAsync(language, cancellationToken);
        var catalog = await _shippingCatalog.ListAsync(cancellationToken);
        var services = catalog.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToList();

        var result = new List<StorefrontShippingMethodView>();
        foreach (var service in services)
        {
            if (!enabled.Contains(service.Code))
            {
                continue;
            }

            var serviceName = service.Translations
                .OrderByDescending(t => t.LanguageId == langId)
                .Select(t => t.Name)
                .FirstOrDefault()
                ?? ShippingMethodRegistry.ResolveLabel(service.Code, service.Code);
            var serviceOptions = service.Options.Where(o => o.IsActive).ToList();
            if (serviceOptions.Count == 0)
            {
                TryAddMethod(result, service.Code, serviceName, null, service.IconKey, cartSubtotal, provinceName);
                continue;
            }

            foreach (var option in serviceOptions)
            {
                var optionName = option.Translations
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
            rate.LeadDays == 0 ? "آماده امروز" : $"حدود {ToPersianLeadDays(rate.LeadDays)} روز"));
    }

    private static string ToPersianLeadDays(int days)
    {
        var raw = Math.Max(0, days).ToString();
        var chars = raw.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c).ToArray();
        return new string(chars);
    }

    private async Task<CartPage> RequireCartAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(cartId, guestSecret, cancellationToken)
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.ShippingCartMissing);
        if (cart.Lines.Count == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingCartEmpty);
        }

        return cart;
    }

    private async Task<CartShippingDraft?> LoadDraftAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var draft = await _drafts.GetByCartIdAsync(cartId, cancellationToken);
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
        if (string.Equals(draft.GuestSecretHash, hash, StringComparison.Ordinal))
        {
            return;
        }

        // پس از ادغام ورود، راز مهمان از نشست حذف می‌شود اما پیش‌نویس همان سبد مالک باقی می‌ماند.
        if (_actor.IsAuthenticated && string.IsNullOrEmpty(hash))
        {
            return;
        }

        throw new StorefrontOrderException(StorefrontOrderErrors.ShippingCartForbidden);
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
            StorefrontRecipientNames.Display(draft.FirstName, draft.LastName, draft.RecipientName),
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
            draft.CustomerNote,
            draft.FirstName,
            draft.LastName);
}
