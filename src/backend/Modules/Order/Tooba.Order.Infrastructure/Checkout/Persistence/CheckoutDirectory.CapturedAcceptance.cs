using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Infrastructure.Checkout.Persistence;

/// <summary>
/// پذیرش گروه‌های ارسال capture‌شده در <b>همان</b> مسیر واقعی submit (TB-233 / 233-R1).
/// </summary>
/// <remarks>
/// <para>
/// این قطعه هیچ مسیر موازی نمی‌سازد: فقط از داخل <see cref="CheckoutProcessManager.SubmitAsync"/> و
/// <b>پیش از</b> نخستین رزرو/نوشتن سفارش صدا زده می‌شود.
/// </para>
/// <para>
/// <b>No bypass.</b> نشانگر capture از <b>پایگاه دادهٔ خود Order</b> خوانده می‌شود، نه از شاهد فراخوان:
/// پس یک فراخوان داخلی/مستقیم که شاهد را حذف کند هم نمی‌تواند یک سبد capture‌شده را «قدیمی» جا بزند.
/// </para>
/// <para>
/// <b>Facts from this submit.</b> مبلغ کالای هر گروه از همان خطوط <c>SellerOrder</c> این submit با
/// <see cref="ShippingGroupGoodsFactsBuilder"/> ساخته می‌شود؛ هیچ Preview/217 دوباره صدا زده نمی‌شود.
/// </para>
/// </remarks>
public sealed partial class CheckoutDirectory
{
    private const string CapturedConflict = StorefrontCapturedSelectionReasons.Conflict;
    private const string CapturedUnavailable = StorefrontCapturedSelectionReasons.Unavailable;
    private const string CapturedIntentInvalid = StorefrontCapturedSelectionReasons.IntentInvalid;
    private const string CapturedCouponUnsupported = StorefrontCapturedSelectionReasons.CouponUnavailable;
    private const string CapturedNotReady = StorefrontCapturedPreviewReasons.PreviewNotReady;

    /// <inheritdoc />
    async Task<IReadOnlyList<OrderShippingGroupPlan>> ICheckoutSubmitHost.AcceptCapturedShippingAsync(
        SubmitCheckoutCommand command,
        CartSnapshot cart,
        IReadOnlyList<SellerOrder> sellerOrders,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(cart);

        var callerPlans = command.ShippingGroups ?? [];
        var callerSuppliedEvidence = callerPlans.Any(plan => plan.AcceptedEvidence is not null);

        // یک probe از پایگاه دادهٔ خود Order، مستقل از شاهد فراخوان: نشانگر capture پنهان نمی‌ماند.
        var stored = await ReadStoredCapturedDraftAsync(command.CartId, cancellationToken);
        var storedCaptured = stored is
        {
            ShippingGroups.Count: > 0,
        } && stored.ShippingGroups.Any(group => group.AcceptedEvidence is not null);
        var revisionIntent = !string.IsNullOrWhiteSpace(command.ExpectedSelectionRevision);

        // مسیر واقعاً قدیمی: هیچ نشانگر ذخیره‌شده، هیچ شاهد فراخوان و هیچ قصد capture — بدون خواندن
        // هیچ ماژول بیرونی و با همان پلن ورودی.
        if (!storedCaptured && !callerSuppliedEvidence && !revisionIntent)
        {
            return callerPlans;
        }

        var options = _capturedPreview?.Value;
        if (options is null || options.CheckoutEnabled is false)
        {
            // دروازهٔ ثبت نهایی خاموش: هیچ گذر capture‌شده‌ای (حتی با شاهد ساختگی) پذیرفته نمی‌شود.
            throw new ContractOperationException(CapturedNotReady);
        }

        if (storedCaptured is false || stored is null)
        {
            // شاهد فراخوان/قصد بدون شاهد ذخیره‌شده: هرگز legacy نمی‌شود.
            throw new ContractOperationException(CapturedNotReady);
        }

        // کوپن در این برش پشتیبانی نمی‌شود: هیچ تخفیفی بی‌شاهد مشترک خواندن/نوشتن/ثبت اعمال نمی‌شود.
        if (!string.IsNullOrWhiteSpace(command.CouponCode))
        {
            throw new ContractOperationException(CapturedCouponUnsupported);
        }

        // نسخهٔ انتخاب باید صریح و دقیقاً همان ریشهٔ ذخیره‌شده باشد؛ «۰» یعنی نبودِ صریح.
        if (!TryParseSelectionRevision(command.ExpectedSelectionRevision, out var expectedRevision))
        {
            throw new ContractOperationException(CapturedIntentInvalid);
        }

        if (expectedRevision != stored.SelectionRevision)
        {
            throw new ContractOperationException(CapturedConflict);
        }

        // مقصد پرسیده‌شده باید همان مقصد قیمت‌گذاری‌شدهٔ ذخیره‌شده باشد (نه مقصد ارزان‌تر/دیگر).
        var destination = await ResolveCurrentDestinationAsync(command, cancellationToken);

        var groups = stored.ShippingGroups
            .OrderBy(group => group.ShippingGroupId)
            .ToArray();
        if (groups.Length == 0 || groups.Any(group => group.AcceptedEvidence is null))
        {
            // شاهد ناقص/ترکیبی هرگز بخشی از یک سفارش capture‌شده نمی‌شود.
            throw new ContractOperationException(CapturedUnavailable);
        }

        foreach (var group in groups)
        {
            var evidence = group.AcceptedEvidence!;
            if (!SameDestination(stored, destination)
                || !string.Equals(evidence.Currency, group.Currency, StringComparison.Ordinal))
            {
                throw new ContractOperationException(CapturedConflict);
            }
        }

        // افراز کامل سبد با همان خطوط ذخیره‌شده؛ هیچ خطی جا نمی‌افتد یا اضافه نمی‌شود.
        var membership = ShippingGroupGoodsFactsBuilder.ValidateMembership(
            groups.Select(group => new CheckoutShippingGroupMembership(
                group.ShippingGroupId.ToString("N"),
                group.Lines.OrderBy(line => line.CartLineId).Select(line => line.CartLineId).ToArray()))
                .ToArray(),
            cart);

        // فکت‌های همین submit از خطوط واقعی SellerOrder — بدون هیچ نقل‌قول دوباره.
        var orderLines = sellerOrders.SelectMany(order => order.Lines).ToList();
        var linesByCartLineId = ShippingGroupGoodsFactsBuilder.BuildAuthoritativeLinesByCartLineId(
            orderLines,
            cart,
            _orderLineIdByCartLineId);
        var currency = StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart);
        var facts = new CheckoutShippingGroupGoodsFactsResult(
            cart.CartId,
            cart.Version,
            currency,
            membership
                .Select(group => ShippingGroupGoodsFactsBuilder.BuildGroupFacts(group, linesByCartLineId, currency))
                .ToArray());

