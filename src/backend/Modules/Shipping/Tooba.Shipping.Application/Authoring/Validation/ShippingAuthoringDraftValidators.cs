using FluentValidation;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Domain.Authoring;

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
