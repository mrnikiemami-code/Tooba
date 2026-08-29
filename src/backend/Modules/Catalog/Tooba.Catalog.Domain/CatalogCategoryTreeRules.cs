namespace Tooba.Catalog.Domain;

/// <summary>
/// قواعد سلسله‌مراتب رده: خود-والد و descendant-as-parent ممنوع؛ ترتیب خواهر/برادر جداست.
/// سطح محصول: Level = 1 + تعداد اجداد (ParentId)؛ فقط سطح ۳ قابل اختصاص به محصول است.
/// </summary>
public static class CatalogCategoryTreeRules
{
    /// <summary>سطح قابل اختصاص محصول به رده (سطح سوم).</summary>
    public const int ProductAssignableLevel = 3;

    /// <summary>پیام خطای اختصاص ردهٔ غیرفعال برای محصول.</summary>
    public const string ProductAssignableLevelRequiredMessageFa =
        "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.";

    /// <summary>
    /// آیا <paramref name="nodeId"/> زیر درخت <paramref name="ancestorId"/> است؟
    /// </summary>
    public static bool IsDescendant(
        Guid ancestorId,
        Guid nodeId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (ancestorId == nodeId)
        {
            return false;
        }

        var current = nodeId;
        var guard = 0;
        while (parentById.TryGetValue(current, out var parent) && parent is Guid p)
        {
            if (p == ancestorId)
            {
                return true;
            }

            current = p;
            if (++guard > parentById.Count + 2)
            {
                throw new InvalidOperationException("حلقهٔ موجود در درخت رده تشخیص داده شد.");
            }
        }

        return false;
    }

    /// <summary>
    /// سطح رده = ۱ + تعداد اجداد از طریق ParentId. ریشه (بدون والد) سطح ۱ است.
    /// </summary>
    public static int GetCategoryLevel(Guid categoryId, IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (!parentById.ContainsKey(categoryId))
        {
            throw new InvalidOperationException("رده در Catalog این Tenant وجود ندارد.");
        }

        var ancestors = 0;
        var current = categoryId;
        var guard = 0;
        while (parentById.TryGetValue(current, out var parent) && parent is Guid p)
        {
            ancestors++;
            current = p;
            if (++guard > parentById.Count + 2)
            {
                throw new InvalidOperationException("حلقهٔ موجود در درخت رده تشخیص داده شد.");
            }
        }

        return 1 + ancestors;
    }

    /// <summary>آیا رده برای اختصاص به محصول مجاز است؟ (فقط سطح ۳)</summary>
    public static bool IsAssignableProductCategory(
        Guid categoryId,
        IReadOnlyDictionary<Guid, Guid?> parentById) =>
        GetCategoryLevel(categoryId, parentById) == ProductAssignableLevel;

    /// <summary>اختصاص محصول به ردهٔ سطح ۱ یا ۲ را رد می‌کند.</summary>
    public static void EnsureAssignableProductCategory(
        Guid categoryId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (!IsAssignableProductCategory(categoryId, parentById))
        {
            throw new InvalidOperationException(ProductAssignableLevelRequiredMessageFa);
        }
    }

    /// <summary>جابه‌جایی را بدون ایجاد حلقه اعتبارسنجی می‌کند.</summary>
    public static void ValidateMove(
        Guid categoryId,
        Guid? newParentId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        ValidateNoCycle(categoryId, newParentId, parentById);
    }

    /// <summary>خود-والد و والد بودن descendant را رد می‌کند.</summary>
    public static void ValidateNoCycle(
        Guid categoryId,
        Guid? newParentId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (newParentId is null || newParentId == Guid.Empty)
        {
            return;
        }

        if (newParentId == categoryId)
        {
            throw new InvalidOperationException("رده نمی‌تواند والد خودش باشد؛ حلقهٔ درخت طبقه‌بندی ممنوع است.");
        }

        if (!parentById.ContainsKey(newParentId.Value))
        {
            throw new InvalidOperationException("ردهٔ والد در Catalog این Tenant وجود ندارد.");
        }

        if (IsDescendant(categoryId, newParentId.Value, parentById))
        {
            throw new InvalidOperationException("نمی‌توان رده را زیر نوادهٔ خودش قرار داد؛ حلقهٔ درخت ممنوع است.");
        }
    }
}