        // مسئولیت/نوع/مالک: فقط SingleStore با مالک Store و همان tenant مورداعتماد.
        var tenantId = ResolveCapturedStoreTenantId();
        if (tenantId is null)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        var offerIds = cart.Lines.Select(line => line.OfferId).Distinct().ToArray();
        var assignments = await _offerAssignments!.GetAssignmentsAsync(offerIds, cancellationToken);
        var typeIds = groups
            .Select(group => group.AcceptedEvidence!.ShippingTypeId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var types = typeIds.Length == 0
            ? Array.Empty<ShippingTypeSnapshot>()
            : (await _shippingTypes!.GetByIdsAsync(typeIds, cancellationToken)).ToArray();

        var sellerByOffer = cart.Lines.ToDictionary(line => line.OfferId, line => line.SellerPartyId);
        var offerByCartLineId = cart.Lines.ToDictionary(line => line.LineId, line => line.OfferId);
        foreach (var group in groups)
        {
            var evidence = group.AcceptedEvidence!;
            foreach (var line in group.Lines)
            {
                if (!offerByCartLineId.TryGetValue(line.CartLineId, out var offerId)
                    || !assignments.TryGetValue(offerId, out var assignment)
                    // مسئولیت صریح ۲۲۸: نبود/ناشناخته/Seller هرگز به Store بازتفسیر نمی‌شود.
                    || assignment.Responsibility != OfferFulfillmentResponsibility.Platform
                    || assignment.ShippingTypeId is not Guid assignedType
                    || assignedType != evidence.ShippingTypeId
                    || !types.Any(type => type.ShippingTypeId == assignedType && type.IsActive))
                {
                    throw new ContractOperationException(CapturedUnavailable);
                }
            }
        }

        if (assignments.Count != offerIds.Length)
        {
            // حتی یک خط بدون مسئولیت صریح = کل انتخاب نامعتبر؛ هیچ گروهی بی‌صدا حذف نمی‌شود.
            throw new ContractOperationException(CapturedUnavailable);
        }

        // یک اشاره‌گر ۲۱۹ برای هر جفت مالک+Binding ذخیره‌شده (نه enumeration همهٔ نامزدهای ممکن).
        var capturedPlans = new List<StorefrontCapturedGroupPlan>(groups.Length);
        var pointers = new Dictionary<string, ShippingSelectedCapturedRevisionResult>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var evidence = group.AcceptedEvidence!;
            var serviceCode = group.ServiceCode?.Trim();
            if (string.IsNullOrEmpty(serviceCode))
            {
                throw new ContractOperationException(CapturedUnavailable);
            }

            var cacheKey = string.Join(
                '\u001F',
                tenantId,
                CanonicalBinding(serviceCode),
                CanonicalBinding(group.OptionCode));
            if (!pointers.TryGetValue(cacheKey, out var pointer))
            {
                pointer = await _selectedCaptures!.ReadAsync(
                    new ShippingSelectedCapturedRevisionRequest(
                        ShippingCapturedQuoteOwnerKind.Store,
                        tenantId,
                        null,
                        serviceCode,
                        group.OptionCode),
                    cancellationToken);
                pointers[cacheKey] = pointer;
            }

            if (pointer.Verdict != ShippingSelectedCaptureVerdict.Selected
                || pointer.DraftId != evidence.DraftId
                || pointer.SourceRevision != evidence.SourceRevision
                || pointer.SelectionVersion != evidence.SelectionVersion
                || pointer.SnapshotId != evidence.SnapshotId
                || pointer.FormatVersion != evidence.FormatVersion
                || pointer.ShippingTypeId != evidence.ShippingTypeId)
            {
                // اشاره‌گر عوض‌شده/ناموجود: انتخاب جدید لازم است؛ هیچ fallback به «آخرین» نیست.
                throw new ContractOperationException(CapturedConflict);
            }

            var candidate = new StorefrontCapturedCandidatePlan(
                CanonicalBinding(serviceCode) + "\u001F"
                    + (CanonicalBinding(group.OptionCode) ?? string.Empty),
                ShippingCapturedQuoteOwnerKind.Store,
                tenantId,
                null,
                evidence.DraftId,
                evidence.SourceRevision,
                destination,
                serviceCode,
                group.OptionCode,
                options.AvailableDateCount ?? 0,
                evidence.SnapshotId,
                evidence.SelectionVersion);

            capturedPlans.Add(new StorefrontCapturedGroupPlan(candidate.MethodCode, group.Lines
                .OrderBy(line => line.CartLineId)
                .Select(line => line.CartLineId)
                .ToArray(), [candidate]));
        }

