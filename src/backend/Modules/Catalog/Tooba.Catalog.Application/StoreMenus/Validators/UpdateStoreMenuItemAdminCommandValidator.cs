using FluentValidation;
using Tooba.Catalog.Application.StoreMenus.Commands;

namespace Tooba.Catalog.Application.StoreMenus.Validators;

/// <summary>Transport validator for UpdateStoreMenuItemAdminCommand.</summary>
public sealed class UpdateStoreMenuItemAdminCommandValidator : AbstractValidator<UpdateStoreMenuItemAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateStoreMenuItemAdminCommandValidator()
    {
        RuleFor(x => x.MenuId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Label).NotEmpty().WithErrorCode("menu.item.label.required");
    }
}