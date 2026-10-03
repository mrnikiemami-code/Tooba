using FluentValidation;
using Tooba.Localization.Application.Languages.Queries;

namespace Tooba.Localization.Application.Languages.Validators;

/// <summary>
/// Marker validator for parameterless <see cref="ListLanguagesAdminQuery"/>.
/// Classification: VALIDATOR_REQUIRED_PRESENT (no transport fields).
/// </summary>
public sealed class ListLanguagesAdminQueryValidator : AbstractValidator<ListLanguagesAdminQuery>
{
    /// <summary>Registers no field rules (parameterless query).</summary>
    public ListLanguagesAdminQueryValidator()
    {
    }
}
