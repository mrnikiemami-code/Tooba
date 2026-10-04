using Tooba.BuildingBlocks.Results;

namespace Tooba.Offer.Contracts.ReturnPolicy;

/// <summary>
/// resolve سیاست مؤثر در زمان خرید.
/// </summary>
public interface IReturnPolicyResolver
{
    /// <summary>گزینه‌های حاکمیت جاری.</summary>
    ReturnPolicyOptions Options { get; }

    /// <summary>اعتبارسنجی انتخاب Offer قبل از ذخیره.</summary>
    Result ValidateOfferChoice(string choice, int? customReturnWindowDays);

    /// <summary>resolve مؤثر برای checkout snapshot.</summary>
    ResolvedReturnPolicy ResolveForCheckout(string choice, int? customReturnWindowDays, bool? categoryForcesNonReturnable = null);
}
