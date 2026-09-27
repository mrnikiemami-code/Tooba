using FluentValidation;
using Tooba.Content.Application.Validators;
using Tooba.Content.Application.Media.Commands;

namespace Tooba.Content.Application.Media.Validators;

public sealed class PatchGalleryMediaCommandValidator : AbstractValidator<PatchGalleryMediaCommand>
{
    public PatchGalleryMediaCommandValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty().WithErrorCode(ContentValidationCodes.ArticleIdRequired);
        RuleFor(x => x.MediaAssetId).NotEmpty().WithErrorCode(ContentValidationCodes.MediaAssetIdRequired);
    }
}
