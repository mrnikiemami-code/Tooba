using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Categories.Commands;

namespace Tooba.Content.Application.Categories.Validators;

public sealed class ReorderCategoriesCommandValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesCommandValidator()
    {
        RuleFor(x => x.Items).NotNull().NotEmpty().WithErrorCode(ContentValidationCodes.ReorderItemsRequired);
    }
}
