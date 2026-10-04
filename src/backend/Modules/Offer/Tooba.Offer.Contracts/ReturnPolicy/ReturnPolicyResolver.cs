using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Contracts.Errors;

namespace Tooba.Offer.Contracts.ReturnPolicy;

/// <summary>
/// پیاده‌سازی حاکمیت فروشگاه/پلتفرم برای سیاست مرجوعی.
/// </summary>
/// <remarks>
/// This governance implementation intentionally lives in the Offer Contracts assembly because Order
/// checkout consumes it through the pre-existing Contracts-only Offer edge
/// (<c>Tooba.Order.Infrastructure</c> → <c>Tooba.Offer.Contracts</c>) and must not take an
/// Offer.Application reference. It is split into its own responsibility file; the port and the
/// boundary vocabulary remain in <see cref="IReturnPolicyResolver"/>,
/// <see cref="ReturnPolicyOptions"/>, <see cref="OfferReturnPolicyChoices"/> and
/// <see cref="ResolvedReturnPolicy"/>.
/// </remarks>
public sealed class ReturnPolicyResolver : IReturnPolicyResolver
{
    private readonly ReturnPolicyOptions _options;

    /// <summary>resolver را با گزینه‌ها می‌سازد.</summary>
    public ReturnPolicyResolver(ReturnPolicyOptions options) =>
        _options = options ?? new ReturnPolicyOptions();

    /// <inheritdoc />
    public ReturnPolicyOptions Options => _options;

    /// <inheritdoc />
    public Result ValidateOfferChoice(string choice, int? customReturnWindowDays)
    {
        var normalized = OfferReturnPolicyChoices.Normalize(choice);
        if (normalized == OfferReturnPolicyChoices.Default)
        {
            return Result.Success();
        }

        if (!_options.SellerCanOverrideReturnPolicy)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.ReturnPolicyOverrideDenied));
        }

        if (normalized == OfferReturnPolicyChoices.NonReturnable)
        {
            if (!_options.AllowNonReturnableOffers)
            {
                return Result.Failure(new SemanticError(OfferErrorCodes.NonReturnableDenied));
            }

            return Result.Success();
        }

        if (normalized == OfferReturnPolicyChoices.Custom)
        {
            if (customReturnWindowDays is null)
            {
                return Result.Failure(new SemanticError(OfferErrorCodes.CustomReturnWindowRequired));
            }

            if (customReturnWindowDays < _options.MinReturnWindowDays
                || customReturnWindowDays > _options.MaxReturnWindowDays)
            {
                return Result.Failure(new SemanticError(
                    OfferErrorCodes.CustomReturnWindowOutOfRange,
                    new Dictionary<string, string?>
                    {
                        ["min"] = _options.MinReturnWindowDays.ToString(),
                        ["max"] = _options.MaxReturnWindowDays.ToString(),
                    }));
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public ResolvedReturnPolicy ResolveForCheckout(
        string choice,
        int? customReturnWindowDays,
        bool? categoryForcesNonReturnable = null)
    {
        if (categoryForcesNonReturnable == true)
        {
            return new ResolvedReturnPolicy(false, 0, "category_restriction", "غیرقابل مرجوعی");
        }

        var normalized = OfferReturnPolicyChoices.Normalize(choice);
        if (ValidateOfferChoice(normalized, customReturnWindowDays).IsFailure)
        {
            // Invalid seller choice at checkout falls back to the safe store default.
            normalized = OfferReturnPolicyChoices.Default;
            customReturnWindowDays = null;
        }

        if (normalized == OfferReturnPolicyChoices.NonReturnable)
        {
            return new ResolvedReturnPolicy(false, 0, "offer_override", "غیرقابل مرجوعی");
        }

        if (normalized == OfferReturnPolicyChoices.Custom && customReturnWindowDays is int days)
        {
            return new ResolvedReturnPolicy(
                true,
                days,
                "offer_override",
                FormatWindowLabel(days));
        }

        var defaults = Math.Max(0, _options.DefaultReturnWindowDays);
        return new ResolvedReturnPolicy(
            defaults > 0,
            defaults,
            "platform_default",
            defaults > 0 ? FormatWindowLabel(defaults) : "غیرقابل مرجوعی");
    }

    private static string FormatWindowLabel(int days) => $"{days} روز پس از تحویل";
}
