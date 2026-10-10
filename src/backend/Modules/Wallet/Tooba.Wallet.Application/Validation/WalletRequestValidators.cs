using FluentValidation;
using Tooba.Wallet.Application.Admin.Commands;

using Tooba.Wallet.Application.Customer.Commands;
using Tooba.Wallet.Application.Admin.Models;
using Tooba.Wallet.Application.Admin.Queries;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Application.Validation;

/// <summary>
/// Transport-shape validators for Wallet (AMSC W1). Exactly the endpoint-reachable requests whose
/// shape can be invalid receive a validator; requests deliberately without one are recorded in the W1
/// evidence with a stable reason (a route-constrained <c>:guid</c> or a server-derived actor). Only
/// the transport shape is checked here — business/domain rules stay in Domain/Application.
/// </summary>
public sealed class RedeemCustomerGiftCardCommandValidator : AbstractValidator<RedeemCustomerGiftCardCommand>
{
    /// <summary>Defines the transport-shape rules for customer gift-card redemption.</summary>
    public RedeemCustomerGiftCardCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithErrorCode(WalletValidationCodes.CodeRequired)
            .Length(6, 64)
            .WithErrorCode(WalletValidationCodes.CodeLength);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(GiftCardRedemption.IdempotencyKeyMaxLength)
            .WithErrorCode(WalletValidationCodes.IdempotencyKeyTooLong);
    }
}

/// <summary>Transport-shape validator for admin gift-card issue.</summary>
public sealed class IssueAdminGiftCardCommandValidator : AbstractValidator<IssueAdminGiftCardCommand>
{
    /// <summary>Defines the transport-shape rules for admin gift-card issue.</summary>
    public IssueAdminGiftCardCommandValidator()
    {
        RuleFor(x => x.InitialAmount)
            .GreaterThan(0m)
            .WithErrorCode(WalletValidationCodes.InitialAmountPositive);
        RuleFor(x => x.Currency)
            .Must(BeEmptyOrValidLength)
            .WithErrorCode(WalletValidationCodes.CurrencyInvalid);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(GiftCard.IdempotencyKeyMaxLength)
            .WithErrorCode(WalletValidationCodes.IdempotencyKeyTooLong);
    }

    private static bool BeEmptyOrValidLength(string? currency) =>
        string.IsNullOrWhiteSpace(currency)
        || currency.Trim().Length is >= 3 and <= 8;
}

/// <summary>Transport-shape validator for the admin gift-card list filter.</summary>
public sealed class ListAdminGiftCardsQueryValidator : AbstractValidator<ListAdminGiftCardsQuery>
{
    private const int SearchMaxLength = 200;

    /// <summary>Defines the transport-shape rules for the admin gift-card list filter.</summary>
    public ListAdminGiftCardsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(BeKnownStatusOrEmpty)
            .WithErrorCode(WalletValidationCodes.StatusInvalid);
        RuleFor(x => x.Q)
            .MaximumLength(SearchMaxLength)
            .WithErrorCode(WalletValidationCodes.SearchTooLong);
    }

    private static bool BeKnownStatusOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<GiftCardStatus>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
}

/// <summary>Transport-shape validator for the admin wallet adjustment body.</summary>
public sealed class AdjustAdminWalletCommandValidator : AbstractValidator<AdjustAdminWalletCommand>
{
    private const int ReasonMaxLength = 500;

    /// <summary>Defines the transport-shape rules for the admin wallet adjustment body.</summary>
    public AdjustAdminWalletCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0m)
            .WithErrorCode(WalletValidationCodes.InitialAmountPositive);
        RuleFor(x => x.Direction)
            .Must(BeKnownDirection)
            .WithErrorCode(WalletValidationCodes.StatusInvalid);
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithErrorCode(WalletValidationCodes.CodeRequired)
            .MaximumLength(ReasonMaxLength)
            .WithErrorCode(WalletValidationCodes.SearchTooLong);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(WalletLedgerEntry.IdempotencyKeyMaxLength)
            .WithErrorCode(WalletValidationCodes.IdempotencyKeyTooLong);
    }

    private static bool BeKnownDirection(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && Enum.TryParse<LedgerDirection>(raw, ignoreCase: true, out var parsed)
        && Enum.IsDefined(parsed);
}
