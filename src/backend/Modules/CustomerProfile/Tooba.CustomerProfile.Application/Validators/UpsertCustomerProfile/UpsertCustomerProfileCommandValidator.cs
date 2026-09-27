using FluentValidation;
using Tooba.CustomerProfile.Application.Commands.UpsertCustomerProfile;

namespace Tooba.CustomerProfile.Application.Validators.UpsertCustomerProfile;

/// <summary>Stable transport validation codes for customer profile write shape.</summary>
public static class CustomerProfileValidationCodes
{
    /// <summary>Display name must be present after trim (transport shape only).</summary>
    public const string DisplayNameRequired = "customer.profile.display_name_required";
}

/// <summary>
/// Transport-shape validator for <see cref="UpsertCustomerProfileCommand"/>.
/// Length/business rules remain in Domain; ActorUserId is server-trusted and not validated as payload.
/// </summary>
public sealed class UpsertCustomerProfileCommandValidator : AbstractValidator<UpsertCustomerProfileCommand>
{
    /// <summary>Registers non-blank DisplayName transport rule.</summary>
    public UpsertCustomerProfileCommandValidator()
    {
        RuleFor(x => x.Input.DisplayName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode(CustomerProfileValidationCodes.DisplayNameRequired);
    }
}
