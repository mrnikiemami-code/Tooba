using FluentValidation;
using Tooba.Content.Application.Commands.UpdateCategorySeo;

namespace Tooba.Content.Application.Validators.UpdateCategorySeo;

public sealed class UpdateCategorySeoCommandValidator : AbstractValidator<UpdateCategorySeoCommand>
{
    public UpdateCategorySeoCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithErrorCode(ContentValidationCodes.CategoryIdRequired);
    }
}
