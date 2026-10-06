using FluentValidation;
using Tooba.Media.Application.Assets.Commands;

namespace Tooba.Media.Application.Assets.Validators;

/// <summary>
/// Transport-shape validation for <see cref="UploadMediaAssetCommand"/>.
/// The content stream is only null-checked here and is never read or sought; MIME allow-list,
/// size ceiling and persistence policy stay in Infrastructure/Application.
/// </summary>
public sealed class UploadMediaAssetCommandValidator : AbstractValidator<UploadMediaAssetCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public UploadMediaAssetCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotNull()
            .WithErrorCode(MediaValidationCodes.UploadContentRequired);
        RuleFor(x => x.OriginalFileName)
            .NotEmpty()
            .WithErrorCode(MediaValidationCodes.UploadOriginalFileNameRequired);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithErrorCode(MediaValidationCodes.UploadContentTypeRequired);
    }
}
