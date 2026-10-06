using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Contracts.Errors;

namespace Tooba.Notification.Application.Composition;

/// <summary>
/// Maps the module's typed faults into <see cref="Result"/> failures by stable code.
/// Notification raises one typed, code-carrying fault mechanism and it is mapped here by its
/// declared stable code: <see cref="ContractOperationException"/> (via
/// <see cref="ContractOperationException.Code"/>). Classification is by typed code only — never
/// by message/prose heuristics. A contract fault whose code is not a declared Notification code,
/// and every unknown exception, propagates untouched to the canonical global exception boundary.
/// Mirrors the certified Cart/Fulfillment/Inventory/Content/Localization/Media <c>*Operation</c>
/// seam so Notification can be extracted into an isolated microservice without leaking result
/// mapping into callers.
/// </summary>
public static class NotificationOperation
{
    /// <summary>Runs an operation and maps a typed fault to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (NotificationErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
    }

    /// <summary>Runs an operation without a value and maps a typed fault to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex) when (NotificationErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
    }
}
