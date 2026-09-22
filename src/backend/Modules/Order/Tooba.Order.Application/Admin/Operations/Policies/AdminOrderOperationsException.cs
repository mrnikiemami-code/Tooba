namespace Tooba.Order.Application.Admin.Operations.Policies;

/// <summary>
/// Expected admin-order-operations failure carrying a stable semantic code (not HTTP).
/// Orchestration catches this and converts to <c>Result.Failure(new SemanticError(Code))</c>.
/// </summary>
public sealed class AdminOrderOperationsException : Exception
{
    /// <summary>Creates a typed ops failure.</summary>
    public AdminOrderOperationsException(string code, string? message = null)
        : base(message ?? code)
    {
        Code = code;
    }

    /// <summary>Stable semantic error code.</summary>
    public string Code { get; }
}
