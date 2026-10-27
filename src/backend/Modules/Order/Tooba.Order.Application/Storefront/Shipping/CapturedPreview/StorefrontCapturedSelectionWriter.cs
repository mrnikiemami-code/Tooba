using System.Globalization;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedPreview;

/// <summary>
/// کدهای پایدار رد انتخاب capture‌شده (TB-232). هیچ‌کدام «آمادهٔ تسویه» یا مجوز نیستند.
/// </summary>
public static class StorefrontCapturedSelectionReasons
{
    /// <summary>کلید نوشتن خاموش است؛ قصد capture هرگز به مسیر قدیمی نمی‌افتد.</summary>
    public const string Disabled = "storefront.captured_selection.disabled";

    /// <summary>ترکیب قصد capture با shorthand قدیمی یا نامعلوم/تکراری/ناقص است.</summary>
    public const string IntentInvalid = "storefront.captured_selection.intent_invalid";

    /// <summary>گروه/نوع/نامزد قیمت‌دار نیست (یا مسئولیت/انتساب کامل نیست).</summary>
    public const string Unavailable = "storefront.captured_selection.unavailable";

    /// <summary>تاریخ انتخاب‌شده در تقویم همان گروه نیست.</summary>
    public const string DateUnavailable = "storefront.captured_selection.date_unavailable";

    /// <summary>بازتاب قصد با وضعیت جاری سرور نمی‌خواند (سبد/اشاره‌گر/ردیف/مقصد/نسخه).</summary>
    public const string Conflict = "storefront.captured_selection.conflict";

    /// <summary>نشانی آماده‌شده به مقصد یکتا و پشتیبانی‌شده نگاشت نشد.</summary>
    public const string DestinationUnverified = "storefront.captured_selection.destination_unverified";

    /// <summary>پیش‌نویس ذخیره‌شده شاهد capture دارد و مسیر قدیمی نمی‌تواند آن را جایگزین/بازقیمت کند.</summary>
    public const string EvidencePresent = "storefront.captured_selection.evidence_present";

    /// <summary>
    /// TB-232-R1: قصد کوپن در این برش پشتیبانی نمی‌شود. خواندن capture هیچ قصد کوپنی ندارد، پس
    /// پذیرفتن کوپن در نوشتن می‌توانست مبلغ کالای دیگری را با همان توکن‌های بازتاب ذخیره کند؛
    /// به‌جای نادیده‌گرفتن بی‌صدا، پیش از هر نقل‌قول/نوشتنی صریحاً رد می‌شود.
    /// </summary>
    public const string CouponUnavailable = "storefront.captured_selection.coupon_unavailable";

    /// <summary>
    /// TB-232-R1: در همان context کار افزودن/تغییر/حذفِ نامرتبطِ در انتظار وجود دارد. نوشتن پیش از
    /// هر تغییر/تراکنش رد می‌شود تا کار فراخوان هرگز flush/پاک نشود.
    /// </summary>
    public const string PendingWork = "storefront.captured_selection.pending_work";
}

