using FluentValidation;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Scheduling;

namespace Tooba.Shipping.Application.Authoring.Validation;

/// <summary>
/// کدهای اعتبارسنجی شکل (transport) نویسندگی پیش‌نویس. این کدها در پوشش <c>validation.failed</c>
/// برمی‌گردند و جای خطاهای معنایی دامنه (که کاتالوگ <c>shipping.*</c> دارند) را نمی‌گیرند.
/// </summary>
public static class ShippingAuthoringValidationCodes
{
    /// <summary>گروه‌های منطقه‌ای غایب‌اند.</summary>
    public const string RegionGroupsRequired = "shipping.validation.region_groups_required";

    /// <summary>تعداد گروه‌های منطقه‌ای از سقف فنی payload بیشتر است.</summary>
    public const string RegionGroupsTooMany = "shipping.validation.region_groups_too_many";

    /// <summary>یک گروه/انتخاب از نظر شکل نامعتبر است (شناسهٔ خالی یا سطح ناسازگار).</summary>
    public const string RegionSelectionInvalid = "shipping.validation.region_selection_invalid";

    /// <summary>شناسهٔ پیش‌نویس غایب/خالی است.</summary>
    public const string DraftIdRequired = "shipping.validation.draft_id_required";

    /// <summary>بازبینی مورد انتظار غایب یا نامعتبر است.</summary>
    public const string ExpectedRevisionInvalid = "shipping.validation.expected_revision_invalid";

    /// <summary>شناسهٔ گروه منطقه‌ای غایب/خالی است.</summary>
    public const string GroupIdRequired = "shipping.validation.group_id_required";

    /// <summary>مجموعهٔ ردیف‌های نرخ غایب است (برخلاف <c>[]</c> که پیکربندی ناقص معتبر است).</summary>
    public const string RateRowsRequired = "shipping.validation.rate_rows_required";

    /// <summary>تعداد ردیف‌های نرخ از سقف فنی payload بیشتر است.</summary>
    public const string RateRowsTooMany = "shipping.validation.rate_rows_too_many";

    /// <summary>یک ردیف نرخ از نظر شکل نامعتبر است (null، شناسهٔ خالی، بازهٔ ناسازگار، فیلد غایب).</summary>
    public const string RateRowShapeInvalid = "shipping.validation.rate_row_shape_invalid";

    /// <summary>شناسهٔ ردیف تکراری در همان مجموعه است.</summary>
    public const string RateRowIdDuplicate = "shipping.validation.rate_row_id_duplicate";

    /// <summary>مبلغ یک ردیف نرخ منفی، خارج از محدوده یا با دقت اعشاری غیرقابل‌نمایش است.</summary>
    public const string RateRowAmountInvalid = "shipping.validation.rate_row_amount_invalid";

    /// <summary>بازهٔ مدت تحویل یک ردیف نرخ نامعتبر است.</summary>
    public const string RateRowDurationInvalid = "shipping.validation.rate_row_duration_invalid";

    /// <summary>مجموعهٔ ترجمه‌های نام غایب است (برخلاف <c>[]</c> که «بدون تغییر» است).</summary>
    public const string MethodTranslationsRequired = "shipping.validation.method_translations_required";

    /// <summary>مجموعهٔ عنوان‌های گروه غایب است (برخلاف <c>[]</c> که «بدون تغییر» است).</summary>
    public const string GroupTitlesRequired = "shipping.validation.group_titles_required";

    /// <summary>یک ورودی ارائه از نظر شکل نامعتبر است (null، شناسهٔ خالی، زبان تکراری، متن غایب/بلند).</summary>
    public const string PresentationEntryInvalid = "shipping.validation.presentation_entry_invalid";

    /// <summary>تعداد ورودی‌های یک فهرست ارائه از سقف فنی payload بیشتر است.</summary>
    public const string PresentationEntriesTooMany = "shipping.validation.presentation_entries_too_many";

    /// <summary>مجموعهٔ روزهای هفته غایب است (برخلاف هفت روز که پیکربندی معتبر است).</summary>
    public const string ScheduleDaysRequired = "shipping.validation.schedule_days_required";

    /// <summary>یک روز هفته از نظر شکل نامعتبر است (null، پرچم کاری غایب، بازهٔ ناسازگار).</summary>
    public const string ScheduleDayEntryInvalid = "shipping.validation.schedule_day_entry_invalid";

