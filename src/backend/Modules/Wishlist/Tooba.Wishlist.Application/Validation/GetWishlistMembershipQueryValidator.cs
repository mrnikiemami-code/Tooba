using FluentValidation;
using Tooba.Wishlist.Application.Customer.Queries;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Application.Validation;

/// <summary>اعتبارسنجی شکل انتقال <see cref="GetWishlistMembershipQuery"/>.</summary>
public sealed class GetWishlistMembershipQueryValidator : AbstractValidator<GetWishlistMembershipQuery>
{
    /// <summary>قواعد شکل اولیه را ثبت می‌کند؛ Actor از مرز اعتماد است.</summary>
    public GetWishlistMembershipQueryValidator()
    {
        RuleFor(x => x.ProductIds)
            .NotNull()
            .WithErrorCode(WishlistErrorCodes.ProductIdsRequired);
    }
}
