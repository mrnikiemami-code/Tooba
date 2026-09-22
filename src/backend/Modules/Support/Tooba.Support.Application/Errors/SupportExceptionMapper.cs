using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Support.Application.Errors;

/// <summary>
/// Maps known Support directory/domain stable machine codes to SemanticError.
/// Exact message match only — no Contains, no StartsWith prose heuristics; unknowns rethrow.
/// </summary>
public static class SupportExceptionMapper
{
    /// <summary>
    /// Known exact machine codes emitted by Support Domain/Application/Infrastructure.
    /// Mapped to the use-case public outcome code supplied by the caller.
    /// </summary>
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        "support.ticket_not_found",
        "support.reply_closed",
        "support.related_order_id_invalid",
        "support.ticket.id_required",
        "support.requester_required",
        "support.seller_party_required",
        "support.subject_invalid",
        "support.close_not_allowed",
        "support.reopen_not_allowed",
        "support.related_id_without_type",
        "support.related_type_invalid",
        "support.related_id_required",
        "support.idempotency_key_invalid",
        "support.message.ids_required",
        "support.message.body_invalid",
        "support.message.internal_admin_only",
        "support.category_invalid",
        "support.priority_invalid",
        "support.status_invalid",
        "support.requester_kind_invalid",
        "support.outbox.emit_not_supported",
    };

    /// <summary>True when message is an exact known Support machine code.</summary>
    public static bool TryMapExact(string? message, string publicErrorCode, out SemanticError error)
    {
        if (message is not null && KnownCodes.Contains(message))
        {
            error = new SemanticError(publicErrorCode);
            return true;
        }

        error = default!;
        return false;
    }

    /// <summary>Converts a known Support InvalidOperationException into a SemanticError. Unknowns rethrow.</summary>
    public static SemanticError ToSemanticError(InvalidOperationException exception, string publicErrorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (TryMapExact(exception.Message, publicErrorCode, out var error))
            return error;
        throw exception;
    }

    /// <summary>Runs a Support directory action and maps only known expected failures to Result.</summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action, string publicErrorCode)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, publicErrorCode, out var error))
        {
            return Result.Failure<T>(error);
        }
    }
}
