namespace Tooba.Host.Grid;

/// <summary>
/// ارزیابی left-to-right برای Advanced Filter: ((A op1 B) op2 C).
/// </summary>
public static class AdminProductGridAdvancedFilterEvaluator
{
    /// <summary>اتصال‌دهنده‌های and/or را به ترتیب چپ→راست روی مجموعهٔ idها اعمال می‌کند.</summary>
    public static HashSet<Guid> EvaluateLeftToRight(
        IReadOnlyList<HashSet<Guid>> conditionSets,
        IReadOnlyList<string> connectors)
    {
        if (conditionSets.Count == 0)
        {
            return [];
        }

        var result = new HashSet<Guid>(conditionSets[0]);
        for (var index = 1; index < conditionSets.Count; index++)
        {
            var connector = index - 1 < connectors.Count ? connectors[index - 1] : "and";
            var next = conditionSets[index];
            if (string.Equals(connector, "or", StringComparison.OrdinalIgnoreCase))
            {
                result.UnionWith(next);
            }
            else
            {
                result.IntersectWith(next);
            }
        }

        return result;
    }
}
