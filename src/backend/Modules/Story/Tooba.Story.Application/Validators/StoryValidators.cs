using FluentValidation;
using Tooba.Story.Application.Commands.Admin;
using Tooba.Story.Application.Commands.Seller;

namespace Tooba.Story.Application.Validators;

/// <summary>کدهای شکل انتقال Story.</summary>
public static class StoryValidationCodes
{
    public const string TitleRequired = "story.title.required";
}

/// <summary>اعتبارسنجی ایجاد ادمین.</summary>
public sealed class CreateAdminStoryCommandValidator : AbstractValidator<CreateAdminStoryCommand>
{
    public CreateAdminStoryCommandValidator()
    {
        RuleFor(x => x.Input.Title).NotEmpty().WithErrorCode(StoryValidationCodes.TitleRequired);
    }
}

/// <summary>اعتبارسنجی ایجاد فروشنده.</summary>
public sealed class CreateSellerStoryDraftCommandValidator : AbstractValidator<CreateSellerStoryDraftCommand>
{
    public CreateSellerStoryDraftCommandValidator()
    {
        RuleFor(x => x.Input.Title).NotEmpty().WithErrorCode(StoryValidationCodes.TitleRequired);
    }
}
