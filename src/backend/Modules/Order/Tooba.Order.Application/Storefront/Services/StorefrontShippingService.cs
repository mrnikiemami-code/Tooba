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
        IClock clock)
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

        // TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: افق هر گروه پایدار جداگانه؛ هیچ اشتراک یا
        // دیرترین-حداقلِ سراسری اینجا اعمال نمی‌شود. اسکالرهای بالا فقط نمای سازگاری‌اند.
        var groupHorizons = draft is null || draft.ShippingGroups.Count == 0
            ? null
            : draft.ShippingGroups
                .OrderBy(g => g.ShippingGroupId)
                .Select(g => BuildGroupHorizon(g, today, _shippingOptions))
                .ToArray();

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
            groupHorizons);
    }

    /// <summary>
    /// افق زمان‌بندی یک گروه پایدار: حداقل تاریخ از آماده‌سازی <b>همان فروشنده</b> و روزهای حمل همان
    /// روش ساخته می‌شود (و هرگز به گذشته نمی‌رود)، و تاریخ/پنجره‌های مجاز از همان حداقل آغاز می‌شوند.
    /// هیچ تاریخ یا پنجره‌ای از گروه دیگر، از کلاینت یا تقویم ساختگی گرفته نمی‌شود.
    /// </summary>
    private static StorefrontShippingGroupHorizonView BuildGroupHorizon(
        CartShippingDraftGroup group,
        DateOnly today,
        ShippingMethodsOptions options)
    {
        var sellerPreparationDays = StorefrontShippingCalculator.SellerPreparationDays(group.SellerPartyId, options);
        var recalculated = StorefrontShippingCalculator.ComputeMinimumDeliveryDate(
            today, sellerPreparationDays, group.LeadDays);
        // وعدهٔ ذخیره‌شده دست‌نخورده می‌ماند، ولی هرگز تاریخ گذشته پیشنهاد نمی‌شود.
        var minimum = group.MinimumDeliveryDate > recalculated ? group.MinimumDeliveryDate : recalculated;

        return new StorefrontShippingGroupHorizonView(
            group.ShippingGroupId,
            group.SellerPartyId,
            group.MethodCode,
            minimum.ToString("yyyy-MM-dd"),
            StorefrontShippingCalculator.BuildDeliveryDates(minimum, options.DeliveryHorizonDays)
                .Select(d => new StorefrontDeliveryDateOption(
                    d.ToString("yyyy-MM-dd"),
                    StorefrontShippingCalculator.FormatDeliveryDateLabelFa(d, today),
                    StorefrontShippingCalculator.FormatDeliveryDateSubLabelFa(d),
                    d == minimum))
                .ToArray(),
            StorefrontShippingCalculator.DayTimeWindows
                .Select(t => new StorefrontDeliveryTimeOption(t.Value, t.LabelFa))
                .ToArray(),
            group.SelectedDeliveryDate?.ToString("yyyy-MM-dd"),
            group.SelectedDeliveryTimeWindow,
            group.LeadDays,
            sellerPreparationDays);
    }

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
        await RequireCartAsync(cartId, guestSecret, cancellationToken);

        var draft = await _drafts.GetByCartIdAsync(cartId, cancellationToken)
            ?? throw new StorefrontOrderException(StorefrontOrderErrors.ShippingSelectionRequired);
        EnsureDraftOwnership(draft, guestSecret);

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
        var methods = await LoadEligibleMethodsAsync(
            cartSubtotal,
            prepared.Shipping.ProvinceName,
            null,
            cancellationToken);

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
                UuidV7.New);
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
            BuildOrderGroupPlans(draft));
    }

    /// <summary>
    /// پلن گروه‌های ارسال پایدار سفارش از پیش‌نویس M1. فقط انتقال snapshot معتبر backend است؛
    /// هیچ قیمت/مجوز جدیدی اینجا ساخته نمی‌شود.
    /// </summary>
    private static IReadOnlyList<OrderShippingGroupPlan> BuildOrderGroupPlans(CartShippingDraft draft) =>
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
                group.Lines.Select(x => x.CartLineId).ToArray()))
            .ToArray();

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
                TryAddMethod(result, code, label, service.Code, service.IconKey, cartSubtotal, provinceName, option.Code);
            }
        }

        return await ApplyOneLevelDisplayNamesAsync(result, langId, cancellationToken);
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
                : draft.ShippingGroups.Select(MapGroup).ToArray());

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
}
