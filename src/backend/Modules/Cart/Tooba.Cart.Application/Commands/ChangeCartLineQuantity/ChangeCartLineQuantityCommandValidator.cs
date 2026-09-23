using FluentValidation;
using Tooba.Cart.Application.Validation;

namespace Tooba.Cart.Application.Commands.ChangeCartLineQuantity;

/// <summary>
/// Transport validation for <see cref="ChangeCartLineQuantityCommand"/>.
/// Zero quantity keeps its existing remove-line semantics and is not rejected here.
/// </summary>
public sealed class ChangeCartLineQuantityCommandValidator : AbstractValidator<ChangeCartLineQuantityCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public ChangeCartLineQuantityCommandValidator()
    {
        CartFluentRules.RequireCartId(this, x => x.CartId);
        CartFluentRules.RequireLineId(this, x => x.LineId);
        CartFluentRules.RequireExpectedVersionMin(this, x => x.ExpectedVersion);
    }
}
