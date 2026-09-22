using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Ports;

/// <summary>Wire/destination helpers — ParseDestination only; exception mapping lives in ReturnsExceptionMapper.</summary>
public static class ReturnSemanticMapper
{
    /// <summary>Parse مقصد بازپرداخت؛ نامعتبر → SemanticError.</summary>
    public static Result<RefundDestination> ParseDestination(string? value) =>
        ReturnsExceptionMapper.ParseDestination(value);
}
