namespace Tooba.BuildingBlocks;

/// <summary>
/// Promotes InvalidOperationException whose Message is already a stable machine code
/// into <see cref="ContractOperationException"/>. Allowed only at owning Infrastructure /
/// contract adapter boundaries — not in foreign Application layers.
/// </summary>
public static class ContractOperationFault
{
    /// <summary>
    /// True when <paramref name="message"/> looks like a dotted stable error code
    /// (e.g. fulfillment.pack.requires_processing), not localized prose.
    /// </summary>
    public static bool LooksLikeStableCode(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        // Stable codes are ASCII dotted tokens; reject whitespace / Persian / Latin prose.
        foreach (var ch in message)
        {
            if (ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '.' or '_' or '-')
            {
                continue;
            }

            return false;
        }

        return message.Contains('.', StringComparison.Ordinal);
    }

    /// <summary>Runs an action and promotes known stable-code IOE to <see cref="ContractOperationException"/>.</summary>
    public static async Task<T> MapAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (LooksLikeStableCode(ex.Message))
        {
            throw new ContractOperationException(ex.Message, ex);
        }
    }

    /// <summary>Void overload of MapAsync.</summary>
    public static async Task MapAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (LooksLikeStableCode(ex.Message))
        {
            throw new ContractOperationException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Like MapAsync but remaps the stable code via <paramref name="mapCode"/>
    /// (owning-module catalog normalization).
    /// </summary>
    public static async Task<T> MapAsync<T>(Func<Task<T>> action, Func<string, string> mapCode)
    {
        try
        {
            return await action();
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (LooksLikeStableCode(ex.Message))
        {
            throw new ContractOperationException(mapCode(ex.Message), ex);
        }
    }

    /// <summary>Void overload with code remapping.</summary>
    public static async Task MapAsync(Func<Task> action, Func<string, string> mapCode)
    {
        try
        {
            await action();
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (LooksLikeStableCode(ex.Message))
        {
            throw new ContractOperationException(mapCode(ex.Message), ex);
        }
    }
}
