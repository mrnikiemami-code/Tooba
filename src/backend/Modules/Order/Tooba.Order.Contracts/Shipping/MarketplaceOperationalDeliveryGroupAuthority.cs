namespace Tooba.Order.Contracts.Shipping;

/// <summary>
/// TB-253: server-owned operational delivery grouping.
/// Cross-seller merge is Marketplace-responsibility only; Store is never promoted; Seller stays separate.
/// Unknown/empty origin never proves equality.
/// </summary>
public static class MarketplaceOperationalDeliveryGroupAuthority
{
    /// <summary>Persisted owner kind value for Marketplace.</summary>
    public const int OwnerKindMarketplace = 1;

    /// <summary>Persisted owner kind value for Store.</summary>
    public const int OwnerKindStore = 0;

    /// <summary>Persisted owner kind value for Seller.</summary>
    public const int OwnerKindSeller = 2;

    /// <summary>Separate: committed origin identity absent.</summary>
    public const string SeparateMissingOrigin = "missing_origin";

    /// <summary>Separate: seller-owned responsibility.</summary>
    public const string SeparateSellerResponsibility = "seller_responsibility";

    /// <summary>Separate: store-owned responsibility.</summary>
    public const string SeparateStoreResponsibility = "store_responsibility";

    /// <summary>Separate: incompatible dispatch commitment.</summary>
    public const string SeparateIncompatibleCommitment = "incompatible_commitment";

    /// <summary>Separate: destination fingerprint mismatch.</summary>
    public const string SeparateDestinationMismatch = "destination_mismatch";

    /// <summary>Separate: legacy/incomplete evidence.</summary>
    public const string SeparateLegacyEvidence = "legacy_evidence";

    /// <summary>Refusal: consolidation requires Marketplace ownership.</summary>
    public const string ConsolidateNotMarketplace = "order.operational_group.consolidate_not_marketplace";

    /// <summary>Refusal: selected groups are operationally incompatible.</summary>
    public const string ConsolidateIncompatible = "order.operational_group.consolidate_incompatible";

    /// <summary>Refusal: consolidation requires multiple sellers.</summary>
    public const string ConsolidateRequiresMultiSeller = "order.operational_group.consolidate_requires_multi_seller";

    /// <summary>Refusal: CarrierApi cross-group consolidation unsupported.</summary>
    public const string ConsolidateCarrierUnsupported = "order.operational_group.consolidate_carrier_unsupported";

    /// <summary>One accepted shipping group fact surface for projection/mutation.</summary>
    public sealed record GroupFact(
        Guid ShippingGroupId,
        Guid CheckoutId,
        Guid SellerOrderId,
        Guid SellerPartyId,
        string MethodCode,
        string? ServiceCode,
        string? OptionCode,
        string MethodLabel,
        string Currency,
        decimal FeeAmount,
        int OwnerKind,
        string? OwnerTenantKey,
        Guid? OwnerSellerPartyId,
        DateOnly? ReceivingDate,
        long? ReceivingOpenTicks,
        long? ReceivingCloseTicks,
        string? ReceivingTimeZone,
        string TransportMode,
        string? OriginLocationCode,
        string? OriginPostalAddress,
        Guid? CarrierAccountId,
        string? ProviderCode,
        string? ProviderServiceCode,
        int? ConfigurationVersion,
        string DestinationFingerprint,
        IReadOnlyList<Guid> OrderLineIds,
        bool CreateParcelEligible,
        string? CreateParcelBlockReasonFa);

    /// <summary>Projected operational delivery group (immutable accepted groups as members).</summary>
    public sealed record OperationalGroup(
        string OperationalGroupKey,
        string MethodLabel,
        string MethodCode,
        string? ServiceCode,
        string? OptionCode,
        DateOnly? DeliveryDate,
        string? DeliveryTimeWindow,
        string? ReceivingTimeZone,
        string? ResponsibilityLabelFa,
        bool IsCrossSellerMarketplace,
        string? SeparateReasonCode,
        string? SeparateReasonFa,
        bool ConsolidateEligible,
        string? ConsolidateBlockReasonFa,
        decimal CombinedFeeTotal,
        string Currency,
        IReadOnlyList<GroupFact> Members);

