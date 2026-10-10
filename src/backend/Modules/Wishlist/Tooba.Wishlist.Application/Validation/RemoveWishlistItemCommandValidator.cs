using FluentValidation;
using Tooba.Wishlist.Application.Customer.Commands;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Application.Validation;

/// <summary>اعتبارسنجی شکل انتقال <see cref="RemoveWishlistItemCommand"/>.</summary>
public sealed class RemoveWishlistItemCommandValidator : AbstractValidator<RemoveWishlistItemCommand>
{
    /// <summary>قواعد شکل اولیه را ثبت می‌کند؛ Actor از مرز اعتماد است.</summary>
    public RemoveWishlistItemCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithErrorCode(WishlistErrorCodes.ProductIdRequired);
    }
}
