using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Application;

namespace Tooba.Host.Admin;

/// <summary>نقشهٔ پیش‌نمایش backend به ویوی Admin و اعتبارسنجی بدون coercion خاموش.</summary>
public static class ReservationPolicyAdminComposer
{
    /// <summary>حداقل دقیقه.</summary>
    public const int MinMinutes = 1;

    /// <summary>حداکثر دقیقه (۳۰ روز).</summary>
    public const int MaxMinutes = 24 * 60 * 30;

    /// <summary>حداقل سقف چرخه.</summary>
    public const int MinCycles = 1;

    /// <summary>حداکثر سقف چرخه.</summary>
    public const int MaxCycles = 20;

    /// <summary>آستانهٔ هشدار غیرمسدودکننده برای رزرو خیلی طولانی (۲۴ ساعت).</summary>
    public const int LongHoldWarningMinutes = 24 * 60;

    /// <summary>مجوز صریح فروشنده؛ در کاتالوگ فعلی وجود ندارد.</summary>
    public const string SellerMutatePermission = "reservation.policy.mutate";

    /// <summary>ویوی فروشگاه.</summary>
    public static ReservationPolicyEditorView ForStore(ReservationPolicyPreview preview, bool canEdit) =>
        Map(
            "store",
            null,
            preview.AfterStore,
            preview.StoreInitialOverride,
            preview.StoreRetryOverride,
            preview.StoreMaxOverride,
            preview.Platform,
            canEdit,
            inheritFa: "ارث‌بری از سامانه",
            inheritEn: "Inherit from platform");

    /// <summary>ویوی دسته.</summary>
    public static ReservationPolicyEditorView ForCategory(
        Guid categoryId,
        ReservationPolicyPreview preview,
        bool canEdit) =>
        Map(
            "category",
            categoryId,
            preview.AfterCategory,
            preview.CategoryInitialOverride,
            preview.CategoryRetryOverride,
            preview.CategoryMaxOverride,
            preview.AfterStore,
            canEdit,
            inheritFa: "ارث‌بری از فروشگاه",
            inheritEn: "Inherit from store");

    /// <summary>ویوی پیشنهاد.</summary>
    public static ReservationPolicyEditorView ForOffer(
        Guid offerId,
        ReservationPolicyPreview preview,
        bool canEdit) =>
        Map(
            "offer",
            offerId,
            preview.AfterOffer,
            preview.OfferInitialOverride,
            preview.OfferRetryOverride,
            preview.OfferMaxOverride,
            preview.AfterCategory,
            canEdit,
            inheritFa: "ارث‌بری از دسته یا فروشگاه",
            inheritEn: "Inherit from category or store");

    /// <summary>دستهٔ اصلی محصول Offer را پیدا می‌کند.</summary>
    public static async Task<Guid?> ResolveOfferCategoryAsync(
        OfferDbContext offers,
        CatalogDbContext catalog,
        Guid offerId,
        CancellationToken cancellationToken)
    {
        var offer = await offers.Offers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        var variant = await catalog.Variants.AsNoTracking()
            .SingleOrDefaultAsync(x => x.VariantId == offer.CatalogVariantId, cancellationToken);
        if (variant is null)
        {
            return null;
        }

        return await catalog.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == variant.ProductId && x.Role == CatalogProductCategoryRole.Primary)
            .Select(x => (Guid?)x.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>دستهٔ اصلی چند Offer را بدون N+1 برمی‌گرداند.</summary>
    public static async Task<IReadOnlyDictionary<Guid, Guid?>> ResolveOfferCategoriesAsync(
        OfferDbContext offers,
        CatalogDbContext catalog,
        IReadOnlyList<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        var map = offerIds.Distinct().ToDictionary(x => x, _ => (Guid?)null);
        if (map.Count == 0)
        {
            return map;
        }

        var offerRows = await offers.Offers.AsNoTracking()
            .Where(x => map.Keys.Contains(x.OfferId))
            .Select(x => new { x.OfferId, x.CatalogVariantId })
            .ToListAsync(cancellationToken);
        var variantIds = offerRows.Select(x => x.CatalogVariantId).Distinct().ToArray();
        var variants = await catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToArray();
        var categories = await catalog.ProductCategories.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && x.Role == CatalogProductCategoryRole.Primary)
            .Select(x => new { x.ProductId, x.CategoryId })
            .ToListAsync(cancellationToken);
        var variantToProduct = variants.ToDictionary(x => x.VariantId, x => x.ProductId);
        var productToCategory = categories.ToDictionary(x => x.ProductId, x => (Guid?)x.CategoryId);
        foreach (var row in offerRows)
        {
            if (variantToProduct.TryGetValue(row.CatalogVariantId, out var productId)
                && productToCategory.TryGetValue(productId, out var categoryId))
            {
                map[row.OfferId] = categoryId;
            }
        }

        return map;
    }

