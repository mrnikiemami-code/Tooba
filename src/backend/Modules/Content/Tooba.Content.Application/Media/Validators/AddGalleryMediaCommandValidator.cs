using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Media.Commands;

namespace Tooba.Content.Application.Media.Validators;

public sealed class AddGalleryMediaCommandValidator : AbstractValidator<AddGalleryMediaCommand>
{
    public AddGalleryMediaCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.MediaAssetIds).NotNull().NotEmpty().WithErrorCode(ContentValidationCodes.GalleryIdsRequired);
    }
}
