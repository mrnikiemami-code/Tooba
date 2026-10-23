using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن <b>یک</b> سند کامل ویرایشگر برای یک پیش‌نویس موجود: درخت منطقه‌ای با ردیف/عنوان، متن روش،
/// برنامهٔ هفتگی، تنظیمات، انتخاب نوع و registry زبان فعال — همه از یک snapshot ریشه.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
public sealed record GetShippingAuthoringEditorDocumentQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId)
    : IRequest<Result<ShippingAuthoringEditorDocumentItem>>;

/// <summary>
/// هندلر خواندن سند ویرایشگر. هیچ انتخاب زبان/ارز از locale یا query string نمی‌آید و هیچ شناسهٔ
/// جغرافیایی برچسب جعلی نمی‌گیرد؛ فکت نوع یک <b>مشاهدهٔ</b> کاتالوگ است، نه snapshot توزیع‌شده.
/// </summary>
public sealed class GetShippingAuthoringEditorDocumentHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingAuthoringContextSource contextSource,
    IShippingTypeCatalogReader catalog,
    IShippingAuthoringDestinationFactsReader destinationFacts)
    : IRequestHandler<GetShippingAuthoringEditorDocumentQuery, Result<ShippingAuthoringEditorDocumentItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringEditorDocumentItem>> Handle(
        GetShippingAuthoringEditorDocumentQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (draft.IsFailure)
        {
            return Result.Failure<ShippingAuthoringEditorDocumentItem>(draft.Errors);
        }

        var snapshot = await contextSource.ReadAsync(cancellationToken);
        var context = ShippingAuthoringContextProjection.Project(snapshot);
        if (context.IsFailure)
        {
            // Invalid/unavailable language/default/currency configuration fails closed; authored text
            // and geography are never presented against a fabricated registry.
            return Result.Failure<ShippingAuthoringEditorDocumentItem>(context.Errors);
        }

        var type = await ShippingTypeAssignmentComposition.ComposeAsync(draft.Value, catalog, cancellationToken);
        if (type.IsFailure)
        {
            return Result.Failure<ShippingAuthoringEditorDocumentItem>(type.Errors);
        }

        // Authoritative parent facts for the STORED typed selections of this owner's draft, read only
        // after the owner-scoped draft read above. A localized label uses the real default content
        // language of the authoritative context, never a locale guess.
        var facts = await destinationFacts.ReadAsync(
            ShippingAuthoringEditorDocumentComposition.SelectedKeys(draft.Value),
            context.Value.DefaultContentLanguageId,
            cancellationToken);
        if (facts.IsFailure)
        {
            return Result.Failure<ShippingAuthoringEditorDocumentItem>(facts.Errors);
        }

        return Result.Success(
            ShippingAuthoringEditorDocumentComposition.Map(
                draft.Value,
                request.Owner,
                context.Value,
                type.Value,
                facts.Value));
    }
}

