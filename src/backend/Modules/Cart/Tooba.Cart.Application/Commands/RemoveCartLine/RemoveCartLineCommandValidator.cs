using FluentValidation;
using Tooba.Cart.Application.Validation;

namespace Tooba.Cart.Application.Commands.RemoveCartLine;

/// <summary>Transport validation for <see cref="RemoveCartLineCommand"/>.</summary>
public sealed class RemoveCartLineCommandValidator : AbstractValidator<RemoveCartLineCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public RemoveCartLineCommandValidator()
    {
        CartFluentRules.RequireCartId(this, x => x.CartId);
        CartFluentRules.RequireLineId(this, x => x.LineId);
        CartFluentRules.RequireExpectedVersionMin(this, x => x.ExpectedVersion);
    }
}
