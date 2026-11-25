using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Offers.Commands.CreateOffer;
using Tooba.Offer.Application.Offers.Policies;
using Tooba.Offer.Application.Offers.Queries.GetAllowedOfferChannels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Endpoints.Errors;
using Xunit;

namespace Tooba.Offer.Tests.Application;

public sealed class OfferChannelPolicyTests
{
    [Fact]
    public void SingleStore_allows_only_direct()
    {
        Assert.Equal(new[] { SalesChannel.Direct }, OfferChannelPolicy.Allowed(ToobaEdition.SingleStore));
        Assert.True(OfferChannelPolicy.IsAllowed(ToobaEdition.SingleStore, SalesChannel.Direct));
        Assert.False(OfferChannelPolicy.IsAllowed(ToobaEdition.SingleStore, SalesChannel.Marketplace));
        Assert.Equal(SalesChannel.Direct, OfferChannelPolicy.Default(ToobaEdition.SingleStore));
    }

    [Fact]
    public void Marketplace_allows_only_marketplace()
    {
        Assert.Equal(new[] { SalesChannel.Marketplace }, OfferChannelPolicy.Allowed(ToobaEdition.Marketplace));
        Assert.True(OfferChannelPolicy.IsAllowed(ToobaEdition.Marketplace, SalesChannel.Marketplace));
        Assert.False(OfferChannelPolicy.IsAllowed(ToobaEdition.Marketplace, SalesChannel.Direct));
        Assert.Equal(SalesChannel.Marketplace, OfferChannelPolicy.Default(ToobaEdition.Marketplace));
    }

    [Fact]
    public void Unset_and_unknown_editions_allow_no_channel()
    {
        Assert.Empty(OfferChannelPolicy.Allowed(ToobaEdition.Unset));
        Assert.Empty(OfferChannelPolicy.Allowed((ToobaEdition)99));
        Assert.False(OfferChannelPolicy.IsAllowed(ToobaEdition.Unset, SalesChannel.Direct));
        Assert.False(OfferChannelPolicy.IsAllowed((ToobaEdition)99, SalesChannel.Marketplace));
        Assert.Null(OfferChannelPolicy.Default(ToobaEdition.Unset));
    }

    [Theory]
    [InlineData(ToobaEdition.SingleStore, "Direct")]
    [InlineData(ToobaEdition.Marketplace, "Marketplace")]
    public async Task Allowed_channel_query_projects_edition_policy(ToobaEdition edition, string expected)
    {
        var sender = BuildSender(edition);

        var result = await sender.Send(new GetAllowedOfferChannelsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { expected }, result.Value);
    }

    [Fact]
    public async Task Allowed_channel_query_is_empty_when_edition_is_unset()
    {
        var sender = BuildSender(ToobaEdition.Unset);

        var result = await sender.Send(new GetAllowedOfferChannelsQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void Resolver_accepts_enum_names_case_insensitively_and_defaults_from_the_edition()
    {
        Assert.Equal(
            SalesChannel.Marketplace,
            OfferChannelRequestResolver.ResolveRequired("marketplace", ToobaEdition.Marketplace));
        Assert.Equal(
            SalesChannel.Direct,
            OfferChannelRequestResolver.ResolveRequired(null, ToobaEdition.SingleStore));
        Assert.Equal(
            SalesChannel.Marketplace,
            OfferChannelRequestResolver.ResolveRequired("  ", ToobaEdition.Marketplace));
        Assert.Null(OfferChannelRequestResolver.ResolveOptional(null, ToobaEdition.Marketplace));
    }

    [Fact]
    public void Resolver_fails_closed_for_unpermitted_unknown_or_unresolvable_channels()
    {
        var denied = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveRequired("Marketplace", ToobaEdition.SingleStore));
        Assert.Equal(OfferErrorCodes.ChannelNotAllowed, denied.Error.Code);

        var unknown = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveRequired("Bogus", ToobaEdition.Marketplace));
        Assert.Equal(OfferErrorCodes.ChannelNotAllowed, unknown.Error.Code);

        var optionalDenied = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveOptional("Marketplace", ToobaEdition.SingleStore));
        Assert.Equal(OfferErrorCodes.ChannelNotAllowed, optionalDenied.Error.Code);

        var required = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveRequired(null, ToobaEdition.Unset));
        Assert.Equal(OfferErrorCodes.ChannelRequired, required.Error.Code);
    }

    private static ISender BuildSender(ToobaEdition edition)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateOfferCommand).Assembly);
        services.AddSingleton<ICurrentEdition>(new FixedEdition(edition));
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private sealed class FixedEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current { get; } = new EditionContext(edition, "test");
    }
}
