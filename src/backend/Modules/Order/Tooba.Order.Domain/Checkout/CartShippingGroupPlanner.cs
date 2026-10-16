using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Checkout;

/// <summary>هویت خط سبد authoritative از Cart برای عضویت گروه.</summary>
public sealed record CartShippingGroupCartLine(Guid LineId, Guid SellerPartyId, decimal Quantity);

/// <summary>
/// درخواست گروه از سمت کلاینت: فقط قصد انتخاب، بدون هیچ مبلغی.
/// </summary>
/// <param name="MethodCode">کد روش canonical همین گروه.</param>
/// <param name="ServiceCode">کد سرویس اعلامی؛ باید با هویت authoritative بخواند.</param>
/// <param name="OptionCode">کد گزینهٔ اعلامی؛ باید با هویت authoritative بخواند.</param>
/// <param name="OriginContext">بافت مبدأ؛ در این اسلایس فقط خالی مجاز است.</param>
/// <param name="CartLineIds">خطوط سبد همین گروه (تمام-خط).</param>
/// <param name="SelectedDeliveryDate">
/// TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: زمان‌بندی <b>همین</b> گروه. در نبود آن مقدار اسکالر
/// قدیمی به‌کار می‌رود (سازگاری تک‌گروهی)؛ هیچ‌گاه مقدار یک گروه روی گروه دیگر کپی نمی‌شود.
/// </param>
/// <param name="SelectedDeliveryTimeWindow">پنجرهٔ ساعتی همین گروه.</param>
public sealed record CartShippingGroupRequest(
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string? OriginContext,
    IReadOnlyList<Guid> CartLineIds,
    DateOnly? SelectedDeliveryDate = null,
    string? SelectedDeliveryTimeWindow = null);

/// <summary>روش واجد شرایط authoritative که Order آن را از کاتالوگ/نرخ resolve می‌کند.</summary>
/// <param name="SellerPartyId">
/// TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: فروشنده‌ای که این وعده برای او محاسبه شده است. تهی یعنی
/// وعدهٔ مستقل از فروشنده (فراخوان‌های قدیمی)؛ مقدار‌دار یعنی این ردیف فقط برای همان فروشنده معتبر است
/// و هرگز برای فروشندهٔ دیگر به‌کار نمی‌رود (وگرنه وعدهٔ گروه دیگر دزدیده می‌شود).
/// </param>
public sealed record CartShippingGroupEligibleMethod(
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string Label,
    decimal PriceAmount,
    string Currency,
    int LeadDays,
    DateOnly MinimumDeliveryDate,
    Guid? SellerPartyId = null);

