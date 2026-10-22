using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// ایجاد یک پیش‌نویس کامل برای Binding روش ارسال. مالک از scope سرورساخته می‌آید و هرگز از بدنه.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست (پس از مجوزدهی).</param>
/// <param name="Request">بدنهٔ ایجاد؛ فاقد مالک و شناسهٔ پیش‌نویس.</param>
public sealed record CreateShippingAuthoringDraftCommand(
    ShippingAuthoringOwnerScope Owner,
    ShippingAuthoringDraftCreateRequest Request) : IRequest<Result<ShippingAuthoringDraftItem>>;

/// <summary>هندلر ایجاد پیش‌نویس: Binding، جغرافیا و ناسازگاری پیش از هر نوشتن سنجیده می‌شوند.</summary>
public sealed class CreateShippingAuthoringDraftHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingAuthoringDestinationResolver destinationResolver,
    IShippingMethodOptionValidator optionValidator)
    : IRequestHandler<CreateShippingAuthoringDraftCommand, Result<ShippingAuthoringDraftItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraftItem>> Handle(
        CreateShippingAuthoringDraftCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        var groups = ShippingAuthoringDraftWorkflow.ToDomainGroups(request.Request.RegionGroups);
        var prepared = await ShippingAuthoringDraftWorkflow.PrepareAsync(
            request.Request.ServiceCode,
            request.Request.OptionCode,
            request.Request.RegionGroups,
            optionValidator,
            destinationResolver,
            cancellationToken);
        if (prepared.IsFailure)
        {
            return Result.Failure<ShippingAuthoringDraftItem>(prepared.Errors);
        }

        try
        {
            var created = await directory.CreateAsync(
                request.Owner,
                request.Request.ServiceCode,
                request.Request.OptionCode,
                groups,
                cancellationToken);
            return created.IsFailure
                ? Result.Failure<ShippingAuthoringDraftItem>(created.Errors)
                : Result.Success(ShippingAuthoringDraftWorkflow.Map(created.Value, request.Owner));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringDraftItem>(exception.Error);
        }
    }
}

/// <summary>
/// جایگزینی درخت منطقه‌ای یک پیش‌نویس موجود با بازبینی مورد انتظار. Binding و مالک تغییرناپذیرند.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">بدنهٔ جایگزینی.</param>
public sealed record ReplaceShippingAuthoringDraftRegionsCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringDraftReplaceRegionsRequest Request) : IRequest<Result<ShippingAuthoringDraftItem>>;

/// <summary>
/// هندلر جایگزینی: کل درخت پیشنهادی پیش از تغییر وضعیت ردیف‌ها اعتبارسنجی می‌شود تا جایگزینی
/// نامعتبر هیچ گروه/انتخاب/بازبینی قبلی را تغییر ندهد.
/// </summary>
public sealed class ReplaceShippingAuthoringDraftRegionsHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingAuthoringDestinationResolver destinationResolver,
    IShippingMethodOptionValidator optionValidator)
    : IRequestHandler<ReplaceShippingAuthoringDraftRegionsCommand, Result<ShippingAuthoringDraftItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraftItem>> Handle(
        ReplaceShippingAuthoringDraftRegionsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        var existing = await directory.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (existing.IsFailure)
        {
            return Result.Failure<ShippingAuthoringDraftItem>(existing.Errors);
        }

        var groups = ShippingAuthoringDraftWorkflow.ToDomainGroups(request.Request.RegionGroups);
        var prepared = await ShippingAuthoringDraftWorkflow.PrepareAsync(
            existing.Value.ServiceCode,
            existing.Value.OptionCode,
            request.Request.RegionGroups,
            optionValidator,
            destinationResolver,
            cancellationToken);
        if (prepared.IsFailure)
        {
            return Result.Failure<ShippingAuthoringDraftItem>(prepared.Errors);
        }

        try
        {
            var replaced = await directory.ReplaceRegionGroupsAsync(
                request.Owner,
                request.DraftId,
                request.Request.ExpectedRevision,
                groups,
                cancellationToken);
            return replaced.IsFailure
                ? Result.Failure<ShippingAuthoringDraftItem>(replaced.Errors)
                : Result.Success(ShippingAuthoringDraftWorkflow.Map(replaced.Value, request.Owner));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringDraftItem>(exception.Error);
        }
    }
}

/// <summary>
/// گردش‌کار مشترک نوشتن پیش‌نویس: تبدیل شکل انتقال به شکل دامنه، اثبات Binding و جغرافیا، و
/// قاعدهٔ ناسازگاری روی همهٔ گروه‌های همان پیش‌نویس.
/// </summary>
internal static class ShippingAuthoringDraftWorkflow
{
    /// <summary>شکل انتقال را به ورودی دامنه تبدیل می‌کند (بدون هیچ قاعدهٔ تجاری).</summary>
    /// <param name="groups">گروه‌های بدنه.</param>
    public static IReadOnlyList<ShippingAuthoringRegionGroupInput> ToDomainGroups(
        IReadOnlyList<ShippingAuthoringRegionGroupWrite>? groups)
    {
        var source = groups ?? [];
        var result = new List<ShippingAuthoringRegionGroupInput>(source.Count);
        foreach (var group in source)
        {
            if (group is null)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
            }

            var nodes = (group.Areas ?? [])
                .Where(area => area is not null)
                .Select(area => new ShippingDestinationNode(ToDomainKind(area.Kind), SelectedId(area)))
                .ToArray();
            result.Add(new ShippingAuthoringRegionGroupInput(group.GroupId, nodes));
        }

