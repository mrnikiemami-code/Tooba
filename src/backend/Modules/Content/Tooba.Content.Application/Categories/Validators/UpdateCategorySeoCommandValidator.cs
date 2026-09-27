using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Categories.Commands;

namespace Tooba.Content.Application.Categories.Validators;

public sealed class UpdateCategorySeoCommandValidator : AbstractValidator<UpdateCategorySeoCommand>
{
    public UpdateCategorySeoCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithErrorCode(ContentValidationCodes.CategoryIdRequired);
    }
}
