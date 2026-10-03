using FluentValidation;
using Tooba.Catalog.Application.StoreMenus.Commands;

namespace Tooba.Catalog.Application.StoreMenus.Validators;

/// <summary>Transport validator for AddStoreMenuItemAdminCommand.</summary>
public sealed class AddStoreMenuItemAdminCommandValidator : AbstractValidator<AddStoreMenuItemAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public AddStoreMenuItemAdminCommandValidator()
    {
        RuleFor(x => x.MenuId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Label).NotEmpty().WithErrorCode("menu.item.label.required");
    }
}