        return result;
    }

    /// <summary>Binding روش و هندسهٔ مقصد را می‌سنجد و ناسازگاری سراسری پیش‌نویس را رد می‌کند.</summary>
    /// <param name="serviceCode">کد خدمت.</param>
    /// <param name="optionCode">گزینهٔ اختیاری.</param>
    /// <param name="groups">گروه‌های بدنه.</param>
    /// <param name="optionValidator">درز هویت روش Fulfillment.</param>
    /// <param name="destinationResolver">درز اثبات جغرافیا.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public static async Task<Result<bool>> PrepareAsync(
        string serviceCode,
        string? optionCode,
        IReadOnlyList<ShippingAuthoringRegionGroupWrite> groups,
        IShippingMethodOptionValidator optionValidator,
        IShippingAuthoringDestinationResolver destinationResolver,
        CancellationToken cancellationToken)
    {
        var methodValid = await optionValidator.ValidateAsync(serviceCode, optionCode, cancellationToken);
        if (methodValid.IsFailure)
        {
            return Result.Failure<bool>(methodValid.Errors);
        }

        var selections = groups.SelectMany(group => group.Areas ?? []).ToArray();
        var resolved = await destinationResolver.ResolveAsync(selections, cancellationToken);
        if (resolved.IsFailure)
        {
            return Result.Failure<bool>(resolved.Errors);
        }

        // The rule spans ALL groups of this draft, and it runs on proven ancestry chains, never on
        // raw GUID equality: a province and one of its cities conflict even at arbitrary depth.
        if (ShippingDestinationSelectionOverlap.FindConflict(resolved.Value) is not null)
        {
            return Result.Failure<bool>(new SemanticError(ShippingErrorCodes.AuthoringRegionSelectionOverlap));
        }

        return Result.Success(true);
    }

    /// <summary>پیش‌نویس دامنه را به پاسخ نویسندگی نگاشت می‌کند.</summary>
    /// <param name="draft">پیش‌نویس.</param>
    /// <param name="owner">مالک مجاز همین درخواست (برای نمای مالک).</param>
    public static ShippingAuthoringDraftItem Map(ShippingAuthoringDraft draft, ShippingAuthoringOwnerScope owner)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(owner);

        return new ShippingAuthoringDraftItem(
            draft.DraftId,
            new ShippingAuthoringOwnerView(
                owner.Kind.ToString(),
                owner.Kind == ShippingAuthoringOwnerKind.Seller ? owner.SellerPartyId : null),
            draft.ServiceCode,
            draft.OptionCode,
            draft.Revision,
            draft.CreatedAt,
            draft.UpdatedAt,
            draft.RegionGroups
                .OrderBy(group => group.Order)
                .Select(group => new ShippingAuthoringRegionGroupItem(
                    group.GroupId,
                    group.Order,
                    group.Areas
                        .Select(area => new ShippingAuthoringSelectedAreaItem(ToContractKind(area.NodeKind), area.NodeId))
                        .OrderBy(area => area.Kind)
                        .ThenBy(area => area.Id)
                        .ToArray()))
                .ToArray());
    }

    private static Guid SelectedId(ShippingAuthoringSelectedAreaWrite area) => area.Kind switch
    {
        ShippingAuthoringAreaKind.Country => area.CountryId,
        ShippingAuthoringAreaKind.AdministrativeArea => area.AdministrativeAreaId ?? Guid.Empty,
        ShippingAuthoringAreaKind.City => area.CityId ?? Guid.Empty,
        ShippingAuthoringAreaKind.ServiceArea => area.ServiceAreaId ?? Guid.Empty,
        _ => Guid.Empty,
    };

    private static ShippingDestinationNodeKind ToDomainKind(ShippingAuthoringAreaKind kind) => kind switch
    {
        ShippingAuthoringAreaKind.Country => ShippingDestinationNodeKind.Country,
        ShippingAuthoringAreaKind.AdministrativeArea => ShippingDestinationNodeKind.AdministrativeArea,
        ShippingAuthoringAreaKind.City => ShippingDestinationNodeKind.City,
        ShippingAuthoringAreaKind.ServiceArea => ShippingDestinationNodeKind.ServiceArea,
        _ => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid)),
    };

    private static ShippingAuthoringAreaKind ToContractKind(ShippingDestinationNodeKind kind) => kind switch
    {
        ShippingDestinationNodeKind.Country => ShippingAuthoringAreaKind.Country,
        ShippingDestinationNodeKind.AdministrativeArea => ShippingAuthoringAreaKind.AdministrativeArea,
        ShippingDestinationNodeKind.City => ShippingAuthoringAreaKind.City,
        ShippingDestinationNodeKind.ServiceArea => ShippingAuthoringAreaKind.ServiceArea,
        _ => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid)),
    };
}
