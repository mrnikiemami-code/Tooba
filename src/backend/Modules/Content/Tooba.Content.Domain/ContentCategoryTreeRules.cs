namespace Tooba.Content.Domain;

/// <summary>قواعد درخت دسته‌بندی مقاله — حداکثر عمق ۲ (ریشه=۱، زیردسته=۲).</summary>
public static class ContentCategoryTreeRules
{
    /// <summary>حداکثر عمق مجاز درخت دسته‌بندی مقاله.</summary>
    public const int MaxDepth = 2;

    /// <summary>عمق گره را محاسبه می‌کند (ریشه بدون والد = ۱).</summary>
    public static int ComputeDepth(Guid? categoryId, IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (categoryId is null)
        {
            return 0;
        }

        if (!parentById.ContainsKey(categoryId.Value))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.NotFound);
        }

        var depth = 1;
        var current = categoryId.Value;
        var guard = 0;
        while (parentById.TryGetValue(current, out var parent) && parent is Guid p)
        {
            depth++;
            current = p;
            if (++guard > parentById.Count + 2)
            {
                throw new InvalidOperationException(ContentCategoryErrorCodes.CycleDetected);
            }
        }

        return depth;
    }

    /// <summary>ارتفاع زیردرخت شامل خود گره (برگ = ۱).</summary>
    public static int ComputeSubtreeHeight(
        Guid categoryId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        var childrenByParent = new Dictionary<Guid, List<Guid>>();
        foreach (var (id, parent) in parentById)
        {
            if (parent is not Guid parentId)
            {
                continue;
            }

            if (!childrenByParent.TryGetValue(parentId, out var list))
            {
                list = [];
                childrenByParent[parentId] = list;
            }

            list.Add(id);
        }

        int Height(Guid id)
        {
            if (!childrenByParent.TryGetValue(id, out var children) || children.Count == 0)
            {
                return 1;
            }

            var maxChild = 0;
            foreach (var child in children)
            {
                maxChild = Math.Max(maxChild, Height(child));
            }

            return 1 + maxChild;
        }

        return Height(categoryId);
    }

    /// <summary>آیا nodeId زیرمجموعهٔ ancestorId است.</summary>
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
                throw new InvalidOperationException(ContentCategoryErrorCodes.CycleDetected);
            }
        }

        return false;
    }

    /// <summary>ایجاد فرزند زیر والد را اعتبارسنجی می‌کند.</summary>
    public static void ValidateCreateUnderParent(
        Guid parentId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (!parentById.ContainsKey(parentId))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.InvalidParent);
        }

        var parentDepth = ComputeDepth(parentId, parentById);
        if (parentDepth >= MaxDepth)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.MaxDepthExceeded);
        }
    }

    /// <summary>جابه‌جایی والد را اعتبارسنجی می‌کند.</summary>
    public static void ValidateMove(
        Guid categoryId,
        Guid? newParentId,
        IReadOnlyDictionary<Guid, Guid?> parentById,
        IReadOnlyDictionary<Guid, string> languageById)
    {
        if (newParentId is null)
        {
            return;
        }

        if (newParentId == categoryId)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.SelfParent);
        }

        if (!parentById.ContainsKey(categoryId) || !parentById.ContainsKey(newParentId.Value))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.NotFound);
        }

        if (!string.Equals(languageById[categoryId], languageById[newParentId.Value], StringComparison.Ordinal))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.CrossLanguageParent);
        }

        if (IsDescendant(categoryId, newParentId.Value, parentById))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.DescendantParent);
        }

        var parentDepth = ComputeDepth(newParentId.Value, parentById);
        var subtreeHeight = ComputeSubtreeHeight(categoryId, parentById);
        if (parentDepth + subtreeHeight > MaxDepth)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.MaxDepthExceeded);
        }
    }
}