    /// <summary>شناسهٔ منطقهٔ زمانی در بدنه غایب یا بیش‌ازحد طولانی است.</summary>
    public const string ScheduleTimeZoneIdInvalid = "shipping.validation.schedule_timezone_id_invalid";

    /// <summary>زمان پرداخت کرایه در بدنه غایب یا خارج از مقادیر تعریف‌شده است.</summary>
    public const string MethodSettingsFreightModeInvalid = "shipping.validation.method_settings_freight_mode_invalid";

    /// <summary>حالت محاسبهٔ کرایه در بدنه غایب یا خارج از مقادیر تعریف‌شده است.</summary>
    public const string MethodSettingsCalculationModeInvalid =
        "shipping.validation.method_settings_calculation_mode_invalid";

    /// <summary>شناسهٔ نوع ارسال در بدنه خالی (Guid.Empty) است؛ خالی هرگز «انتخاب‌نشده» نیست.</summary>
    public const string TypeSelectionInvalid = "shipping.validation.type_selection_invalid";

    /// <summary>
    /// نسخهٔ مورد انتظار اشاره‌گر انتخاب در بدنه غایب یا منفی است؛ غیبت هرگز «صفر» نیست.
    /// </summary>
    public const string ExpectedSelectionVersionInvalid =
        "shipping.validation.expected_selection_version_invalid";
}

/// <summary>اعتبارسنجی شکل درخواست ایجاد پیش‌نویس.</summary>
public sealed class CreateShippingAuthoringDraftCommandValidator : AbstractValidator<CreateShippingAuthoringDraftCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public CreateShippingAuthoringDraftCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ServiceCode)
            .NotEmpty()
            .WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingAuthoringDraft.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.OptionCode)
            .MaximumLength(ShippingAuthoringDraft.OptionCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.OptionCodeTooLong)
            .When(x => x.Request?.OptionCode is not null);

        // R190-01: "required" must not sit behind the collection's own null guard. FluentValidation
        // applies a trailing When(...) to EVERY preceding rule in the chain, so the old placement also
        // skipped NotNull and an omitted/null JSON collection reached the handler. The required rule is
        // now unconditional (guarded only by the parent Request), and size/shape rules are separate and
        // run only for a present collection.
        RuleFor(x => x.Request.RegionGroups)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.RegionGroupsRequired)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.RegionGroups)
            .Must(groups => groups!.Count <= ShippingAuthoringDraft.MaxRegionGroups)
            .WithErrorCode(ShippingAuthoringValidationCodes.RegionGroupsTooMany)
            .Must(groups => ShippingAuthoringShape.IsWellFormed(groups!))
            .WithErrorCode(ShippingAuthoringValidationCodes.RegionSelectionInvalid)
            .When(x => x.Request?.RegionGroups is not null);
    }
}

/// <summary>اعتبارسنجی شکل درخواست جایگزینی درخت منطقه‌ای.</summary>
public sealed class ReplaceShippingAuthoringDraftRegionsCommandValidator
    : AbstractValidator<ReplaceShippingAuthoringDraftRegionsCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public ReplaceShippingAuthoringDraftRegionsCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        // R190-01: same unconditional required rule as create; [] stays a valid incomplete draft, but a
        // missing/null collection is a shape failure before any handler, reference read or database work.
        RuleFor(x => x.Request.RegionGroups)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.RegionGroupsRequired)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.RegionGroups)
            .Must(groups => groups!.Count <= ShippingAuthoringDraft.MaxRegionGroups)
            .WithErrorCode(ShippingAuthoringValidationCodes.RegionGroupsTooMany)
            .Must(groups => ShippingAuthoringShape.IsWellFormed(groups!))
            .WithErrorCode(ShippingAuthoringValidationCodes.RegionSelectionInvalid)
            .When(x => x.Request?.RegionGroups is not null);
    }
}

/// <summary>اعتبارسنجی شکل خواندن یک پیش‌نویس: شناسهٔ مسیر باید غیرخالی باشد.</summary>
public sealed class GetShippingAuthoringDraftQueryValidator : AbstractValidator<GetShippingAuthoringDraftQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringDraftQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);
    }
}

