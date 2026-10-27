using System.Globalization;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Domain;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedPreview;

/// <summary>
/// کدهای پایدار دلیل وضعیت پیش‌نمایش خواندنی گروه‌های capture‌شده.
/// </summary>
public static class StorefrontCapturedPreviewReasons
{
    /// <summary>تصویر سبد با آفر/فروشندهٔ خط نمی‌خواند یا انتساب ناقص است.</summary>
    public const string AssignmentContradictory = "storefront.captured_preview.assignment_contradictory";

    /// <summary>آفر خط در انتساب‌های خوانده‌شده نیست.</summary>
    public const string AssignmentMissing = "storefront.captured_preview.assignment_missing";

    /// <summary>آفر هنوز مسئولیت انجام ارسال نگرفته است.</summary>
    public const string Unassigned = "storefront.captured_preview.unassigned";

    /// <summary>مقدار مسئولیت خارج از دو خانوادهٔ شناخته‌شده است؛ هیچ نگاشت پیش‌فرضی جای آن نیست.</summary>
    public const string ResponsibilityInvalid = "storefront.captured_preview.responsibility_invalid";

    /// <summary>نوع ارسال انتساب‌شده در کاتالوگ فعال نیست.</summary>
    public const string TypeUnavailable = "storefront.captured_preview.type_unavailable";

    /// <summary>مسئولیت Seller در نصب SingleStore پشتیبانی نمی‌شود (نه بازتفسیر به Store).</summary>
    public const string SellerOwnerUnsupported = "storefront.captured_preview.seller_owner_unsupported";

    /// <summary>
    /// زمینهٔ نصب/tenant مورداعتماد هیچ مالک پشتیبانی‌شده‌ای نمی‌سازد (نصب ناشناخته، tenant غیرفعال/تهی
    /// یا tenant باقی‌مانده در Marketplace). هیچ مالک پیش‌فرضی جای آن نمی‌نشیند.
    /// </summary>
    public const string OwnerUnavailable = "storefront.captured_preview.owner_unavailable";

    /// <summary>هیچ capture انتخاب‌شده‌ای برای Bindingهای همان نوع پیدا نشد.</summary>
    public const string NoSelectedCapture = "storefront.captured_preview.no_selected_capture";

    /// <summary>مقصد تأییدشده برای قیمت‌گذاری موجود نیست.</summary>
    public const string DestinationRequired = "storefront.captured_preview.destination_required";

    /// <summary>ترکیب سرور نتوانست افراز/فکت‌های سبد را تأیید کند.</summary>
    public const string CompositionRefused = "storefront.captured_preview.composition_refused";

    /// <summary>سیاست تعداد تاریخ‌های پیش‌نمایش تنظیم نشده یا خارج از محدودهٔ مجاز است.</summary>
    public const string AvailableDateCountInvalid = "storefront.captured_preview.available_date_count_invalid";

    /// <summary>رد کردن انتخاب/ثبت تا برش parity و شواهد فشردهٔ سفارش — هرگز «آمادهٔ تسویه» نیست.</summary>
    public const string PreviewNotReady = "storefront.captured_preview.not_ready";
}

/// <summary>
/// نتیجهٔ resolve مشترک خواندن و انتخاب (TB-232): یا نمای نهایی/زودتر، یا زمینهٔ تایپ‌شدهٔ همان
/// افراز/نامزد/قیمت‌ها. هیچ‌کدام از این دو مسیر قیمت را از نو حساب نمی‌کند و هر دو از همین یک
/// resolve تغذیه می‌شوند.
/// </summary>
/// <param name="Early">نمای نهایی وقتی ترکیب ساخته نشد (خاموش/ناقص/بدون مقصد/امتناع).</param>
/// <param name="Context">زمینهٔ تایپ‌شدهٔ قیمت‌دار وقتی ترکیب ساخته شد.</param>
public sealed record StorefrontCapturedResolution(
    StorefrontCapturedPreviewView? Early,
    StorefrontCapturedSelectionContext? Context);

