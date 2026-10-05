using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Errors;

namespace Tooba.Identity.Application.Composition;

/// <summary>
/// Maps Identity faults into <see cref="Result"/> by stable machine code through the canonical
/// typed contract fault. Unknown exceptions propagate untouched to the global exception boundary.
/// </summary>
public static class IdentityOperation
{
    /// <summary>Runs an action and maps known Identity faults to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
    }

    /// <summary>Runs an action and maps known Identity faults to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
    }

    /// <summary>Collapses an authentication outcome into Result with the public failure code.</summary>
    public static Result<AuthenticationTicket> FromAuthResult(AuthenticationResult result, string failureCode)
    {
        if (result.Succeeded && result.Ticket is not null && !string.IsNullOrEmpty(result.Ticket.RefreshToken))
        {
            return Result.Success(result.Ticket);
        }

        return Result.Failure<AuthenticationTicket>(new SemanticError(failureCode));
    }
}