        var composed = await _capturedComposer!.ComposeFromFactsAsync(
            new StorefrontCapturedGroupProjectionRequest(command, capturedPlans),
            facts,
            cancellationToken);

        // مقایسهٔ کامل شاهد ذخیره‌شده با نقل‌قول/تقویم همین لحظه؛ هیچ مقداری بازنویسی نمی‌شود.
        var accepted = new List<OrderShippingGroupPlan>(groups.Length);
        for (var index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            var evidence = group.AcceptedEvidence!;
            var projection = composed.Groups[index];
            var candidate = projection.Candidates.SingleOrDefault();
            if (candidate is null
                || candidate.Outcome != StorefrontCapturedCandidateOutcome.Quoted
                || candidate.OwnerResult is not { IsQuoted: true } quote)
            {
                throw new ContractOperationException(CapturedConflict);
            }

            var slot = candidate.Receiving?.AvailableDates
                .FirstOrDefault(available =>
                    available.Date == evidence.ReceivingDate
                    && available.OpensAt.Ticks == evidence.ReceivingOpenTicks
                    && available.ClosesAt.Ticks == evidence.ReceivingCloseTicks);
            if (slot is null
                || !string.Equals(quote.CurrencyCode, evidence.Currency, StringComparison.Ordinal)
                || quote.GroupId != evidence.AreaGroupId
                || quote.RateRowId != evidence.RateRowId
                || quote.ShippingTypeId != evidence.ShippingTypeId
                || quote.FreightAmount != evidence.FreightAmount
                || quote.PackagingAmount != evidence.PackagingAmount
                || quote.PayableAtCheckoutAmount != evidence.PayableAtCheckoutAmount
                || quote.CarrierCollectAmount != evidence.CarrierCollectAmount
                || quote.MinDeliveryDays != evidence.MinimumDeliveryDays
                || quote.MaxDeliveryDays != evidence.MaximumDeliveryDays
                || !string.Equals(
                    quote.ReceivingWeek?.TimeZoneId,
                    evidence.ReceivingTimeZone,
                    StringComparison.Ordinal))
            {
                throw new ContractOperationException(CapturedConflict);
            }

            accepted.Add(new OrderShippingGroupPlan(
                group.ShippingGroupId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.MethodLabel,
                group.OriginContext,
                // پرداخت در checkout دقیقاً payable است؛ کرایهٔ collect جدا و بی‌جمع‌شدن می‌ماند.
                evidence.PayableAtCheckoutAmount,
                group.Currency,
                group.MinimumDeliveryDate,
                evidence.ReceivingDate,
                group.SelectedDeliveryTimeWindow,
                group.LeadDays,
                group.Lines.OrderBy(line => line.CartLineId).Select(line => line.CartLineId).ToArray(),
                evidence));
        }

