using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.AddressBook.Contracts.Ports;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;
using Tooba.Order.Application.Storefront.Geography;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Shipping.OwnedRate;
using Tooba.Order.Application.Storefront.Shipping.Recovery;
using Tooba.Order.Contracts.Storefront;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;

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
    private readonly IShippingOneLevelStorefrontLabelReader _oneLevelLabels;
    private readonly IStorefrontShippingDraftStore _drafts;
    private readonly ILanguageLookup _languages;
    private readonly ShippingMethodsOptions _shippingOptions;
    private readonly IShippingCatalogSeedPort _shippingSeed;
    private readonly IOrderStorefrontActor _actor;
    private readonly IClock _clock;
    private readonly StorefrontShippingOwnedRateResolver? _ownedRates;
    private readonly IShippingOneLevelCanonicalMethodReader? _canonicalMethods;
    private readonly Shipping.CapturedPreview.StorefrontCapturedPreviewPlanner? _capturedPreview;
    private readonly Shipping.CapturedPreview.StorefrontCapturedSelectionWriter? _capturedSelection;

    /// <summary>
    /// وابستگی اختیاریِ آخر عمداً پیش‌فرض‌دار است: هر ترکیب‌کننده‌ای که آن را تأمین نکند رفتار قدیمی را
    /// عیناً می‌گیرد (دروازه خاموش، بدون هیچ کوئری به Shipping).
    /// </summary>
    public StorefrontShippingService(
        ICartPresentationGateway carts,
        StorefrontCheckoutService checkouts,
        Tooba.AddressBook.Contracts.Ports.IAddressBookCheckoutLookup addresses,
        IShippingCatalogReader shippingCatalog,
        IShippingOneLevelStorefrontLabelReader oneLevelLabels,
        IStorefrontShippingDraftStore drafts,
        ILanguageLookup languages,
        ShippingMethodsOptions shippingOptions,
        IShippingCatalogSeedPort shippingSeed,
        IOrderStorefrontActor actor,
        IClock clock,
        StorefrontShippingOwnedRateResolver? ownedRates = null,
        IShippingOneLevelCanonicalMethodReader? canonicalMethods = null,
        Shipping.CapturedPreview.StorefrontCapturedPreviewPlanner? capturedPreview = null,
        Shipping.CapturedPreview.StorefrontCapturedSelectionWriter? capturedSelection = null)
    {
        _carts = carts;
        _checkouts = checkouts;
        _addresses = addresses;
        _shippingCatalog = shippingCatalog;
        _oneLevelLabels = oneLevelLabels;
        _drafts = drafts;
        _languages = languages;
        _shippingOptions = shippingOptions;
        _shippingSeed = shippingSeed;
        _actor = actor;
        _clock = clock;
        _ownedRates = ownedRates;
        _canonicalMethods = canonicalMethods;
        _capturedPreview = capturedPreview;
        _capturedSelection = capturedSelection;
    }

    /// <summary>
    /// دروازهٔ یگانهٔ «پیش‌نمایش آمادهٔ تسویه نیست»: در حالت روشن، انتخاب/ثبت پیش از هر نوشتن رد می‌شود.
    /// </summary>
    private void EnsureCapturedPreviewIsNotCheckoutReady()
    {
        if (_capturedPreview?.IsEnabled == true)
        {
            throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedPreviewReasons.PreviewNotReady);
        }
    }

    /// <summary>تصویر ارسال برای سبد جاری.</summary>
    public async Task<StorefrontShippingProjection> ProjectAsync(
        Guid cartId,
        string? guestSecret,
        string? provinceName,
        string? methodCode,
        string? language,
        CancellationToken cancellationToken,
        string? cityName = null,
        Tooba.Geography.Contracts.Places.DestinationIdentifiers? destination = null)
    {
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);
        var (effectiveCurrency, cartSubtotal) = StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal(cart);
        var sellerIds = cart.Lines.Select(x => x.SellerPartyId).Distinct().ToArray();
        var maxPrep = StorefrontShippingCalculator.MaxSellerPreparationDays(sellerIds, _shippingOptions);
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var eligible = await LoadEligibleMethodsAsync(
            cartSubtotal,
            provinceName,
            language,
            cancellationToken,
            effectiveCurrency,
            cityName,
            destination);
        var methods = eligible.Methods;

        var draft = await LoadDraftAsync(cart.CartId, guestSecret, cancellationToken);
        var revalidationMessage = (string?)null;
        if (draft is not null)
        {
            // بازیابی فقط-خواندنی: سبد چندفروشنده‌ای shorthand خالی دارد، پس اعتبار انتخاب ذخیره‌شده از
            // گروه‌های پایدار همان پیش‌نویس سنجیده می‌شود (نه از یک کد روش خالی). هیچ نوشتنی رخ نمی‌دهد.
            var stillValid = StorefrontShippingDraftRecovery.IsStillValid(
                draft,
                methods,
                provinceName,
                cart.Lines.Select(x => x.SellerPartyId).Distinct().ToArray(),
                _shippingOptions);
            if (!stillValid)
            {
                revalidationMessage = StorefrontShippingDraftRecovery.RevalidationMessage;
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

        // سبد چندگروهیِ ذخیره‌شده بدون methodCode: حداقل تحویل نباید خالی بماند، وگرنه تاریخ بازیابی‌شده
        // هرگز با وعدهٔ واقعی سرور سنجیده نمی‌شود. مقدار از دیرترین روز آماده‌سازی گروه‌های ذخیره‌شده و
        // همان ماشین‌حساب canonical ساخته می‌شود؛ هیچ تاریخ یا محاسبهٔ تقویمی در کلاینت انجام نمی‌شود.
        if (selected is null && draft is not null
            && StorefrontShippingDraftRecovery.ResolveProjectedLeadDays(draft, methods) is int groupedLeadDays)
        {
            minDate = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(today, maxPrep, groupedLeadDays);
        }
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

        // TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150: افق هر (فروشنده × روش واجد شرایط) جداگانه؛
        // هیچ اشتراک یا دیرترین-حداقلِ سراسری اینجا اعمال نمی‌شود. اسکالرهای بالا فقط نمای
        // سازگاری تک‌گروهی‌اند. در سبد تک‌خطی هیچ افقی ساخته نمی‌شود تا مسیر قدیمی دست‌نخورده بماند.
        var groupHorizons = BuildGroupHorizons(cart, methods, draft, today);

        // TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229: فقط‌خواندنی و پیش‌فرض-خاموش. روشن‌بودن کلید
        // یعنی مسیر واقعی «سبد → انتساب Offer → انتخاب ۲۱۹ → یک ترکیب ۲۱۸» خوانده می‌شود؛ انتخاب/ثبت
        // همان کلید را صریحاً رد می‌کند. خاموش یعنی صفر فراخوانی و همان پاسخ قبلی.
        Models.StorefrontCapturedPreviewView? capturedPreviewView = null;
        if (_capturedPreview?.IsEnabled == true)
        {
            var previewLanguageId = await ResolveLanguageIdAsync(language, cancellationToken);
            capturedPreviewView = await _capturedPreview.BuildAsync(
                cart,
                _actor.BuildCartAccess(guestSecret),
                destination,
                effectiveCurrency,
                previewLanguageId,
                cancellationToken);
        }

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
            StorefrontIranGeography.Provinces,
            groupHorizons.Length == 0 ? null : groupHorizons,
            eligible.Unavailable,
            capturedPreviewView);
    }

    /// <summary>
    /// افق زمان‌بندی به‌ازای هر جفت (فروشندهٔ سبد × روش واجد شرایط). حداقل تاریخ هر ردیف از آماده‌سازی
    /// <b>همان فروشنده</b> و روزهای حمل <b>همان روش</b> ساخته می‌شود (و هرگز به گذشته نمی‌رود). اگر گروه
    /// ذخیره‌شده‌ای با همان فروشنده/روش وجود داشته باشد، <c>ShippingGroupId</c> و انتخاب ذخیره‌شدهٔ
    /// همان گروه ضمیمه می‌شود؛ وگرنه ردیف فقط با کلید پایدار مرحلهٔ پیش‌نویس برمی‌گردد.
    /// کلاینت هیچ تاریخ/شناسه‌ای نمی‌سازد و هیچ گروهی از فهرست حذف/ادغام نمی‌شود.
    /// </summary>
    private StorefrontShippingGroupHorizonView[] BuildGroupHorizons(
        CartPage cart,
        IReadOnlyList<StorefrontShippingMethodView> methods,
        CartShippingDraft? draft,
        DateOnly today)
    {
        // کارت تک‌خطی همان مسیر و همان ظاهر قدیمی را نگه می‌دارد؛ افق گروهی تنها برای سبد چندخطی است.
        if (cart.Lines.Count <= 1 || methods.Count == 0)
        {
            return [];
        }

        var persistedGroups = draft?.ShippingGroups ?? [];
        var sellers = cart.Lines
            .Select(x => x.SellerPartyId)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        var eligibleMethods = methods
            .OrderBy(m => m.MethodCode, StringComparer.Ordinal)
            .ToArray();

        var horizons = new List<StorefrontShippingGroupHorizonView>(sellers.Length * eligibleMethods.Length);
        foreach (var seller in sellers)
        {
            var preparationDays = StorefrontShippingCalculator.SellerPreparationDays(seller, _shippingOptions);
            foreach (var method in eligibleMethods)
            {
                var persisted = persistedGroups.FirstOrDefault(g =>
                    g.SellerPartyId == seller
                    && string.Equals(g.MethodCode, method.MethodCode, StringComparison.OrdinalIgnoreCase));

                var recalculated = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(
                    today, preparationDays, method.LeadDays);
                // وعدهٔ ذخیره‌شدهٔ همان گروه دست‌نخورده می‌ماند، ولی تاریخ گذشته هرگز پیشنهاد نمی‌شود.
                var minimum = persisted is not null && persisted.MinimumDeliveryDate > recalculated
                    ? persisted.MinimumDeliveryDate
                    : recalculated;

                horizons.Add(new StorefrontShippingGroupHorizonView(
                    DraftStageGroupKey(seller, method.MethodCode),
                    persisted?.ShippingGroupId,
                    seller,
                    method.MethodCode,
                    minimum.ToString("yyyy-MM-dd"),
                    StorefrontShippingCalculator.BuildDeliveryDates(minimum, _shippingOptions.DeliveryHorizonDays)
                        .Select(d => new StorefrontDeliveryDateOption(
                            d.ToString("yyyy-MM-dd"),
                            StorefrontShippingCalculator.FormatDeliveryDateLabelFa(d, today),
                            StorefrontShippingCalculator.FormatDeliveryDateSubLabelFa(d),
                            d == minimum))
                        .ToArray(),
                    StorefrontShippingCalculator.DayTimeWindows
                        .Select(t => new StorefrontDeliveryTimeOption(t.Value, t.LabelFa))
                        .ToArray(),
                    persisted?.SelectedDeliveryDate?.ToString("yyyy-MM-dd"),
                    persisted?.SelectedDeliveryTimeWindow,
                    method.LeadDays,
                    preparationDays));
            }
        }

        return horizons.ToArray();
    }

    /// <summary>
    /// کلید پایدار سرور-مالکِ گروه در مرحلهٔ پیش‌نویس (پیش از نخستین ذخیره): ترکیب قطعی فروشندهٔ
    /// سبد و کد روش canonical. کلاینت هرگز شناسهٔ گروه نمی‌سازد و این کلید پس از ذخیره با
    /// <c>ShippingGroupId</c> بازconciliation می‌شود.
    /// </summary>
    private static string DraftStageGroupKey(Guid sellerPartyId, string methodCode)
        => $"{sellerPartyId:N}|{(methodCode ?? string.Empty).Trim().ToLowerInvariant()}";

    /// <summary>
    /// پیش‌نویس پرداخت پیش از سفارش برای سبد فعال: انتخاب ارسال ذخیره‌شده + قیمت authoritative بک‌اند
    /// (همان ارزیابی تجاری <c>/checkout/preview</c>). هیچ CheckoutGroup/سفارش/رزروی ساخته نمی‌شود،
    /// سبد Convert نمی‌شود، سهمیهٔ abuse مصرف نمی‌شود و خروجی <c>CheckoutId = null</c> دارد.
    /// </summary>
    public async Task<StorefrontCheckoutPage> ProjectPaymentDraftAsync(
        Guid cartId,
        string? guestSecret,
        string? couponCode,
        CancellationToken cancellationToken)
    {
        // دسترسی سبد اول بررسی می‌شود تا cartId ناشناس/بیگانه پیش از هر خواندن پیش‌نویس یکسان ۴۰۴ بگیرد.
        // TB-235-R1: همین صفحهٔ <b>مجاز سرور</b> به نمای داخلی داده می‌شود؛ هیچ بارگذاری دوبارهٔ سبد در
        // مسیر عمومی انجام نمی‌شود (دایرکتوری همچنان نسخهٔ authoritative خودش را مستقل می‌سنجد).
        var cart = await RequireCartAsync(cartId, guestSecret, cancellationToken);

        var draft = await _drafts.GetByCartIdAsync(cartId, cancellationToken)
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.ShippingSelectionRequired);
        EnsureDraftOwnership(draft, guestSecret);

        // TB-232/234: پیش‌نویس دارای شاهد capture هرگز با مسیر قدیمی بازقیمت نمی‌شود. با دروازهٔ منسجم،
        // همان هستهٔ ۲۳۳ آن را فقط-خواندنی اعتبارسنجی می‌کند؛ با دروازهٔ ناهمساز/خاموش همان رد قبلی است.
        if (draft.ShippingGroups.Any(group => group.AcceptedEvidence is not null))
        {
            if (_capturedPreview?.IsCapturedCommitEnabled != true || _capturedSelection is null)
            {
                throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedPreviewReasons.PreviewNotReady);
            }

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedSelectionReasons.CouponUnavailable);
            }

            return await _checkouts.ProjectCapturedPaymentDraftAsync(
                cart,
                guestSecret,
                draft,
                couponCode,
                expectedSelectionRevision: null,
                cancellationToken);
        }

        return await _checkouts.PreviewAsync(
            cartId,
            guestSecret,
            couponCode,
            cancellationToken,
            shipping: new StorefrontCheckoutShippingInput(
                draft.RecipientName,
                draft.ContactMobile,
                draft.ProvinceName,
                draft.CityName,
                draft.PostalAddress,
                draft.PostalCode,
                draft.SavedAddressId,
                draft.FirstName,
                draft.LastName),
            shippingMethodCode: draft.ShippingMethodCode,
            shippingMethodLabel: draft.ShippingMethodLabel,
            shippingAmount: draft.ShippingAmount,
            minimumDeliveryDate: draft.MinimumDeliveryDate,
            requestedDeliveryDate: draft.SelectedDeliveryDate,
            requestedDeliveryTimeWindow: draft.SelectedDeliveryTimeWindow,
            customerNote: draft.CustomerNote,
            shippingGroups: BuildOrderGroupPlans(draft));
    }

    /// <summary>پیش‌نویس ارسال را ذخیره و اعتبارسنجی می‌کند.</summary>
    public async Task<StorefrontShippingDraftView> SaveSelectionAsync(
        StorefrontShippingSelectionRequest request,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        // TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229: پیش‌نمایش capture‌شده عمداً آمادهٔ تسویه
        // نیست. این رد پیش از هر خواندن سبد/انتخاب/رزرو/تبدیل انجام می‌شود تا کدهای روش capture‌شده
        // هرگز به مسیر قیمت/ثبت قدیمی نیفتند. خاموش ⇒ رفتار قبلی عیناً.
        if (request.CapturedGroups is { Count: > 0 })
        {
            // TB-232: قصد capture‌شده مسیر مستقل اتمیک دارد؛ بدون کلید نوشتن هرگز به مسیر قدیمی
            // نمی‌افتد (fail-closed) و هیچ مبلغ/مالکی از بدنه پذیرفته نمی‌شود.
            if (_capturedSelection is null || !_capturedSelection.IsEnabled)
            {
                throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedSelectionReasons.Disabled);
            }

            return await SaveCapturedSelectionAsync(request, guestSecret, cancellationToken);
        }

        EnsureCapturedPreviewIsNotCheckoutReady();

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

        var (soleCurrency, cartSubtotal) = StorefrontCartCurrencyCompatibility.ResolveSoleCurrencyAndSubtotal(cart);
        var eligibleMethods = await LoadEligibleMethodsAsync(
            cartSubtotal,
            prepared.Shipping.ProvinceName,
            null,
            cancellationToken,
            soleCurrency,
            prepared.Shipping.CityName);
        // نرخ ذخیره‌شده فقط از فهرست authoritative می‌آید؛ اگر روش به هر دلیل نرخ معتبر نداشته باشد
        // در این فهرست نیست و انتخاب/ادامه با آن صریحاً رد می‌شود (هیچ مبلغ ساختگی ذخیره نمی‌شود).
        var methods = eligibleMethods.Methods;

        var sellerIds = cart.Lines.Select(x => x.SellerPartyId).Distinct().ToArray();
        var maxPrep = StorefrontShippingCalculator.MaxSellerPreparationDays(sellerIds, _shippingOptions);
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        // قصد گروه‌ها: یا فهرست افزایشی جدید، یا shorthand تک‌روشی قدیمی به‌صورت یک گروه کامل.
        var requestedGroups = BuildRequestedGroups(request, cart);
        // نوشتن چندگروهی فقط وقتی است که کلاینت بیش از یک گروه اعلام کرده باشد؛ در آن حالت هر گروه
        // باید زمان‌بندی مستقل خودش را داشته باشد و هیچ مقدار اسکالری روی همه کپی نمی‌شود.
        var multiGroupWrite = request.ShippingGroups is { Count: > 1 };

        var requestedCodes = requestedGroups
            .Select(g => (g.MethodCode ?? string.Empty).Trim())
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var requestedMethods = methods
            .Where(m => requestedCodes.Contains(m.MethodCode, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        // هویت/برچسب/مبلغ/ارز فقط از روش واجد شرایط authoritative می‌آید؛ حداقل تحویل اما به‌ازای
        // **هر فروشنده** جداگانه ساخته می‌شود (آماده‌سازی همان فروشنده + روزهای حمل همان روش) تا دو
        // فروشنده با روش یکسان دو وعدهٔ مستقل بگیرند.
        var eligible = sellerIds
            .SelectMany(seller => requestedMethods.Select(m => new CartShippingGroupEligibleMethod(
                m.MethodCode,
                m.ServiceCode,
                m.OptionCode,
                m.Label,
                m.PriceAmount,
                soleCurrency,
                m.LeadDays,
                StorefrontShippingCalculator.ComputeGroupMinimumDeliveryDate(
                    today, seller, m.LeadDays, _shippingOptions),
                seller)))
            .ToArray();

        // shorthand قدیمی: فقط برای سبد تک‌گروهی/تک‌روشی معتبر است و همان‌جا fail-closed بررسی می‌شود.
        var legacyDateText = (request.SelectedDeliveryDate ?? string.Empty).Trim();
        DateOnly? legacySelectedDate = DateOnly.TryParse(legacyDateText, out var parsedLegacy)
            ? parsedLegacy
            : null;
        var legacyWindow = (request.SelectedDeliveryTimeWindow ?? string.Empty).Trim();

        if (!multiGroupWrite && legacySelectedDate is null)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingDeliveryInvalid);
        }

        var sellerByLine = cart.Lines.ToDictionary(x => x.LineId, x => x.SellerPartyId);
        var validatedGroups = new List<CartShippingGroupRequest>(requestedGroups.Count);

        // اعتبارسنجی زمان‌بندی به‌ازای هر گروه و در برابر حداقل **همان گروه** (نه دیرترین حداقل سراسری).
        foreach (var requested in requestedGroups)
        {
            var firstLine = requested.CartLineIds is { Count: > 0 } ? requested.CartLineIds[0] : Guid.Empty;
            if (!sellerByLine.TryGetValue(firstLine, out var groupSeller))
            {
                throw new StorefrontOrderException(StorefrontOrderErrors.ShippingMethodUnavailable);
            }

            var groupMethod = eligible.FirstOrDefault(e =>
                    e.SellerPartyId == groupSeller
                    && string.Equals(
                        e.MethodCode,
                        (requested.MethodCode ?? string.Empty).Trim(),
                        StringComparison.OrdinalIgnoreCase))
                ?? throw new StorefrontOrderException(StorefrontOrderErrors.ShippingMethodUnavailable);

            // زمان‌بندی مختص همین گروه مقدم است؛ در نوشتن چندگروهی نبودش fail-closed رد می‌شود
            // (هیچ تاریخ/پنجرهٔ اسکالری روی گروه‌ها کپی نمی‌شود).
            var groupDate = requested.SelectedDeliveryDate;
            if (groupDate is null)
            {
                if (multiGroupWrite)
                {
                    throw new StorefrontOrderException(StorefrontOrderErrors.ShippingDeliveryInvalid);
                }

                groupDate = legacySelectedDate;
            }

            var groupWindow = string.IsNullOrWhiteSpace(requested.SelectedDeliveryTimeWindow)
                ? (multiGroupWrite ? string.Empty : legacyWindow)
                : requested.SelectedDeliveryTimeWindow!.Trim();

            if (groupDate is null || groupWindow.Length == 0)
            {
                throw new StorefrontOrderException(StorefrontOrderErrors.ShippingDeliverySlotUnavailable);
            }

            StorefrontShippingCalculator.EnsureDeliveryNotEarlier(groupDate.Value, groupMethod.MinimumDeliveryDate);
            var groupWindows = StorefrontShippingCalculator.ValidTimeWindows(
                groupDate.Value, groupMethod.MinimumDeliveryDate);
            if (groupWindows.All(w => w.Value != groupWindow))
            {
                throw new StorefrontOrderException(StorefrontOrderErrors.ShippingDeliverySlotUnavailable);
            }

            // مقادیر canonical تأییدشده به planner می‌روند تا گروه دقیقاً زمان‌بندی خودش را ذخیره کند.
            validatedGroups.Add(requested with
            {
                SelectedDeliveryDate = groupDate,
                SelectedDeliveryTimeWindow = groupWindow,
            });
        }

        if (request.CustomerNote is { Length: > CartShippingDraft.CustomerNoteMaxLength })
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingNoteTooLong);
        }

        var now = _clock.UtcNow;
        var hash = HashGuestSecret(guestSecret);

        // TB-SHIPPING-GROUP-ID-CONTINUITY-153: پیش‌نویس مورداعتماد همین سبد پیش از برنامه‌ریزی خوانده
        // می‌شود تا شناسهٔ پایدار هر گروه ذخیره‌شده برای همان تعهد منطقی بازاستفاده شود (یک خواندن،
        // همان تعداد خواندن قبلی)؛ هیچ شناسه‌ای از کلاینت به‌عنوان حقیقت پذیرفته نمی‌شود.
        var existing = await _drafts.GetByCartIdAsync(cart.CartId, cancellationToken);

        // TB-232: پیش‌نویس دارای شاهد capture هرگز با مسیر قدیمی بازقیمت/بی‌شاهد نمی‌شود؛ حتی وقتی
        // کلیدها بعداً خاموش شوند این رد باقی می‌ماند (بدون downgrade بی‌صدا).
        if (existing is not null && existing.ShippingGroups.Any(group => group.AcceptedEvidence is not null))
        {
            throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedSelectionReasons.EvidencePresent);
        }

        // عضویت گروه‌ها: هر خط سبد دقیقاً یک بار، فروشنده از Cart، روش متعارف واجد شرایط.
        IReadOnlyList<CartShippingDraftGroup> groups;
        try
        {
            groups = CartShippingGroupPlanner.Plan(
                cart.CartId,
                cart.Lines
                    .Select(x => new CartShippingGroupCartLine(x.LineId, x.SellerPartyId, x.Quantity))
                    .ToArray(),
                validatedGroups,
                eligible,
                // زمان‌بندی فقط از خودِ هر گروه می‌آید؛ هیچ اسکالر سراسری اینجا کپی نمی‌شود.
                null,
                null,
                now,
                UuidV7.New,
                existing?.ShippingGroups);
        }
        catch (ContractOperationException ex)
        {
            throw new StorefrontOrderException(MapGroupFault(ex.Code), ex.Message);
        }

        var primary = groups[0];
        // اسکالرهای تک‌گروهی فقط برای سبد تک‌گروهی مقدار می‌گیرند؛ در سبد چندگروهی هیچ تاریخ/پنجرهٔ
        // سراسریِ authoritative وجود ندارد و این ستون‌ها خالی می‌مانند (گروه‌ها صاحب زمان‌بندی‌اند).
        var scalarDeliveryDate = multiGroupWrite ? null : primary.SelectedDeliveryDate;
        var scalarDeliveryWindow = multiGroupWrite ? null : primary.SelectedDeliveryTimeWindow;
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
                primary.MethodCode,
                primary.MethodLabel,
                primary.ShippingAmount,
                primary.MinimumDeliveryDate,
                scalarDeliveryDate,
                scalarDeliveryWindow,
                request.CustomerNote,
                now);
            existing.ApplyRecipientNames(names.First, names.Last);
            if (_actor.IsAuthenticated)
            {
                existing.ClearGuestSecret();
            }

            existing.AddShippingGroups(groups, now);
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
                primary.MethodCode,
                primary.MethodLabel,
                primary.ShippingAmount,
                primary.MinimumDeliveryDate,
                scalarDeliveryDate,
                scalarDeliveryWindow,
                request.CustomerNote,
                now);
            existing.ApplyRecipientNames(names.First, names.Last);
            if (_actor.IsAuthenticated)
            {
                existing.ClearGuestSecret();
            }

            // جایگزینی دومرحله‌ای: حذف گروه‌های قبلی پیش از درج مجدد خطوطی که همان cart_line_id را
            // دارند، تا ایندکس یگانهٔ خط در یک batch واحد نقض نشود.
            existing.ClearShippingGroups(now);
            await _drafts.SaveAsync(existing, isNew: false, cancellationToken);
            existing.AddShippingGroups(groups, now);
            await _drafts.SaveAsync(existing, isNew: false, cancellationToken);
        }

        return MapDraft(existing);
    }

    /// <summary>
    /// TB-232: نوشتن انتخاب capture‌شده با بازاعتبارسنجی کامل سرور.
    /// </summary>
    /// <remarks>
    /// کل بدنه یک <b>قصد نامعتمد</b> است: هیچ مالک/فروشنده/مبلغ/شاهد/تاریخی از آن حقیقت نیست. نشانی از
    /// مسیر سمت سرور (همان آماده‌سازی قدیمی) خوانده می‌شود، قیمت‌ها/تقویم/اشاره‌گرها دوباره resolve
    /// می‌شوند و تنها نوشتن، جایگزینی اتمیک گروه‌های همین پیش‌نویس است. پیش از این نقطه همهٔ خواندن‌های
    /// بیرونی و اعتبارسنجی‌ها انجام شده است.
    /// </remarks>
    private async Task<StorefrontShippingDraftView> SaveCapturedSelectionAsync(
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

        // همین مسیر موجود: نشانی ذخیره‌شده/گیرنده از سمت سرور، همان چیزی که واقعاً قیمت می‌گیرد.
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

        // TB-232-R1: خواندن تازه/جدا از ردیف ذخیره‌شده — نه identity mapِ کهنه — تا CAS و گروه‌های
        // قابل‌بازاستفاده از حقیقت ذخیره‌شده بیایند و کار در انتظار نامرتبط پیش از هر تغییر رد شود.
        var existing = await _drafts.GetForCapturedSelectionAsync(cart.CartId, cancellationToken);
        if (existing is not null)
        {
            EnsureDraftOwnership(existing, guestSecret);
        }

        var draft = await _capturedSelection!.SaveAsync(
            cart,
            _actor.BuildCartAccess(guestSecret),
            prepared,
            existing,
            HashGuestSecret(guestSecret),
            request,
            cancellationToken);

        return MapDraft(draft);
    }

    /// <summary>انتخاب معتبر را به Checkout ثبت و برای /payment برمی‌گرداند.</summary>
    public async Task<StorefrontCheckoutPage> CommitAsync(
        Guid cartId,
        string? guestSecret,
        int expectedCartVersion,
        string idempotencyKey,
        string? couponCode,
        string? expectedSelectionRevision,
        CancellationToken cancellationToken)
    {
        // TB-234: مسیر ثبت نهایی capture‌شده فقط وقتی باز است که هر چهار کلید هم‌زمان و منسجم روشن باشند؛
        // «فقط نوشتن انتخاب» هرگز ثبت نهایی را مجاز نمی‌کند. ناهمسازی همین‌جا و پیش از هر خواندن رد می‌شود
        // تا پیش‌نویس capture‌شده هرگز با دروازهٔ نیمه‌باز به مسیر قدیمی نیفتد.
        var capturedCommitEnabled = _capturedPreview?.IsCapturedCommitEnabled == true && _capturedSelection is not null;
        if (!capturedCommitEnabled)
        {
            EnsureCapturedPreviewIsNotCheckoutReady();
        }

        var draft = await _drafts.GetByCartIdAsync(cartId, cancellationToken)
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.ShippingSelectionRequired);
        EnsureDraftOwnership(draft, guestSecret);

        // TB-232/234: پیش‌نویس دارای شاهد capture فقط از مسیر capture‌شده و فقط با دروازهٔ منسجم ثبت می‌شود.
        if (draft.ShippingGroups.Any(group => group.AcceptedEvidence is not null))
        {
            if (!capturedCommitEnabled)
            {
                throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedPreviewReasons.PreviewNotReady);
            }

            return await SubmitCapturedSelectionAsync(
                cartId,
                guestSecret,
                expectedCartVersion,
                idempotencyKey,
                couponCode,
                expectedSelectionRevision,
                draft,
                cancellationToken);
        }

        // پیش‌نمایش روشن ولی بدون شاهد ذخیره‌شده: همان رد قبلی، هرگز مسیر قدیمی.
        if (_capturedPreview?.IsEnabled == true)
        {
            throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedPreviewReasons.PreviewNotReady);
        }

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
                draft.LastName,
                // Replay the saved group membership so a genuine multi-group selection is
                // revalidated as-is instead of being collapsed into the legacy shorthand.
                draft.ShippingGroups.Count == 0
                    ? null
                    : draft.ShippingGroups
                        .Select(group => new StorefrontShippingGroupSelection(
                            group.MethodCode,
                            group.Lines.Select(x => x.CartLineId).ToArray(),
                            group.ServiceCode,
                            group.OptionCode,
                            group.OriginContext))
                        .ToArray()),
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
            draft.CustomerNote,
            BuildOrderGroupPlans(draft),
            expectedSelectionRevision);
    }

    /// <summary>
    /// ثبت نهایی انتخاب capture‌شده از <b>ریشهٔ ذخیره‌شده</b> — فقط انتقال به عضو موجود دامنهٔ checkout.
    /// </summary>
    /// <remarks>
    /// کوپن در این برش پشتیبانی نمی‌شود و <b>پیش از هر خواندن/رزرو/نوشتن</b> با کد پایدار همان نویسندهٔ
    /// capture رد می‌شود؛ هیچ تخفیفی ساکت حذف یا جایگزین نمی‌شود.
    /// </remarks>
    private Task<StorefrontCheckoutPage> SubmitCapturedSelectionAsync(
        Guid cartId,
        string? guestSecret,
        int expectedCartVersion,
        string idempotencyKey,
        string? couponCode,
        string? expectedSelectionRevision,
        CartShippingDraft draft,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            throw new StorefrontOrderException(Shipping.CapturedPreview.StorefrontCapturedSelectionReasons.CouponUnavailable);
        }

        return _checkouts.SubmitCapturedSelectionAsync(
            cartId,
            guestSecret,
            expectedCartVersion,
            idempotencyKey,
            draft,
            couponCode,
            expectedSelectionRevision,
            cancellationToken);
    }

    /// <summary>
    /// پلن گروه‌های ارسال پایدار سفارش از پیش‌نویس M1. فقط انتقال snapshot معتبر backend است؛
    /// هیچ قیمت/مجوز جدیدی اینجا ساخته نمی‌شود.
    /// </summary>
    internal static IReadOnlyList<OrderShippingGroupPlan> BuildOrderGroupPlans(CartShippingDraft draft) =>
        draft.ShippingGroups
            .Select(group => new OrderShippingGroupPlan(
                group.ShippingGroupId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.MethodLabel,
                group.OriginContext,
                group.ShippingAmount,
                group.Currency,
                group.MinimumDeliveryDate,
                group.SelectedDeliveryDate,
                group.SelectedDeliveryTimeWindow,
                group.LeadDays,
                group.Lines.Select(x => x.CartLineId).ToArray(),
                group.AcceptedEvidence))
            .ToArray();

    private async Task<StorefrontEligibleMethods> LoadEligibleMethodsAsync(
        decimal cartSubtotal,
        string? provinceName,
        string? language,
        CancellationToken cancellationToken,
        string? currencyCode = null,
        string? cityName = null,
        Tooba.Geography.Contracts.Places.DestinationIdentifiers? destination = null)
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
                TryAddMethod(result, code, label, service.Code, service.IconKey, cartSubtotal, provinceName, option.Code);
            }
        }

        // TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163: دروازهٔ نرخ مالک. خاموش (پیش‌فرض) یعنی
        // صفر فراخوانی و همان فهرست/قیمت/خطای قدیمی. روشن یعنی تنها نرخ authoritative مالک قیمت
        // می‌دهد و هر روش بدون نرخ مالک از فهرست حذف می‌شود (نه صفر، نه رایگان، نه نرخ سرویس/provider).
        IReadOnlyList<StorefrontShippingUnavailableMethodView>? unavailable = null;
        if (_ownedRates is not null)
        {
            // TB-SHIPPING-NEW-METHOD-ACTIVATION-170: روش تازهٔ canonical هیچ ردیف گزینهٔ قدیمی ندارد
            // و در فهرست قدیمی نیست. فقط وقتی دروازهٔ نرخ مالک روشن است هویت آن خوانده می‌شود و فقط
            // با نرخ واقعی همان دور به مشتری نمایش داده می‌شود؛ دروازهٔ خاموش ⇒ صفر فراخوانی.
            var canonicalOnly = await LoadCanonicalOnlyMethodsAsync(cancellationToken);
            var legacyCandidates = result
                .Select(m => new StorefrontShippingOwnedRateCandidate(m.MethodCode, m.ServiceCode, m.OptionCode))
                .ToArray();

            var resolution = await _ownedRates.ResolveAsync(
                legacyCandidates
                    .Concat(canonicalOnly.Select(m => new StorefrontShippingOwnedRateCandidate(
                        m.MethodCode, m.ServiceCode, m.OptionCode)))
                    .ToArray(),
                provinceName,
                cityName,
                destination,
                cartSubtotal,
                currencyCode,
                cancellationToken);

            if (resolution.Evaluated)
            {
                var priced = new List<StorefrontShippingMethodView>(result.Count + canonicalOnly.Count);
                foreach (var method in result)
                {
                    if (resolution.Quoted.TryGetValue(method.MethodCode, out var quote))
                    {
                        // مبلغ authoritative جایگزین مبلغ قدیمی می‌شود؛ هیچ مبلغی از مسیر قدیمی روی آن نمی‌نشیند.
                        priced.Add(method with { PriceAmount = quote.Amount, IsFree = quote.Amount == 0m });
                    }
                }

                foreach (var method in canonicalOnly)
                {
                    // نبود نرخ مالک ⇒ روش تازه اصلاً وارد فهرست نمی‌شود (نه صفر، نه رایگان، نه وعدهٔ ساختگی).
                    if (!resolution.Quoted.TryGetValue(method.MethodCode, out var quote))
                    {
                        continue;
                    }

                    var leadDays = Math.Max(0, quote.LeadDays);
                    priced.Add(new StorefrontShippingMethodView(
                        method.MethodCode,
                        method.MethodCode,
                        method.ServiceCode,
                        string.IsNullOrWhiteSpace(method.IconKey) ? method.ServiceCode : method.IconKey,
                        quote.Amount,
                        leadDays,
                        quote.Amount == 0m,
                        leadDays == 0 ? "آماده امروز" : $"حدود {ToPersianLeadDays(leadDays)} روز",
                        method.OptionCode));
                }

                result = priced;
                // فهرست «ردشده» فقط روش‌های مسیر قدیمی را نگه می‌دارد؛ روش تازهٔ بدون نرخ ساکت و
                // غیرقابل‌انتخاب می‌ماند تا هیچ روش نیمه‌پیکربندی به UI مشتری معرفی نشود.
                var legacyCodes = legacyCandidates
                    .Select(c => c.MethodCode)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                unavailable = resolution.Unavailable.Count == 0
                    ? []
                    : resolution.Unavailable
                        .Where(u => legacyCodes.Contains(u.MethodCode))
                        .Select(u => new StorefrontShippingUnavailableMethodView(u.MethodCode, u.ReasonCode))
                        .ToArray();
            }
        }

        return new StorefrontEligibleMethods(
            await ApplyOneLevelDisplayNamesAsync(result, langId, cancellationToken),
            unavailable);
    }

    /// <summary>
    /// روش‌های تازهٔ canonical (بدون ردیف گزینهٔ قدیمی) هنگام روشن‌بودن دروازهٔ نرخ مالک.
    /// </summary>
    /// <remarks>
    /// دروازهٔ خاموش یا نبود درز ⇒ صفر فراخوانی و فهرست خالی (رفتار قدیمی عیناً). هر خطای غیرمنتظرهٔ
    /// مرجع هم به فهرست خالی ترجمه می‌شود: نبودِ مدرک هرگز به نمایش روش یا قیمت ساختگی تبدیل نمی‌شود.
    /// </remarks>
    private async Task<IReadOnlyList<ShippingOneLevelCanonicalMethodSnapshot>> LoadCanonicalOnlyMethodsAsync(
        CancellationToken cancellationToken)
    {
        if (_canonicalMethods is null || _ownedRates?.IsEnabled != true)
        {
            return [];
        }

        try
        {
            return await _canonicalMethods.ListActiveUnboundAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// جایگزینی فقط-برچسب نام روش یک‌سطحی روی واجدین شرایط قدیمی.
    /// </summary>
    /// <remarks>
    /// این متد هیچ‌چیز جز <see cref="StorefrontShippingMethodView.Label"/> را تغییر نمی‌دهد: کد روش،
    /// کد سرویس، آیکون، قیمت، روزهای تحویل و برچسب تخمین دست‌نخورده می‌مانند. واجد شرایط بودن همان
    /// منبع قدیمی (سرویس/گزینهٔ فعال + EnabledCodes + پیکربندی نرخ) باقی می‌ماند، چون این متد فقط
    /// پس از ساخت فهرست اجرا می‌شود و هیچ روشی اضافه/حذف نمی‌کند. اگر درز Fulfillment خالی برگرداند
    /// (دروازهٔ خاموش، نبود جدول، کد ناشناخته یا نبود ترجمهٔ زبان درخواستی) همان برچسب قدیمی
    /// عیناً نمایش داده می‌شود.
    /// </remarks>
    private async Task<IReadOnlyList<StorefrontShippingMethodView>> ApplyOneLevelDisplayNamesAsync(
        List<StorefrontShippingMethodView> methods,
        Guid languageId,
        CancellationToken cancellationToken)
    {
        if (methods.Count == 0)
        {
            return methods;
        }

        var codes = methods.Select(m => m.MethodCode).Distinct(StringComparer.Ordinal).ToArray();
        var oneLevelNames = await _oneLevelLabels.ResolveDisplayNamesAsync(codes, languageId, cancellationToken);
        if (oneLevelNames.Count == 0)
        {
            return methods;
        }

        for (var i = 0; i < methods.Count; i++)
        {
            if (oneLevelNames.TryGetValue(methods[i].MethodCode, out var name)
                && !string.IsNullOrWhiteSpace(name))
            {
                methods[i] = methods[i] with { Label = name };
            }
        }

        return methods;
    }

    private void TryAddMethod(
        List<StorefrontShippingMethodView> result,
        string methodCode,
        string label,
        string? serviceCode,
        string iconKey,
        decimal cartSubtotal,
        string? provinceName,
        string? optionCode = null)
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
            rate.LeadDays == 0 ? "آماده امروز" : $"حدود {ToPersianLeadDays(rate.LeadDays)} روز",
            // The option identity is carried as its own field: the legacy method code composes
            // "service:option", and consumers must never treat that composite as a service code.
            string.IsNullOrWhiteSpace(optionCode) ? null : optionCode));
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

    /// <summary>
    /// قصد گروه‌های ارسال را از درخواست می‌سازد. فهرست افزایشی <c>ShippingGroups</c> مقدم است؛
    /// در نبود آن همان انتخاب تک‌روشی گذشته به‌عنوان shorthand سازگار به یک گروه تمام-سبد تبدیل می‌شود.
    /// هیچ خط یا مبلغی از کلاینت به‌عنوان حقیقت پذیرفته نمی‌شود: خطوط پس از این‌جا با مالکیت Cart اعتبارسنجی می‌شوند.
    /// </summary>
    private static IReadOnlyList<CartShippingGroupRequest> BuildRequestedGroups(
        StorefrontShippingSelectionRequest request,
        CartPage cart)
    {
        if (request.ShippingGroups is { Count: > 0 })
        {
            return request.ShippingGroups
                .Select(group => new CartShippingGroupRequest(
                    group.ShippingMethodCode ?? string.Empty,
                    group.ServiceCode,
                    group.OptionCode,
                    group.OriginContext,
                    group.CartLineIds ?? [],
                    // TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: زمان‌بندی همان گروه از همان گروه.
                    string.IsNullOrWhiteSpace(group.SelectedDeliveryDate)
                        ? null
                        : DateOnly.TryParse(group.SelectedDeliveryDate.Trim(), out var date) ? date : null,
                    group.SelectedDeliveryTimeWindow))
                .ToArray();
        }

        return
        [
            new CartShippingGroupRequest(
                request.ShippingMethodCode ?? string.Empty,
                null,
                null,
                null,
                cart.Lines.Select(x => x.LineId).ToArray())
        ];
    }

    /// <summary>
    /// نقض‌های عضویت/هویت گروه را به سطح خطای محلی‌سازی‌شدهٔ فروشگاهی نگاشت می‌کند.
    /// روش ناواجد/ناهمخوان به «روش در دسترس نیست» و بقیهٔ نقض‌های داده به رد صریح ارسال نگاشت می‌شوند.
    /// </summary>
    private static string MapGroupFault(string fault) => fault switch
    {
        CartShippingGroupFault.MethodNotEligible => StorefrontOrderErrors.ShippingMethodUnavailable,
        CartShippingGroupFault.MethodIdentityMismatch => StorefrontOrderErrors.ShippingMethodUnavailable,
        CartShippingGroupFault.MethodCodeRequired => StorefrontOrderErrors.ShippingMethodUnavailable,
        _ => StorefrontOrderErrors.ShippingRejected,
    };

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
            draft.LastName,
            draft.ShippingGroups.Count == 0
                ? null
                : draft.ShippingGroups.Select(MapGroup).ToArray(),
            draft.SelectionRevision.ToString(CultureInfo.InvariantCulture));

    private static StorefrontShippingGroupView MapGroup(CartShippingDraftGroup group) =>
        new(
            group.ShippingGroupId,
            group.SellerPartyId,
            group.MethodCode,
            group.ServiceCode,
            group.OptionCode,
            group.MethodLabel,
            group.ShippingAmount,
            group.Currency,
            group.MinimumDeliveryDate.ToString("yyyy-MM-dd"),
            group.SelectedDeliveryDate?.ToString("yyyy-MM-dd"),
            group.SelectedDeliveryTimeWindow,
            group.LeadDays,
            group.Lines.Count,
            group.Lines.Select(x => x.CartLineId).ToArray());

    /// <summary>
    /// فهرست واجدین شرایط به‌همراه دلایل پایدار روش‌هایی که دروازهٔ نرخ مالک رد کرده است.
    /// </summary>
    /// <param name="Methods">روش‌های قابل انتخاب (در حالت روشن، فقط دارای نرخ authoritative).</param>
    /// <param name="Unavailable">
    /// تهی یعنی دروازهٔ نرخ مالک اجرا نشده و رفتار قدیمی عیناً حفظ شده است؛ در غیر این صورت
    /// روش‌های حذف‌شده با دلیل پایدار.
    /// </param>
    private sealed record StorefrontEligibleMethods(
        IReadOnlyList<StorefrontShippingMethodView> Methods,
        IReadOnlyList<StorefrontShippingUnavailableMethodView>? Unavailable);
}
