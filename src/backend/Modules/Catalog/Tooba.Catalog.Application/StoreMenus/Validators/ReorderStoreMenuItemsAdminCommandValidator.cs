using FluentValidation;
using Tooba.Catalog.Application.StoreMenus.Commands;

namespace Tooba.Catalog.Application.StoreMenus.Validators;

/// <summary>Transport validator for ReorderStoreMenuItemsAdminCommand.</summary>
public sealed class ReorderStoreMenuItemsAdminCommandValidator : AbstractValidator<ReorderStoreMenuItemsAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReorderStoreMenuItemsAdminCommandValidator()
    {
        RuleFor(x => x.MenuId).NotEmpty();
        RuleFor(x => x.ItemIds).NotNull();
    }
}