/// <summary>
/// نگاشت یک snapshot ریشه به سند ویرایشگر. هیچ قاعدهٔ تجاری و هیچ فراخوانی بیرونی اینجا نیست.
/// </summary>
internal static class ShippingAuthoringEditorDocumentComposition
{
    /// <summary>سند کامل را از ریشهٔ ذخیره‌شده و واقعیت‌های پیش‌خوانده‌شده می‌سازد.</summary>
    /// <param name="draft">ریشهٔ ذخیره‌شده.</param>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="context">زمینهٔ typed زبان/ارز.</param>
    /// <param name="type">انتخاب نوع با حل واقعی.</param>
    /// <param name="destinationFacts">فکت‌های پیش‌خوانده‌شدهٔ مقصد یا تهی برای فهرست خالی.</param>
    public static ShippingAuthoringEditorDocumentItem Map(
        ShippingAuthoringDraft draft,
        ShippingAuthoringOwnerScope owner,
        ShippingAuthoringContextView context,
        ShippingAuthoringDraftTypeAssignmentItem type,
        IReadOnlyList<ShippingAuthoringDestinationFactItem>? destinationFacts = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(type);

        return new ShippingAuthoringEditorDocumentItem(
            draft.DraftId,
            new ShippingAuthoringOwnerView(
                owner.Kind.ToString(),
                owner.Kind == ShippingAuthoringOwnerKind.Seller ? owner.SellerPartyId : null),
            draft.ServiceCode,
            draft.OptionCode,
            draft.Revision,
            draft.CreatedAt,
            draft.UpdatedAt,
            type,
            draft.MethodSettings is { } settings
                ? new ShippingAuthoringEditorSettingsItem(
                    settings.FreightPaymentMode,
                    settings.CalculationMode)
                : null,
            ShippingAuthoringPresentationWorkflow.Map(draft, context),
            draft.WeeklySchedule is { } schedule
                ? new ShippingAuthoringEditorScheduleItem(
                    schedule.TimeZoneId,
                    schedule.Days
                        .OrderBy(day => day.Weekday)
                        .Select(day => new ShippingAuthoringEditorScheduleDayItem(
                            day.Weekday,
                            day.IsWorking,
                            day.StartTime,
                            day.EndTime))
                        .ToArray())
                : null,
            draft.RegionGroups
                .OrderBy(group => group.Order)
                .Select(group => new ShippingAuthoringEditorGroupItem(
                    group.GroupId,
                    group.Order,
                    group.CurrencyCode,
                    group.Areas
                        .Select(area => new ShippingAuthoringSelectedAreaItem(
                            ToContractKind(area.NodeKind),
                            area.NodeId))
                        .OrderBy(area => area.Kind)
                        .ThenBy(area => area.Id)
                        .ToArray(),
                    group.RateRows
                        .Select(row => new ShippingAuthoringRateRowItem(
                            row.RateRowId,
                            row.MinWeightGrams,
                            row.MaxWeightGrams,
                            row.MinItemCount,
                            row.MaxItemCount,
                            row.MinMerchandiseAmount,
                            row.MaxMerchandiseAmount,
                            row.FreightAmount,
                            row.PackagingAmount,
                            row.MinDeliveryDays,
                            row.MaxDeliveryDays))
                        .ToArray(),
                    group.Titles
                        .OrderBy(title => title.LanguageId)
                        .Select(title => new ShippingAuthoringEditorGroupTitleItem(title.LanguageId, title.Title))
                        .ToArray()))
                .ToArray())
        {
            DestinationFacts = destinationFacts ?? [],
        };
    }

    /// <summary>
    /// کلیدهای typed و متمایز <b>ذخیره‌شدهٔ</b> همین پیش‌نویس، به ترتیب گروه و هویت؛ تنها ورودی مجاز
    /// خواندن فکت‌های مقصد است (نه فهرست دلخواه مالک دیگر).
    /// </summary>
    /// <param name="draft">ریشهٔ ذخیره‌شده.</param>
    public static IReadOnlyList<ShippingAuthoringSelectedAreaItem> SelectedKeys(ShippingAuthoringDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        return draft.RegionGroups
            .OrderBy(group => group.Order)
            .SelectMany(group => group.Areas
                .OrderBy(area => area.NodeId)
                .ThenBy(area => area.NodeKind)
                .Select(area => new ShippingAuthoringSelectedAreaItem(ToContractKind(area.NodeKind), area.NodeId)))
            .Distinct()
            .ToArray();
    }

    private static ShippingAuthoringAreaKind ToContractKind(ShippingDestinationNodeKind kind) => kind switch
    {
        ShippingDestinationNodeKind.Country => ShippingAuthoringAreaKind.Country,
        ShippingDestinationNodeKind.AdministrativeArea => ShippingAuthoringAreaKind.AdministrativeArea,
        ShippingDestinationNodeKind.City => ShippingAuthoringAreaKind.City,
        ShippingDestinationNodeKind.ServiceArea => ShippingAuthoringAreaKind.ServiceArea,
        _ => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid)),
    };
}