    /// <summary>
    /// Projects accepted groups into operational delivery groups.
    /// Does not mutate historical Order rows.
    /// </summary>
    public static IReadOnlyList<OperationalGroup> Project(IReadOnlyList<GroupFact> groups)
    {
        if (groups is null || groups.Count == 0)
        {
            return [];
        }

        var ordered = groups
            .OrderBy(g => g.ReceivingDate?.ToString("yyyy-MM-dd") ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(g => g.ReceivingOpenTicks ?? long.MinValue)
            .ThenBy(g => NormalizeCode(g.MethodCode), StringComparer.Ordinal)
            .ThenBy(g => g.SellerPartyId)
            .ThenBy(g => g.ShippingGroupId)
            .ToList();

        var assigned = new HashSet<Guid>();
        var result = new List<OperationalGroup>();

        foreach (var seed in ordered)
        {
            if (!assigned.Add(seed.ShippingGroupId))
            {
                continue;
            }

            var members = new List<GroupFact> { seed };
            if (IsMarketplaceOwner(seed))
            {
                foreach (var candidate in ordered)
                {
                    if (assigned.Contains(candidate.ShippingGroupId))
                    {
                        continue;
                    }

                    if (!AreMarketplaceCompatible(seed, candidate, out _))
                    {
                        continue;
                    }

                    assigned.Add(candidate.ShippingGroupId);
                    members.Add(candidate);
                }
            }

            result.Add(BuildOperationalGroup(members));
        }

        return result;
    }

    /// <summary>
    /// Validates that shipment-bound accepted groups may join one consolidated package.
    /// CarrierApi cross-account consolidation is refused; Manual Marketplace multi-seller only.
    /// </summary>
    public static void EnsureConsolidateAllowed(IReadOnlyList<GroupFact> selectedGroups)
    {
        if (selectedGroups is null || selectedGroups.Count == 0)
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateRequiresMultiSeller);
        }

        var distinct = selectedGroups
            .GroupBy(x => x.ShippingGroupId)
            .Select(g => g.First())
            .ToList();

