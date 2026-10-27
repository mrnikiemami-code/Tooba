using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Models;
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

    /// <summary>نوع ارسال انتساب‌شده در کاتالوگ فعال نیست.</summary>
    public const string TypeUnavailable = "storefront.captured_preview.type_unavailable";

    /// <summary>مسئولیت Seller در نصب SingleStore پشتیبانی نمی‌شود (نه بازتفسیر به Store).</summary>
    public const string SellerOwnerUnsupported = "storefront.captured_preview.seller_owner_unsupported";

    /// <summary>هیچ capture انتخاب‌شده‌ای برای Bindingهای همان نوع پیدا نشد.</summary>
    public const string NoSelectedCapture = "storefront.captured_preview.no_selected_capture";

    /// <summary>مقصد تأییدشده برای قیمت‌گذاری موجود نیست.</summary>
    public const string DestinationRequired = "storefront.captured_preview.destination_required";

    /// <summary>ترکیب سرور نتوانست افراز/فکت‌های سبد را تأیید کند.</summary>
    public const string CompositionRefused = "storefront.captured_preview.composition_refused";

    /// <summary>رد کردن انتخاب/ثبت تا برش parity و شواهد فشردهٔ سفارش — هرگز «آمادهٔ تسویه» نیست.</summary>
    public const string PreviewNotReady = "storefront.captured_preview.not_ready";
}

