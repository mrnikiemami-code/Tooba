using FluentValidation;
using Tooba.Catalog.Application.StoreMenus.Commands;

namespace Tooba.Catalog.Application.StoreMenus.Validators;

/// <summary>Transport validator for UpdateStoreMenuAdminCommand.</summary>
public sealed class UpdateStoreMenuAdminCommandValidator : AbstractValidator<UpdateStoreMenuAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateStoreMenuAdminCommandValidator()
    {
        RuleFor(x => x.MenuId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Title).NotEmpty().WithErrorCode("menu.title.required");
    }
}