using FluentValidation;
using Tooba.Media.Application.Assets.Queries;

namespace Tooba.Media.Application.Assets.Validators;

/// <summary>Transport-shape validation for <see cref="GetMediaAssetQuery"/>.</summary>
public sealed class GetMediaAssetQueryValidator : AbstractValidator<GetMediaAssetQuery>
{
    /// <summary>Registers id presence.</summary>
    public GetMediaAssetQueryValidator()
    {
        RuleFor(x => x.MediaAssetId)
            .NotEmpty()
            .WithErrorCode(MediaValidationCodes.MediaAssetIdRequired);
    }
}
