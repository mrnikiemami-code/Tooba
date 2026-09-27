using FluentValidation;
using Tooba.Content.Application.Commands.ReorderCategories;

namespace Tooba.Content.Application.Validators.ReorderCategories;

public sealed class ReorderCategoriesCommandValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesCommandValidator()
    {
        RuleFor(x => x.Items).NotNull().NotEmpty().WithErrorCode(ContentValidationCodes.ReorderItemsRequired);
    }
}
