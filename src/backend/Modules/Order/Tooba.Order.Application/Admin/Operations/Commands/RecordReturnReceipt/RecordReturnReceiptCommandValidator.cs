using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RecordReturnReceipt;

/// <summary>Syntactic envelope for the TB-258 origin receipt command.</summary>
public sealed class RecordReturnReceiptCommandValidator : AbstractValidator<RecordReturnReceiptCommand>
{
    public RecordReturnReceiptCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}