/// <summary>
/// سازندهٔ واقعی برنامهٔ گروه‌های ارسال: سبد واقعی → انتساب‌های صریح Offer → اشاره‌گرهای انتخاب‌شدهٔ
/// ۲۱۹ → <b>یک</b> ترکیب ۲۱۸.
/// </summary>
/// <remarks>
/// <para>
/// <b>No browser identity.</b> نه مالک، نه پیش‌نویس، نه بازبینی و نه مبلغی از کلاینت پذیرفته نمی‌شود.
/// مسئولیت از فیلد صریح همان آفر و مالک Seller از فروشندهٔ واقعی همان آفر می‌آید؛ Platform از زمینهٔ
/// trusted نصب. آفر بدون مسئولیت استنتاج نمی‌شود.
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

    /// <summary>پیش‌نمایش گروه‌ها را می‌سازد؛ خطاهای تجاری به نمای صادقانه ترجمه می‌شوند.</summary>
    public async Task<StorefrontCapturedPreviewView> BuildAsync(
        CartPage cart,
        CartAccess access,
        DestinationIdentifiers? destination,
        string? currency,
        Guid languageId,
        int availableDateCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(access);

        var currencyCode = string.IsNullOrWhiteSpace(currency) ? cart.DefaultCurrency : currency;

        // خاموش (پیش‌فرض): صفر فراخوانی به Offer/Fulfillment/Shipping و صفر تغییر در مسیر قدیمی.
        if (options.Value.Enabled is false)
        {
            return new StorefrontCapturedPreviewView(
                false, currencyCode, DateTimeOffset.UtcNow, [], null, null, null);
        }
        if (cart.Lines.Count == 0)
        {
            return Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentContradictory);
        }

        IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot> assignmentById;
        try
        {
            assignmentById = await assignments.GetAssignmentsAsync(
                cart.Lines.Select(line => line.OfferId).Distinct().ToArray(),
                cancellationToken);
        }
        catch (ContractOperationException)
        {
            return Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentContradictory);
        }

        // هر خط سبد باید انتساب همان آفر و همان فروشنده را داشته باشد؛ ردیف جاافتاده یا ناسازگار
        // کل مسیر خواندنی را fail-closed می‌کند (هیچ پیش‌فرضی جای آن نمی‌نشیند).
        foreach (var line in cart.Lines)
        {
            if (!assignmentById.TryGetValue(line.OfferId, out var assignment) || assignment is null)
            {
                return Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentMissing);
            }

            if (assignment.OfferId != line.OfferId || assignment.SellerPartyId != line.SellerPartyId)
            {
                return Empty(currencyCode, StorefrontCapturedPreviewReasons.AssignmentContradictory);
            }
        }

        // نوع‌های انتساب‌شده یک‌بار خوانده می‌شوند تا هم فعال‌بودن و هم برچسب DB تأیید شود.
        var requestedTypeIds = cart.Lines
            .Select(line => assignmentById[line.OfferId].ShippingTypeId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var typeById = requestedTypeIds.Length == 0
            ? new Dictionary<Guid, ShippingTypeSnapshot>()
            : (await shippingTypes.GetByIdsAsync(requestedTypeIds, cancellationToken))
                .Where(type => type is not null)
                .ToDictionary(type => type.ShippingTypeId);

        var plan = BuildPartition(cart, assignmentById, typeById, out var groups);
        if (plan.Count == 0)
        {
            return Empty(currencyCode, StorefrontCapturedPreviewReasons.Unassigned);
        }

        if (destination is null)
        {
            return new StorefrontCapturedPreviewView(
                true,
                currencyCode,
                DateTimeOffset.UtcNow,
                groups.Select(group => group with
                {
                    UnavailableReason = StorefrontCapturedPreviewReasons.DestinationRequired,
                }).ToArray(),
                null,
                null,
                StorefrontCapturedPreviewReasons.DestinationRequired);
        }

        // کش کردن خواندن ۲۱۹ بر اساس مالک کامل + Binding: هر جفت دقیقاً یک‌بار در این درخواست.
        var bindings = await ResolveCatalogBindingsAsync(cancellationToken);
        var pointerCache = new Dictionary<string, ShippingSelectedCapturedRevisionResult>(StringComparer.Ordinal);
        var plans = new List<StorefrontCapturedGroupPlan>(plan.Count);
        var unavailable = new Dictionary<string, string>(StringComparer.Ordinal);
        var provenance = new HashSet<(long?, Guid?)>();

        foreach (var group in plan)
        {
            var candidates = new List<StorefrontCapturedCandidatePlan>();
            if (group.UnavailableReason is not null || group.TypeId is null)
            {
                plans.Add(new StorefrontCapturedGroupPlan(group.GroupKey, group.CartLineIds, candidates));
                unavailable[group.GroupKey] = group.UnavailableReason ?? StorefrontCapturedPreviewReasons.TypeUnavailable;
                continue;
            }

            foreach (var binding in bindings)
            {
                var cacheKey = string.Join(
                    '\u001f',
                    (int)group.OwnerKind,
                    group.OwnerTenantId ?? string.Empty,
                    group.OwnerSellerPartyId?.ToString("N") ?? string.Empty,
                    binding.ServiceCode,
                    binding.OptionCode ?? string.Empty);
                if (!pointerCache.TryGetValue(cacheKey, out var pointer))
                {
                    pointer = await selectedCaptures.ReadAsync(
                        new ShippingSelectedCapturedRevisionRequest(
                            group.OwnerKind,
                            group.OwnerTenantId,
                            group.OwnerSellerPartyId,
                            binding.ServiceCode,
                            binding.OptionCode),
                        cancellationToken);
                    pointerCache[cacheKey] = pointer;
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
                if (StorefrontCapturedPublicationBinding.Canonical(pointer.ServiceCode)
                        != StorefrontCapturedPublicationBinding.Canonical(binding.ServiceCode)
                    || (StorefrontCapturedPublicationBinding.Canonical(pointer.OptionCode) ?? string.Empty)
                        != (StorefrontCapturedPublicationBinding.Canonical(binding.OptionCode) ?? string.Empty))
                {
                    continue;
                }

                var label = SelectLabel(pointer.Translations, languageId) ?? binding.ServiceCode;
                candidates.Add(new StorefrontCapturedCandidatePlan(
                    label,
                    group.OwnerKind,
                    group.OwnerTenantId,
                    group.OwnerSellerPartyId,
                    draftId,
                    revision,
                    destination,
                    pointer.ServiceCode!,
                    pointer.OptionCode,
                    availableDateCount));
                provenance.Add((pointer.SelectionVersion, pointer.SnapshotId));
            }

            if (candidates.Count == 0)
            {
                unavailable[group.GroupKey] = StorefrontCapturedPreviewReasons.NoSelectedCapture;
            }

            plans.Add(new StorefrontCapturedGroupPlan(group.GroupKey, group.CartLineIds, candidates));
        }

        if (plans.All(p => p.Candidates.Count == 0))
        {
            // هیچ نامزدی نیست: گروه‌ها با دلیل صریح برمی‌گردند و صفر فراخوانی ۲۱۸/۲۱۷ انجام می‌شود.
            return new StorefrontCapturedPreviewView(
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
                StorefrontCapturedPreviewReasons.NoSelectedCapture);
        }

        var command = new SubmitCheckoutCommand(
            cart.CartId,
            access,
            cart.Version,
            OrderMode.OnlinePurchase,
            BuyerPartyId: null,
            PlacedByUserId: Guid.Empty,
            IdempotencyKey: "captured-preview",
            TaxJurisdiction: cart.Market,
            CouponCode: null);

        StorefrontCapturedGroupProjectionResult composed;
        try
        {
            composed = await composer.ComposeAsync(
                new StorefrontCapturedGroupProjectionRequest(command, plans),
                cancellationToken);
        }
        catch (ContractOperationException)
        {
            return Empty(currencyCode, StorefrontCapturedPreviewReasons.CompositionRefused, enabled: true);
        }

        var views = new List<StorefrontCapturedPreviewGroupView>(composed.Groups.Count);
        foreach (var composedGroup in composed.Groups)
        {
            var planned = plan.First(group => group.GroupKey == composedGroup.GroupKey);
            var methods = composedGroup.Candidates
                .Where(candidate => candidate.Outcome == StorefrontCapturedCandidateOutcome.Quoted
                    && candidate.OwnerResult is not null)
                .Select(candidate => new StorefrontCapturedPreviewMethodView(
                    candidate.Identity.MethodCode,
                    candidate.Identity.ServiceCode,
                    candidate.Identity.OptionCode,
                    candidate.OwnerResult!.FreightAmount,
                    candidate.OwnerResult.PackagingAmount,
                    candidate.OwnerResult.PayableAtCheckoutAmount,
                    candidate.OwnerResult.CarrierCollectAmount,
                    composedGroup.Currency,
                    candidate.Receiving?.EarliestEligibleDate?.ToString("yyyy-MM-dd"),
                    candidate.Receiving is null
                        ? []
                        : candidate.Receiving.AvailableDates
                            .Select(slot => new StorefrontDeliveryDateOption(
                                slot.Date.ToString("yyyy-MM-dd"),
                                slot.Date.ToString("yyyy-MM-dd"),
                                $"{slot.OpensAt:HH\\:mm}–{slot.ClosesAt:HH\\:mm}",
                                candidate.Receiving.EarliestEligibleDate == slot.Date))
                            .ToArray(),
                    candidate.Receiving?.UnavailableReason
                        ?? (candidate.Outcome == StorefrontCapturedCandidateOutcome.Quoted
                            ? null
                            : candidate.ReasonCode)))
                .ToArray();

            var methodReason = composedGroup.Candidates
                .Where(candidate => candidate.Outcome != StorefrontCapturedCandidateOutcome.Quoted)
                .Select(candidate => candidate.ReasonCode)
                .FirstOrDefault();

            var reason = planned.UnavailableReason
                ?? (unavailable.TryGetValue(composedGroup.GroupKey, out var plannedReason)
                    ? plannedReason
                    : methods.Length == 0
                        ? methodReason ?? StorefrontCapturedPreviewReasons.NoSelectedCapture
                        : null);

            views.Add(new StorefrontCapturedPreviewGroupView(
                composedGroup.GroupKey,
                planned.TypeLabel,
                composedGroup.SellerPartyId,
                composedGroup.CartLineIds,
                composedGroup.FinalGoodsAmountAfterDiscountAndTax,
                methods,
                reason));
        }

        // provenance فقط وقتی یکتاست اعلام می‌شود؛ دو اشاره‌گر متفاوت هرگز به یک مقدار تقلیل نمی‌یابد.
        var single = provenance.Count == 1 ? provenance.Single() : ((long?, Guid?)?)null;

        return new StorefrontCapturedPreviewView(
            true,
            composed.Currency,
            composed.EvaluatedAtUtc,
            views,
            single?.Item1,
            single?.Item2,
            null);
    }

    private static StorefrontCapturedPreviewView Empty(string currency, string reason, bool enabled = true) =>
        new(enabled, currency, DateTimeOffset.UtcNow, [], null, null, reason);

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
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var service in snapshot.Where(service => service.IsActive))
        {
            var options = service.Options.Where(option => option.IsActive).ToArray();
            if (options.Length == 0)
            {
                if (seen.Add(service.Code + "\u001f"))
                {
                    bindings.Add((service.Code, null));
                }

                continue;
            }

            foreach (var option in options)
            {
                if (seen.Add(service.Code + "\u001f" + option.Code))
                {
                    bindings.Add((service.Code, option.Code));
                }
            }
        }

        return bindings;
    }

    /// <summary>افراز کامل خطوط سبد با کلید پایدار مالک+نوع+فروشنده؛ هیچ خطی جا نمی‌افتد یا تکرار نمی‌شود.</summary>
    private List<PlannedGroup> BuildPartition(
        CartPage cart,
        IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot> assignmentById,
        IReadOnlyDictionary<Guid, ShippingTypeSnapshot> typeById,
        out List<StorefrontCapturedPreviewGroupView> groups)
    {
        var planned = new Dictionary<string, PlannedGroup>(StringComparer.Ordinal);
        var order = new List<string>();
        var singleStoreTenantId = ResolveStoreTenantId();
        var marketplace = edition.Current?.Edition == ToobaEdition.Marketplace;

        foreach (var line in cart.Lines)
        {
            var assignment = assignmentById[line.OfferId];
            string key;
            var reason = (string?)null;
            ShippingCapturedQuoteOwnerKind ownerKind = ShippingCapturedQuoteOwnerKind.Store;
            string? ownerTenantId = null;
            Guid? ownerSellerId = null;
            var typeId = assignment.ShippingTypeId;

            switch (assignment.Responsibility)
            {
                case null:
                    key = $"cap|unassigned|{line.OfferId:N}";
                    reason = StorefrontCapturedPreviewReasons.Unassigned;
                    break;
                case OfferFulfillmentResponsibility.Seller when !marketplace:
                    key = $"cap|seller|{line.SellerPartyId:N}|{typeId?.ToString("N") ?? "none"}";
                    reason = StorefrontCapturedPreviewReasons.SellerOwnerUnsupported;
                    break;
                case OfferFulfillmentResponsibility.Seller:
                    ownerKind = ShippingCapturedQuoteOwnerKind.Seller;
                    ownerSellerId = line.SellerPartyId;
                    key = $"cap|seller|{line.SellerPartyId:N}|{typeId?.ToString("N") ?? "none"}";
                    if (typeId is null || !typeById.TryGetValue(typeId.Value, out var sellerType) || !sellerType.IsActive)
                    {
                        reason = StorefrontCapturedPreviewReasons.TypeUnavailable;
                    }

                    break;
                case OfferFulfillmentResponsibility.Platform when marketplace:
                    ownerKind = ShippingCapturedQuoteOwnerKind.Marketplace;
                    key = $"cap|platform|marketplace|{line.SellerPartyId:N}|{typeId?.ToString("N") ?? "none"}";
                    if (typeId is null || !typeById.TryGetValue(typeId.Value, out var mpType) || !mpType.IsActive)
                    {
                        reason = StorefrontCapturedPreviewReasons.TypeUnavailable;
                    }

                    break;
                default:
                    ownerKind = ShippingCapturedQuoteOwnerKind.Store;
                    ownerTenantId = singleStoreTenantId;
                    key = $"cap|platform|store|{line.SellerPartyId:N}|{typeId?.ToString("N") ?? "none"}";
                    if (singleStoreTenantId is null)
                    {
                        reason = StorefrontCapturedPreviewReasons.SellerOwnerUnsupported;
                    }
                    else if (typeId is null || !typeById.TryGetValue(typeId.Value, out var storeType) || !storeType.IsActive)
                    {
                        reason = StorefrontCapturedPreviewReasons.TypeUnavailable;
                    }

                    break;
            }

            if (!planned.TryGetValue(key, out var group))
            {
                group = new PlannedGroup(
                    key,
                    line.SellerPartyId,
                    typeId,
                    TypeLabel(typeId, typeById),
                    ownerKind,
                    ownerTenantId,
                    ownerSellerId,
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
                0m,
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

    private string? ResolveStoreTenantId()
    {
        if (edition.Current?.Edition != ToobaEdition.SingleStore)
        {
            return null;
        }

        var current = tenant.Current;
        return current is null || current.Status != TenantStatus.Active || string.IsNullOrWhiteSpace(current.TenantId.Value)
            ? null
            : current.TenantId.Value.Trim();
    }

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
