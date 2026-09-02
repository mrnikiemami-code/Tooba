namespace Tooba.Content.Domain;

/// <summary>قواعد درخت دسته‌بندی مقاله — بدون سقف L1/L2/L3.</summary>
public static class ContentCategoryTreeRules
{
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
    }
}