/// <summary>اعتبارسنجی شکل خواندن ردیف‌های نرخ یک گروه: هر دو شناسهٔ مسیر باید غیرخالی باشند.</summary>
public sealed class GetShippingAuthoringGroupRateRowsQueryValidator
    : AbstractValidator<GetShippingAuthoringGroupRateRowsQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringGroupRateRowsQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.GroupIdRequired);
    }
}

/// <summary>
/// اعتبارسنجی شکل جایگزینی ردیف‌های نرخ: هویت مسیر، نشانگر همزمانی، الزام مجموعه (نه خالی‌بودن) و
/// قواعد شکل/دامنهٔ هر ردیف، همه پیش از هر خواندن خارجی یا نوشتن.
/// </summary>
/// <remarks>
/// مجموعهٔ غایب/تهی(<c>null</c>) با کد الزام رد می‌شود، اما <c>[]</c> معتبر است: پاک‌کردن ردیف‌ها یک
/// پیکربندی ناقص است، نه پوشش آزاد. قواعد بازه/مبلغ/مدت اینجا و در دامنه تکرار می‌شوند تا فراخوان
/// مستقیم (بدون HTTP) هم نتواند دادهٔ نامعتبر را وارد کند.
/// </remarks>
public sealed class ReplaceShippingAuthoringGroupRateRowsCommandValidator
    : AbstractValidator<ReplaceShippingAuthoringGroupRateRowsCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public ReplaceShippingAuthoringGroupRateRowsCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.GroupId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.GroupIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        // Same repaired R190-01 placement: the required rule is unconditional (guarded only by the
        // parent Request) and the collection rules are separate, so an omitted JSON property can never
        // read as "empty".
        RuleFor(x => x.Request.Rows)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.RateRowsRequired)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.Rows)
            .Must(rows => rows!.Count <= ShippingAuthoringRateRow.MaxRowsPerGroup)
            .WithErrorCode(ShippingAuthoringValidationCodes.RateRowsTooMany)
            .Must(rows => ShippingAuthoringRateRowShape.HasCoherentIntervals(rows!))
            .WithErrorCode(ShippingAuthoringValidationCodes.RateRowShapeInvalid)
            .Must(ShippingAuthoringRateRowShape.HasUniqueRetainedIds)
            .WithErrorCode(ShippingAuthoringValidationCodes.RateRowIdDuplicate)
            .Must(rows => ShippingAuthoringRateRowShape.HasValidAmounts(rows!))
            .WithErrorCode(ShippingAuthoringValidationCodes.RateRowAmountInvalid)
            .Must(rows => ShippingAuthoringRateRowShape.HasValidDurations(rows!))
            .WithErrorCode(ShippingAuthoringValidationCodes.RateRowDurationInvalid)
            .When(x => x.Request?.Rows is not null);
    }
}

