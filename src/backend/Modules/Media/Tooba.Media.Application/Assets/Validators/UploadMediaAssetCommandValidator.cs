using FluentValidation;
using Tooba.Media.Application.Assets.Commands;
using Tooba.Media.Contracts.Errors;

namespace Tooba.Media.Application.Assets.Validators;

/// <summary>Transport-shape validation for <see cref="UploadMediaAssetCommand"/>.</summary>
public sealed class UploadMediaAssetCommandValidator : AbstractValidator<UploadMediaAssetCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public UploadMediaAssetCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotNull()
            .WithErrorCode(MediaErrorCodes.ValidationFailed);
        RuleFor(x => x.OriginalFileName)
            .NotEmpty()
            .WithErrorCode(MediaErrorCodes.ValidationFailed);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithErrorCode(MediaErrorCodes.ValidationFailed);
    }
}
