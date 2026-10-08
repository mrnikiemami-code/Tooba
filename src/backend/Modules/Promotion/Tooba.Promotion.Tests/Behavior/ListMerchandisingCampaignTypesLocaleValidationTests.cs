using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Promotion.Application.Merchandising.Admin.Queries;
using Tooba.Promotion.Application.Merchandising.Models;
using Tooba.Promotion.Application.Merchandising.Ports;
using Tooba.Promotion.Application.Validation;
using Tooba.Promotion.Contracts.Errors;
using Tooba.Promotion.Endpoints.Errors;
using Xunit;

namespace Tooba.Promotion.Tests.Behavior;

/// <summary>
/// TB-TMAR-PROMOTION-AMSC-001-W3-R2 — behavior lock for the repaired transport-shape coverage of
/// <see cref="ListMerchandisingCampaignTypesQuery"/>. The read binds a client-supplied locale, so a
/// malformed locale must fail through the canonical <c>promotion.validation.locale_invalid</c> code and
/// the canonical <c>validation.failed</c> envelope before the composer is ever invoked, while a valid or
/// absent locale preserves the existing behavior. These tests fail against the pre-repair tree (no
/// validator existed for this request).
/// </summary>
public sealed class ListMerchandisingCampaignTypesLocaleValidationTests
{
    private static readonly IValidator<ListMerchandisingCampaignTypesQuery> Validator =
        new ListMerchandisingCampaignTypesQueryValidator();

    [Theory]
    [InlineData("fa-IR!!")]
    [InlineData("en US")]
    [InlineData("fa_IR_*")]
    [InlineData("../../etc")]
    public async Task Malformed_locale_fails_with_the_canonical_locale_code(string locale)
    {
        var result = await Validator.ValidateAsync(new ListMerchandisingCampaignTypesQuery(locale));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == PromotionValidationCodes.LocaleInvalid);
    }

    [Theory]
    [InlineData("fa-IR")]
    [InlineData("en-US")]
    [InlineData("fa")]
    [InlineData("EN_us")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Valid_or_absent_locale_stays_valid(string? locale)
    {
        var result = await Validator.ValidateAsync(new ListMerchandisingCampaignTypesQuery(locale));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Malformed_locale_maps_to_the_canonical_validation_envelope_with_the_locale_code()
    {
        var catalog = new ErrorDefinitionCatalog(
            [new FoundationErrorCatalogContributor(), new PromotionErrorCatalogContributor()]);
        var mapper = new SafeErrorMapper(catalog);

        var validation = Validator.Validate(new ListMerchandisingCampaignTypesQuery("fa-IR!!"));
        var mapped = mapper.Map(new ValidationException(validation.Errors));

        Assert.Equal(400, mapped.StatusCode);
        Assert.Equal("validation.failed", mapped.ErrorCode);
        Assert.NotNull(mapped.ValidationErrors);
        Assert.Contains(PromotionValidationCodes.LocaleInvalid, mapped.ValidationErrors!["Locale"]);
    }

    [Fact]
    public async Task Malformed_locale_is_rejected_before_the_composer_runs()
    {
        var composer = new ProbeComposer();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMerchandisingCampaignAdminComposer>(composer);
        services.AddValidatorsFromAssembly(typeof(ListMerchandisingCampaignTypesQuery).Assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ListMerchandisingCampaignTypesQuery).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new ListMerchandisingCampaignTypesQuery("fa-IR!!")));

        Assert.False(composer.Invoked);
    }

    [Fact]
    public async Task Valid_locale_reaches_the_composer_unchanged()
    {
        var composer = new ProbeComposer();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMerchandisingCampaignAdminComposer>(composer);
        services.AddValidatorsFromAssembly(typeof(ListMerchandisingCampaignTypesQuery).Assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ListMerchandisingCampaignTypesQuery).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        var result = await sender.Send(new ListMerchandisingCampaignTypesQuery("fa-IR"));

        Assert.True(result.IsSuccess);
        Assert.True(composer.Invoked);
        Assert.Equal("fa-IR", composer.LastLocale);
    }

    private sealed class ProbeComposer : IMerchandisingCampaignAdminComposer
    {
        public bool Invoked { get; private set; }

        public string? LastLocale { get; private set; }

        public Guid ResolveStoreId() => Guid.NewGuid();

        public Task<AdminMerchCampaignTypesResponse> ListTypesAsync(string? locale, CancellationToken cancellationToken)
        {
            Invoked = true;
            LastLocale = locale;
            return Task.FromResult(new AdminMerchCampaignTypesResponse(Array.Empty<AdminMerchCampaignTypeOption>()));
        }

        public Task<AdminMerchCampaignListResponse> ListAsync(string? search, string? lifecycle, Guid? promotionTypeId, string? runtimeWindow, int skip, int take, string? locale, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchCampaignDetail?> GetAsync(Guid campaignId, string? locale, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchCampaignDetail> CreateAsync(AdminMerchCampaignWriteRequest body, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchCampaignDetail?> UpdateAsync(Guid campaignId, AdminMerchCampaignUpdateRequest body, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> PublishAsync(Guid campaignId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ArchiveAsync(Guid campaignId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchCampaignDetail?> AddMemberAsync(Guid campaignId, Guid sellerOfferId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchCampaignDetail?> RemoveMemberAsync(Guid campaignId, Guid sellerOfferId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchCampaignDetail?> ReorderMembersAsync(Guid campaignId, IReadOnlyList<Guid> orderedSellerOfferIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchCampaignDetail?> SetMemberPriceAsync(Guid campaignId, Guid sellerOfferId, AdminMerchMemberPriceRequest body, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminMerchOfferCandidateResponse> ListOfferCandidatesAsync(string? search, int skip, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
