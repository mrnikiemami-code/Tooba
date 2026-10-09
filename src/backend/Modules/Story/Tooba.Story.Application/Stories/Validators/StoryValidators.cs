using FluentValidation;
using Tooba.Story.Application.Stories.Commands.Admin;
using Tooba.Story.Application.Stories.Commands.Seller;
using Tooba.Story.Application.Stories.Queries.Admin;
using Tooba.Story.Application.Stories.Queries.Storefront;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Rules;

namespace Tooba.Story.Application.Stories.Validators;

/// <summary>
/// کدهای ماشینی پایدار اعتبارسنجی شکل transport برای Story.
/// این کدها داخل پوشش استاندارد <c>validation.failed</c> به کلاینت می‌رسند و متن قابل‌نمایش آن‌ها
/// از مجموعهٔ منابع <c>StoryErrors.resx</c> / <c>StoryErrors.fa.resx</c> تأمین می‌شود؛ بنابراین هرگز
/// نباید متن فارسی/انگلیسی به‌جای کد در Validator قرار گیرد.
/// </summary>
public static class StoryValidationCodes
{
    /// <summary>عنوان استوری الزامی است.</summary>
    public const string TitleRequired = "story.title.required";

    /// <summary>فهرست شناسهٔ استوری‌ها برای مرتب‌سازی الزامی است.</summary>
    public const string StoryIdsRequired = "story.ids.required";

    /// <summary>فهرست شناسهٔ آیتم‌ها برای مرتب‌سازی الزامی است.</summary>
    public const string ItemIdsRequired = "story.itemIds.required";

    /// <summary>نوع رسانهٔ آیتم الزامی است.</summary>
    public const string MediaTypeRequired = "story.mediaType.required";

    /// <summary>بدنهٔ درخواست گرید الزامی است.</summary>
    public const string GridRequestRequired = "story.grid.request.required";

    /// <summary>مقدار وضعیت بازبینی نامعتبر است.</summary>
    public const string ReviewStatusInvalid = "story.reviewStatus.invalid";

    /// <summary>دلیل رد استوری الزامی است.</summary>
    public const string RejectionReasonRequired = "story.rejectionReason.required";

    /// <summary>بازهٔ زمان‌بندی نامعتبر است (پایان پیش از شروع).</summary>
    public const string ScheduleRangeInvalid = "story.schedule.range.invalid";

    /// <summary>مقدار locale ورودی از نظر شکل نامعتبر است.</summary>
    public const string LocaleInvalid = "story.locale.invalid";

    /// <summary>مقدار market ورودی از نظر شکل نامعتبر است.</summary>
    public const string MarketInvalid = "story.market.invalid";
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

/// <summary>
/// اعتبارسنجی شکل transport برای فهرست عمومی فروشگاه.
/// مقادیر <c>Locale</c> و <c>Market</c> از سمت فراخوان کنترل می‌شوند و به فیلتر دیتابیس می‌رسند؛
/// بنابراین شکل آن‌ها باید در مرز transport اعتبارسنجی شود. مقدار خالی مجاز است و به معنای «بدون فیلتر» است.
/// این Validator تنها شکل ورودی را بررسی می‌کند و مالک قواعد دامنه/دسترسی نیست.
/// </summary>
public sealed class GetPublicStoriesQueryValidator : AbstractValidator<GetPublicStoriesQuery>
{
    public GetPublicStoriesQueryValidator()
    {
        RuleFor(x => x.Locale)
            .Must(BeValidLocaleOrEmpty)
            .WithErrorCode(StoryValidationCodes.LocaleInvalid);

        RuleFor(x => x.Market)
            .Must(BeValidMarketOrEmpty)
            .WithErrorCode(StoryValidationCodes.MarketInvalid);
    }

    private static bool BeValidLocaleOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (raw.Length <= StoryRules.LocaleMaxLength && raw.All(char.IsLetterOrDigit));

    private static bool BeValidMarketOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (raw.Length <= StoryRules.MarketMaxLength && raw.All(char.IsLetterOrDigit));
}
