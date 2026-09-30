using Tooba.BuildingBlocks;
using Tooba.PageComposition.Contracts.Errors;

namespace Tooba.PageComposition.Application;

/// <summary>Maps known PageComposition InvalidOperation failures to stable SemanticError codes.</summary>
public static class PageCompositionFailureMapper
{
    /// <summary>Converts an InvalidOperationException into a SemanticException with a stable code.</summary>
    public static SemanticException ToSemantic(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = exception.Message ?? string.Empty;

        if (message.Contains("Tenant", StringComparison.OrdinalIgnoreCase)
            || message.Contains("resolve", StringComparison.OrdinalIgnoreCase))
        {
            return new SemanticException(new SemanticError(PageCompositionErrorCodes.TenantMissing));
        }

        if (message.Contains("یافت نشد", StringComparison.Ordinal))
            return new SemanticException(new SemanticError(PageCompositionErrorCodes.SectionMissing));

        if (message.Contains("کاتالوگ", StringComparison.Ordinal))
            return new SemanticException(new SemanticError(PageCompositionErrorCodes.SectionTypeRejected));

        if (message.Contains("ممنوع", StringComparison.Ordinal)
            || message.Contains("ناشناخته", StringComparison.Ordinal))
        {
            return new SemanticException(new SemanticError(PageCompositionErrorCodes.ConfigRejected));
        }

        return new SemanticException(new SemanticError(PageCompositionErrorCodes.MutationRejected));
    }
}
