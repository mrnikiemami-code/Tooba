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
    /// <remarks>
    /// TB-SHIPPING-GROUP-ID-CONTINUITY-153: <c>reusableGroups</c> گروه‌های <b>ذخیره‌شدهٔ همان پیش‌نویس</b>
    /// هستند که اجازهٔ بازاستفادهٔ شناسه دارند. اگر گروهی دقیقاً همان تعهد منطقی درخواست باشد (همان سبد،
    /// فروشنده، روش/سرویس/گزینه و همان عضویت خطوط)، همان <c>ShippingGroupId</c> پایدار می‌ماند؛ وگرنه
    /// شناسهٔ تازه ساخته می‌شود. این لیست فقط از پیش‌نویس مورداعتماد سرور پر می‌شود و هیچ شناسه‌ای از کلاینت
    /// پذیرفته نمی‌شود.
    /// </remarks>
    public static IReadOnlyList<CartShippingDraftGroup> Plan(
        Guid cartId,
        IReadOnlyList<CartShippingGroupCartLine> cartLines,
        IReadOnlyList<CartShippingGroupRequest> requests,
        IReadOnlyList<CartShippingGroupEligibleMethod> eligibleMethods,
        DateOnly? selectedDeliveryDate,
        string? selectedDeliveryTimeWindow,
        DateTimeOffset now,
        Func<Guid> newGroupId,
        IReadOnlyList<CartShippingDraftGroup>? reusableGroups = null)
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
        var reusedIds = new HashSet<Guid>();

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

            // TB-SHIPPING-GROUP-ID-CONTINUITY-153: هویت پایدار فقط برای همان تعهد منطقی بازاستفاده
            // می‌شود؛ تغییر فروشنده/روش/سرویس/گزینه یا عضویت خطوط شناسهٔ تازه می‌گیرد.
            var reusedId = FindReusableGroupId(
                reusableGroups, reusedIds, cartId, seller, method, request.CartLineIds);

            var group = CartShippingDraftGroup.Create(
                reusedId ?? newGroupId(),
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

    /// <summary>
    /// TB-SHIPPING-GROUP-ID-CONTINUITY-153: نخستین گروه ذخیره‌شده‌ای که <b>همان تعهد منطقی</b> درخواست
    /// است را برمی‌گرداند (برای حفظ <c>ShippingGroupId</c> پایدار). هر شناسه حداکثر یک بار بازاستفاده
    /// می‌شود تا هرگز دو گروه شناسهٔ یکسان نگیرند. منبع تصمیم فقط پیش‌نویس مورداعتماد سرور است.
    /// </summary>
    private static Guid? FindReusableGroupId(
        IReadOnlyList<CartShippingDraftGroup>? reusableGroups,
        HashSet<Guid> reusedIds,
        Guid cartId,
        Guid sellerPartyId,
        CartShippingGroupEligibleMethod method,
        IReadOnlyList<Guid> cartLineIds)
    {
        if (reusableGroups is not { Count: > 0 })
        {
            return null;
        }

        foreach (var candidate in reusableGroups)
        {
            if (candidate.ShippingGroupId == Guid.Empty || reusedIds.Contains(candidate.ShippingGroupId))
            {
                continue;
            }

            if (!IsSameLogicalObligation(candidate, cartId, sellerPartyId, method, cartLineIds))
            {
                continue;
            }

            reusedIds.Add(candidate.ShippingGroupId);
            return candidate.ShippingGroupId;
        }

        return null;
    }

    /// <summary>
    /// آیا گروه ذخیره‌شده و درخواست جدید یک تعهد منطقی واحدند؟ برابری باید کامل باشد: همان سبد،
    /// همان فروشنده، همان روش canonical با همان هویت سرویس/گزینه، همان بافت مبدأ (که فعلاً فقط خالی
    /// مجاز است) و <b>همان مجموعهٔ خطوط</b>. تاریخ/پنجره عمداً در این هویت نیستند، پس تغییر زمان‌بندی
    /// یا تلاش تکراری (idempotent) شناسه را پایدار نگه می‌دارد، ولی تغییر عضویت/فروشنده/روش شناسهٔ
    /// تازه می‌گیرد و هرگز دادهٔ گروه دیگر بازاستفاده نمی‌شود.
    /// </summary>
    public static bool IsSameLogicalObligation(
        CartShippingDraftGroup existing,
        Guid cartId,
        Guid sellerPartyId,
        CartShippingGroupEligibleMethod method,
        IReadOnlyList<Guid> cartLineIds)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(cartLineIds);

        if (existing.ShippingGroupId == Guid.Empty
            || existing.CartId != cartId
            || existing.SellerPartyId != sellerPartyId)
        {
            return false;
        }

        if (!string.Equals(
                (existing.MethodCode ?? string.Empty).Trim(),
                (method.MethodCode ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return SameCode(existing.ServiceCode, method.ServiceCode)
            && SameCode(existing.OptionCode, method.OptionCode)
            && existing.OriginContext is null
            && SameLineMembership(existing, cartLineIds);
    }

    /// <summary>برابری دقیق دو کد اختیاری پس از trim؛ خالی و تهی یکسان‌اند (نه wildcard).</summary>
    private static bool SameCode(string? left, string? right) =>
        string.Equals(
            (left ?? string.Empty).Trim(),
            (right ?? string.Empty).Trim(),
            StringComparison.OrdinalIgnoreCase);

    private static bool SameLineMembership(CartShippingDraftGroup existing, IReadOnlyList<Guid> cartLineIds)
    {
        if (cartLineIds.Count == 0 || existing.Lines.Count != cartLineIds.Count)
        {
            return false;
        }

        var existingLines = existing.Lines.Select(x => x.CartLineId).ToHashSet();
        if (existingLines.Count != cartLineIds.Count)
        {
            return false;
        }

        return cartLineIds.All(existingLines.Contains);
    }
}
