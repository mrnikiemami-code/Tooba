using FluentValidation;
using Tooba.Wishlist.Application.Customer.Commands;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Application.Validation;

/// <summary>اعتبارسنجی شکل انتقال <see cref="AddWishlistItemCommand"/>.</summary>
public sealed class AddWishlistItemCommandValidator : AbstractValidator<AddWishlistItemCommand>
{
    /// <summary>قواعد شکل اولیه را ثبت می‌کند؛ Actor از مرز اعتماد است.</summary>
    public AddWishlistItemCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithErrorCode(WishlistErrorCodes.ProductIdRequired);
    }
}
