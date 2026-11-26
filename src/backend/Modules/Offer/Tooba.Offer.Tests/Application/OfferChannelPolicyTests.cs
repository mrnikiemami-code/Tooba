using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Offers.Commands.CreateOffer;
using Tooba.Offer.Application.Offers.Policies;
using Tooba.Offer.Application.Offers.Queries.GetAllowedOfferChannels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Endpoints.Errors;
using Tooba.StoreContext.Contracts.Current;
using Xunit;

namespace Tooba.Offer.Tests.Application;

public sealed class OfferChannelPolicyTests
{
    [Theory]
    [InlineData("Direct", SalesChannel.Direct)]
    [InlineData("marketplace", SalesChannel.Marketplace)]
    [InlineData("Api", SalesChannel.Api)]
    public void Configured_named_channel_is_the_only_allowed_channel(string configured, SalesChannel expected)
    {
        Assert.Equal(new[] { expected }, OfferChannelPolicy.Allowed(configured));
        Assert.True(OfferChannelPolicy.IsAllowed(configured, expected));
        Assert.Null(OfferChannelPolicy.Default("not-a-channel"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("Direct,Marketplace")]
    [InlineData("NotAChannel")]
    public void Undefined_numeric_composite_or_unknown_values_fail_closed(string configured)
    {
        Assert.Empty(OfferChannelPolicy.Allowed(configured));
        Assert.Null(OfferChannelPolicy.Default(configured));
    }

    [Fact]
    public async Task Allowed_channel_query_projects_store_commerce_authority()
    {
        var sender = BuildSender("Marketplace");
        var result = await sender.Send(new GetAllowedOfferChannelsQuery());
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Marketplace" }, result.Value);
    }

    [Fact]
    public async Task Allowed_channel_query_is_empty_when_store_commerce_is_unset()
    {
        var sender = BuildSender(null);
        var result = await sender.Send(new GetAllowedOfferChannelsQuery());
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void Resolver_accepts_enum_names_case_insensitively_and_defaults_from_the_store()
    {
        Assert.Equal(SalesChannel.Marketplace, OfferChannelRequestResolver.ResolveRequired("marketplace", "Marketplace"));
        Assert.Equal(SalesChannel.Direct, OfferChannelRequestResolver.ResolveRequired(null, "Direct"));
        Assert.Equal(SalesChannel.Marketplace, OfferChannelRequestResolver.ResolveRequired("  ", "Marketplace"));
        Assert.Null(OfferChannelRequestResolver.ResolveOptional(null, "Marketplace"));
    }

    [Fact]
    public void Resolver_fails_closed_for_unpermitted_unknown_or_unresolvable_channels()
    {
        var denied = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveRequired("Marketplace", "Direct"));
        Assert.Equal(OfferErrorCodes.ChannelNotAllowed, denied.Error.Code);

        var unknown = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveRequired("Bogus", "Marketplace"));
        Assert.Equal(OfferErrorCodes.ChannelNotAllowed, unknown.Error.Code);

        var optionalDenied = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveOptional("Direct", "Marketplace"));
        Assert.Equal(OfferErrorCodes.ChannelNotAllowed, optionalDenied.Error.Code);

        var required = Assert.Throws<SemanticException>(
            () => OfferChannelRequestResolver.ResolveRequired(null, null));
        Assert.Equal(OfferErrorCodes.ChannelRequired, required.Error.Code);
    }

    private static ISender BuildSender(string? channel)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateOfferCommand).Assembly);
        services.AddSingleton<ICurrentStoreCommerceContext>(new FixedStoreCommerce(channel));
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private sealed class FixedStoreCommerce(string? channel) : ICurrentStoreCommerceContext
    {
        public StoreCommerceContext? Current { get; } = new StoreCommerceContext(null, null, channel);
    }
}