    /// <summary>اعتبارسنجی بازه بدون clamp خاموش.</summary>
    public static void ValidateWrite(ReservationPolicyWriteRequest body)
    {
        ValidateMinutes(body.InitialReservationHoldMinutes, "reservation.policy.initial.invalid", "مدت رزرو اولیه باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        ValidateMinutes(body.RetryReservationHoldMinutes, "reservation.policy.retry.invalid", "مدت رزرو مجدد باید عددی صحیح بین ۱ و ۴۳۲۰۰ دقیقه باشد.");
        if (body.MaxReservationCycles is { } max && (max < MinCycles || max > MaxCycles))
        {
            throw new PlatformHttpException(400, "حداکثر دفعات رزرو باید عددی صحیح بین ۱ و ۲۰ باشد.", "reservation.policy.max.invalid");
        }
    }

    /// <summary>override را می‌نویسد یا ردیف را برای ارث‌بری حذف می‌کند.</summary>
    public static async Task ReplaceOverrideAsync(
        CatalogDbContext catalog,
        string scopeKind,
        Guid scopeId,
        ReservationPolicyWriteRequest body,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ValidateWrite(body);
        var row = await catalog.ReservationCyclePolicyOverrides
            .SingleOrDefaultAsync(x => x.ScopeKind == scopeKind && x.ScopeId == scopeId, cancellationToken);
        var oldInitial = row?.InitialReservationHoldMinutes;
        var oldRetry = row?.RetryReservationHoldMinutes;
        var oldMax = row?.MaxReservationCycles;
        var empty = body.InitialReservationHoldMinutes is null
            && body.RetryReservationHoldMinutes is null
            && body.MaxReservationCycles is null;
        if (empty)
        {
            if (row is not null)
            {
                catalog.ReservationCyclePolicyOverrides.Remove(row);
            }
        }
        else if (row is null)
        {
            catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
                scopeKind,
                scopeId,
                body.InitialReservationHoldMinutes,
                body.RetryReservationHoldMinutes,
                body.MaxReservationCycles,
                now));
        }
        else
        {
            row.Apply(
                body.InitialReservationHoldMinutes,
                body.RetryReservationHoldMinutes,
                body.MaxReservationCycles,
                now);
        }

