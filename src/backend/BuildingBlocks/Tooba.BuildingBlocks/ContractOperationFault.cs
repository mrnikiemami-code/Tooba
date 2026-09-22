namespace Tooba.BuildingBlocks;

/// <summary>
/// Reserved for typed contract-fault helpers. Must never classify exception Message text.
/// Prefer throwing/catching <see cref="ContractOperationException"/> by <see cref="ContractOperationException.Code"/>.
/// </summary>
public static class ContractOperationFault
{
    /// <summary>
    /// Runs an action and lets typed/unknown faults propagate unchanged.
    /// Does not inspect Message.
    /// </summary>
    public static Task<T> PassThroughAsync<T>(Func<Task<T>> action) => action();

    /// <summary>Void overload of <see cref="PassThroughAsync{T}"/>.</summary>
    public static Task PassThroughAsync(Func<Task> action) => action();
}
