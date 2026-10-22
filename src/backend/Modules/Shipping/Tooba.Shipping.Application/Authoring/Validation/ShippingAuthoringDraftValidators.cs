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