        RecordField(catalog, scopeKind, scopeId, "InitialReservationHoldMinutes", oldInitial, body.InitialReservationHoldMinutes, actorUserId, now);
        RecordField(catalog, scopeKind, scopeId, "RetryReservationHoldMinutes", oldRetry, body.RetryReservationHoldMinutes, actorUserId, now);
        RecordField(catalog, scopeKind, scopeId, "MaxReservationCycles", oldMax, body.MaxReservationCycles, actorUserId, now);
        await catalog.SaveChangesAsync(cancellationToken);
    }

    /// <summary>override فروشگاه را جدا از مهلت پرداخت می‌نویسد.</summary>
    public static void ReplaceStore(
        StoreHoldPolicySettings store,
        ReservationPolicyWriteRequest body,
        Guid actorUserId,
        DateTimeOffset now,
        CatalogDbContext catalog)
    {
        ValidateWrite(body);
        RecordField(catalog, "store", null, "InitialReservationHoldMinutes", store.InitialReservationHoldMinutes, body.InitialReservationHoldMinutes, actorUserId, now);
        RecordField(catalog, "store", null, "RetryReservationHoldMinutes", store.RetryReservationHoldMinutes, body.RetryReservationHoldMinutes, actorUserId, now);
        RecordField(catalog, "store", null, "MaxReservationCycles", store.MaxReservationCycles, body.MaxReservationCycles, actorUserId, now);
        store.ReplaceReservationCycle(
            body.InitialReservationHoldMinutes,
            body.RetryReservationHoldMinutes,
            body.MaxReservationCycles,
            now);
    }

    private static void RecordField(
        CatalogDbContext catalog,
        string level,
        Guid? scopeId,
        string field,
        int? oldValue,
        int? newValue,
        Guid actorUserId,
        DateTimeOffset now)
    {
        if (oldValue == newValue)
        {
            return;
        }

        catalog.ReservationPolicyAuditEvents.Add(
            ReservationPolicyAuditEvent.Create(level, scopeId, field, oldValue, newValue, actorUserId, now));
    }

    private static void ValidateMinutes(int? value, string code, string title)
    {
        if (value is null)
        {
            return;
        }

        if (value < MinMinutes || value > MaxMinutes)
        {
            throw new PlatformHttpException(400, title, code);
        }
    }

    private static ReservationPolicyEditorView Map(
        string scope,
        Guid? scopeId,
        ReservationPolicyLayerPreview effective,
        int? initialOverride,
        int? retryOverride,
        int? maxOverride,
        ReservationPolicyLayerPreview inherited,
        bool canEdit,
        string inheritFa,
        string inheritEn)
    {
        var stricter = scope == "offer"
            && (effective.InitialHoldMinutes < inherited.InitialHoldMinutes
                || effective.RetryHoldMinutes < inherited.RetryHoldMinutes
                || effective.MaxCycles < inherited.MaxCycles);
        var longHold = scope == "offer"
            && ((initialOverride ?? 0) >= LongHoldWarningMinutes
                || (retryOverride ?? 0) >= LongHoldWarningMinutes);
        return new ReservationPolicyEditorView(
            scope,
            scopeId,
            Field(
                initialOverride,
                effective.InitialHoldMinutes,
                effective.InitialSource,
                "مدت رزرو اولیه",
                "Initial reservation hold",
                "از ورود سفارش به مرحله پرداخت شروع می‌شود. شکست پرداخت داخل رزرو فعال، تایمر را از نو شروع نمی‌کند.",
                "Starts when the order enters payment. A failed payment inside an active reservation does not reset the timer."),
            Field(
                retryOverride,
                effective.RetryHoldMinutes,
                effective.RetrySource,
                "مدت رزرو مجدد",
                "Retry reservation hold",
                "فقط پس از پایان رزرو قبلی و بازپس‌گیری موفق موجودی اعمال می‌شود.",
                "Applies only after the previous reservation ended and stock was reacquired."),
            Field(
                maxOverride,
                effective.MaxCycles,
                effective.MaxSource,
                "حداکثر دفعات رزرو",
                "Maximum reservation cycles",
                "شامل رزرو اولیه است. عدد ۱ یعنی فقط همان چرخه اول.",
                "Includes the initial reservation. 1 means only the first cycle."),
            canEdit,
            false,
            stricter,
            longHold,
            inheritFa,
            inheritEn,
            "در سفارش‌های چندکالایی، سخت‌گیرانه‌ترین سیاست اقلام اعمال می‌شود.",
            "For multi-item orders, the strictest line policy is applied.",
            "برای فروش‌های پرترافیک می‌توانید زمان رزرو این پیشنهاد را کوتاه‌تر کنید تا موجودی برای مدت طولانی قفل نشود.",
            "For high-traffic sales you can shorten this offer hold so inventory is not locked for long.",
            "این پیشنهاد از سیاست رزرو کوتاه‌تری نسبت به تنظیمات فروشگاه استفاده می‌کند.",
            "This offer uses a shorter reservation policy than the inherited settings.",
            "زمان رزرو طولانی می‌تواند در فروش‌های پرترافیک باعث قفل‌شدن موجودی شود.",
            "A long reservation hold can lock inventory during high-traffic sales.");
    }

    private static ReservationPolicyFieldView Field(
        int? overrideValue,
        int effective,
        string source,
        string labelFa,
        string labelEn,
        string helperFa,
        string helperEn) =>
        new(
            overrideValue,
            effective,
            source,
            overrideValue is not null,
            SourceFa(source),
            SourceEn(source),
            labelFa,
            labelEn,
            helperFa,
            helperEn);

    private static string SourceFa(string source) => source switch
    {
        "store" => "فروشگاه",
        "category" => "دسته",
        "offer" => "پیشنهاد",
        _ => "سامانه",
    };

    private static string SourceEn(string source) => source switch
    {
        "store" => "Store",
        "category" => "Category",
        "offer" => "Offer",
        _ => "Platform",
    };
}
