using FluentValidation;
using Tooba.Catalog.Application.StoreMenus.Commands;

namespace Tooba.Catalog.Application.StoreMenus.Validators;

/// <summary>Transport validator for CreateStoreMenuAdminCommand.</summary>
public sealed class CreateStoreMenuAdminCommandValidator : AbstractValidator<CreateStoreMenuAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public CreateStoreMenuAdminCommandValidator()
    {
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Title).NotEmpty().WithErrorCode("menu.title.required");
        RuleFor(x => x.Body.MenuKey).NotEmpty().WithErrorCode("menu.key.required");
    }
}