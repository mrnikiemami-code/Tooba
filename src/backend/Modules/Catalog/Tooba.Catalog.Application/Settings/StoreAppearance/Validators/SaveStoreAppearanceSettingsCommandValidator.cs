using FluentValidation;
using Tooba.Catalog.Application.Settings.StoreAppearance.Commands;

namespace Tooba.Catalog.Application.Settings.StoreAppearance.Validators;

/// <summary>Transport validator for SaveStoreAppearanceSettingsCommand.</summary>
public sealed class SaveStoreAppearanceSettingsCommandValidator : AbstractValidator<SaveStoreAppearanceSettingsCommand>
{
    /// <summary>Creates the validator.</summary>
    public SaveStoreAppearanceSettingsCommandValidator()
    {
        RuleFor(x => x.Model).NotNull();
    }
}