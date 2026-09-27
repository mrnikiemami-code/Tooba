using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Media.Commands;

namespace Tooba.Content.Application.Media.Validators;

public sealed class ReorderGalleryCommandValidator : AbstractValidator<ReorderGalleryCommand>
{
    public ReorderGalleryCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.OrderedMediaAssetIds).NotNull().WithErrorCode(ContentValidationCodes.GalleryIdsRequired);
    }
}
