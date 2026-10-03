using FluentValidation;
using Tooba.OperatorProfile.Application.Admin.Queries;
using Tooba.OperatorProfile.Contracts.Errors;

namespace Tooba.OperatorProfile.Application.Admin.Validators;

/// <summary>Transport-shape validation for <see cref="GetOperatorProfileQuery"/>.</summary>
public sealed class GetOperatorProfileQueryValidator : AbstractValidator<GetOperatorProfileQuery>
{
    /// <summary>Registers actor id presence rule.</summary>
    public GetOperatorProfileQueryValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode(OperatorProfileErrorCodes.ActorRequired);
    }
}