/// <summary>
/// قواعد شکل/دامنهٔ ردیف نرخ در لایهٔ transport. هیچ قاعدهٔ تجاری اینجا نیست: فقط «آیا این payload
/// قابل تفسیر و در محدودهٔ canonical است».
/// </summary>
internal static class ShippingAuthoringRateRowShape
{
    /// <summary>بازه‌ها، شناسه‌ها و عناصر null شکل معتبر دارند.</summary>
    /// <param name="rows">ردیف‌ها.</param>
    public static bool HasCoherentIntervals(IReadOnlyList<ShippingAuthoringRateRowWrite> rows)
    {
        foreach (var row in rows)
        {
            if (row is null || row.RateRowId is { } id && id == Guid.Empty)
            {
                return false;
            }

            if (!IsCoherentInterval(row.MinWeightGrams, row.MaxWeightGrams, 0, ShippingAuthoringRateRowRules.WeightGramsMaxValue)
                || !IsCoherentInterval(
                    row.MinItemCount,
                    row.MaxItemCount,
                    1,
                    ShippingAuthoringRateRowRules.ItemCountMaxValue)
                || !IsCoherentAmountInterval(row.MinMerchandiseAmount, row.MaxMerchandiseAmount))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>شناسه‌های نگه‌داشته‌شده در یک مجموعه تکراری نیستند.</summary>
    /// <param name="rows">ردیف‌ها.</param>
    public static bool HasUniqueRetainedIds(IReadOnlyList<ShippingAuthoringRateRowWrite> rows)
        => rows
            .Where(row => row is not null && row.RateRowId.HasValue)
            .Select(row => row!.RateRowId!.Value)
            .Distinct()
            .Count() == rows.Count(row => row is not null && row.RateRowId.HasValue);

    /// <summary>هزینه‌های حمل و بسته‌بندی حاضر، غیرمنفی و در دقت canonical هستند.</summary>
    /// <param name="rows">ردیف‌ها.</param>
    public static bool HasValidAmounts(IReadOnlyList<ShippingAuthoringRateRowWrite> rows)
        => rows.All(row => row is not null
            && row.FreightAmount is { } freight
            && row.PackagingAmount is { } packaging
            && IsValidAmount(freight)
            && IsValidAmount(packaging));

    /// <summary>مدت تحویل حاضر و معتبر است؛ کمینه می‌تواند مساوی بیشینه باشد.</summary>
    /// <param name="rows">ردیف‌ها.</param>
    public static bool HasValidDurations(IReadOnlyList<ShippingAuthoringRateRowWrite> rows)
        => rows.All(row => row is not null
            && row.MinDeliveryDays is { } min
            && row.MaxDeliveryDays is { } max
            && min >= 0
            && max >= min
            && max <= ShippingAuthoringRateRowRules.DaysMaxValue);

    private static bool IsValidAmount(decimal amount)
        => amount >= 0m
           && amount <= ShippingAuthoringRateRowRules.AmountMaxValue
           && ShippingAuthoringRateRowRules.IsRepresentableAmount(amount);

    private static bool IsCoherentInterval(long? min, long? max, long minimumAllowed, long maximumAllowed)
        => min is not { } lower
            ? max is null
            : lower >= minimumAllowed
              && lower <= maximumAllowed
              && (max is not { } upper || (upper > lower && upper <= maximumAllowed));

    private static bool IsCoherentAmountInterval(decimal? min, decimal? max)
    {
        if (min is not { } lower)
        {
            return max is null;
        }

        if (!IsValidAmount(lower))
        {
            return false;
        }

        return max is not { } upper
               || (upper > lower && IsValidAmount(upper));
    }
}

/// <summary>
/// قواعد شکل خالص درخت منطقه‌ای: هیچ قاعدهٔ تجاری/جغرافیایی اینجا نیست، فقط «آیا این payload
/// قابل تفسیر است». سقف‌ها فنی‌اند تا یک payload نامحدود به یک عملیات پرهزینه تبدیل نشود.
/// </summary>
internal static class ShippingAuthoringShape
{
    /// <summary>آیا همهٔ گروه‌ها و انتخاب‌ها شکل معتبر دارند.</summary>
    /// <param name="groups">گروه‌های بدنه.</param>
    public static bool IsWellFormed(IReadOnlyList<ShippingAuthoringRegionGroupWrite> groups)
    {
        var total = 0;
        foreach (var group in groups)
        {
            if (group is null || group.GroupId == Guid.Empty)
            {
                return false;
            }

            var areas = group.Areas;
            if (areas is null || areas.Count > ShippingAuthoringDraft.MaxSelectionsPerGroup)
            {
                return false;
            }

            total += areas.Count;
            if (total > ShippingAuthoringDraft.MaxSelectionsPerDraft)
            {
                return false;
            }

            foreach (var area in areas)
            {
                if (!IsCoherent(area))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsCoherent(ShippingAuthoringSelectedAreaWrite? area)
    {
        if (area is null || area.CountryId == Guid.Empty)
        {
            return false;
        }

        return area.Kind switch
        {
            ShippingAuthoringAreaKind.Country =>
                area.AdministrativeAreaId is null && area.CityId is null && area.ServiceAreaId is null,
            ShippingAuthoringAreaKind.AdministrativeArea =>
                area.AdministrativeAreaId is { } adminArea && adminArea != Guid.Empty
                && area.CityId is null && area.ServiceAreaId is null,
            ShippingAuthoringAreaKind.City =>
                area.CityId is { } city && city != Guid.Empty && area.ServiceAreaId is null,
            ShippingAuthoringAreaKind.ServiceArea =>
                area.CityId is { } city && city != Guid.Empty
                && area.ServiceAreaId is { } serviceArea && serviceArea != Guid.Empty,
            _ => false,
        };
    }
}

/// <summary>
/// اعتبارسنجی شکل PATCH ارائه: هویت مسیر، نشانگر همزمانی، الزام هر دو فهرست (نه خالی‌بودن)، یکتایی
/// جفت‌ها و طول متن، همه پیش از هر خواندن خارجی یا نوشتن.
/// </summary>
/// <remarks>
/// <c>[]</c> معتبر است و یعنی «هیچ تغییر»؛ فقط <c>null</c>/غایب شکل نامعتبر است. زبان‌ها در این لایه
/// فقط شناسه‌اند: فعال‌بودن آن‌ها با منبع authoritative در handler/دامنه سنجیده می‌شود.
/// </remarks>
public sealed class ReplaceShippingAuthoringPresentationCommandValidator
    : AbstractValidator<ReplaceShippingAuthoringPresentationCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public ReplaceShippingAuthoringPresentationCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.MethodTranslations)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.MethodTranslationsRequired)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.MethodTranslations)
            .Must(entries => entries!.Count <= ShippingAuthoringDraft.MaxMethodTranslations)
            .WithErrorCode(ShippingAuthoringValidationCodes.PresentationEntriesTooMany)
            .Must(entries => ShippingAuthoringPresentationShape.HasValidTranslations(entries!))
            .WithErrorCode(ShippingAuthoringValidationCodes.PresentationEntryInvalid)
            .When(x => x.Request?.MethodTranslations is not null);

        RuleFor(x => x.Request.GroupTitles)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.GroupTitlesRequired)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.GroupTitles)
            .Must(entries => entries!.Count <= ShippingAuthoringDraft.MaxGroupTitleEntries)
            .WithErrorCode(ShippingAuthoringValidationCodes.PresentationEntriesTooMany)
            .Must(entries => ShippingAuthoringPresentationShape.HasValidTitles(entries!))
            .WithErrorCode(ShippingAuthoringValidationCodes.PresentationEntryInvalid)
            .When(x => x.Request?.GroupTitles is not null);
    }
}