/// <summary>
/// زمینهٔ تایپ‌شدهٔ resolve: همان گروه‌های افرازشده، همان برنامهٔ نامزدها، همان <b>یک</b> ترکیب ۲۱۸ و
/// همان برچسب‌ها/دلایل. مصرف‌کنندهٔ نوشتن هیچ مبلغ/برچسبی را از این زمینه به‌عنوان ورودی نمی‌پذیرد؛
/// فقط هویت و نتیجهٔ سرور را می‌خواند.
/// </summary>
public sealed record StorefrontCapturedSelectionContext(
    string Currency,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<StorefrontCapturedPreviewGroupView> Groups,
    IReadOnlyList<StorefrontCapturedGroupPlan> Plans,
    StorefrontCapturedGroupProjectionResult Composition,
    /// <summary>
    /// برچسب نمایشی هر <b>تعهد</b> (مالک کامل + Binding) از همان ردیف DB خودش. کلید عمداً هویت کامل
    /// است، نه کد روش: دو مالک (Marketplace/Seller/Store) با همان سرویس/گزینه برچسب‌های جدا دارند.
    /// </summary>
    IReadOnlyDictionary<StorefrontCapturedPublicationBinding, string> Labels,
    IReadOnlyDictionary<string, string> Unavailable,
    (long? SelectionVersion, Guid? SnapshotId)? Provenance);

