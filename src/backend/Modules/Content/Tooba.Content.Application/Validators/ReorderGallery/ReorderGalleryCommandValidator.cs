using FluentValidation;
using Tooba.Content.Application.Commands.ReorderGallery;

namespace Tooba.Content.Application.Validators.ReorderGallery;

public sealed class ReorderGalleryCommandValidator : AbstractValidator<ReorderGalleryCommand>
{
    public ReorderGalleryCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.OrderedMediaAssetIds).NotNull().WithErrorCode(ContentValidationCodes.GalleryIdsRequired);
    }
}