        var sellerCount = distinct.Select(x => x.SellerPartyId).Distinct().Count();
        if (sellerCount < 2)
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateRequiresMultiSeller);
        }

        if (distinct.Any(x => !IsMarketplaceOwner(x)))
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateNotMarketplace);
        }

        if (distinct.Any(x => IsCarrierApi(x.TransportMode)))
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateCarrierUnsupported);
        }

        var projected = Project(distinct);
        if (projected.Count != 1 || !projected[0].IsCrossSellerMarketplace)
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateIncompatible);
        }
    }

    /// <summary>Derives accepted method code from compatible members; rejects client mismatch.</summary>
    public static string DeriveAcceptedMethodCode(
        IReadOnlyList<GroupFact> members,
        string? clientShippingMethodCode)
    {
        if (members is null || members.Count == 0)
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateIncompatible);
        }

        var codes = members
            .Select(m => NormalizeCode(m.MethodCode))
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (codes.Count != 1)
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateIncompatible);
        }

        var accepted = codes[0];
        var client = NormalizeCode(clientShippingMethodCode);
        if (!string.IsNullOrEmpty(client) && !string.Equals(client, accepted, StringComparison.Ordinal))
        {
            throw new MarketplaceOperationalDeliveryGroupException(ConsolidateIncompatible);
        }

        // Preserve original casing from first member method code head.
        var raw = (members[0].MethodCode ?? string.Empty).Trim();
        return raw.Split('\u001f')[0].Split(':')[0].Trim();
    }

    /// <summary>True when both facts share one Marketplace operational merge key.</summary>
    public static bool AreMarketplaceCompatible(GroupFact a, GroupFact b, out string? refusalCode)
    {
        refusalCode = null;
        if (a.CheckoutId == Guid.Empty
            || b.CheckoutId == Guid.Empty
            || a.CheckoutId != b.CheckoutId)
        {
            refusalCode = SeparateIncompatibleCommitment;
            return false;
        }

        if (string.IsNullOrWhiteSpace(a.Currency)
            || string.IsNullOrWhiteSpace(b.Currency)
            || !string.Equals(a.Currency.Trim(), b.Currency.Trim(), StringComparison.Ordinal))
        {
            refusalCode = SeparateIncompatibleCommitment;
            return false;
        }

        if (!IsMarketplaceOwner(a) || !IsMarketplaceOwner(b))
        {
            refusalCode = SeparateSellerResponsibility;
            return false;
        }

        if (!SameMarketplaceOwnerIdentity(a, b))
        {
            refusalCode = SeparateIncompatibleCommitment;
            return false;
        }

        if (string.IsNullOrWhiteSpace(a.DestinationFingerprint)
            || string.IsNullOrWhiteSpace(b.DestinationFingerprint)
            || !string.Equals(a.DestinationFingerprint, b.DestinationFingerprint, StringComparison.Ordinal))
        {
            refusalCode = SeparateDestinationMismatch;
            return false;
        }

        if (!TryResolveOriginIdentity(a, out var originA) || !TryResolveOriginIdentity(b, out var originB))
        {
            refusalCode = SeparateMissingOrigin;
            return false;
        }

        if (!string.Equals(originA, originB, StringComparison.Ordinal))
        {
            refusalCode = SeparateIncompatibleCommitment;
            return false;
        }

        if (!HasAuthoritativeReceivingChoice(a) || !HasAuthoritativeReceivingChoice(b))
        {
            refusalCode = SeparateIncompatibleCommitment;
            return false;
        }

        if (!ExactDispatchIdentity(a, b))
        {
            refusalCode = SeparateIncompatibleCommitment;
            return false;
        }

        if (!CompatibleTransport(a, b, out var transportRefusal))
        {
            refusalCode = transportRefusal ?? SeparateIncompatibleCommitment;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Chosen receiving day/window must be present and bounded; missing values never equal.
    /// </summary>
    public static bool HasAuthoritativeReceivingChoice(GroupFact g) =>
        g.ReceivingDate is not null
        && g.ReceivingOpenTicks is long open
        && g.ReceivingCloseTicks is long close
        && open >= 0
        && close > open
        && close <= TimeSpan.TicksPerDay
        && !string.IsNullOrWhiteSpace(g.ReceivingTimeZone);

    /// <summary>True when owner kind is Marketplace with null tenant/seller.</summary>
    public static bool IsMarketplaceOwner(GroupFact g) =>
        g.OwnerKind == OwnerKindMarketplace
        && g.OwnerSellerPartyId is null
        && string.IsNullOrWhiteSpace(g.OwnerTenantKey);

    /// <summary>True when owner kind is Store.</summary>
    public static bool IsStoreOwner(GroupFact g) => g.OwnerKind == OwnerKindStore;

    /// <summary>True when owner kind is Seller.</summary>
    public static bool IsSellerOwner(GroupFact g) => g.OwnerKind == OwnerKindSeller;

    /// <summary>Builds a stable destination fingerprint from committed recipient fields.</summary>
    public static string BuildDestinationFingerprint(
        string? province,
        string? city,
        string? postalCode,
        string? postalAddress)
    {
        static string N(string? v) => (v ?? string.Empty).Trim();
        var fingerprint = string.Join(
            '\u001f',
            N(province),
            N(city),
            N(postalCode),
            N(postalAddress));
        return fingerprint.All(c => c == '\u001f') ? string.Empty : fingerprint;
    }

    /// <summary>Parses owner kind from history string and/or facts int.</summary>
    public static int ParseOwnerKind(string? ownerKind, int? ownerKindInt = null)
    {
        if (ownerKindInt is int n && n is OwnerKindStore or OwnerKindMarketplace or OwnerKindSeller)
        {
            return n;
        }

        var raw = (ownerKind ?? string.Empty).Trim();
        if (raw.Equals("Marketplace", StringComparison.OrdinalIgnoreCase))
        {
            return OwnerKindMarketplace;
        }

        if (raw.Equals("Store", StringComparison.OrdinalIgnoreCase))
        {
            return OwnerKindStore;
        }

        if (raw.Equals("Seller", StringComparison.OrdinalIgnoreCase))
        {
            return OwnerKindSeller;
        }

        return -1;
    }

    /// <summary>
    /// Origin identity must be a non-empty committed dispatch origin.
    /// Empty values never compare equal.
    /// </summary>
    public static bool TryResolveOriginIdentity(GroupFact g, out string identity)
    {
        var location = NormalizeNullable(g.OriginLocationCode);
        if (!string.IsNullOrEmpty(location))
        {
            identity = "loc:" + location;
            return true;
        }

        var postal = NormalizeNullable(g.OriginPostalAddress);
        if (!string.IsNullOrEmpty(postal))
        {
            identity = "addr:" + postal;
            return true;
        }

        identity = string.Empty;
        return false;
    }

    private static OperationalGroup BuildOperationalGroup(IReadOnlyList<GroupFact> members)
    {
        var seed = members[0];
        var sellerCount = members.Select(x => x.SellerPartyId).Distinct().Count();
        var isMarketplace = members.All(IsMarketplaceOwner);
        var isCrossSellerMarketplace = isMarketplace && sellerCount >= 2;

        string? separateCode = null;
        string? separateFa = null;
        if (!isCrossSellerMarketplace)
        {
            if (IsSellerOwner(seed))
            {
                separateCode = SeparateSellerResponsibility;
                separateFa = "ارسال بر عهده فروشنده است و با فروشندگان دیگر تجمیع نمی‌شود.";
            }
            else if (IsStoreOwner(seed))
            {
                separateCode = SeparateStoreResponsibility;
                separateFa = "ارسال فروشگاهی جداگانه باقی می‌ماند.";
            }
            else if (isMarketplace && !TryResolveOriginIdentity(seed, out _))
            {
                separateCode = SeparateMissingOrigin;
                separateFa = "مبدأ ارسال ثبت‌شده نیست؛ گروه‌ها جدا نگه داشته شدند.";
            }
            else if (string.Equals(seed.TransportMode, "LegacyIncomplete", StringComparison.OrdinalIgnoreCase))
            {
                separateCode = SeparateLegacyEvidence;
                separateFa = "شاهد اجرایی کامل نیست؛ گروه‌ها جدا نگه داشته شدند.";
            }
            else if (members.Count == 1 && isMarketplace)
            {
                separateCode = null;
                separateFa = null;
            }
            else
            {
                separateCode = SeparateIncompatibleCommitment;
                separateFa = "تعهدهای ارسال سازگار نیستند و جدا نمایش داده می‌شوند.";
            }
        }

        // Compatibility projection never claims executable consolidate readiness.
        // Callers attach shipment/permission readiness separately.
        var consolidateEligible = false;
        string? consolidateBlock = isCrossSellerMarketplace
            ? (members.Any(m => IsCarrierApi(m.TransportMode))
                ? "تجمیع حامل API بین گروه‌ها پشتیبانی نمی‌شود؛ هر گروه جداگانه ارسال شود."
                : "برای تجمیع باید مرسوله‌های واجد شرایط همهٔ اعضای گروه آماده باشند.")
            : (IsStoreOwner(seed) || IsSellerOwner(seed)
                ? null
                : separateFa ?? "تجمیع فقط برای ارسال مارکت‌پلیس سازگار مجاز است.");

        var window = FormatWindow(seed.ReceivingOpenTicks, seed.ReceivingCloseTicks);
        var responsibility = isMarketplace
            ? "ارسال توسط مارکت‌پلیس"
            : IsSellerOwner(seed)
                ? "ارسال توسط فروشنده"
                : IsStoreOwner(seed)
                    ? "ارسال فروشگاه"
                    : null;

        // Hide routine Store/Seller separation prose from normal cards; keep only actionable Marketplace blockers.
        if (!isCrossSellerMarketplace && (IsStoreOwner(seed) || IsSellerOwner(seed)))
        {
            separateFa = null;
        }

        var feeTotal = members.All(m => string.Equals(m.Currency.Trim(), seed.Currency.Trim(), StringComparison.Ordinal))
            ? members.Sum(m => m.FeeAmount)
            : 0m;

        var key = BuildOperationalGroupKey(members, isMarketplace);

        return new OperationalGroup(
            key,
            string.IsNullOrWhiteSpace(seed.MethodLabel) ? seed.MethodCode : seed.MethodLabel,
            seed.MethodCode,
            seed.ServiceCode,
            seed.OptionCode,
            seed.ReceivingDate,
            window,
            seed.ReceivingTimeZone,
            responsibility,
            isCrossSellerMarketplace,
            separateCode,
            separateFa,
            consolidateEligible,
            consolidateBlock,
            feeTotal,
            seed.Currency,
            members);
    }

    /// <summary>
    /// Deterministic collision-resistant identity scoped to checkout and full compatibility/member set.
    /// </summary>
    public static string BuildOperationalGroupKey(IReadOnlyList<GroupFact> members, bool isMarketplace)
    {
        var seed = members[0];
        TryResolveOriginIdentity(seed, out var origin);
        var memberPart = string.Join(
            ',',
            members.Select(m => m.ShippingGroupId).OrderBy(x => x).Select(x => x.ToString("N")));
        var providerPart = isMarketplace && IsCarrierApi(seed.TransportMode)
            ? string.Join(
                ':',
                seed.CarrierAccountId?.ToString("N") ?? string.Empty,
                seed.ConfigurationVersion?.ToString() ?? string.Empty,
                seed.ProviderCode ?? string.Empty,
                seed.ProviderServiceCode ?? string.Empty)
            : "manual";
        return string.Join(
            '|',
            seed.CheckoutId.ToString("N"),
            seed.Currency.Trim(),
            NormalizeCode(seed.MethodCode),
            seed.ServiceCode ?? string.Empty,
            seed.OptionCode ?? string.Empty,
            seed.ReceivingDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            seed.ReceivingOpenTicks?.ToString() ?? string.Empty,
            seed.ReceivingCloseTicks?.ToString() ?? string.Empty,
            seed.ReceivingTimeZone ?? string.Empty,
            seed.DestinationFingerprint,
            isMarketplace ? "mp" : $"seller:{seed.SellerPartyId:N}",
            string.IsNullOrEmpty(origin) ? $"solo:{memberPart}" : origin,
            providerPart,
            memberPart);
    }

    /// <summary>
    /// Attaches executable consolidate readiness without changing compatibility membership.
    /// </summary>
    public static OperationalGroup WithConsolidateReadiness(
        OperationalGroup group,
        bool everyMemberHasEligibleCreatedShipment,
        bool actorMayManage,
        bool anyPaymentLocked)
    {
        if (!group.IsCrossSellerMarketplace)
        {
            return group with
            {
                ConsolidateEligible = false,
                ConsolidateBlockReasonFa = group.ConsolidateBlockReasonFa,
            };
        }

        if (group.Members.Any(m => IsCarrierApi(m.TransportMode)))
        {
            return group with
            {
                ConsolidateEligible = false,
                ConsolidateBlockReasonFa =
                    "تجمیع حامل API بین گروه‌ها پشتیبانی نمی‌شود؛ هر گروه جداگانه ارسال شود.",
            };
        }

        if (anyPaymentLocked)
        {
            return group with
            {
                ConsolidateEligible = false,
                ConsolidateBlockReasonFa = "پرداخت/وضعیت سفارش اجازهٔ تجمیع نمی‌دهد.",
            };
        }

        if (!actorMayManage)
        {
            return group with
            {
                ConsolidateEligible = false,
                ConsolidateBlockReasonFa = "مجوز عملیات تجمیع برای این کاربر فعال نیست.",
            };
        }

        if (!everyMemberHasEligibleCreatedShipment)
        {
            return group with
            {
                ConsolidateEligible = false,
                ConsolidateBlockReasonFa =
                    "برای تجمیع باید مرسولهٔ ایجادشدهٔ واجد شرایط از هر عضو گروه موجود باشد.",
            };
        }

        return group with
        {
            ConsolidateEligible = true,
            ConsolidateBlockReasonFa = null,
        };
    }

    private static bool SameMarketplaceOwnerIdentity(GroupFact a, GroupFact b) =>
        a.OwnerSellerPartyId is null
        && b.OwnerSellerPartyId is null
        && string.IsNullOrWhiteSpace(a.OwnerTenantKey)
        && string.IsNullOrWhiteSpace(b.OwnerTenantKey);

    private static bool ExactDispatchIdentity(GroupFact a, GroupFact b) =>
        string.Equals(NormalizeCode(a.MethodCode), NormalizeCode(b.MethodCode), StringComparison.Ordinal)
        && string.Equals(NormalizeNullable(a.ServiceCode), NormalizeNullable(b.ServiceCode), StringComparison.Ordinal)
        && string.Equals(NormalizeNullable(a.OptionCode), NormalizeNullable(b.OptionCode), StringComparison.Ordinal)
        && a.ReceivingDate == b.ReceivingDate
        && a.ReceivingOpenTicks == b.ReceivingOpenTicks
        && a.ReceivingCloseTicks == b.ReceivingCloseTicks
        && string.Equals(
            NormalizeNullable(a.ReceivingTimeZone),
            NormalizeNullable(b.ReceivingTimeZone),
            StringComparison.Ordinal);

    private static bool CompatibleTransport(GroupFact a, GroupFact b, out string? refusal)
    {
        refusal = null;
        if (!TryClassifyTransport(a.TransportMode, out var modeA)
            || !TryClassifyTransport(b.TransportMode, out var modeB))
        {
            refusal = SeparateLegacyEvidence;
            return false;
        }

        if (modeA != modeB)
        {
            refusal = SeparateIncompatibleCommitment;
            return false;
        }

        if (modeA == TransportKind.LegacyIncomplete)
        {
            refusal = SeparateLegacyEvidence;
            return false;
        }

        if (modeA == TransportKind.Manual)
        {
            return true;
        }

        // CarrierApi: exact provider binding required for display merge; consolidate still refused later.
        if (a.CarrierAccountId is null
            || b.CarrierAccountId is null
            || a.CarrierAccountId != b.CarrierAccountId
            || a.ConfigurationVersion is null
            || b.ConfigurationVersion is null
            || a.ConfigurationVersion != b.ConfigurationVersion
            || string.IsNullOrWhiteSpace(a.ProviderCode)
            || string.IsNullOrWhiteSpace(b.ProviderCode)
            || !string.Equals(a.ProviderCode, b.ProviderCode, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(a.ProviderServiceCode)
            || string.IsNullOrWhiteSpace(b.ProviderServiceCode)
            || !string.Equals(a.ProviderServiceCode, b.ProviderServiceCode, StringComparison.Ordinal))
        {
            refusal = SeparateIncompatibleCommitment;
            return false;
        }

        return true;
    }

    private enum TransportKind
    {
        Manual,
        CarrierApi,
        LegacyIncomplete,
    }

    private static bool TryClassifyTransport(string? transportMode, out TransportKind kind)
    {
        kind = TransportKind.LegacyIncomplete;
        if (string.Equals(transportMode, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            kind = TransportKind.Manual;
            return true;
        }

        if (string.Equals(transportMode, "CarrierApi", StringComparison.OrdinalIgnoreCase))
        {
            kind = TransportKind.CarrierApi;
            return true;
        }

        if (string.Equals(transportMode, "LegacyIncomplete", StringComparison.OrdinalIgnoreCase))
        {
            kind = TransportKind.LegacyIncomplete;
            return true;
        }

        return false;
    }

    private static bool IsCarrierApi(string? transportMode) =>
        string.Equals(transportMode, "CarrierApi", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCode(string? code) =>
        (code ?? string.Empty).Trim().TrimEnd('\u001f', ':').ToLowerInvariant();

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? FormatWindow(long? openTicks, long? closeTicks)
    {
        if (openTicks is null || closeTicks is null)
        {
            return null;
        }

        var open = TimeSpan.FromTicks(openTicks.Value);
        var close = TimeSpan.FromTicks(closeTicks.Value);
        return $"{open:hh\\:mm}–{close:hh\\:mm}";
    }
}

/// <summary>Typed refusal for operational consolidate mutations.</summary>
public sealed class MarketplaceOperationalDeliveryGroupException : Exception
{
    /// <summary>Creates a typed consolidate refusal.</summary>
    public MarketplaceOperationalDeliveryGroupException(string code)
        : base(code)
    {
        Code = code;
    }

    /// <summary>Stable machine code.</summary>
    public string Code { get; }
}
