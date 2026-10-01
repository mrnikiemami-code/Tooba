using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Promotion.Contracts.Merchandising;

namespace Tooba.Promotion.Application.Merchandising.Admin;

public static class MerchandisingAdminErrorCodes
{
    public const string Missing = "merchandising.campaign.missing";
    public const string Validation = "campaign.validation";
    public const string Publish = "campaign.publish";
    public const string Member = "campaign.member";
    public const string Reorder = "campaign.reorder";
    public const string Price = "campaign.price";
}

internal static class MerchandisingAdminResult
{
    public static Result<T> OrMissing<T>(T? value) where T : class =>
        value is null ? Result.Failure<T>(new SemanticError(MerchandisingAdminErrorCodes.Missing)) : Result.Success(value);
}

public sealed record ListMerchandisingCampaignsQuery(string? Search, string? Lifecycle, Guid? PromotionTypeId, string? RuntimeWindow, int Skip, int Take, string? Locale) : IRequest<Result<AdminMerchCampaignListResponse>>;
public sealed class ListMerchandisingCampaignsQueryHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<ListMerchandisingCampaignsQuery, Result<AdminMerchCampaignListResponse>>
{ public async Task<Result<AdminMerchCampaignListResponse>> Handle(ListMerchandisingCampaignsQuery r, CancellationToken ct) => Result.Success(await c.ListAsync(r.Search, r.Lifecycle, r.PromotionTypeId, r.RuntimeWindow, r.Skip, r.Take, r.Locale, ct)); }

public sealed record ListMerchandisingCampaignTypesQuery(string? Locale) : IRequest<Result<AdminMerchCampaignTypesResponse>>;
public sealed class ListMerchandisingCampaignTypesQueryHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<ListMerchandisingCampaignTypesQuery, Result<AdminMerchCampaignTypesResponse>>
{ public async Task<Result<AdminMerchCampaignTypesResponse>> Handle(ListMerchandisingCampaignTypesQuery r, CancellationToken ct) => Result.Success(await c.ListTypesAsync(r.Locale, ct)); }

public sealed record ListMerchandisingOfferCandidatesQuery(string? Search, int Skip, int Take) : IRequest<Result<AdminMerchOfferCandidateResponse>>;
public sealed class ListMerchandisingOfferCandidatesQueryHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<ListMerchandisingOfferCandidatesQuery, Result<AdminMerchOfferCandidateResponse>>
{ public async Task<Result<AdminMerchOfferCandidateResponse>> Handle(ListMerchandisingOfferCandidatesQuery r, CancellationToken ct) => Result.Success(await c.ListOfferCandidatesAsync(r.Search, r.Skip, r.Take, ct)); }

public sealed record GetMerchandisingCampaignQuery(Guid CampaignId, string? Locale) : IRequest<Result<AdminMerchCampaignDetail>>;
public sealed class GetMerchandisingCampaignQueryHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<GetMerchandisingCampaignQuery, Result<AdminMerchCampaignDetail>>
{ public async Task<Result<AdminMerchCampaignDetail>> Handle(GetMerchandisingCampaignQuery r, CancellationToken ct) => MerchandisingAdminResult.OrMissing(await c.GetAsync(r.CampaignId, r.Locale, ct)); }

public sealed record CreateMerchandisingCampaignCommand(AdminMerchCampaignWriteRequest Body) : IRequest<Result<AdminMerchCampaignDetail>>;
public sealed class CreateMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<CreateMerchandisingCampaignCommand, Result<AdminMerchCampaignDetail>>
{ public async Task<Result<AdminMerchCampaignDetail>> Handle(CreateMerchandisingCampaignCommand r, CancellationToken ct) { try { return Result.Success(await c.CreateAsync(r.Body, ct)); } catch (InvalidOperationException) { return Result.Failure<AdminMerchCampaignDetail>(new SemanticError(MerchandisingAdminErrorCodes.Validation)); } } }

public sealed record UpdateMerchandisingCampaignCommand(Guid CampaignId, AdminMerchCampaignUpdateRequest Body) : IRequest<Result<AdminMerchCampaignDetail>>;
public sealed class UpdateMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<UpdateMerchandisingCampaignCommand, Result<AdminMerchCampaignDetail>>
{ public async Task<Result<AdminMerchCampaignDetail>> Handle(UpdateMerchandisingCampaignCommand r, CancellationToken ct) { try { return MerchandisingAdminResult.OrMissing(await c.UpdateAsync(r.CampaignId, r.Body, ct)); } catch (InvalidOperationException) { return Result.Failure<AdminMerchCampaignDetail>(new SemanticError(MerchandisingAdminErrorCodes.Validation)); } } }