/// <summary>
/// نویسندهٔ اتمیک انتخاب capture‌شده: همان resolve خواندن، اعتبارسنجی قصد نامعتمد، ساخت شاهد فشردهٔ ۲۳۱
/// روی گروه‌های واقعی و یک جایگزینی اتمیک با CAS نسخهٔ انتخاب.
/// </summary>
/// <remarks>
/// <para>
/// <b>No client authority.</b> هیچ مالک/فروشنده/مبلغ/صلاحیت/شاهد از بدنه پذیرفته نمی‌شود؛ کلیدهای
/// کلاینت فقط برنامهٔ سرور را <b>مکان‌یابی</b> می‌کنند و هر مقدار بازتابی فقط برای مقایسه است.
/// </para>
/// <para>
/// <b>One atomic write.</b> حذف/درج گروه‌ها و ریشه در یک تراکنش انجام می‌شود؛ هیچ حذف جزئی durable
/// نمی‌شود و هیچ سفارش/رزرو/پرداخت/تبدیلی ساخته نمی‌شود. مالک هر گروه همان مالک resolve‌شدهٔ سرور است:
/// Store با tenant فعال، Marketplace بدون کلید و Seller با فروشندهٔ واقعی همان گروه.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedSelectionWriter(
    StorefrontCapturedPreviewPlanner planner,
    IStorefrontShippingDraftStore drafts,
    IGeographyLookup? geography,
    IClock clock)
{
    private const string SupportedCountryCode = "IR";

    /// <summary>آیا نوشتن انتخاب capture‌شده روشن است.</summary>
    public bool IsEnabled => planner.SelectionEnabled;

    /// <summary>
    /// انتخاب capture‌شده را با بازاعتبارسنجی کامل سرور ثبت می‌کند.
    /// </summary>
    /// <param name="cart">سبد authoritative همین درخواست.</param>
    /// <param name="access">دسترسی سبد از مرز اعتماد.</param>
    /// <param name="prepared">نشانی/گیرندهٔ سمت سرور (همان چیزی که واقعاً قیمت می‌گیرد).</param>
    /// <param name="existing">پیش‌نویس موجود یا تهی.</param>
    /// <param name="guestSecretHash">هش راز مهمان (یا تهی).</param>
    /// <param name="request">قصد انتخاب نامعتمد.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task<CartShippingDraft> SaveAsync(
        CartPage cart,
        CartAccess access,
        StorefrontCheckoutPlacement prepared,
        CartShippingDraft? existing,
        string? guestSecretHash,
        StorefrontShippingSelectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(prepared);
        ArgumentNullException.ThrowIfNull(request);

        var intents = request.CapturedGroups;
        if (intents is not { Count: > 0 })
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        // خاموش‌بودن کلید نوشتن حتی وقتی کسی مستقیم این نویسنده را صدا بزند fail-closed است.
        if (!IsEnabled)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.Disabled);
        }

        // هر shorthand قدیمی در حضور قصد capture مبهم است و پذیرفته نمی‌شود (بدون fallback).
        if (!string.IsNullOrWhiteSpace(request.ShippingMethodCode)
            || !string.IsNullOrWhiteSpace(request.SelectedDeliveryDate)
            || !string.IsNullOrWhiteSpace(request.SelectedDeliveryTimeWindow)
            || request.ShippingGroups is { Count: > 0 })
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        // TB-236: قصد کوپن یک توکنِ تأیید است، نه سیاست تخفیف. فاصله/تهی یعنی نبود و متن فقط trim
        // می‌شود (بدون case-fold/برش)؛ قیمت/تخفیف همیشه از همان مسیر ۲۱۷ می‌آید.
        var couponIntent = StorefrontCouponIntent.Normalize(request.CouponCode);

        // شکل ارزانِ قصد پیش از هر خواندن بیرونی: کلید تکراری/تهی و توکن نسخهٔ اجباری.
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var intent in intents)
        {
            if (intent is null
                || string.IsNullOrWhiteSpace(intent.GroupKey)
                || (intent.ServiceCode ?? string.Empty).Trim().Length == 0
                || !seenKeys.Add(intent.GroupKey))
            {
                throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
            }
        }

        // TB-232-R1: نسخهٔ موردانتظار باید <b>صریح</b> بیاید؛ نبود/خالی «نسخهٔ صفر» نیست. متن «0» همان
        // نبود/ردیف قدیمی است و همچنان معتبر می‌ماند.
        var expectedRevision = ParseExpectedRevision(request.ExpectedSelectionRevision);

        // مقصد قیمت = نشانی واقعیِ آماده‌شده سمت سرور، نه یک جفت دلخواه مرورگر.
        var destination = await ResolvePreparedDestinationAsync(prepared, cancellationToken);

        var resolved = await planner.ResolveAsync(
            cart,
            access,
            destination,
            currency: null,
            languageId: Guid.Empty,
            cancellationToken,
            couponIntent);

        if (resolved.Context is not { } context)
        {
            throw Refuse(resolved.Early?.NoticeReason ?? StorefrontCapturedSelectionReasons.Unavailable);
        }

        var now = clock.UtcNow;

        // TB-233-R3: شناسهٔ واقعی آدرس ذخیره‌شده فقط از منشأ سرور می‌آید؛ تصویر snapshot قدیمی تهی است
        // و «نامعلوم» هرگز به یک شناسهٔ حدسی تبدیل نمی‌شود.
        var sourceSavedAddressId = prepared.SourceSavedAddressId ?? prepared.Shipping.SavedAddressId;

        var requests = new List<CartShippingGroupRequest>(intents.Count);
        var eligible = new List<CartShippingGroupEligibleMethod>(intents.Count);
        var commitments = new List<CapturedCommitment>(intents.Count);

        foreach (var intent in intents)
        {
            var plan = context.Plans.FirstOrDefault(group => string.Equals(group.GroupKey, intent.GroupKey, StringComparison.Ordinal))
                ?? throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
            var composedGroup = context.Composition.Groups
                .FirstOrDefault(group => string.Equals(group.GroupKey, plan.GroupKey, StringComparison.Ordinal))
                ?? throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);

            var candidate = plan.Candidates.FirstOrDefault(c =>
                    SameCode(c.ServiceCode, intent.ServiceCode) && SameCode(c.OptionCode, intent.OptionCode))
                ?? throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);

            var composed = composedGroup.Candidates
                .FirstOrDefault(c => string.Equals(c.Identity.MethodCode, candidate.MethodCode, StringComparison.Ordinal))
                ?? throw Refuse(StorefrontCapturedSelectionReasons.Unavailable);

            // هیچ گروهی نباید بی‌قیمت/بدون‌تاریخ عبور کند.
            if (composed.Outcome != StorefrontCapturedCandidateOutcome.Quoted
                || composed.OwnerResult is not { } quote
                || quote.IsQuoted is false
                || composed.Receiving is not { } receiving)
            {
                throw Refuse(StorefrontCapturedSelectionReasons.Unavailable);
            }

            if (candidate.OwnerKind is not (ShippingCapturedQuoteOwnerKind.Store
                or ShippingCapturedQuoteOwnerKind.Marketplace
                or ShippingCapturedQuoteOwnerKind.Seller))
            {
                // گونهٔ مالک ناشناخته هرگز با پیش‌فرض Store جایگزین نمی‌شود.
                throw Refuse(StorefrontCapturedSelectionReasons.Unavailable);
            }

            AssertEcho(intent, candidate, quote);
            AssertGoodsEcho(intent, composedGroup);

            var expectedDate = (intent.SelectedDeliveryDate ?? string.Empty).Trim();
            var slot = receiving.AvailableDates.FirstOrDefault(
                s => string.Equals(
                    s.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    expectedDate,
                    StringComparison.Ordinal));
            if (slot is null)
            {
                throw Refuse(StorefrontCapturedSelectionReasons.DateUnavailable);
            }

            // فروشندهٔ همین گروه از خطوط authoritative همان گروه می‌آید (همان فروشنده‌ای که
            // CartShippingGroupPlanner برای یکتایی/تعهد استفاده می‌کند)، نه از یک مقدار اختیاری ترکیب.
            var sellerPartyId = CartSellerOf(cart, plan.CartLineIds);

            var evidence = BuildEvidence(candidate, quote, slot, sellerPartyId);
            var window = string.Create(
                CultureInfo.InvariantCulture,
                $"{slot.OpensAt:HH\\:mm}-{slot.ClosesAt:HH\\:mm}");

            requests.Add(new CartShippingGroupRequest(
                candidate.MethodCode,
                candidate.ServiceCode,
                candidate.OptionCode,
                OriginContext: null,
                plan.CartLineIds,
                SelectedDeliveryDate: slot.Date,
                SelectedDeliveryTimeWindow: window));

            var label = StorefrontCapturedPreviewPlanner.LabelOf(context.Labels, candidate)
                ?? candidate.ServiceCode;
            eligible.Add(new CartShippingGroupEligibleMethod(
                candidate.MethodCode,
                candidate.ServiceCode,
                candidate.OptionCode,
                label,
                quote.PayableAtCheckoutAmount!.Value,
                quote.CurrencyCode!,
                quote.MinDeliveryDays ?? 0,
                slot.Date,
                sellerPartyId));

            // TB-232-R1: شاهد به «تعهد کاملِ همان گروه سرور» بسته می‌شود، نه به کد روش. دو فروشندهٔ
            // متفاوت با همان سرویس/گزینهٔ Store دقیقاً یک MethodCode دارند؛ کلید روش باعث می‌شد شاهد
            // گروه دوم شاهد گروه اول را بازنویسی کند.
            commitments.Add(new CapturedCommitment(
                plan.GroupKey,
                sellerPartyId,
                candidate.MethodCode,
                candidate.ServiceCode,
                candidate.OptionCode,
                plan.CartLineIds.ToHashSet(),
                composedGroup.FinalGoodsAmountAfterDiscountAndTax,
                evidence));
        }

        // همهٔ گروه‌های سرور باید انتخاب شوند؛ گروه ناقص/ناشناخته کل نوشتن را می‌بندد.
        if (seenKeys.Count != context.Plans.Count)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        IReadOnlyList<CartShippingDraftGroup> groups;
        try
        {
            groups = CartShippingGroupPlanner.Plan(
                cart.CartId,
                cart.Lines.Select(x => new CartShippingGroupCartLine(x.LineId, x.SellerPartyId, x.Quantity)).ToArray(),
                requests,
                eligible,
                selectedDeliveryDate: null,
                selectedDeliveryTimeWindow: null,
                now,
                UuidV7.New,
                existing?.ShippingGroups);
        }
        catch (ContractOperationException)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        // شاهد فقط روی گروه ساخته‌شدهٔ <b>همان تعهد</b> می‌نشیند. تطبیق کامل است (فروشنده + روش +
        // سرویس/گزینه + مجموعهٔ خطوط)؛ نه ایندکس لیست، نه برچسب، نه اولین تطبیق. تطبیق صفر یا بیش از
        // یک (تعهد مبهم) کل نوشتن را رد می‌کند.
        AttachEvidenceToOneGroupEach(groups, commitments, now);

        var primary = groups[0];
        var multiGroup = groups.Count > 1;
        var isNew = existing is null;
        var draft = existing ?? CartShippingDraft.Create(
            cart.CartId,
            guestSecretHash,
            cart.Version,
            prepared.Shipping.RecipientName,
            prepared.Shipping.ContactMobile,
            prepared.Shipping.ProvinceName,
            prepared.Shipping.CityName,
            prepared.Shipping.PostalAddress,
            prepared.Shipping.PostalCode,
            sourceSavedAddressId,
            multiGroup ? string.Empty : primary.MethodCode,
            multiGroup ? string.Empty : primary.MethodLabel,
            groups.Sum(x => x.ShippingAmount),
            multiGroup ? groups.Max(x => x.MinimumDeliveryDate) : primary.MinimumDeliveryDate,
            multiGroup ? null : primary.SelectedDeliveryDate,
            multiGroup ? null : primary.SelectedDeliveryTimeWindow,
            request.CustomerNote,
            now);

        if (!isNew)
        {
            draft.Replace(
                guestSecretHash,
                cart.Version,
                prepared.Shipping.RecipientName,
                prepared.Shipping.ContactMobile,
                prepared.Shipping.ProvinceName,
                prepared.Shipping.CityName,
                prepared.Shipping.PostalAddress,
                prepared.Shipping.PostalCode,
                sourceSavedAddressId,
                multiGroup ? string.Empty : primary.MethodCode,
                multiGroup ? string.Empty : primary.MethodLabel,
                groups.Sum(x => x.ShippingAmount),
                multiGroup ? groups.Max(x => x.MinimumDeliveryDate) : primary.MinimumDeliveryDate,
                multiGroup ? null : primary.SelectedDeliveryDate,
                multiGroup ? null : primary.SelectedDeliveryTimeWindow,
                request.CustomerNote,
                now);
        }

        draft.ApplyRecipientNames(prepared.Shipping.FirstName, prepared.Shipping.LastName);
        draft.SetDestinationIdentity(
            destination.CountryId,
            destination.AdministrativeAreaId,
            destination.CityId,
            destination.ServiceAreaId,
            now);
        // TB-236: توکن قصد کوپن همین انتخاب؛ «نبود» صریحاً null است و هیچ کد/صفر جای آن نمی‌نشیند.
        draft.SetSelectionCoupon(couponIntent, now);

        if (isNew)
        {
            draft.AddShippingGroups(groups, now);
        }

        return await drafts.ReplaceCapturedSelectionAsync(draft, groups, isNew, expectedRevision, cancellationToken);
    }

    /// <summary>
    /// بازتاب مبلغ کالای دیده‌شدهٔ همان گروه (TB-236) فقط <b>مقایسه</b> می‌شود: اگر کلاینت مقداری
    /// فرستاده باشد و با مبلغ authoritative همین ترکیب نخواند، وضعیت بین خواندن و نوشتن عوض شده و
    /// انتخاب کهنه رد می‌شود. نبودِ مقدار هرگز «صفر» نیست و هیچ مبلغی از کلاینت ذخیره نمی‌شود.
    /// </summary>
    private static void AssertGoodsEcho(
        StorefrontCapturedGroupSelection intent,
        StorefrontCapturedGroupProjection composedGroup)
    {
        var echo = (intent.ExpectedGoodsAmount ?? string.Empty).Trim();
        if (echo.Length == 0)
        {
            return;
        }

        if (!decimal.TryParse(echo, NumberStyles.Number, CultureInfo.InvariantCulture, out var expected)
            || expected != composedGroup.FinalGoodsAmountAfterDiscountAndTax)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.Conflict);
        }
    }

    /// <summary>مقصد از نشانی آماده‌شدهٔ سمت سرور resolve می‌شود (بدون tuple دلخواه مرورگر).</summary>
    private async Task<DestinationIdentifiers> ResolvePreparedDestinationAsync(
        StorefrontCheckoutPlacement prepared,
        CancellationToken cancellationToken)
    {
        if (geography is null)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.DestinationUnverified);
        }

        var resolution = await geography.ResolveLegacyAsync(
            SupportedCountryCode,
            prepared.Shipping.ProvinceName,
            prepared.Shipping.CityName,
            serviceAreaName: null,
            cancellationToken);

        if (resolution.Outcome != DestinationResolutionOutcome.Resolved || resolution.Identifiers is not { } resolved)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.DestinationUnverified);
        }

        return resolved;
    }

    /// <summary>
    /// بازتاب کلاینت فقط مقایسه می‌شود: هر مقدار گم/ناهمخوان یعنی وضعیت عوض شده است و هیچ مبلغی از
    /// کلاینت پذیرفته نمی‌شود.
    /// </summary>
    private static void AssertEcho(
        StorefrontCapturedGroupSelection intent,
        StorefrontCapturedCandidatePlan candidate,
        Tooba.Shipping.Contracts.Rates.ShippingCapturedQuoteResult quote)
    {
        if (!SameGuid(intent.ExpectedSnapshotId, candidate.SnapshotId)
            || !SameLong(intent.ExpectedSourceRevision, candidate.SourceRevision)
            || !SameLong(intent.ExpectedSelectionVersion, candidate.SelectionVersion)
            || !SameGuid(intent.ExpectedAreaGroupId, quote.GroupId)
            || !SameGuid(intent.ExpectedRateRowId, quote.RateRowId))
        {
            throw Refuse(StorefrontCapturedSelectionReasons.Conflict);
        }
    }

    /// <summary>شاهد فشردهٔ ۲۳۱ از همان نتیجهٔ سرور ساخته می‌شود (بدون بازمحاسبهٔ مبلغ/تعرفه).</summary>
    private static ShippingGroupAcceptedEvidence BuildEvidence(
        StorefrontCapturedCandidatePlan candidate,
        Tooba.Shipping.Contracts.Rates.ShippingCapturedQuoteResult quote,
        StorefrontShippingReceivingSlot slot,
        Guid groupSellerPartyId)
    {
        if (quote.FormatVersion is not { } formatVersion || formatVersion <= 0)
        {
            // نسخهٔ قالب ناشناخته/تهی یعنی قیمت مالک شاهد معتبر نمی‌سازد.
            throw Refuse(StorefrontCapturedSelectionReasons.Unavailable);
        }

        // مالک شاهد از همان سیاست مشترک/نامزد سرور می‌آید: Store با tenant، Marketplace بدون کلید و
        // Seller فقط با فروشندهٔ <b>واقعی همین گروه</b>. هیچ‌کدام از مالک‌ها به دیگری بازتفسیر نمی‌شود.
        var owner = new StorefrontCapturedOwner(
            candidate.OwnerKind,
            candidate.OwnerTenantId,
            candidate.OwnerSellerPartyId);

        try
        {
            var domainOwner = owner.ToDomainOwner();
            domainOwner.EnsureCoherentWithGroupSeller(groupSellerPartyId);
            return ShippingGroupAcceptedEvidence.Create(
                candidate.SnapshotId,
                candidate.DraftId,
                candidate.SourceRevision,
                formatVersion,
                candidate.SelectionVersion,
                domainOwner,
                quote.GroupId ?? Guid.Empty,
                quote.RateRowId ?? Guid.Empty,
                quote.ShippingTypeId,
                quote.FreightAmount ?? -1m,
                quote.PackagingAmount ?? -1m,
                quote.CurrencyCode ?? string.Empty,
                quote.PayableAtCheckoutAmount ?? -1m,
                quote.CarrierCollectAmount ?? -1m,
                slot.Date,
                slot.OpensAt.Ticks,
                slot.ClosesAt.Ticks,
                quote.ReceivingWeek?.TimeZoneId ?? string.Empty,
                quote.MinDeliveryDays,
                quote.MaxDeliveryDays);
        }
        catch (ContractOperationException)
        {
            // مقدار ناقص/غیرقابل‌نمایش یا مالک ناسازگار یعنی قیمت مالک برای شاهد کافی نیست؛ هیچ
            // clamp/گردکردن/جایگزینی نیست.
            throw Refuse(StorefrontCapturedSelectionReasons.Unavailable);
        }
    }

    private static long ParseExpectedRevision(string? text)
    {
        // TB-232-R1: نبود/خالی «صفر» نیست؛ نسخهٔ موردانتظار باید صریح باشد. متن «0» معتبر است.
        if (string.IsNullOrWhiteSpace(text)
            || !long.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        return parsed;
    }

    /// <summary>
    /// یک تعهد سرور برای یک گروه: کلید پایدار گروه، فروشندهٔ واقعی، هویت Binding و مجموعهٔ خطوط.
    /// </summary>
    private sealed record CapturedCommitment(
        string GroupKey,
        Guid SellerPartyId,
        string MethodCode,
        string? ServiceCode,
        string? OptionCode,
        IReadOnlySet<Guid> LineIds,
        decimal GoodsAmount,
        ShippingGroupAcceptedEvidence Evidence);

    /// <summary>
    /// هر گروه ساخته‌شده دقیقاً شاهد تعهد <b>همان</b> گروه را می‌گیرد. تطبیق صفر یا بیش از یک ⇒ رد کل
    /// نوشتن؛ هیچ شاهد گروه دیگری به ارث نمی‌رسد و هیچ گروهی بی‌شاهد نمی‌ماند.
    /// </summary>
    private static void AttachEvidenceToOneGroupEach(
        IReadOnlyList<CartShippingDraftGroup> groups,
        IReadOnlyList<CapturedCommitment> commitments,
        DateTimeOffset now)
    {
        if (groups.Count != commitments.Count)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        var remaining = new List<CapturedCommitment>(commitments);
        foreach (var group in groups)
        {
            var matches = remaining.Where(commitment => SameObligation(commitment, group)).ToArray();
            if (matches.Length != 1)
            {
                // هیچ تعهدی یا بیش از یک تعهد با همان فروشنده/روش/هویت/خطوط ⇒ مبهم؛ هیچ‌کدام پذیرفته نمی‌شود.
                throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
            }

            remaining.Remove(matches[0]);
            try
            {
                group.ReplaceAcceptedEvidence(matches[0].Evidence, now);
                // TB-236: مبلغ کالای تأییدشده از همان ترکیب authoritative همین انتخاب، روی همان گروه.
                group.ConfirmGoodsAmount(matches[0].GoodsAmount, now);
            }
            catch (ContractOperationException)
            {
                throw Refuse(StorefrontCapturedSelectionReasons.Unavailable);
            }
        }

        if (remaining.Count != 0)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }
    }

    private static bool SameObligation(CapturedCommitment commitment, CartShippingDraftGroup group)
    {
        if (group.SellerPartyId != commitment.SellerPartyId
            || !string.Equals(
                (group.MethodCode ?? string.Empty).Trim(),
                (commitment.MethodCode ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase)
            || !SameCode(group.ServiceCode, commitment.ServiceCode)
            || !SameCode(group.OptionCode, commitment.OptionCode))
        {
            return false;
        }

        if (group.Lines.Count != commitment.LineIds.Count)
        {
            return false;
        }

        return group.Lines.All(line => commitment.LineIds.Contains(line.CartLineId));
    }

    /// <summary>
    /// فروشندهٔ authoritative همین گروه از خطوط همان گروه در سبد؛ نبود/تناقض یعنی قصد نامعتبر.
    /// </summary>
    private static Guid CartSellerOf(CartPage cart, IReadOnlyList<Guid> cartLineIds)
    {
        if (cartLineIds.Count == 0)
        {
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        var sellers = cartLineIds
            .Select(lineId => cart.Lines.FirstOrDefault(line => line.LineId == lineId)?.SellerPartyId)
            .ToArray();

        if (sellers.Any(seller => seller is null) || sellers.Distinct().Count() != 1)
        {
            // خط ناشناس یا فروشندهٔ مختلط در یک گروه هرگز گروه معتبر نمی‌سازد.
            throw Refuse(StorefrontCapturedSelectionReasons.IntentInvalid);
        }

        return sellers[0]!.Value;
    }

    private static bool SameCode(string? left, string? right) =>
        string.Equals(
            (left ?? string.Empty).Trim(),
            (right ?? string.Empty).Trim(),
            StringComparison.OrdinalIgnoreCase);

    private static bool SameGuid(string? expected, Guid? actual) =>
        Guid.TryParse((expected ?? string.Empty).Trim(), out var parsed)
        && actual is Guid real
        && parsed == real;

    private static bool SameLong(string? expected, long actual) =>
        long.TryParse((expected ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
        && parsed == actual;

    private static StorefrontOrderException Refuse(string reason) => new(reason);
}