/// <summary>
/// سازندهٔ قطعی (pure) نقشهٔ گروه‌های ارسال پیش‌نویس سبد.
/// تمام حقیقت از منابع authoritative می‌آید: خطوط و مالکیت فروشنده از Cart، هویت/برچسب/مبلغ/ارز و
/// روزهای آماده‌سازی از روش واجد شرایط. هیچ مبلغ، برچسب، فروشنده یا قیمتی از کلاینت پذیرفته نمی‌شود.
/// هر نقض، <see cref="ContractOperationException"/> با کد پایدار <see cref="CartShippingGroupFault"/> می‌دهد.
/// </summary>
public static class CartShippingGroupPlanner
{
    /// <summary>
    /// نقشهٔ گروه‌ها را می‌سازد و ناوردایی‌های عضویت را اعمال می‌کند:
    /// هر خط سبد باید دقیقاً یک بار پوشش داده شود؛ خط ناشناس، خط تکراری، خط جاافتاده،
    /// فروشندهٔ مختلط در یک گروه، روش ناواجد و مبدأ نامعلوم رد می‌شوند.
    /// </summary>
    public static IReadOnlyList<CartShippingDraftGroup> Plan(
        Guid cartId,
        IReadOnlyList<CartShippingGroupCartLine> cartLines,
        IReadOnlyList<CartShippingGroupRequest> requests,
        IReadOnlyList<CartShippingGroupEligibleMethod> eligibleMethods,
        DateOnly? selectedDeliveryDate,
        string? selectedDeliveryTimeWindow,
        DateTimeOffset now,
        Func<Guid> newGroupId)
    {
        if (cartId == Guid.Empty)
        {
            throw new ContractOperationException(CartShippingGroupFault.CartRequired);
        }

        ArgumentNullException.ThrowIfNull(cartLines);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(eligibleMethods);
        ArgumentNullException.ThrowIfNull(newGroupId);

        if (cartLines.Count == 0)
        {
            throw new ContractOperationException(CartShippingGroupFault.MissingLineMembership);
        }

        if (requests.Count == 0)
        {
            throw new ContractOperationException(CartShippingGroupFault.GroupWithoutLines);
        }

        var sellerByLine = cartLines.ToDictionary(x => x.LineId, x => x.SellerPartyId);
        var assigned = new HashSet<Guid>();
        var groups = new List<CartShippingDraftGroup>(requests.Count);
        var seenSellerMethod = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            if (request.CartLineIds is null || request.CartLineIds.Count == 0)
            {
                throw new ContractOperationException(CartShippingGroupFault.GroupWithoutLines);
            }

            // فروشنده پیش از resolve روش لازم است: وعده/حداقل تحویل هر گروه فقط برای فروشندهٔ خودش
            // محاسبه شده است و نباید از ردیف فروشندهٔ دیگری برداشته شود.
            var seller = sellerByLine.TryGetValue(request.CartLineIds[0], out var firstSeller)
                ? firstSeller
                : throw new ContractOperationException(CartShippingGroupFault.UnknownLine);

            var method = ResolveMethod(request, seller, eligibleMethods);

            var sellerMethodKey = $"{seller:N}|{method.MethodCode}";
            if (!seenSellerMethod.Add(sellerMethodKey))
            {
                // Same seller + same canonical method must collapse into ONE group in v1
                // (whole-line membership, no quantity split).
                throw new ContractOperationException(CartShippingGroupFault.DuplicateSellerMethod);
            }

            // زمان‌بندی مستقل همین گروه مقدم است؛ اسکالر فقط سازگاری تک‌گروهی/قدیمی است.
            var groupDate = request.SelectedDeliveryDate ?? selectedDeliveryDate;
            var groupWindow = request.SelectedDeliveryTimeWindow ?? selectedDeliveryTimeWindow;

            var group = CartShippingDraftGroup.Create(
                newGroupId(),
                cartId,
                seller,
                method.MethodCode,
                method.ServiceCode,
                method.OptionCode,
                method.Label,
                request.OriginContext,
                method.PriceAmount,
                method.Currency,
                method.MinimumDeliveryDate,
                groupDate,
                groupWindow,
                method.LeadDays,
                now);

            foreach (var lineId in request.CartLineIds)
            {
                if (lineId == Guid.Empty)
                {
                    throw new ContractOperationException(CartShippingGroupFault.LineIdentityRequired);
                }

                if (!sellerByLine.TryGetValue(lineId, out var lineSeller))
                {
                    throw new ContractOperationException(CartShippingGroupFault.UnknownLine);
                }

                if (!assigned.Add(lineId))
                {
                    throw new ContractOperationException(CartShippingGroupFault.DuplicateLineMembership);
                }

                group.AddWholeLine(lineId, lineSeller);
            }

            group.EnsureHasLines();
            groups.Add(group);
        }

        if (assigned.Count != cartLines.Count || cartLines.Any(x => !assigned.Contains(x.LineId)))
        {
            throw new ContractOperationException(CartShippingGroupFault.MissingLineMembership);
        }

        // Deterministic order keeps the legacy scalar projection stable.
        return groups
            .OrderBy(x => x.SellerPartyId)
            .ThenBy(x => x.MethodCode, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// روش متعارف را برای <b>همان فروشنده</b> resolve می‌کند و کد سرویس/گزینهٔ اعلامی را با هویت
    /// authoritative تطبیق می‌دهد. روش ناواجد یا هویت ناهمخوان رد می‌شود؛ کلاینت نمی‌تواند روش بسازد.
    /// ردیف دارای فروشنده فقط برای همان فروشنده معتبر است و هرگز وعدهٔ فروشندهٔ دیگر را به‌کار نمی‌برد.
    /// </summary>
    private static CartShippingGroupEligibleMethod ResolveMethod(
        CartShippingGroupRequest request,
        Guid sellerPartyId,
        IReadOnlyList<CartShippingGroupEligibleMethod> eligibleMethods)
    {
        var code = (request.MethodCode ?? string.Empty).Trim();
        if (code.Length == 0)
        {
            throw new ContractOperationException(CartShippingGroupFault.MethodCodeRequired);
        }

        var sameCode = eligibleMethods
            .Where(m => string.Equals(m.MethodCode, code, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var method = sameCode.FirstOrDefault(m => m.SellerPartyId == sellerPartyId)
            ?? sameCode.FirstOrDefault(m => m.SellerPartyId is null)
            ?? throw new ContractOperationException(CartShippingGroupFault.MethodNotEligible);

        if (!MatchesDeclared(request.ServiceCode, method.ServiceCode)
            || !MatchesDeclared(request.OptionCode, method.OptionCode))
        {
            throw new ContractOperationException(CartShippingGroupFault.MethodIdentityMismatch);
        }

        return method;
    }

    private static bool MatchesDeclared(string? declared, string? authoritative)
    {
        var left = (declared ?? string.Empty).Trim();
        if (left.Length == 0)
        {
            return true;
        }

        return string.Equals(left, (authoritative ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