public sealed record PublishMerchandisingCampaignCommand(Guid CampaignId) : IRequest<Result>;
public sealed class PublishMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<PublishMerchandisingCampaignCommand, Result>
{ public async Task<Result> Handle(PublishMerchandisingCampaignCommand r, CancellationToken ct) { try { return await c.PublishAsync(r.CampaignId, ct) ? Result.Success() : Result.Failure(new SemanticError(MerchandisingAdminErrorCodes.Missing)); } catch (InvalidOperationException) { return Result.Failure(new SemanticError(MerchandisingAdminErrorCodes.Publish)); } } }

public sealed record ArchiveMerchandisingCampaignCommand(Guid CampaignId) : IRequest<Result>;
public sealed class ArchiveMerchandisingCampaignCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<ArchiveMerchandisingCampaignCommand, Result>
{ public async Task<Result> Handle(ArchiveMerchandisingCampaignCommand r, CancellationToken ct) => await c.ArchiveAsync(r.CampaignId, ct) ? Result.Success() : Result.Failure(new SemanticError(MerchandisingAdminErrorCodes.Missing)); }

public sealed record AddMerchandisingCampaignMemberCommand(Guid CampaignId, Guid SellerOfferId) : IRequest<Result<AdminMerchCampaignDetail>>;
public sealed class AddMerchandisingCampaignMemberCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<AddMerchandisingCampaignMemberCommand, Result<AdminMerchCampaignDetail>>
{ public async Task<Result<AdminMerchCampaignDetail>> Handle(AddMerchandisingCampaignMemberCommand r, CancellationToken ct) { try { return MerchandisingAdminResult.OrMissing(await c.AddMemberAsync(r.CampaignId, r.SellerOfferId, ct)); } catch (InvalidOperationException) { return Result.Failure<AdminMerchCampaignDetail>(new SemanticError(MerchandisingAdminErrorCodes.Member)); } } }

public sealed record RemoveMerchandisingCampaignMemberCommand(Guid CampaignId, Guid SellerOfferId) : IRequest<Result<AdminMerchCampaignDetail>>;
public sealed class RemoveMerchandisingCampaignMemberCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<RemoveMerchandisingCampaignMemberCommand, Result<AdminMerchCampaignDetail>>
{ public async Task<Result<AdminMerchCampaignDetail>> Handle(RemoveMerchandisingCampaignMemberCommand r, CancellationToken ct) => MerchandisingAdminResult.OrMissing(await c.RemoveMemberAsync(r.CampaignId, r.SellerOfferId, ct)); }

public sealed record ReorderMerchandisingCampaignMembersCommand(Guid CampaignId, IReadOnlyList<Guid> OrderedSellerOfferIds) : IRequest<Result<AdminMerchCampaignDetail>>;
public sealed class ReorderMerchandisingCampaignMembersCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<ReorderMerchandisingCampaignMembersCommand, Result<AdminMerchCampaignDetail>>
{ public async Task<Result<AdminMerchCampaignDetail>> Handle(ReorderMerchandisingCampaignMembersCommand r, CancellationToken ct) { try { return MerchandisingAdminResult.OrMissing(await c.ReorderMembersAsync(r.CampaignId, r.OrderedSellerOfferIds, ct)); } catch (InvalidOperationException) { return Result.Failure<AdminMerchCampaignDetail>(new SemanticError(MerchandisingAdminErrorCodes.Reorder)); } } }

public sealed record SetMerchandisingCampaignMemberPriceCommand(Guid CampaignId, Guid SellerOfferId, AdminMerchMemberPriceRequest Body) : IRequest<Result<AdminMerchCampaignDetail>>;
public sealed class SetMerchandisingCampaignMemberPriceCommandHandler(IMerchandisingCampaignAdminComposer c) : IRequestHandler<SetMerchandisingCampaignMemberPriceCommand, Result<AdminMerchCampaignDetail>>
{ public async Task<Result<AdminMerchCampaignDetail>> Handle(SetMerchandisingCampaignMemberPriceCommand r, CancellationToken ct) { try { return MerchandisingAdminResult.OrMissing(await c.SetMemberPriceAsync(r.CampaignId, r.SellerOfferId, r.Body, ct)); } catch (InvalidOperationException) { return Result.Failure<AdminMerchCampaignDetail>(new SemanticError(MerchandisingAdminErrorCodes.Price)); } } }
