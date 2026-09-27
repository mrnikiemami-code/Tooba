using FluentValidation;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Validators;

/// <summary>Transport: actor must be present after Admin auth (Policy coerce is Host parity).</summary>
public sealed class SaveCheckoutIdentitySettingsCommandValidator
    : AbstractValidator<SaveCheckoutIdentitySettingsCommand>
{
    /// <summary>Creates the validator.</summary>
    public SaveCheckoutIdentitySettingsCommandValidator()
    {
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(CatalogValidationCodes.CheckoutIdentityActorRequired);
    }
}