/// <summary>
/// اعتبارسنجی شکل خواندن ارائه: شناسهٔ مسیر باید غیرخالی باشد.
/// </summary>
public sealed class GetShippingAuthoringPresentationQueryValidator
    : AbstractValidator<GetShippingAuthoringPresentationQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringPresentationQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);
    }
}

/// <summary>
/// قواعد شکل/طول متن ارائه در لایهٔ transport. هیچ قاعدهٔ زبانی/تجاری اینجا نیست: فقط «آیا این payload
/// قابل تفسیر و در محدودهٔ فنی است».
/// </summary>
internal static class ShippingAuthoringPresentationShape
{
    /// <summary>ورودی‌های نام: عنصر null، زبان خالی/تکراری، نام خالی/بلند و توضیح بلند ممنوع.</summary>
    /// <param name="entries">ورودی‌ها.</param>
    public static bool HasValidTranslations(IReadOnlyList<ShippingAuthoringMethodTranslationWrite> entries)
    {
        var seen = new HashSet<Guid>();
        foreach (var entry in entries)
        {
            if (entry is null
                || entry.LanguageId == Guid.Empty
                || !seen.Add(entry.LanguageId)
                || string.IsNullOrWhiteSpace(entry.Name)
                || entry.Name.Trim().Length > ShippingAuthoringPresentationRules.NameMaxLength
                || entry.Description?.Trim().Length > ShippingAuthoringPresentationRules.DescriptionMaxLength)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>ورودی‌های عنوان: عنصر null، شناسهٔ خالی و جفت (گروه، زبان) تکراری ممنوع.</summary>
    /// <param name="entries">ورودی‌ها.</param>
    public static bool HasValidTitles(IReadOnlyList<ShippingAuthoringGroupTitleWrite> entries)
    {
        var seen = new HashSet<(Guid GroupId, Guid LanguageId)>();
        foreach (var entry in entries)
        {
            if (entry is null
                || entry.GroupId == Guid.Empty
                || entry.LanguageId == Guid.Empty
                || !seen.Add((entry.GroupId, entry.LanguageId))
                || entry.Title?.Trim().Length > ShippingAuthoringPresentationRules.TitleMaxLength)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// اعتبارسنجی شکل خواندن برنامهٔ عملیاتی هفتگی: شناسهٔ مسیر باید غیرخالی باشد.
/// </summary>
public sealed class GetShippingAuthoringWeeklyScheduleQueryValidator
    : AbstractValidator<GetShippingAuthoringWeeklyScheduleQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringWeeklyScheduleQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);
    }
}

/// <summary>
/// اعتبارسنجی شکل جایگزینی برنامهٔ هفتگی: هویت مسیر، نشانگر همزمانی، الزام مجموعهٔ روزها و
/// منطقهٔ زمانی، و شکل هر روز، همه پیش از هر خواندن خارجی یا نوشتن.
/// </summary>
/// <remarks>
/// شکل فقط سنجیده می‌شود: «دقیقاً هفت روز یکتا» و «زمان قابل‌حل» در دامنه پیش‌سنجی می‌شوند تا یک
/// فراخوان مستقیم (بدون HTTP) هم نتواند هفتهٔ ناقص یا منطقهٔ زمانی ساختگی بنویسد. پرچم کاری nullable
/// است و نبودش هرگز به «تعطیل» تبدیل نمی‌شود.
/// </remarks>
public sealed class ReplaceShippingAuthoringWeeklyScheduleCommandValidator
    : AbstractValidator<ReplaceShippingAuthoringWeeklyScheduleCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public ReplaceShippingAuthoringWeeklyScheduleCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.TimeZoneId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.ScheduleTimeZoneIdInvalid)
            .MaximumLength(ShippingRatePolicyLimits.TimeZoneIdMaxLength)
            .WithErrorCode(ShippingAuthoringValidationCodes.ScheduleTimeZoneIdInvalid)
            .When(x => x.Request is not null);