        _ = now;
        return accepted;
    }

    /// <summary>
    /// خواندن <b>تازه و بدون ردیابی</b> ریشهٔ پیش‌نویس با شاهد و اعضای گروه‌ها؛ هرگز از گراف tracked
    /// شامل‌های قدیمی استفاده نمی‌شود.
    /// </summary>
    private async Task<CartShippingDraft?> ReadStoredCapturedDraftAsync(
        Guid cartId,
        CancellationToken cancellationToken)
    {
        if (cartId == Guid.Empty)
        {
            return null;
        }

        return await _db.ShippingDrafts
            .AsNoTracking()
            .Include(x => x.ShippingGroups)
            .ThenInclude(group => group.Lines)
            .Where(x => x.CartId == cartId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// مقصد نهاییِ همین submit را از نام‌های معتبر سرور resolve می‌کند؛ ابهام/عدم‌تطبیق رد می‌شود.
    /// </summary>
    private async Task<DestinationIdentifiers> ResolveCurrentDestinationAsync(
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        if (_geography is null)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        var resolution = await _geography.ResolveLegacyAsync(
            countryCode: null,
            string.IsNullOrWhiteSpace(command.ProvinceName) ? null : command.ProvinceName,
            string.IsNullOrWhiteSpace(command.CityName) ? null : command.CityName,
            serviceAreaName: null,
            cancellationToken);

        if (resolution.Outcome != DestinationResolutionOutcome.Resolved
            || resolution.Identifiers is not { } identifiers)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        return identifiers;
    }

    /// <summary>مقایسهٔ هویت یکتای مقصد فعلی با مقصد قیمت‌گذاری‌شدهٔ ذخیره‌شده روی ریشهٔ پیش‌نویس.</summary>
    private static bool SameDestination(CartShippingDraft draft, DestinationIdentifiers current) =>
        draft.DestinationCountryId is Guid country
        && country == current.CountryId
        && draft.DestinationAdministrativeAreaId == current.AdministrativeAreaId
        && draft.DestinationCityId == current.CityId
        // ناحیهٔ خدمت پشتیبانی نمی‌شود مگر آدرس مورداعتماد همان هویت را بیان کند؛ هیچ حدسی نیست.
        && draft.DestinationServiceAreaId is null
        && current.ServiceAreaId is null;

    private string? ResolveCapturedStoreTenantId()
    {
        if (_edition?.Current?.Edition != ToobaEdition.SingleStore)
        {
            return null;
        }

        var current = _tenant?.Current;
        return current is null || current.Status != TenantStatus.Active || string.IsNullOrWhiteSpace(current.TenantId.Value)
            ? null
            : current.TenantId.Value.Trim();
    }

    /// <summary>کلید مقایسهٔ پایدار Binding (حساس به حروف نیست) — همان سیاست نام‌گذاری پایه.</summary>
    private static string CanonicalBinding(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();

    /// <summary>
    /// تفسیر دقیق متن نسخهٔ انتخاب: فقط رقم‌های AscIی نامنفی و بدون فاصله/علامت. نبود/تهی/بدشکل یعنی
    /// «قصد نامعتبر» و هرگز به معنای «۰» یا «هر نسخه‌ای» نیست.
    /// </summary>
    /// <param name="text">متن خام نسخهٔ انتخاب از درخواست.</param>
    /// <param name="revision">نسخهٔ تفسیرشده وقتی متن معتبر است.</param>
    public static bool TryParseSelectionRevision(string? text, out long revision)
    {
        revision = 0;
        if (string.IsNullOrEmpty(text) || text.Length != text.Trim().Length)
        {
            return false;
        }

        foreach (var character in text)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out revision)
            && revision >= 0;
    }
}
