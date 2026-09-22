namespace Tooba.BuildingBlocks;

/// <summary>
/// Expected cross-module admin/operation failure carrying a stable semantic <see cref="Code"/>
/// (not prose). Owning Domain/Directory throw this directly; adapters must not infer Code from Message.
/// </summary>
public sealed class ContractOperationException : Exception
{
    /// <summary>Creates a typed contract-boundary failure.</summary>
    public ContractOperationException(string code, Exception? innerException = null)
        : base(code, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>Stable semantic error code (property — not Message-based classification).</summary>
    public string Code { get; }
}
