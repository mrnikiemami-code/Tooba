using FluentValidation;
using Tooba.Wishlist.Application.Customer.Commands;
using Tooba.Wishlist.Application.Customer.Queries;

namespace Tooba.Wishlist.Application.Customer.Validators;

/// <summary>کدهای پایدار خطای شکل انتقال Wishlist.</summary>
public static class WishlistValidationCodes
{
    /// <summary>شناسهٔ محصول مسیر الزامی/معتبر است.</summary>
    public const string ProductIdRequired = "customer.wishlist.product_id_required";

    /// <summary>بدنهٔ عضویت باید مجموعهٔ شناسه داشته باشد.</summary>
    public const string ProductIdsRequired = "customer.wishlist.product_ids_required";
}

/// <summary>اعتبارسنجی شکل انتقال <see cref="AddWishlistItemCommand"/>.</summary>
public sealed class AddWishlistItemCommandValidator : AbstractValidator<AddWishlistItemCommand>
{
    /// <summary>قواعد شکل اولیه را ثبت می‌کند؛ Actor از مرز اعتماد است.</summary>
    public AddWishlistItemCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithErrorCode(WishlistValidationCodes.ProductIdRequired);
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
            .WithErrorCode(WishlistValidationCodes.ProductIdRequired);
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
            .WithErrorCode(WishlistValidationCodes.ProductIdsRequired);
    }
}
