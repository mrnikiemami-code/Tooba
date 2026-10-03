using FluentValidation;
using Tooba.Wishlist.Application.Customer.Commands;
using Tooba.Wishlist.Application.Customer.Queries;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Application.Customer.Validators;

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
