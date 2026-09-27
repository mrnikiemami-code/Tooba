using FluentValidation;
using Tooba.Content.Application.Commands.PatchGalleryMedia;

namespace Tooba.Content.Application.Validators.PatchGalleryMedia;

public sealed class PatchGalleryMediaCommandValidator : AbstractValidator<PatchGalleryMediaCommand>
{
    public PatchGalleryMediaCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.MediaAssetId).NotEmpty().WithErrorCode(ContentValidationCodes.MediaAssetIdRequired);
    }
}