/// <summary>
/// سازندهٔ واقعی برنامهٔ گروه‌های ارسال: سبد واقعی → انتساب‌های صریح Offer → اشاره‌گرهای انتخاب‌شدهٔ
/// ۲۱۹ → <b>یک</b> ترکیب ۲۱۸.
/// </summary>
/// <remarks>
/// <para>
/// <b>No browser identity.</b> نه مالک، نه پیش‌نویس، نه بازبینی و نه مبلغی از کلاینت پذیرفته نمی‌شود.
/// مسئولیت از فیلد صریح همان آفر و مالک Seller از فروشندهٔ واقعی همان آفر می‌آید؛ Platform از زمینهٔ
/// trusted نصب. آفر بدون مسئولیت استنتاج نمی‌شود و مقدار مسئولیت ناشناخته هم رد می‌شود.
/// </para>
/// <para>
/// <b>Read only.</b> هیچ انتخاب/سفارش/رزرو/پرداخت/تبدیل سبد نوشته نمی‌شود.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedPreviewPlanner(
    IOfferFulfillmentAssignmentLookup assignments,
    IShippingCatalogReader catalog,
    IShippingTypeReader shippingTypes,
    IShippingSelectedCapturedRevisionLookup selectedCaptures,
    StorefrontCapturedGroupProjectionComposer composer,
    ICurrentEdition edition,
    ICurrentTenant tenant,
    Microsoft.Extensions.Options.IOptions<StorefrontCapturedPreviewOptions> options)
{
    /// <summary>
    /// آیا کلید خواندنی روشن است. همان منبع یگانه‌ای است که انتخاب/ثبت را هم می‌بندد.
    /// </summary>
    public bool IsEnabled => options.Value.Enabled;

    /// <summary>آیا نوشتن انتخاب capture‌شده (نویسندهٔ اتمیک ۲۳۲) مجاز است. مستقل از کلید خواندن است.</summary>
    public bool SelectionEnabled => options.Value.SelectionEnabled;

    /// <summary>
    /// آیا <b>ثبت نهایی</b> سفارش capture‌شده از مسیر عمومی مجاز است. هر چهار شرط باید هم‌زمان برقرار
    /// باشند: خواندن، نوشتن انتخاب، ثبت نهایی و تعداد تاریخ معتبر. روشن‌بودن فقط
    /// <see cref="SelectionEnabled"/> هرگز ثبت نهایی را مجاز نمی‌کند.
    /// </summary>
    public bool IsCapturedCommitEnabled =>
        options.Value.Enabled
        && options.Value.SelectionEnabled
        && options.Value.CheckoutEnabled
        && options.Value.HasValidAvailableDateCount;

    /// <summary>پیش‌نمایش گروه‌ها را می‌سازد؛ خطاهای تجاری به نمای صادقانه ترجمه می‌شوند.</summary>
    /// <remarks>
    /// تعداد تاریخ‌های تقویم فقط از سیاست صریح پیش‌نمایش (<c>AvailableDateCount</c>) می‌آید؛ افق روش‌ها
    /// هرگز به‌عنوان «تعداد تاریخ قابل ارائه» بازتفسیر نمی‌شود.
    /// </remarks>
    /// <summary>پیش‌نمایش گروه‌ها را می‌سازد؛ خطاهای تجاری به نمای صادقانه ترجمه می‌شوند.</summary>
    public async Task<StorefrontCapturedPreviewView> BuildAsync(
        CartPage cart,
        CartAccess access,
        DestinationIdentifiers? destination,
        string? currency,
        Guid languageId,
        CancellationToken cancellationToken,
        string? couponCode = null)
    {
        var resolution = await ResolveAsync(cart, access, destination, currency, languageId, cancellationToken, couponCode);
        if (resolution.Early is { } early)
        {
            return early;
        }

        var context = resolution.Context!;
        var composed = context.Composition;
        var plans = context.Plans;
        var groups = context.Groups;
        var labels = context.Labels;
        var unavailable = context.Unavailable;
        var today = DateOnly.FromDateTime(composed.EvaluatedAtUtc.UtcDateTime);

        var views = new List<StorefrontCapturedPreviewGroupView>(composed.Groups.Count);
        foreach (var composedGroup in composed.Groups)
        {
            var readGroup = groups.First(group => group.GroupKey == composedGroup.GroupKey);
            var methods = composedGroup.Candidates
                .Where(candidate => candidate.Outcome == StorefrontCapturedCandidateOutcome.Quoted
                    && candidate.OwnerResult is not null)
                .Select(candidate => new StorefrontCapturedPreviewMethodView(
                    LabelOf(labels, candidate.Identity) ?? candidate.Identity.ServiceCode,
                    candidate.Identity.ServiceCode,
                    candidate.Identity.OptionCode,
                    ToText(candidate.OwnerResult!.FreightAmount),
                    ToText(candidate.OwnerResult.PackagingAmount),
                    ToText(candidate.OwnerResult.PayableAtCheckoutAmount),
                    ToText(candidate.OwnerResult.CarrierCollectAmount),
                    string.IsNullOrWhiteSpace(candidate.OwnerResult.CurrencyCode)
                        ? null
                        : candidate.OwnerResult.CurrencyCode,
                    candidate.Receiving?.EarliestEligibleDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    candidate.Receiving is null
                        ? []
                        : candidate.Receiving.AvailableDates
                            .Select(slot => new StorefrontCapturedDateSlotView(
                                slot.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                StorefrontShippingCalculator.FormatDeliveryDateLabelFa(slot.Date, today),
                                StorefrontShippingCalculator.FormatDeliveryDateSubLabelFa(slot.Date),
                                $"{slot.OpensAt.ToString("HH\\:mm", CultureInfo.InvariantCulture)}–{slot.ClosesAt.ToString("HH\\:mm", CultureInfo.InvariantCulture)}",
                                candidate.Receiving.EarliestEligibleDate == slot.Date))
                            .ToArray(),
                    candidate.Receiving?.UnavailableReason
                        ?? (candidate.Outcome == StorefrontCapturedCandidateOutcome.Quoted
                            ? null
                            : candidate.ReasonCode),
                    candidate.Identity.SnapshotId == Guid.Empty
                        ? null
                        : candidate.Identity.SnapshotId.ToString("D"),
                    candidate.Identity.SourceRevision > 0
                        ? candidate.Identity.SourceRevision.ToString(CultureInfo.InvariantCulture)
                        : null,
                    candidate.Identity.SelectionVersion > 0
                        ? candidate.Identity.SelectionVersion.ToString(CultureInfo.InvariantCulture)
                        : null,
                    candidate.OwnerResult.GroupId?.ToString("D"),
                    candidate.OwnerResult.RateRowId?.ToString("D")))
                .ToArray();

            var methodReason = composedGroup.Candidates
                .Where(candidate => candidate.Outcome != StorefrontCapturedCandidateOutcome.Quoted)
                .Select(candidate => candidate.ReasonCode)
                .FirstOrDefault();

            var reason = readGroup.UnavailableReason
                ?? (unavailable.TryGetValue(composedGroup.GroupKey, out var plannedReason)
                    ? plannedReason
                    : methods.Length == 0
                        ? methodReason ?? StorefrontCapturedPreviewReasons.NoSelectedCapture
                        : null);

            views.Add(new StorefrontCapturedPreviewGroupView(
                composedGroup.GroupKey,
                readGroup.TypeLabel,
                composedGroup.SellerPartyId,
                composedGroup.CartLineIds,
                ToText(composedGroup.FinalGoodsAmountAfterDiscountAndTax),
                methods,
                reason));
        }

        return new StorefrontCapturedPreviewView(
            true,
            composed.Currency,
            composed.EvaluatedAtUtc,
            views,
            context.Provenance?.Item1,
            context.Provenance?.Item2,
            null);
    }

    public async Task<StorefrontCapturedResolution> ResolveAsync(
        CartPage cart,
        CartAccess access,
        DestinationIdentifiers? destination,
        string? currency,
        Guid languageId,
        CancellationToken cancellationToken,
        string? couponCode = null)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(access);

        var currencyCode = string.IsNullOrWhiteSpace(currency) ? cart.DefaultCurrency : currency;

        // خاموش (پیش‌فرض): صفر فراخوانی به Offer/Fulfillment/Shipping و صفر تغییر در مسیر قدیمی.
        if (options.Value.Enabled is false)
        {
            return Early(new StorefrontCapturedPreviewView(
                false, currencyCode, DateTimeOffset.UtcNow, [], null, null, null));
        }

        // روشن‌بودن کلید بدون سیاست تعداد تاریخ معتبر یعنی هیچ قیمت/تاریخی ادعا نمی‌شود (fail-closed).
        if (options.Value.HasValidAvailableDateCount is false || options.Value.AvailableDateCount is not int availableDateCount)
        {
            return Early(new StorefrontCapturedPreviewView(
                true,
                currencyCode,
                DateTimeOffset.UtcNow,
                [],
                null,
                null,
                StorefrontCapturedPreviewReasons.AvailableDateCountInvalid));
        }

        if (cart.Lines.Count == 0)
        {
            return Early(Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentContradictory));
        }

        // شکست واقعی درز بیرونی/لغو فراخوان این‌جا گرفته نمی‌شود: هیچ «انتساب ناقص» یا پیش‌نمایش
        // موفقِ خالی جای آن نمی‌نشیند.
        var assignmentById = await assignments.GetAssignmentsAsync(
            cart.Lines.Select(line => line.OfferId).Distinct().ToArray(),
            cancellationToken);

        var requestedOfferIds = cart.Lines.Select(line => line.OfferId).ToHashSet();

        // ردیف بیرون از درخواست، آفر جاافتاده یا فروشندهٔ ناسازگار کل مسیر خواندنی را fail-closed
        // می‌کند (هیچ پیش‌فرضی جای آن نمی‌نشیند).
        foreach (var (offerId, assignment) in assignmentById)
        {
            if (assignment is null || !requestedOfferIds.Contains(offerId) || assignment.OfferId != offerId)
            {
                return Early(Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentContradictory));
            }
        }

        foreach (var line in cart.Lines)
        {
            if (!assignmentById.TryGetValue(line.OfferId, out var assignment) || assignment is null)
            {
                return Early(Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentMissing));
            }

            if (assignment.OfferId != line.OfferId || assignment.SellerPartyId != line.SellerPartyId)
            {
                return Early(Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentContradictory));
            }
        }

        // نوع‌های انتساب‌شده یک‌بار خوانده می‌شوند تا هم فعال‌بودن و هم برچسب DB تأیید شود.
        var requestedTypeIds = cart.Lines
            .Select(line => assignmentById[line.OfferId].ShippingTypeId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var typeById = new Dictionary<Guid, ShippingTypeSnapshot>();
        if (requestedTypeIds.Length > 0)
        {
            var typeRows = await shippingTypes.GetByIdsAsync(requestedTypeIds, cancellationToken);
            foreach (var type in typeRows)
            {
                // ردیف تهی/تکراری/بیرون از درخواست یعنی پاسخ کاتالوگ قابل اتکا نیست.
                if (type is null
                    || !requestedTypeIds.Contains(type.ShippingTypeId)
                    || !typeById.TryAdd(type.ShippingTypeId, type))
                {
                    return Early(Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentContradictory));
                }
            }
        }

        var plan = BuildPartition(cart, assignmentById, typeById, out var groups);
        if (plan.Count == 0)
        {
            return Early(Empty(currencyCode, StorefrontCapturedPreviewReasons.Unassigned));
        }

        if (destination is null)
        {
            return Early(new StorefrontCapturedPreviewView(
                true,
                currencyCode,
                DateTimeOffset.UtcNow,
                groups.Select(group => group with
                {
                    UnavailableReason = StorefrontCapturedPreviewReasons.DestinationRequired,
                }).ToArray(),
                null,
                null,
                StorefrontCapturedPreviewReasons.DestinationRequired));
        }

        // کش کردن خواندن ۲۱۹ بر اساس هویت تایپ‌شدهٔ «مالک کامل + Binding»: هر جفت دقیقاً یک‌بار در این
        // درخواست (همان سیاست trim + OrdinalIgnoreCase سرویس/گزینه که ۲۱۸/۲۱۹ به کار می‌برند).
        var bindings = await ResolveCatalogBindingsAsync(cancellationToken);
        var pointerCache = new Dictionary<StorefrontCapturedPublicationBinding, ShippingSelectedCapturedRevisionResult>();
        var plans = new List<StorefrontCapturedGroupPlan>(plan.Count);
        var unavailable = new Dictionary<string, string>(StringComparer.Ordinal);
        var labels = new Dictionary<StorefrontCapturedPublicationBinding, string>();
        var provenance = new HashSet<(long?, Guid?)>();

        foreach (var group in plan)
        {
            var candidates = new List<StorefrontCapturedCandidatePlan>();
            if (group.UnavailableReason is not null || group.TypeId is null)
            {
                plans.Add(new StorefrontCapturedGroupPlan(group.GroupKey, group.CartLineIds, candidates, group.TypeLabel));
                unavailable[group.GroupKey] = group.UnavailableReason ?? StorefrontCapturedPreviewReasons.TypeUnavailable;
                continue;
            }

            foreach (var binding in bindings)
            {
                var ownerBinding = new StorefrontCapturedPublicationBinding(
                    group.OwnerKind,
                    group.OwnerTenantId,
                    group.OwnerSellerPartyId,
                    binding.ServiceCode,
                    binding.OptionCode);
                if (!pointerCache.TryGetValue(ownerBinding, out var pointer))
                {
                    pointer = await selectedCaptures.ReadAsync(
                        new ShippingSelectedCapturedRevisionRequest(
                            group.OwnerKind,
                            group.OwnerTenantId,
                            group.OwnerSellerPartyId,
                            binding.ServiceCode,
                            binding.OptionCode),
                        cancellationToken);
                    pointerCache[ownerBinding] = pointer;
                }

                if (pointer.Verdict != ShippingSelectedCaptureVerdict.Selected
                    || pointer.DraftId is not Guid draftId
                    || pointer.SourceRevision is not long revision
                    || pointer.ShippingTypeId is not Guid capturedType
                    || capturedType != group.TypeId.Value)
                {
                    continue;
                }

                // هویت Binding برگشتی باید همان درخواست باشد؛ هیچ تطبیق تقریبی/پارس برچسب مجاز نیست.
                // مالک همان مالک درخواست است (خواندن با هویت دقیق انجام شد)، پس مقایسهٔ سرویس/گزینه با
                // همان سیاست متعارف trim + OrdinalIgnoreCase کافی و دقیق است.
                var pointerBinding = new StorefrontCapturedPublicationBinding(
                    ownerBinding.OwnerKind,
                    ownerBinding.OwnerTenantId,
                    ownerBinding.OwnerSellerPartyId,
                    pointer.ServiceCode,
                    pointer.OptionCode);
                if (!pointerBinding.Equals(ownerBinding))
                {
                    continue;
                }

                // هویت پایدار روش از خود Binding کاتالوگ می‌آید (نه برچسب ترجمه‌شده)؛ برچسب فقط برای
                // نمایش در نقشهٔ همین تعهد نگه داشته می‌شود. دو مالک با همان سرویس/گزینه (و حتی همان
                // برچسب نمایشی) دو تعهد جدا می‌مانند و هرکدام فقط ترجمهٔ DB خودش را می‌گیرد.
                var methodCode = MethodCode(pointer.ServiceCode!, pointer.OptionCode);
                labels[ownerBinding] = SelectLabel(pointer.Translations, languageId) ?? pointer.ServiceCode!;

                candidates.Add(new StorefrontCapturedCandidatePlan(
                    methodCode,
                    group.OwnerKind,
                    group.OwnerTenantId,
                    group.OwnerSellerPartyId,
                    draftId,
                    revision,
                    destination,
                    pointer.ServiceCode!,
                    pointer.OptionCode,
                    availableDateCount,
                    pointer.SnapshotId ?? Guid.Empty,
                    pointer.SelectionVersion ?? 0));
                provenance.Add((pointer.SelectionVersion, pointer.SnapshotId));
            }

            if (candidates.Count == 0)
            {
                unavailable[group.GroupKey] = StorefrontCapturedPreviewReasons.NoSelectedCapture;
            }

            plans.Add(new StorefrontCapturedGroupPlan(group.GroupKey, group.CartLineIds, candidates, group.TypeLabel));
        }

        if (plans.All(p => p.Candidates.Count == 0))
        {
            // هیچ نامزدی نیست: گروه‌ها با دلیل صریح برمی‌گردند و صفر فراخوانی ۲۱۸/۲۱۷ انجام می‌شود.
            return Early(new StorefrontCapturedPreviewView(
                true,
                currencyCode,
                DateTimeOffset.UtcNow,
                groups.Select(group => group with
                {
                    UnavailableReason = group.UnavailableReason
                        ?? (unavailable.TryGetValue(group.GroupKey, out var reason)
                            ? reason
                            : StorefrontCapturedPreviewReasons.NoSelectedCapture),
                }).ToArray(),
                null,
                null,
                StorefrontCapturedPreviewReasons.NoSelectedCapture));
        }

        if (access.UserId is null)
        {
            // مسیر واقعی فکت‌های ۲۱۷ یک کاربر عامل ثبت می‌خواهد و سبد مهمان در این foundation آن را
            // ندارد. پس ترکیب ساخته نمی‌شود؛ نه خطای ۵۰۰ و نه پیش‌نمایش «موفق» بدون مبلغ.
            return Early(Refused(currencyCode, groups));
        }

        var command = new SubmitCheckoutCommand(
            cart.CartId,
            access,
            cart.Version,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            // فکت‌های ۲۱۷ از همان مسیر نقل‌قول Checkout می‌آیند و آن مسیر کاربر عامل واقعی
            // می‌خواهد؛ شناسهٔ ساختگی، ترکیب مثبت را برای هر سبد واردشده می‌شکست.
            PlacedByUserId: access.UserId ?? Guid.Empty,
            IdempotencyKey: "captured-preview",
            // TB-230: the SAME jurisdiction the real submit charges. `cart.Market` is a market code,
            // not a tax jurisdiction key, and quoting with it refused every live priced read
            // (`checkout.tax.unavailable`) while a jurisdiction-keyed rule exists.
            TaxJurisdiction: StorefrontCheckoutService.StorefrontTaxJurisdiction,
            // TB-236: قصد کوپن همان متنی است که خواندن/انتخاب/ثبت می‌بینند و از همان مسیر ۲۱۷
            // (Pricing/Promotion/Tax) قیمت می‌گیرد. هیچ تخفیف/صلاحیتی این‌جا ساخته نمی‌شود.
            CouponCode: StorefrontCouponIntent.Normalize(couponCode));

        StorefrontCapturedGroupProjectionResult composed;
        try
        {
            composed = await composer.ComposeAsync(
                new StorefrontCapturedGroupProjectionRequest(command, plans),
                cancellationToken);
        }
        catch (ContractOperationException error) when (IsKnownCompositionRefusal(error))
        {
            // امتناع تجاری شناخته‌شدهٔ ترکیب: گروه‌ها حفظ می‌شوند و هیچ مبلغ/تاریخی ادعا نمی‌شود؛
            // سبد به «خالی» تقلیل نمی‌یابد. خطای وابستگی/لغو فراخوان همچنان منتشر می‌شود.
            return Early(Refused(currencyCode, groups));
        }

        // provenance فقط وقتی یکتاست اعلام می‌شود؛ دو اشاره‌گر متفاوت هرگز به یک مقدار تقلیل نمی‌یابد.
        var single = provenance.Count == 1 ? provenance.Single() : ((long?, Guid?)?)null;

        return new StorefrontCapturedResolution(
            null,
            new StorefrontCapturedSelectionContext(
                currencyCode,
                composed.EvaluatedAtUtc,
                groups,
                plans,
                composed,
                labels,
                unavailable,
                single));
    }
    /// <summary>متن اعشاری دقیق بدون هیچ تبدیل شناور/گردکردن؛ نبود مقدار تهی می‌ماند.</summary>
    private static string? ToText(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture);

    private static string MethodCode(string serviceCode, string? optionCode) =>
        $"{StorefrontCapturedPublicationBinding.Canonical(serviceCode)}\u001F"
        + $"{StorefrontCapturedPublicationBinding.Canonical(optionCode) ?? string.Empty}";

    /// <summary>امتناع تجاری شناخته‌شدهٔ ترکیب (برنامه/فکت/تعهد)، نه خطای وابستگی.</summary>
    private static bool IsKnownCompositionRefusal(ContractOperationException error) =>
        error.Code is StorefrontCapturedGroupProjectionReasons.PlanInvalid
            or StorefrontCapturedGroupProjectionReasons.FactsMismatch
            or StorefrontCapturedGroupProjectionReasons.BindingMismatch;

    private static StorefrontCapturedPreviewView Empty(string currency, string reason, bool enabled = true) =>
        new(enabled, currency, DateTimeOffset.UtcNow, [], null, null, reason);

    private static StorefrontCapturedResolution Early(StorefrontCapturedPreviewView view) => new(view, null);

    /// <summary>گروه‌ها با دلیل صریح می‌مانند و هیچ مبلغ/تاریخی ادعا نمی‌شود.</summary>
    private static StorefrontCapturedPreviewView Refused(
        string currency,
        IReadOnlyList<StorefrontCapturedPreviewGroupView> groups) =>
        new(
            true,
            currency,
            DateTimeOffset.UtcNow,
            groups.Select(group => group with
            {
                UnavailableReason = group.UnavailableReason
                    ?? StorefrontCapturedPreviewReasons.CompositionRefused,
            }).ToArray(),
            null,
            null,
            StorefrontCapturedPreviewReasons.CompositionRefused);

    private static string? SelectLabel(
        IReadOnlyList<ShippingSelectedCapturedTranslation> translations,
        Guid languageId) =>
        translations
            .OrderByDescending(translation => translation.LanguageId == languageId)
            .Select(translation => translation.Name)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

    /// <summary>Bindingهای واقعی کاتالوگ: سرویس با گزینهٔ تهی یا هر گزینهٔ فعال — بدون پارس برچسب.</summary>
    private async Task<IReadOnlyList<(string ServiceCode, string? OptionCode)>> ResolveCatalogBindingsAsync(
        CancellationToken cancellationToken)
    {
        var snapshot = await catalog.ListAsync(cancellationToken);
        var bindings = new List<(string ServiceCode, string? OptionCode)>();
        var seen = new HashSet<StorefrontCapturedPublicationBinding>();
        foreach (var service in snapshot.Where(service => service.IsActive))
        {
            var options = service.Options.Where(option => option.IsActive).ToArray();
            if (options.Length == 0)
            {
                if (seen.Add(new StorefrontCapturedPublicationBinding(
                        default,
                        null,
                        null,
                        service.Code,
                        null)))
                {
                    bindings.Add((service.Code, null));
                }

                continue;
            }

            foreach (var option in options)
            {
                if (seen.Add(new StorefrontCapturedPublicationBinding(
                        default,
                        null,
                        null,
                        service.Code,
                        option.Code)))
                {
                    bindings.Add((service.Code, option.Code));
                }
            }
        }

        return bindings;
    }

    /// <summary>
    /// افراز کامل خطوط سبد با کلید پایدار مسئولیت+نوع+فروشنده و <b>یک</b> سیاست مالک مشترک؛ هیچ خطی جا
    /// نمی‌افتد یا تکرار نمی‌شود و مالک هر گروه فقط از انتساب صریح همان خطوط resolve می‌شود.
    /// </summary>
    private List<PlannedGroup> BuildPartition(
        CartPage cart,
        IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot> assignmentById,
        IReadOnlyDictionary<Guid, ShippingTypeSnapshot> typeById,
        out List<StorefrontCapturedPreviewGroupView> groups)
    {
        var planned = new Dictionary<string, PlannedGroup>(StringComparer.Ordinal);
        var order = new List<string>();
        var marketplace = edition.Current?.Edition == ToobaEdition.Marketplace;

        foreach (var line in cart.Lines)
        {
            var assignment = assignmentById[line.OfferId];
            var typeId = assignment.ShippingTypeId;

            // یک سیاست واحد برای هر سه مصرف‌کننده (خواندن/نوشتن/ثبت): مالک فقط از زمینهٔ trusted نصب
            // و انتساب واقعی همین خطوط می‌آید؛ هیچ مالکی از بدنه یا از فروشندهٔ «دیگری» ساخته نمی‌شود.
            var resolved = StorefrontCapturedOwnerPolicy.Resolve(
                edition.Current?.Edition,
                tenant.Current,
                assignment.Responsibility,
                line.SellerPartyId);

            string key;
            switch (assignment.Responsibility)
            {
                case null:
                    key = $"cap|unassigned|{line.OfferId:N}";
                    break;
                case OfferFulfillmentResponsibility.Seller:
                    key = $"cap|seller|{line.SellerPartyId:N}|{typeId?.ToString("N") ?? "none"}";
                    break;
                case OfferFulfillmentResponsibility.Platform when marketplace:
                    key = $"cap|platform|marketplace|{line.SellerPartyId:N}|{typeId?.ToString("N") ?? "none"}";
                    break;
                case OfferFulfillmentResponsibility.Platform:
                    key = $"cap|platform|store|{line.SellerPartyId:N}|{typeId?.ToString("N") ?? "none"}";
                    break;
                default:
                    // مقدار مسئولیت ناشناخته: هیچ پیش‌فرض Store/Platformی جای آن نمی‌نشیند.
                    key = $"cap|invalid|{line.OfferId:N}";
                    break;
            }

            var reason = resolved.Failure switch
            {
                StorefrontCapturedOwnerFailure.None => null,
                StorefrontCapturedOwnerFailure.ResponsibilityMissing => StorefrontCapturedPreviewReasons.Unassigned,
                StorefrontCapturedOwnerFailure.ResponsibilityInvalid => StorefrontCapturedPreviewReasons.ResponsibilityInvalid,
                StorefrontCapturedOwnerFailure.SellerUnsupportedInSingleStore => StorefrontCapturedPreviewReasons.SellerOwnerUnsupported,
                _ => StorefrontCapturedPreviewReasons.OwnerUnavailable,
            };

            if (reason is null
                && (typeId is null || !typeById.TryGetValue(typeId.Value, out var type) || !type.IsActive))
            {
                reason = StorefrontCapturedPreviewReasons.TypeUnavailable;
            }

            if (!planned.TryGetValue(key, out var group))
            {
                var owner = resolved.Owner;
                group = new PlannedGroup(
                    key,
                    line.SellerPartyId,
                    typeId,
                    TypeLabel(typeId, typeById),
                    owner?.Kind ?? ShippingCapturedQuoteOwnerKind.Store,
                    owner?.TenantId,
                    owner?.SellerPartyId,
                    [],
                    reason);
                planned[key] = group;
                order.Add(key);
            }

            group.CartLineIds.Add(line.LineId);
            if (reason is not null && group.UnavailableReason is null)
            {
                group.UnavailableReason = reason;
            }
        }

        groups = order
            .Select(key => planned[key])
            .Select(group => new StorefrontCapturedPreviewGroupView(
                group.GroupKey,
                group.TypeLabel,
                group.SellerPartyId,
                group.CartLineIds,
                null,
                [],
                group.UnavailableReason))
            .ToList();

        return order.Select(key => planned[key]).ToList();
    }

    private static string? TypeLabel(
        Guid? typeId,
        IReadOnlyDictionary<Guid, ShippingTypeSnapshot> typeById) =>
        typeId is Guid id && typeById.TryGetValue(id, out var type)
            ? type.Translations.Select(translation => translation.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
            : null;

    /// <summary>
    /// برچسب نمایشی <b>همان تعهد</b> (مالک کامل + Binding)؛ همان برچسبی که ردیف DB همان مالک داد.
    /// کلید هرگز فقط کد روش نیست، وگرنه دو مالک با همان سرویس/گزینه برچسب یکدیگر را بازنویسی می‌کردند.
    /// </summary>
    /// <param name="labels">نقشهٔ برچسب هر تعهد.</param>
    /// <param name="candidate">نامزد صریح سرور.</param>
    internal static string? LabelOf(
        IReadOnlyDictionary<StorefrontCapturedPublicationBinding, string> labels,
        StorefrontCapturedCandidatePlan candidate) =>
        labels.TryGetValue(new StorefrontCapturedPublicationBinding(candidate), out var label) ? label : null;

    private sealed class PlannedGroup(
        string groupKey,
        Guid sellerPartyId,
        Guid? typeId,
        string? typeLabel,
        ShippingCapturedQuoteOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        List<Guid> cartLineIds,
        string? unavailableReason)
    {
        public string GroupKey { get; } = groupKey;
        public Guid SellerPartyId { get; } = sellerPartyId;
        public Guid? TypeId { get; } = typeId;
        public string? TypeLabel { get; } = typeLabel;
        public ShippingCapturedQuoteOwnerKind OwnerKind { get; } = ownerKind;
        public string? OwnerTenantId { get; } = ownerTenantId;
        public Guid? OwnerSellerPartyId { get; } = ownerSellerPartyId;
        public List<Guid> CartLineIds { get; } = cartLineIds;
        public string? UnavailableReason { get; set; } = unavailableReason;
    }
}
