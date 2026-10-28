using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// ذخیرهٔ اتمی سند کامل ویرایشگر یک پیش‌نویس موجود: درخت منطقه‌ای با ردیف/عنوان تودرتو، متن روش، و
/// بخش‌های اختیاری برنامه/تنظیمات/نوع — یک commit و یک افزایش بازبینی ریشه.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">بدنهٔ سند مرکب.</param>
public sealed record ReplaceShippingAuthoringEditorDocumentCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringEditorDocumentReplaceRequest Request)
    : IRequest<Result<ShippingAuthoringEditorDocumentItem>>;

/// <summary>
/// هندلر PUT سند ویرایشگر. ترتیب تصمیم‌ها عمدی و ثابت است: مالک کامل و بازبینی کهنه پیش از هر
/// فراخوانی بیرونی، سپس زمینهٔ typed زبان/ارز، سپس مشاهدهٔ اختیاری نوع، سپس اثبات روش/جغرافیا، و در
/// پایان یک نوشتن اتمی. پاسخ فقط از ریشهٔ واقعاً ذخیره‌شده و فکت‌های <b>پیش از نوشتن</b> ساخته
/// می‌شود؛ هیچ فراخوانی بیرونی پس از commit وجود ندارد.
/// </summary>
public sealed class ReplaceShippingAuthoringEditorDocumentHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingAuthoringDestinationResolver destinationResolver,
    IShippingMethodOptionValidator optionValidator,
    IShippingAuthoringContextSource contextSource,
    IShippingTypeCatalogReader catalog,
    IShippingAuthoringDestinationFactsReader destinationFacts)
    : IRequestHandler<ReplaceShippingAuthoringEditorDocumentCommand, Result<ShippingAuthoringEditorDocumentItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringEditorDocumentItem>> Handle(
        ReplaceShippingAuthoringEditorDocumentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        // Defense in depth for direct (non-HTTP) invocation: an explicit null collection is never
        // "replace with nothing" and never "no change".
        if (request.Request.RegionGroups is null || request.Request.MethodTranslations is null)
        {
            return Result.Failure<ShippingAuthoringEditorDocumentItem>(
                new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
        }

        try
        {
            // (1) Authorized owner + draft FIRST: a foreign/unknown draft is the same safe not-found and
            // causes no foreign read or write body parsing side effect.
            var existing = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
            if (existing.IsFailure)
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(existing.Errors);
            }

            // (2) A stale client wins before ANY foreign I/O, including an otherwise identical document.
            if (existing.Value.Revision != request.Request.ExpectedRevision)
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(
                    new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
            }

            if (!TryMapGroups(
                    request.Request.RegionGroups,
                    out var regionWrites,
                    out var domainGroups,
                    out var groupFailure))
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(groupFailure!);
            }

            IReadOnlyList<ShippingAuthoringDraft.MethodTranslationInput> translations;
            try
            {
                translations = ShippingAuthoringPresentationWorkflow.ToDomainTranslations(
                    request.Request.MethodTranslations);
            }
            catch (SemanticException exception)
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(exception.Error);
            }

            // (3) Authoritative language/currency context is read ONCE, before any mutation; invalid
            // context fails closed rather than writing against a fabricated registry.
            var snapshot = await contextSource.ReadAsync(cancellationToken);
            var context = ShippingAuthoringContextProjection.Project(snapshot);
            if (context.IsFailure)
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(context.Errors);
            }

            // (4) Method binding and geography are proven on the WHOLE proposed tree, including areas of
            // brand-new groups, before the first tracked mutation.
            var prepared = await ShippingAuthoringDraftWorkflow.PrepareAsync(
                existing.Value.ServiceCode,
                existing.Value.OptionCode,
                regionWrites,
                optionValidator,
                destinationResolver,
                cancellationToken);
            if (prepared.IsFailure)
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(prepared.Errors);
            }

            // (5) Type: a supplied wrapper is an explicit intent (null clears, Guid.Empty is invalid, any
            // non-null id needs exactly ONE active fact). An omitted wrapper is NOT revalidated as a new
            // selection; its stored id is only observed so the response is truthful.
            var includeType = request.Request.TypeAssignment is not null;
            var typeId = request.Request.TypeAssignment?.ShippingTypeId;
            ShippingTypeFact? observation = null;
            if (includeType)
            {
                if (typeId == Guid.Empty)
                {
                    return Result.Failure<ShippingAuthoringEditorDocumentItem>(
                        new SemanticError(ShippingErrorCodes.AuthoringTypeAssignmentInvalid));
                }

                if (typeId is { } selected)
                {
                    var facts = await catalog.GetByIdsAsync([selected], cancellationToken);
                    observation = facts.Count == 1 && facts[0].ShippingTypeId == selected ? facts[0] : null;
                    if (observation is null || !observation.IsActive)
                    {
                        return Result.Failure<ShippingAuthoringEditorDocumentItem>(
                            new SemanticError(ShippingErrorCodes.AuthoringTypeSelectionUnavailable));
                    }
                }
            }
            else if (existing.Value.ShippingTypeId is { } stored)
            {
                var facts = await catalog.GetByIdsAsync([stored], cancellationToken);
                if (facts.Count > 1 || (facts.Count == 1 && facts[0].ShippingTypeId != stored))
                {
                    // Inconsistent non-empty facts are an unavailable configuration, never a fabricated
                    // "not found" and never a silent rewrite of the stored id.
                    return Result.Failure<ShippingAuthoringEditorDocumentItem>(
                        new SemanticError(ShippingErrorCodes.AuthoringConfigurationUnavailable));
                }

                observation = facts.Count == 1 ? facts[0] : null;
            }

            IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput>? scheduleDays = null;
            if (request.Request.WeeklySchedule is { } schedule)
            {
                if (schedule.Days is null)
                {
                    return Result.Failure<ShippingAuthoringEditorDocumentItem>(
                        new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
                }

                var days = new List<ShippingAuthoringDraft.ScheduleDayInput>(schedule.Days.Count);
                foreach (var day in schedule.Days)
                {
                    if (day is null || day.IsWorking is not { } isWorking)
                    {
                        return Result.Failure<ShippingAuthoringEditorDocumentItem>(
                            new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
                    }

                    days.Add(new ShippingAuthoringDraft.ScheduleDayInput(
                        day.Weekday,
                        isWorking,
                        day.StartTime,
                        day.EndTime));
                }

                scheduleDays = days;
            }

            var groups = new List<ShippingAuthoringDraft.EditorGroupInput>(domainGroups.Count);            for (var index = 0; index < domainGroups.Count; index++)
            {
                var source = request.Request.RegionGroups[index];
                groups.Add(new ShippingAuthoringDraft.EditorGroupInput(
                    domainGroups[index].GroupId,
                    domainGroups[index].Selections,
                    ShippingAuthoringDraftWorkflow.ToDomainRateRows(source.Rows),
                    source.Titles
                        .Select(title => new ShippingAuthoringDraft.EditorGroupTitleInput(
                            title.LanguageId,
                            title.Title))
                        .ToArray()));
            }

            var input = new ShippingAuthoringDraft.EditorDocumentInput(
                groups,
                translations,
                request.Request.WeeklySchedule is not null,
                request.Request.WeeklySchedule?.TimeZoneId,
                scheduleDays,
                request.Request.MethodSettings is not null,
                request.Request.MethodSettings?.FreightPaymentMode,
                request.Request.MethodSettings?.CalculationMode,
                includeType,
                typeId,
                request.Request.WeeklySchedule?.SlotDurationMinutes);

            // (6) Destination facts for the WHOLE submitted tree are acquired here — before the first
            // tracked mutation and therefore before the commit — so the response can be composed
            // without any foreign read after the write.
            var destinationRead = await destinationFacts.ReadAsync(
                SubmittedKeys(regionWrites),
                context.Value.DefaultContentLanguageId,
                cancellationToken);
            if (destinationRead.IsFailure)
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(destinationRead.Errors);
            }

            // (7) ONE own atomic commit and ONE root revision advance; a rejected child leaves the whole
            // tree untouched.
            var saved = await directory.ReplaceEditorDocumentAsync(
                request.Owner,
                request.DraftId,
                request.Request.ExpectedRevision,
                input,
                context.Value.ContentLanguages.Select(language => language.LanguageId).ToHashSet(),
                context.Value.DefaultCurrency,
                cancellationToken);
            if (saved.IsFailure)
            {
                return Result.Failure<ShippingAuthoringEditorDocumentItem>(saved.Errors);
            }

            // (8) The response is composed from the actually saved root plus facts acquired BEFORE the
            // write: no catalog/context lookup (and no other fallible dependency) runs after commit.
            var typeItem = includeType
                ? ShippingTypeAssignmentComposition.ComposeResolved(saved.Value, observation)
                : ShippingTypeAssignmentComposition.ComposeObserved(saved.Value, observation);

            return Result.Success(
                ShippingAuthoringEditorDocumentComposition.Map(
                    saved.Value,
                    request.Owner,
                    context.Value,
                    typeItem,
                    destinationRead.Value));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringEditorDocumentItem>(exception.Error);
        }
    }

    /// <summary>
    /// کلیدهای typed و متمایز <b>پیشنهادیِ ارسال‌شده</b> (پس از مجوز مالک و اعتبارسنجی canonical)؛
    /// همان مجموعه‌ای که درخت ذخیره‌شده خواهد داشت.
    /// </summary>
    private static IReadOnlyList<ShippingAuthoringSelectedAreaItem> SubmittedKeys(
        IReadOnlyList<ShippingAuthoringRegionGroupWrite> regionWrites)
    {
        var keys = new List<ShippingAuthoringSelectedAreaItem>();
        foreach (var group in regionWrites)
        {
            foreach (var area in group.Areas)
            {
                keys.Add(new ShippingAuthoringSelectedAreaItem(area.Kind, TerminalId(area)));
            }
        }

        return keys.Distinct().ToArray();
    }

    /// <summary>
    /// شناسهٔ <b>پایانه</b> یک انتخاب همان‌گونه که خواندن فکت لازم دارد؛ جفت (نوع، شناسه) کلید است.
    /// </summary>
    private static Guid TerminalId(ShippingAuthoringSelectedAreaWrite area) => area.Kind switch
    {
        ShippingAuthoringAreaKind.Country => area.CountryId,
        ShippingAuthoringAreaKind.AdministrativeArea => area.AdministrativeAreaId ?? Guid.Empty,
        ShippingAuthoringAreaKind.City => area.CityId ?? Guid.Empty,
        ShippingAuthoringAreaKind.ServiceArea => area.ServiceAreaId ?? Guid.Empty,
        _ => Guid.Empty,
    };

    private static bool TryMapGroups(
        IReadOnlyList<ShippingAuthoringEditorGroupWrite> source,
        out List<ShippingAuthoringRegionGroupWrite> regionWrites,
        out IReadOnlyList<ShippingAuthoringRegionGroupInput> domainGroups,
        out SemanticError? failure)
    {
        regionWrites = new List<ShippingAuthoringRegionGroupWrite>(source.Count);
        domainGroups = [];
        failure = null;

        foreach (var group in source)
        {
            if (group is null || group.Areas is null || group.Rows is null || group.Titles is null)
            {
                failure = new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid);
                return false;
            }

            foreach (var area in group.Areas)
            {
                if (area is null)
                {
                    failure = new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid);
                    return false;
                }
            }

            foreach (var row in group.Rows)
            {
                if (row is null)
                {
                    failure = new SemanticError(ShippingErrorCodes.AuthoringRateRowPayloadInvalid);
                    return false;
                }
            }

            foreach (var title in group.Titles)
            {
                if (title is null)
                {
                    failure = new SemanticError(ShippingErrorCodes.AuthoringPresentationPayloadInvalid);
                    return false;
                }
            }

            regionWrites.Add(new ShippingAuthoringRegionGroupWrite(group.GroupId, group.Areas));
        }

        domainGroups = ShippingAuthoringDraftWorkflow.ToDomainGroups(regionWrites);
        return true;
    }
}
