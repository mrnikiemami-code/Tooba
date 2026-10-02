using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Composition;

/// <summary>
/// Maps Identity Contracts faults into Result by stable machine code. Unknown exceptions propagate.
/// </summary>
public static class IdentityOperation
{
    /// <summary>Runs an action and maps known Identity faults to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (IdentityDuplicateIdentifierFault)
        {
            return Result.Failure<T>(new SemanticError(IdentityErrorCodes.IdentifierConflict));
        }
        catch (ArgumentException)
        {
            return Result.Failure<T>(new SemanticError(IdentityErrorCodes.ValidationFailed));
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<T>(new SemanticError(IdentityErrorCodes.ValidationFailed));
        }
    }

    /// <summary>Runs an action and maps known Identity faults to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (IdentityDuplicateIdentifierFault)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.IdentifierConflict));
        }
        catch (ArgumentException)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.ValidationFailed));
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.ValidationFailed));
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