        // Same repaired R190-01 placement: the required rule is unconditional (guarded only by the
        // parent Request) and the shape rules run only for a present collection, so an omitted JSON
        // property can never read as "all closed" or "no change".
        RuleFor(x => x.Request.Days)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.ScheduleDaysRequired)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.Days)
            .Must(days => days!.Count == ShippingAuthoringWeeklySchedule.DaysPerWeek)
            .WithErrorCode(ShippingAuthoringValidationCodes.ScheduleDayEntryInvalid)
            .Must(ShippingAuthoringScheduleShape.HasValidDays)
            .WithErrorCode(ShippingAuthoringValidationCodes.ScheduleDayEntryInvalid)
            .When(x => x.Request?.Days is not null);
    }
}

/// <summary>
/// قواعد شکل روزهای هفته در لایهٔ transport. هیچ قاعدهٔ تجاری اینجا نیست: فقط «آیا این payload
/// قابل تفسیر است»؛ یکتایی هفت‌روزه و حل منطقهٔ زمانی در دامنه سنجیده می‌شود.
/// </summary>
internal static class ShippingAuthoringScheduleShape
{
    /// <summary>روزها: عنصر null، پرچم غایب، روز خارج از ۱..۷ یا جفت ساعت ناسازگار ممنوع.</summary>
    /// <param name="days">روزهای بدنه.</param>
    public static bool HasValidDays(IReadOnlyList<ShippingAuthoringScheduleDayWrite> days)
    {
        foreach (var day in days)
        {
            if (day is null
                || day.IsWorking is null
                || day.Weekday is < ShippingOperatingWindow.MinWeekday or > ShippingOperatingWindow.MaxWeekday)
            {
                return false;
            }

            var hasStart = day.StartTime is not null;
            var hasEnd = day.EndTime is not null;
            if (hasStart != hasEnd)
            {
                return false;
            }

            if (day.IsWorking == true && !hasStart)
            {
                // A working day without an interval would silently invent a schedule.
                return false;
            }

            if (day.StartTime is { } start
                && day.EndTime is { } end
                && end <= start)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// اعتبارسنجی شکل خواندن تنظیمات روش: شناسهٔ مسیر باید غیرخالی باشد.
/// </summary>
public sealed class GetShippingAuthoringMethodSettingsQueryValidator
    : AbstractValidator<GetShippingAuthoringMethodSettingsQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringMethodSettingsQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);
    }
}

