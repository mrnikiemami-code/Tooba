using FluentValidation;
using Tooba.Content.Application.Commands.AddGalleryMedia;

namespace Tooba.Content.Application.Validators.AddGalleryMedia;

public sealed class AddGalleryMediaCommandValidator : AbstractValidator<AddGalleryMediaCommand>
{
    public AddGalleryMediaCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.MediaAssetIds).NotNull().NotEmpty().WithErrorCode(ContentValidationCodes.GalleryIdsRequired);
    }
}
