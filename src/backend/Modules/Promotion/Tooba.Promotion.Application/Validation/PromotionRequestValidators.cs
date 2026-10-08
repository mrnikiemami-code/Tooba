using FluentValidation;
using Tooba.Promotion.Application.Merchandising.Admin.Commands;
using Tooba.Promotion.Application.Merchandising.Admin.Queries;
using Tooba.Promotion.Application.Promotions.Commands;
using Tooba.Promotion.Application.Promotions.Queries;

namespace Tooba.Promotion.Application.Validation;

#pragma warning disable CS1591

/// <summary>
/// Promotion transport-shape validators (AMSC W1). Exactly the endpoint-reachable requests whose shape
/// can be malformed get a validator; the deliberately unvalidated requests are recorded in the W1
/// evidence with a durable reason. Business/domain rules are never duplicated here.
/// </summary>
public sealed class CreateSellerPromotionCommandValidator : AbstractValidator<CreateSellerPromotionCommand>
{
    public CreateSellerPromotionCommandValidator()
    {
        RuleFor(x => x.SellerPartyId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerPartyIdRequired);
        RuleFor(x => x.Input)
            .NotNull()
            .WithErrorCode(PromotionValidationCodes.MutationInputRequired);
    }
}

public sealed class UpdateSellerPromotionCommandValidator : AbstractValidator<UpdateSellerPromotionCommand>
{
    public UpdateSellerPromotionCommandValidator()
    {
        RuleFor(x => x.SellerPartyId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerPartyIdRequired);
        RuleFor(x => x.PromotionId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.PromotionIdRequired);
        RuleFor(x => x.Input)
            .NotNull()
            .WithErrorCode(PromotionValidationCodes.MutationInputRequired);
    }
}

public sealed class ActivateSellerPromotionCommandValidator : AbstractValidator<ActivateSellerPromotionCommand>
{
    public ActivateSellerPromotionCommandValidator()
    {
        RuleFor(x => x.SellerPartyId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerPartyIdRequired);
        RuleFor(x => x.PromotionId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.PromotionIdRequired);
    }
}

public sealed class DeactivateSellerPromotionCommandValidator : AbstractValidator<DeactivateSellerPromotionCommand>
{
    public DeactivateSellerPromotionCommandValidator()
    {
        RuleFor(x => x.SellerPartyId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerPartyIdRequired);
        RuleFor(x => x.PromotionId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.PromotionIdRequired);
    }
}

public sealed class GetSellerPromotionQueryValidator : AbstractValidator<GetSellerPromotionQuery>
{
    public GetSellerPromotionQueryValidator()
    {
        RuleFor(x => x.SellerPartyId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerPartyIdRequired);
        RuleFor(x => x.PromotionId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.PromotionIdRequired);
    }
}

public sealed class GetAdminPromotionQueryValidator : AbstractValidator<GetAdminPromotionQuery>
{
    public GetAdminPromotionQueryValidator()
        => RuleFor(x => x.PromotionId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.PromotionIdRequired);
}

public sealed class DeactivateAdminPromotionCommandValidator : AbstractValidator<DeactivateAdminPromotionCommand>
{
    public DeactivateAdminPromotionCommandValidator()
        => RuleFor(x => x.PromotionId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.PromotionIdRequired);
}

public sealed class ListMerchandisingCampaignsQueryValidator : AbstractValidator<ListMerchandisingCampaignsQuery>
{
    private static readonly string[] Lifecycles = ["Draft", "Published", "Archived"];
    private static readonly string[] RuntimeWindows = ["all", "active", "scheduled", "expired", "draft", "archived"];

    public ListMerchandisingCampaignsQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(PromotionValidationCodes.PageOffsetInvalid);
        RuleFor(x => x.Take)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(PromotionValidationCodes.PageSizeInvalid);
        RuleFor(x => x.Lifecycle)
            .Must(BeKnownLifecycle)
            .WithErrorCode(PromotionValidationCodes.LifecycleInvalid);
        RuleFor(x => x.RuntimeWindow)
            .Must(BeKnownRuntimeWindow)
            .WithErrorCode(PromotionValidationCodes.RuntimeWindowInvalid);
        RuleFor(x => x.Locale)
            .Must(BeWellFormedLocale)
            .WithErrorCode(PromotionValidationCodes.LocaleInvalid);
    }

    private static bool BeKnownLifecycle(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || Lifecycles.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    private static bool BeKnownRuntimeWindow(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || RuntimeWindows.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    private static bool BeWellFormedLocale(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Trim().All(c => char.IsLetter(c) || c == '-' || c == '_');
}

public sealed class ListMerchandisingOfferCandidatesQueryValidator : AbstractValidator<ListMerchandisingOfferCandidatesQuery>
{
    public ListMerchandisingOfferCandidatesQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(PromotionValidationCodes.PageOffsetInvalid);
        RuleFor(x => x.Take)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(PromotionValidationCodes.PageSizeInvalid);
    }
}

public sealed class GetMerchandisingCampaignQueryValidator : AbstractValidator<GetMerchandisingCampaignQuery>
{
    public GetMerchandisingCampaignQueryValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
        RuleFor(x => x.Locale)
            .Must(BeWellFormedLocale)
            .WithErrorCode(PromotionValidationCodes.LocaleInvalid);
    }

    private static bool BeWellFormedLocale(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Trim().All(c => char.IsLetter(c) || c == '-' || c == '_');
}

public sealed class CreateMerchandisingCampaignCommandValidator : AbstractValidator<CreateMerchandisingCampaignCommand>
{
    public CreateMerchandisingCampaignCommandValidator()
        => RuleFor(x => x.Body)
            .NotNull()
            .WithErrorCode(PromotionValidationCodes.CampaignBodyRequired);
}

public sealed class UpdateMerchandisingCampaignCommandValidator : AbstractValidator<UpdateMerchandisingCampaignCommand>
{
    public UpdateMerchandisingCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
        RuleFor(x => x.Body)
            .NotNull()
            .WithErrorCode(PromotionValidationCodes.CampaignUpdateBodyRequired);
    }
}

public sealed class PublishMerchandisingCampaignCommandValidator : AbstractValidator<PublishMerchandisingCampaignCommand>
{
    public PublishMerchandisingCampaignCommandValidator()
        => RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
}

public sealed class ArchiveMerchandisingCampaignCommandValidator : AbstractValidator<ArchiveMerchandisingCampaignCommand>
{
    public ArchiveMerchandisingCampaignCommandValidator()
        => RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
}

public sealed class AddMerchandisingCampaignMemberCommandValidator : AbstractValidator<AddMerchandisingCampaignMemberCommand>
{
    public AddMerchandisingCampaignMemberCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
        RuleFor(x => x.SellerOfferId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerOfferIdRequired);
    }
}

public sealed class RemoveMerchandisingCampaignMemberCommandValidator : AbstractValidator<RemoveMerchandisingCampaignMemberCommand>
{
    public RemoveMerchandisingCampaignMemberCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
        RuleFor(x => x.SellerOfferId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerOfferIdRequired);
    }
}

public sealed class ReorderMerchandisingCampaignMembersCommandValidator : AbstractValidator<ReorderMerchandisingCampaignMembersCommand>
{
    public ReorderMerchandisingCampaignMembersCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
        RuleFor(x => x.OrderedSellerOfferIds)
            .NotNull()
            .WithErrorCode(PromotionValidationCodes.MemberOrderRequired);
        RuleFor(x => x.OrderedSellerOfferIds)
            .Must(ids => ids is { Count: > 0 })
            .When(x => x.OrderedSellerOfferIds is not null)
            .WithErrorCode(PromotionValidationCodes.MemberOrderEmpty);
        RuleFor(x => x.OrderedSellerOfferIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.OrderedSellerOfferIds is { Count: > 0 })
            .WithErrorCode(PromotionValidationCodes.MemberOrderDuplicate);
        RuleFor(x => x.OrderedSellerOfferIds)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .When(x => x.OrderedSellerOfferIds is { Count: > 0 })
            .WithErrorCode(PromotionValidationCodes.MemberOrderEntryRequired);
    }
}

public sealed class SetMerchandisingCampaignMemberPriceCommandValidator : AbstractValidator<SetMerchandisingCampaignMemberPriceCommand>
{
    public SetMerchandisingCampaignMemberPriceCommandValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.CampaignIdRequired);
        RuleFor(x => x.SellerOfferId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(PromotionValidationCodes.SellerOfferIdRequired);
        RuleFor(x => x.Body)
            .NotNull()
            .WithErrorCode(PromotionValidationCodes.MemberPriceBodyRequired);
    }
}