/// <summary>
/// اعتبارسنجی شکل جایگزینی تنظیمات روش: هویت مسیر، نشانگر همزمانی و حضور صریح هر دو انتخاب.
/// </summary>
/// <remarks>
/// هر دو مقدار nullable هستند تا نبودِ فیلد در JSON به مقدار صفر enum تبدیل نشود؛ الزام حضور و
/// تعریف‌شده‌بودن مقدار در همین لایه و دوباره در دامنه سنجیده می‌شود، پس یک فراخوان مستقیم (بدون
/// HTTP) هم نمی‌تواند تنظیمات ناقص بنویسد.
/// </remarks>
public sealed class ReplaceShippingAuthoringMethodSettingsCommandValidator
    : AbstractValidator<ReplaceShippingAuthoringMethodSettingsCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public ReplaceShippingAuthoringMethodSettingsCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.FreightPaymentMode)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.MethodSettingsFreightModeInvalid)
            .Must(mode => mode is null || Enum.IsDefined(mode.Value))
            .WithErrorCode(ShippingAuthoringValidationCodes.MethodSettingsFreightModeInvalid)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.CalculationMode)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.MethodSettingsCalculationModeInvalid)
            .Must(mode => mode is null || Enum.IsDefined(mode.Value))
            .WithErrorCode(ShippingAuthoringValidationCodes.MethodSettingsCalculationModeInvalid)
            .When(x => x.Request is not null);
    }
}

/// <summary>
/// اعتبارسنجی شکل خواندن انتخاب فعلی نوع ارسال: شناسهٔ مسیر باید غیرخالی باشد.
/// </summary>
public sealed class GetShippingAuthoringDraftTypeAssignmentQueryValidator
    : AbstractValidator<GetShippingAuthoringDraftTypeAssignmentQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringDraftTypeAssignmentQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);
    }
}

/// <summary>
/// اعتبارسنجی شکل تعیین/پاک‌کردن نوع ارسال: هویت مسیر، نشانگر همزمانی و شناسهٔ غیرخالی.
/// </summary>
/// <remarks>
/// <b>غیبت فیلد در JSON همین‌جا دیده نمی‌شود</b> چون <c>JsonRequired</c> پیش از این لایه درخواست
/// ناقص را رد می‌کند؛ این لایه فقط <c>Guid.Empty</c> و بازبینی غیرمثبت را می‌سنجد. مقدار
/// <c>null</c> صریح مجاز است و «پاک‌کردن» است، نه نبود انتخاب.
/// </remarks>
public sealed class ReplaceShippingAuthoringDraftTypeAssignmentCommandValidator
    : AbstractValidator<ReplaceShippingAuthoringDraftTypeAssignmentCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public ReplaceShippingAuthoringDraftTypeAssignmentCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.ShippingTypeId)
            .Must(id => id is null || id.Value != Guid.Empty)
            .WithErrorCode(ShippingAuthoringValidationCodes.TypeSelectionInvalid)
            .When(x => x.Request is not null);
    }
}

/// <summary>
/// اعتبارسنجی خواندن سند ویرایشگر: شناسهٔ مسیر باید غیرخالی باشد. سند فقط داخل محدودهٔ کامل مالک
/// خوانده می‌شود، پس شناسهٔ بیگانه همان «یافت نشد» ایمن است.
/// </summary>
public sealed class GetShippingAuthoringEditorDocumentQueryValidator
    : AbstractValidator<GetShippingAuthoringEditorDocumentQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringEditorDocumentQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);
    }
}

/// <summary>
/// اعتبارسنجی شکل سند مرکب ویرایشگر: هویت مسیر، نشانگر همزمانی مثبت، و حضور صریح دو مجموعهٔ اجباری
/// (گروه‌ها و متن روش). غیبت <c>weeklySchedule</c>/<c>methodSettings</c>/<c>typeAssignment</c> عمداً
/// مجاز است و معنای آن «حفظ مقدار ذخیره‌شده» است، نه پاک‌کردن.
/// </summary>
public sealed class ReplaceShippingAuthoringEditorDocumentCommandValidator
    : AbstractValidator<ReplaceShippingAuthoringEditorDocumentCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public ReplaceShippingAuthoringEditorDocumentCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.RegionGroups)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.RegionGroupsRequired)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.MethodTranslations)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid)
            .When(x => x.Request is not null);
    }
}
