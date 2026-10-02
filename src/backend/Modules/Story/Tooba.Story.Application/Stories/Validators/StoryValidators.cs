using FluentValidation;
using Tooba.Story.Application.Stories.Commands.Admin;
using Tooba.Story.Application.Stories.Commands.Seller;
using Tooba.Story.Application.Stories.Queries.Admin;
using Tooba.Story.Domain.Enums;

namespace Tooba.Story.Application.Stories.Validators;

/// <summary>Stable Story transport validation machine codes.</summary>
public static class StoryValidationCodes
{
    public const string TitleRequired = "story.title.required";
    public const string StoryIdsRequired = "story.ids.required";
    public const string ItemIdsRequired = "story.itemIds.required";
    public const string MediaTypeRequired = "story.mediaType.required";
    public const string GridRequestRequired = "story.grid.request.required";
    public const string ReviewStatusInvalid = "story.reviewStatus.invalid";
    public const string RejectionReasonRequired = "story.rejectionReason.required";
    public const string ScheduleRangeInvalid = "story.schedule.range.invalid";
}

/// <summary>اعتبارسنجی ایجاد ادمین.</summary>
public sealed class CreateAdminStoryCommandValidator : AbstractValidator<CreateAdminStoryCommand>
{
    public CreateAdminStoryCommandValidator()
    {
        RuleFor(x => x.Input.Title).NotEmpty().WithErrorCode(StoryValidationCodes.TitleRequired);
    }
}

/// <summary>اعتبارسنجی به‌روزرسانی ادمین.</summary>
public sealed class UpdateAdminStoryCommandValidator : AbstractValidator<UpdateAdminStoryCommand>
{
    public UpdateAdminStoryCommandValidator()
    {
        RuleFor(x => x.Input.Title).NotEmpty().WithErrorCode(StoryValidationCodes.TitleRequired);
    }
}

/// <summary>اعتبارسنجی مرتب‌سازی استوری‌های ادمین.</summary>
public sealed class ReorderAdminStoriesCommandValidator : AbstractValidator<ReorderAdminStoriesCommand>
{
    public ReorderAdminStoriesCommandValidator()
    {
        RuleFor(x => x.StoryIds).NotNull().NotEmpty().WithErrorCode(StoryValidationCodes.StoryIdsRequired);
    }
}

/// <summary>اعتبارسنجی زمان‌بندی ادمین.</summary>
public sealed class ScheduleAdminStoryCommandValidator : AbstractValidator<ScheduleAdminStoryCommand>
{
    public ScheduleAdminStoryCommandValidator()
    {
        RuleFor(x => x.Input)
            .Must(s => s.StartAt is null || s.EndAt is null || s.EndAt >= s.StartAt)
            .WithErrorCode(StoryValidationCodes.ScheduleRangeInvalid);
    }
}

/// <summary>اعتبارسنجی رد استوری.</summary>
public sealed class RejectAdminStoryCommandValidator : AbstractValidator<RejectAdminStoryCommand>
{
    public RejectAdminStoryCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithErrorCode(StoryValidationCodes.RejectionReasonRequired);
    }
}

/// <summary>اعتبارسنجی افزودن آیتم ادمین.</summary>
public sealed class AddAdminStoryItemCommandValidator : AbstractValidator<AddAdminStoryItemCommand>
{
    public AddAdminStoryItemCommandValidator()
    {
        RuleFor(x => x.Input.MediaType).NotEmpty().WithErrorCode(StoryValidationCodes.MediaTypeRequired);
    }
}

/// <summary>اعتبارسنجی به‌روزرسانی آیتم ادمین.</summary>
public sealed class UpdateAdminStoryItemCommandValidator : AbstractValidator<UpdateAdminStoryItemCommand>
{
    public UpdateAdminStoryItemCommandValidator()
    {
        RuleFor(x => x.Input.MediaType).NotEmpty().WithErrorCode(StoryValidationCodes.MediaTypeRequired);
    }
}

/// <summary>اعتبارسنجی مرتب‌سازی آیتم‌های ادمین.</summary>
public sealed class ReorderAdminStoryItemsCommandValidator : AbstractValidator<ReorderAdminStoryItemsCommand>
{
    public ReorderAdminStoryItemsCommandValidator()
    {
        RuleFor(x => x.ItemIds).NotNull().NotEmpty().WithErrorCode(StoryValidationCodes.ItemIdsRequired);
    }
}

/// <summary>اعتبارسنجی فهرست ادمین (اختیاری reviewStatus).</summary>
public sealed class ListAdminStoriesQueryValidator : AbstractValidator<ListAdminStoriesQuery>
{
    public ListAdminStoriesQueryValidator()
    {
        RuleFor(x => x.ReviewStatus)
            .Must(BeValidReviewStatusOrEmpty)
            .WithErrorCode(StoryValidationCodes.ReviewStatusInvalid);
    }

    private static bool BeValidReviewStatusOrEmpty(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        return Enum.TryParse<StoryReviewStatus>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
    }
}

/// <summary>اعتبارسنجی گرید ادمین.</summary>
public sealed class QueryAdminStoryGridQueryValidator : AbstractValidator<QueryAdminStoryGridQuery>
{
    public QueryAdminStoryGridQueryValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(StoryValidationCodes.GridRequestRequired);
        RuleFor(x => x.ReviewStatus)
            .Must(BeValidReviewStatusOrEmpty)
            .WithErrorCode(StoryValidationCodes.ReviewStatusInvalid);
    }

    private static bool BeValidReviewStatusOrEmpty(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        return Enum.TryParse<StoryReviewStatus>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
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

/// <summary>اعتبارسنجی به‌روزرسانی فروشنده.</summary>
public sealed class UpdateSellerStoryCommandValidator : AbstractValidator<UpdateSellerStoryCommand>
{
    public UpdateSellerStoryCommandValidator()
    {
        RuleFor(x => x.Input.Title).NotEmpty().WithErrorCode(StoryValidationCodes.TitleRequired);
    }
}

/// <summary>اعتبارسنجی افزودن آیتم فروشنده.</summary>
public sealed class AddSellerStoryItemCommandValidator : AbstractValidator<AddSellerStoryItemCommand>
{
    public AddSellerStoryItemCommandValidator()
    {
        RuleFor(x => x.Input.MediaType).NotEmpty().WithErrorCode(StoryValidationCodes.MediaTypeRequired);
    }
}

/// <summary>اعتبارسنجی به‌روزرسانی آیتم فروشنده.</summary>
public sealed class UpdateSellerStoryItemCommandValidator : AbstractValidator<UpdateSellerStoryItemCommand>
{
    public UpdateSellerStoryItemCommandValidator()
    {
        RuleFor(x => x.Input.MediaType).NotEmpty().WithErrorCode(StoryValidationCodes.MediaTypeRequired);
    }
}

/// <summary>اعتبارسنجی مرتب‌سازی آیتم‌های فروشنده.</summary>
public sealed class ReorderSellerStoryItemsCommandValidator : AbstractValidator<ReorderSellerStoryItemsCommand>
{
    public ReorderSellerStoryItemsCommandValidator()
    {
        RuleFor(x => x.ItemIds).NotNull().NotEmpty().WithErrorCode(StoryValidationCodes.ItemIdsRequired);
    }
}
