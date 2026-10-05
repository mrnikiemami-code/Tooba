using Tooba.BuildingBlocks;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Domain.Rules;

/// <summary>قواعد امنیتی بدنهٔ HTML مقاله.</summary>
public static class ContentArticleBodyRules
{
    /// <summary>از نگهداری base64 یا data URI در بدنه جلوگیری می‌کند.</summary>
    public static void EnsureNoEmbeddedBinary(string body)
    {
        if (string.IsNullOrEmpty(body)) return;
        if (body.Contains("data:image", StringComparison.OrdinalIgnoreCase)
            || body.Contains("data:application", StringComparison.OrdinalIgnoreCase))
        {
            throw new ContractOperationException(ContentErrorCodes.UnsafeBodyMedia);
        }
    }
}
