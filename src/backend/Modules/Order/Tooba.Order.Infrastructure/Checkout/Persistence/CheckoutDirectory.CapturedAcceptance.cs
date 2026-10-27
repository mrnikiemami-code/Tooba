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
    private const string CapturedDestinationUnverified = StorefrontCapturedSelectionReasons.DestinationUnverified;
    private const string CapturedNotReady = StorefrontCapturedPreviewReasons.PreviewNotReady;

    /// <inheritdoc />
    async Task<IReadOnlyList<OrderShippingGroupPlan>> ICheckoutSubmitHost.AcceptCapturedShippingAsync(
        SubmitCheckoutCommand command,
        CartSnapshot cart,
        IReadOnlyList<SellerOrder> sellerOrders,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => (await AcceptCapturedShippingCoreAsync(
            command,
            cart,
            sellerOrders,
            now,
            requireExplicitSelectionRevision: true,
            cancellationToken)).Groups;

    /// <summary>
    /// هستهٔ <b>یگانهٔ</b> بازاعتبارسنجی یک انتخاب capture‌شده روی نقل‌قول همین لحظه.
    /// </summary>
    /// <remarks>
    /// ثبت نهایی علاوه بر این، نسخهٔ انتخاب <b>صریح</b> و قفل نقطه‌ای ریشهٔ Order را می‌طلبد؛ خواندنِ
    /// فقط-خواندنیِ «پیش‌نویس پرداخت» همان مسیر را بدون نسخهٔ صریح و بدون قفل طی می‌کند تا هیچ نوشتن/قفلی
    /// در مسیر خواندن ساخته نشود. هیچ‌کدام هیچ ماژول بیرونی را دوباره نقل‌قول نمی‌کنند.
    /// <para>
    /// نتیجهٔ تایپ‌شدهٔ پذیرش (گروه‌های پذیرفته‌شده + نسخهٔ واقعی سبد + نسخهٔ واقعی ریشه) برمی‌گردد تا
    /// مصرف‌کنندهٔ خواندنی نمای خود را از <b>همین</b> گروه‌های پذیرفته‌شده بسازد، نه از فرمان کهنه.
    /// </para>
    /// </remarks>
    internal async Task<CapturedShippingAcceptance> AcceptCapturedShippingCoreAsync(
        SubmitCheckoutCommand command,
        CartSnapshot cart,
        IReadOnlyList<SellerOrder> sellerOrders,
        DateTimeOffset now,
        bool requireExplicitSelectionRevision,
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
            return new CapturedShippingAcceptance(callerPlans, cart.Version, null);
        }

        var options = _capturedPreview?.Value;
        if (options is null
            || options.Enabled is false
            || options.SelectionEnabled is false
            || options.CheckoutEnabled is false
            || !options.HasValidAvailableDateCount)
        {
            // دروازهٔ ثبت نهایی خاموش/ناهمساز (یا تعداد تاریخ نامعتبر): هیچ گذر capture‌شده‌ای (حتی با
            // شاهد ساختگی) پذیرفته نمی‌شود.
            throw new ContractOperationException(CapturedNotReady);
        }

        // هر مرجع اختیاریِ لازم برای بازاعتبارسنجی باید واقعاً در همین scope موجود باشد؛ نبودشان
        // هرگز به null-reference یا پذیرش پیش‌فرض تبدیل نمی‌شود.
        if (_offerAssignments is null
            || _shippingTypes is null
            || _selectedCaptures is null
            || _capturedComposition is null
            || _geography is null
            || _addressBook is null
            || _shippingCatalog is null)
        {
            throw new ContractOperationException(CapturedUnavailable);
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

        // نسخهٔ انتخاب باید صریح و دقیقاً همان ریشهٔ ذخیره‌شده باشد؛ «۰» یعنی نبودِ صریح. مسیر
        // فقط-خواندنی (پیش‌نویس پرداخت) نسخهٔ صریح ندارد، پس این شرط و قفل نقطه‌ای را اجرا نمی‌کند.
        if (requireExplicitSelectionRevision)
        {
            if (!TryParseSelectionRevision(command.ExpectedSelectionRevision, out var expectedRevision))
            {
                throw new ContractOperationException(CapturedIntentInvalid);
            }

            if (expectedRevision != stored.SelectionRevision)
            {
                throw new ContractOperationException(CapturedConflict);
            }

            // نسخهٔ سبد ذخیره‌شده باید همان نسخهٔ authoritative جاری و همان نسخهٔ صریح فرمان باشد.
            if (stored.CartVersion != cart.Version || cart.Version != command.ExpectedCartVersion)
            {
                throw new ContractOperationException(CapturedConflict);
            }

            // قفل/بررسی ریشهٔ Order داخل همین تراکنشِ submit: تا پایدارسازی سفارش نگه داشته می‌شود تا
            // هیچ به‌روزرسانی هم‌زمان نتواند سفارشی با نسخهٔ ریشهٔ مخلوط بسازد.
            await LockStoredCapturedRootAsync(command.CartId, expectedRevision, cancellationToken);
        }
        else if (stored.CartVersion != cart.Version)
        {
            // خواندن هم نسخهٔ ذخیره‌شده را با سبد جاری می‌سنجد؛ انتخاب کهنه نمای تازه نمی‌سازد.
            throw new ContractOperationException(CapturedConflict);
        }

        // مقصد نهایی از کشور متعارف (نه null) + نشانی معتبر سرور resolve می‌شود؛ آدرس ذخیره‌شده فقط با
        // هویت واقعی سبد/ثبت‌کننده از کتاب آدرس خوانده می‌شود و هر تغییر/بیگانگی پیش از رزرو رد می‌شود.
        var destination = await ResolveAcceptedAddressAsync(command, cart, stored, cancellationToken);
        if (!SameDestination(stored, destination))
        {
            throw new ContractOperationException(CapturedConflict);
        }

        var groups = stored.ShippingGroups
            .OrderBy(group => group.ShippingGroupId)
            .ToArray();
        if (groups.Length == 0 || groups.Any(group => group.AcceptedEvidence is null))
        {
            // شاهد ناقص/ترکیبی هرگز بخشی از یک سفارش capture‌شده نمی‌شود.
            throw new ContractOperationException(CapturedUnavailable);
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
        if (types.Select(type => type.ShippingTypeId).Distinct().Count() != types.Length
            || types.Length != typeIds.Length)
        {
            // پاسخ دسته‌ای ناقص/تکراری/بیگانه برای نوع‌ها پذیرفته نمی‌شود (بدون حدس نوع فعال).
            throw new ContractOperationException(CapturedUnavailable);
        }

        if (assignments.Count != offerIds.Length)
        {
            // حتی یک خط بدون مسئولیت صریح = کل انتخاب نامعتبر؛ هیچ گروهی بی‌صدا حذف نمی‌شود.
            throw new ContractOperationException(CapturedUnavailable);
        }

        var offerByCartLineId = cart.Lines.ToDictionary(line => line.LineId, line => line.OfferId);
        var sellerByCartLineId = cart.Lines.ToDictionary(line => line.LineId, line => line.SellerPartyId);

        // مالک هر گروه از <b>انتساب‌های واقعی همین لحظه</b> دوباره ساخته می‌شود (همان سیاست مشترک
        // خواندن/نوشتن/ثبت) و سپس با مالک ذخیره‌شده <b>عیناً</b> مقایسه می‌شود؛ شاهد هرگز اعتبارسنج
        // خودش نیست و هیچ مالک/tenant/فروشنده‌ای از خود شاهد یا از بدنه پذیرفته نمی‌شود.
        var groupOwners = new Dictionary<Guid, StorefrontCapturedOwner>(groups.Length);
        foreach (var group in groups)
        {
            var evidence = group.AcceptedEvidence!;
            if (!string.Equals(evidence.Currency, group.Currency, StringComparison.Ordinal))
            {
                throw new ContractOperationException(CapturedConflict);
            }

            groupOwners[group.ShippingGroupId] = ResolveGroupOwner(
                group,
                assignments,
                offerByCartLineId,
                sellerByCartLineId,
                types,
                evidence);
        }

        // یک اشاره‌گر ۲۱۹ برای هر جفت مالک+Binding ذخیره‌شده (نه enumeration همهٔ نامزدهای ممکن).
        var capturedPlans = new List<StorefrontCapturedGroupPlan>(groups.Length);
        var pointers = new Dictionary<StorefrontCapturedPublicationBinding, ShippingSelectedCapturedRevisionResult>();
        // TB-233-R3: یک خواندن فهرست فعال سرویس/گزینه برای همین ثبت نهایی؛ نبود/غیرفعالی هر روش
        // ذخیره‌شده صریحاً رد می‌شود (گزینهٔ تهی همان هویت غایب است، بدون تطبیق تقریبی).
        var catalog = await _shippingCatalog!.ListAsync(cancellationToken);
        foreach (var group in groups)
        {
            var evidence = group.AcceptedEvidence!;
            var serviceCode = group.ServiceCode?.Trim();
            if (string.IsNullOrEmpty(serviceCode))
            {
                throw new ContractOperationException(CapturedUnavailable);
            }

            RequireActiveCatalogBinding(catalog, serviceCode, group.OptionCode);

            // هویت درخواست ۲۱۹ از مالک بازسازی‌شدهٔ همین گروه می‌آید: Store با tenant، Marketplace
            // بدون کلید و Seller با فروشندهٔ واقعی گروه. هیچ مالکی به Store گِرد نمی‌شود.
            var owner = groupOwners[group.ShippingGroupId];
            var binding = StorefrontCapturedPublicationBinding.For(
                owner.Kind,
                owner.TenantId,
                owner.SellerPartyId,
                serviceCode,
                group.OptionCode);
            if (!pointers.TryGetValue(binding, out var pointer))
            {
                pointer = await _selectedCaptures!.ReadAsync(
                    new ShippingSelectedCapturedRevisionRequest(
                        owner.Kind,
                        owner.TenantId,
                        owner.SellerPartyId,
                        serviceCode,
                        group.OptionCode),
                    cancellationToken);
                pointers[binding] = pointer;
            }

            if (pointer.Verdict != ShippingSelectedCaptureVerdict.Selected
                || pointer.DraftId != evidence.DraftId
                || pointer.SourceRevision != evidence.SourceRevision
                || pointer.SelectionVersion != evidence.SelectionVersion
                || pointer.SnapshotId != evidence.SnapshotId
                || pointer.FormatVersion != evidence.FormatVersion
                || pointer.ShippingTypeId != evidence.ShippingTypeId
                // پاسخ مالک باید همان Binding درخواستی را بیان کند؛ پاسخ متناقض پذیرفته نمی‌شود.
                || !string.Equals(
                    NormalizeBindingText(pointer.ServiceCode),
                    NormalizeBindingText(serviceCode),
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    NormalizeBindingText(pointer.OptionCode),
                    NormalizeBindingText(group.OptionCode),
                    StringComparison.OrdinalIgnoreCase))
            {
                // اشاره‌گر عوض‌شده/ناموجود: انتخاب جدید لازم است؛ هیچ fallback به «آخرین» نیست.
                throw new ContractOperationException(CapturedConflict);
            }

            var candidate = new StorefrontCapturedCandidatePlan(
                CanonicalBinding(serviceCode) + "\u001F"
                    + (CanonicalBinding(group.OptionCode) ?? string.Empty),
                owner.Kind,
                owner.TenantId,
                owner.SellerPartyId,
                evidence.DraftId,
                evidence.SourceRevision,
                destination,
                serviceCode,
                group.OptionCode,
                options.AvailableDateCount ?? 0,
                evidence.SnapshotId,
                evidence.SelectionVersion);

            // کلید گروه همان هویت پایدار persist‌شدهٔ گروه ارسال است؛ فکت‌ها و پلن‌ها هرگز با
            // روش/برچسب/اندیس به هم گره نمی‌خورند و دو فروشنده روی یک Binding دو کلید جدا دارند.
            capturedPlans.Add(new StorefrontCapturedGroupPlan(
                group.ShippingGroupId.ToString("N"),
                group.Lines
                    .OrderBy(line => line.CartLineId)
                    .Select(line => line.CartLineId)
                    .ToArray(),
                [candidate]));
        }

        var composed = await _capturedComposition!.ComposeFromFactsAsync(
            command,
            capturedPlans,
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
                // ارجاع‌های نقل‌قول باید همان بازبینی/پیش‌نویس/نسخهٔ قالب شاهد ذخیره‌شده باشند.
                || quote.SnapshotId != evidence.SnapshotId
                || quote.FormatVersion != evidence.FormatVersion
                || quote.DraftId != evidence.DraftId
                || quote.SourceRevision != evidence.SourceRevision
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
        return new CapturedShippingAcceptance(accepted, stored.CartVersion, stored.SelectionRevision);
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
    /// قفل ردیف ریشهٔ پیش‌نویس <b>در همان تراکنش submit</b> و مقایسهٔ نسخهٔ انتخاب با نسخهٔ پذیرفته‌شده.
    /// </summary>
    /// <remarks>
    /// این یک قفل نقطه‌ای روی ردیف Order است (نه قفل توزیع‌شدهٔ Shipping و نه ادعای serializability):
    /// نویسندهٔ انتخاب که ریشه را جایگزین می‌کند همان ردیف را به‌روزرسانی می‌کند، پس تا پایان تراکنش
    /// هیچ نسخهٔ مخلوطی نمی‌تواند پایدار شود. مالکیت نقطه‌ای است: فقط در همان لحظه خوانده می‌شود.
    /// </remarks>
    private async Task LockStoredCapturedRootAsync(
        Guid cartId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        var revisions = await _db.Database
            .SqlQueryRaw<long>(
                "SELECT selection_revision AS \"Value\" FROM \"order\".cart_shipping_drafts "
                + "WHERE cart_id = {0} FOR UPDATE",
                cartId)
            .ToListAsync(cancellationToken);

        if (revisions.Count != 1 || revisions[0] != expectedRevision)
        {
            throw new ContractOperationException(CapturedConflict);
        }
    }

    /// <summary>
    /// مقصد نهاییِ همین submit: کشور متعارف را از <b>دادهٔ مورداعتماد فعلی</b> برای شناسهٔ کشور
    /// قیمت‌گذاری‌شده می‌گیرد (نه null، نه حدس/کشور اول)، نشانی را با هویت واقعی سبد/ثبت‌کننده
    /// اعتبارسنجی می‌کند و سپس متن معتبر سرور را دقیقاً resolve می‌کند.
    /// </summary>
    /// <remarks>
    /// <b>Saved address.</b> وقتی شناسهٔ نشانی ذخیره‌شده روی ریشهٔ قیمت‌گذاری‌شده موجود است، همان
    /// رکورد با actor واقعی از کتاب آدرس خوانده می‌شود و کشور/استان/شهر/کد پستی/نشانی/موبایل آن باید
    /// عیناً همان مقادیر قیمت‌گذاری‌شده باشد؛ نشانی حذف‌شده/بیگانه/تغییرکرده پیش از هر رزرو رد می‌شود.
    /// نشانی بدون شناسه هم باید عیناً با همان تصویر ذخیره‌شده برابر باشد. هیچ شناسه/کاربری از بدنه
    /// مرورگر گرفته نمی‌شود و مالکیت با شناسهٔ کاربر سبد و ثبت‌کننده بررسی می‌شود.
    /// </remarks>
    private async Task<DestinationIdentifiers> ResolveAcceptedAddressAsync(
        SubmitCheckoutCommand command,
        CartSnapshot cart,
        CartShippingDraft draft,
        CancellationToken cancellationToken)
    {
        if (_geography is null || _addressBook is null)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        var countryCode = await ResolveCanonicalCountryCodeAsync(draft.DestinationCountryId, cancellationToken);

        if (draft.SavedAddressId is Guid savedAddressId && savedAddressId != Guid.Empty)
        {
            var actor = ResolveAcceptedAddressActor(command, cart);
            if (actor is null)
            {
                throw new ContractOperationException(CapturedDestinationUnverified);
            }

            var saved = await _addressBook.GetAsync(actor.Value, savedAddressId, cancellationToken);
            if (saved is null
                || !StorefrontAddressCanonical.Same(
                    saved.Country,
                    saved.ProvinceName,
                    saved.CityName,
                    saved.PostalAddress,
                    saved.PostalCode,
                    saved.ContactMobile,
                    countryCode,
                    draft.ProvinceName,
                    draft.CityName,
                    draft.PostalAddress,
                    draft.PostalCode,
                    draft.ContactMobile))
            {
                // رکورد حذف‌شده/بیگانه/تغییرکرده: نشانی قیمت‌گذاری‌شده دیگر معتبر نیست.
                throw new ContractOperationException(CapturedDestinationUnverified);
            }
        }

        // نشانی واقعی که پایدار می‌شود باید عیناً همان نشانیِ قیمت‌گذاری‌شده باشد؛ آدرس ذخیره‌شده و
        // آدرس بدون شناسه هر دو از همین یک مقایسه می‌گذرند (بدون معنای نشانی تازه).
        if (!StorefrontAddressCanonical.Same(
                countryCode,
                command.ProvinceName,
                command.CityName,
                command.PostalAddress,
                command.PostalCode,
                command.ContactMobile,
                countryCode,
                draft.ProvinceName,
                draft.CityName,
                draft.PostalAddress,
                draft.PostalCode,
                draft.ContactMobile))
        {
            throw new ContractOperationException(CapturedDestinationUnverified);
        }

        var resolution = await _geography.ResolveLegacyAsync(
            countryCode,
            string.IsNullOrWhiteSpace(draft.ProvinceName) ? null : draft.ProvinceName,
            string.IsNullOrWhiteSpace(draft.CityName) ? null : draft.CityName,
            // ناحیهٔ خدمت پشتیبانی نمی‌شود و هرگز از نام حدس زده نمی‌شود.
            serviceAreaName: null,
            cancellationToken);

        if (resolution.Outcome != DestinationResolutionOutcome.Resolved
            || resolution.Identifiers is not { } identifiers
            || identifiers.CountryId != draft.DestinationCountryId)
        {
            throw new ContractOperationException(CapturedDestinationUnverified);
        }

        return identifiers;
    }

    /// <summary>
    /// کد کشور متعارف و غیرتهی برای شناسهٔ کشور قیمت‌گذاری‌شده از فهرست مورداعتماد فعلی؛ تطبیق باید
    /// یگانه و دقیق باشد، وگرنه هیچ کشوری جایگزین/پیش‌فرض نمی‌شود.
    /// </summary>
    private async Task<string> ResolveCanonicalCountryCodeAsync(
        Guid? pricedCountryId,
        CancellationToken cancellationToken)
    {
        if (pricedCountryId is not Guid countryId || countryId == Guid.Empty || _geography is null)
        {
            throw new ContractOperationException(CapturedDestinationUnverified);
        }

        var countries = await _geography.ListCountriesAsync(cancellationToken);
        var matches = countries
            .Where(country => country.CountryId == countryId)
            .Select(country => country.CountryCode?.Trim())
            .Where(code => !string.IsNullOrEmpty(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (matches.Length != 1)
        {
            throw new ContractOperationException(CapturedDestinationUnverified);
        }

        return matches[0]!;
    }

    /// <summary>
    /// هویت واقعیِ صاحب نشانی: شناسهٔ کاربر سبد و ثبت‌کننده باید یکی باشند و دسترسی سبد با همان
    /// کاربر هم‌خوان باشد. هیچ userId از فیلدهای مالکِ بدنه گرفته نمی‌شود.
    /// </summary>
    private static Guid? ResolveAcceptedAddressActor(SubmitCheckoutCommand command, CartSnapshot cart)
    {
        if (command.PlacedByUserId == Guid.Empty
            || cart.OwnerUserId is not Guid owner
            || owner != command.PlacedByUserId
            || command.CartAccess.UserId != owner)
        {
            return null;
        }

        return owner;
    }

    /// <summary>
    /// هر روش ذخیره‌شده باید در فهرست <b>فعال</b> فروشگاه دقیقاً یک سرویس هم‌کد و (اگر گزینه دارد)
    /// دقیقاً یک گزینهٔ فعال هم‌کد داشته باشد؛ گزینهٔ تهی همان هویت غایب است و تطبیق تقریبی ممنوع.
    /// </summary>
    private static void RequireActiveCatalogBinding(
        IReadOnlyList<ShippingCatalogServiceSnapshot> catalog,
        string serviceCode,
        string? optionCode)
    {
        var services = catalog
            .Where(service => service.IsActive
                && string.Equals(
                    NormalizeBindingText(service.Code),
                    NormalizeBindingText(serviceCode),
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (services.Length != 1)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        var normalizedOption = NormalizeBindingText(optionCode);
        if (normalizedOption is null)
        {
            return;
        }

        var options = services[0].Options
            .Where(option => option.IsActive
                && string.Equals(
                    NormalizeBindingText(option.Code),
                    normalizedOption,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (options.Length != 1)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }
    }

    private static string? NormalizeBindingText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>مقایسهٔ هویت یکتای مقصد فعلی با مقصد قیمت‌گذاری‌شدهٔ ذخیره‌شده روی ریشهٔ پیش‌نویس.</summary>
    private static bool SameDestination(CartShippingDraft draft, DestinationIdentifiers current) =>
        draft.DestinationCountryId is Guid country
        && country == current.CountryId
        && draft.DestinationAdministrativeAreaId == current.AdministrativeAreaId
        && draft.DestinationCityId == current.CityId
        // ناحیهٔ خدمت پشتیبانی نمی‌شود مگر آدرس مورداعتماد همان هویت را بیان کند؛ هیچ حدسی نیست.
        && draft.DestinationServiceAreaId is null
        && current.ServiceAreaId is null;

    /// <summary>
    /// مالک <b>واقعی</b> یک گروه ذخیره‌شده را دوباره از انتساب‌های همین لحظه می‌سازد (همان
    /// <see cref="StorefrontCapturedOwnerPolicy"/> مشترک خواندن/نوشتن/ثبت) و آن را با فروشندهٔ گروه و
    /// مالک ذخیره‌شده <b>عیناً</b> می‌سنجد.
    /// </summary>
    /// <remarks>
    /// هر خط گروه باید همان فروشندهٔ واقعی گروه باشد و همان مسئولیت/نوع انتساب را بیان کند؛ گروه مخلوط
    /// (دو مسئولیت یا دو نوع در یک گروه) هرگز پذیرفته نمی‌شود. مالک ذخیره‌شده شاهد نیست: اگر با مالک
    /// بازساخته‌شده نخواند، ثبت نهایی رد می‌شود و هیچ مالکی بازنویسی/گِرد نمی‌شود.
    /// </remarks>
    private StorefrontCapturedOwner ResolveGroupOwner(
        CartShippingDraftGroup group,
        IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot> assignments,
        IReadOnlyDictionary<Guid, Guid> offerByCartLineId,
        IReadOnlyDictionary<Guid, Guid> sellerByCartLineId,
        IReadOnlyList<ShippingTypeSnapshot> types,
        ShippingGroupAcceptedEvidence evidence)
    {
        if (group.Lines.Count == 0)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        OfferFulfillmentResponsibility? responsibility = null;
        Guid? assignedTypeId = null;
        foreach (var line in group.Lines)
        {
            // فروشندهٔ هر خط باید همان فروشندهٔ ذخیره‌شدهٔ گروه باشد؛ هیچ خط بیگانه‌ای در گروه نمی‌نشیند.
            if (!sellerByCartLineId.TryGetValue(line.CartLineId, out var seller)
                || seller != group.SellerPartyId)
            {
                throw new ContractOperationException(CapturedConflict);
            }

            if (!offerByCartLineId.TryGetValue(line.CartLineId, out var offerId)
                || !assignments.TryGetValue(offerId, out var assignment)
                || assignment.SellerPartyId != group.SellerPartyId)
            {
                throw new ContractOperationException(CapturedUnavailable);
            }

            if (responsibility is null)
            {
                responsibility = assignment.Responsibility;
                assignedTypeId = assignment.ShippingTypeId;
                continue;
            }

            if (responsibility != assignment.Responsibility || assignedTypeId != assignment.ShippingTypeId)
            {
                // گروه مخلوط (مسئولیت/نوع متفاوت در یک گروه) هرگز یک مالک واحد ندارد.
                throw new ContractOperationException(CapturedConflict);
            }
        }

        // نوع انتساب همین لحظه باید همان نوع شاهد ذخیره‌شده باشد و آن نوع باید واقعاً فعال باشد.
        if (assignedTypeId != evidence.ShippingTypeId)
        {
            throw new ContractOperationException(CapturedConflict);
        }

        if (evidence.ShippingTypeId is Guid typeId
            && !types.Any(type => type.ShippingTypeId == typeId && type.IsActive))
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            _edition?.Current?.Edition,
            _tenant?.Current,
            responsibility,
            group.SellerPartyId);
        if (resolved.Failure != StorefrontCapturedOwnerFailure.None || resolved.Owner is null)
        {
            throw new ContractOperationException(CapturedUnavailable);
        }

        if (!resolved.Owner.IsSameAs(evidence.Owner))
        {
            // مالک ذخیره‌شده با مالک بازساخته‌شدهٔ همین لحظه نمی‌خواند: هیچ بازنویسی/گِرد کردنی نیست.
            throw new ContractOperationException(CapturedConflict);
        }

        return resolved.Owner;
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

/// <summary>
/// نتیجهٔ <b>تایپ‌شدهٔ</b> پذیرش گروه‌های capture‌شده روی نقل‌قول همین لحظه.
/// </summary>
/// <remarks>
/// این مقدار «رسید» پذیرش است: گروه‌های <b>پذیرفته‌شده</b> به‌همراه نسخهٔ <b>واقعی</b> سبد و نسخهٔ
/// <b>واقعی</b> ریشهٔ انتخاب که همین لحظه خوانده شدند. مسیر خواندنی نمای پرداخت باید نمای خود را از
/// همین مقادیر بسازد، نه از فرمان کهنه؛ و نسخهٔ انتخاب به‌شکل متن دقیق به فراخوان برگردانده می‌شود تا
/// CAS بعدی همان نسخه را بفرستد.
/// </remarks>
/// <param name="Groups">گروه‌های پذیرفته‌شده (در مسیر قدیمی همان پلن‌های فراخوان).</param>
/// <param name="CartVersion">نسخهٔ واقعی سبدی که همین پذیرش بر آن اجرا شد.</param>
/// <param name="SelectionRevision">نسخهٔ واقعی ریشهٔ انتخاب، یا تهی در مسیر قدیمی.</param>
internal sealed record CapturedShippingAcceptance(
    IReadOnlyList<OrderShippingGroupPlan> Groups,
    int CartVersion,
    long? SelectionRevision)
{
    /// <summary>همان نسخه به‌شکل <b>متن اعشاری دقیق</b> که کلاینت در CAS بعدی می‌فرستد.</summary>
    public string? SelectionRevisionText =>
        SelectionRevision?.ToString(CultureInfo.InvariantCulture);
}
