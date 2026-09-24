using Tooba.Offer.Application.Policies;
using Tooba.Offer.Contracts.Dtos;
using Xunit;

namespace Tooba.Offer.Tests.Application;

public sealed class PrimaryOfferSelectionPolicyTests
{
    private static readonly Guid VariantA = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid VariantB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2");
    private static readonly Guid UnknownVariant = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb9");

    private static readonly Guid OfferExpensiveInStock = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid OfferCheaperOutOfStock = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid OfferCheaperInStock = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3");

    private static readonly Guid SellerId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc1");

    private static OfferSelectionCandidate Candidate(Guid offerId, decimal amount, decimal available, Guid? variantId = null)
        => new(offerId, variantId ?? VariantA, SellerId, amount, available);

    [Fact]
    public void Empty_candidates_resolve_to_null()
    {
        var policy = new PrimaryOfferSelectionPolicy();
        Assert.Null(policy.Resolve([]));
    }

    [Fact]
    public void In_stock_beats_cheaper_out_of_stock()
    {
        var policy = new PrimaryOfferSelectionPolicy();
        var selected = policy.Resolve(
        [
            Candidate(OfferExpensiveInStock, 2_000_000m, 3),
            Candidate(OfferCheaperOutOfStock, 1_000_000m, 0)
        ]);

        Assert.Equal(OfferExpensiveInStock, selected?.OfferId);
    }

    [Fact]
    public void Lower_amount_wins_among_in_stock()
    {
        var policy = new PrimaryOfferSelectionPolicy();
        var selected = policy.Resolve(
        [
            Candidate(OfferExpensiveInStock, 2_000_000m, 3),
            Candidate(OfferCheaperInStock, 1_500_000m, 2)
        ]);

        Assert.Equal(OfferCheaperInStock, selected?.OfferId);
    }

    [Fact]
    public void Equal_amount_and_availability_break_ties_by_offer_id()
    {
        var policy = new PrimaryOfferSelectionPolicy();
        var selected = policy.Resolve(
        [
            Candidate(OfferCheaperInStock, 1_500_000m, 2),
            Candidate(OfferExpensiveInStock, 1_500_000m, 2)
        ]);

        Assert.Equal(OfferExpensiveInStock, selected?.OfferId);
    }

    [Fact]
    public void Requested_variant_is_retained_when_it_belongs_to_the_product_and_has_a_candidate()
    {
        var policy = new PrimaryOfferSelectionPolicy();
        var candidates = new[] { Candidate(OfferExpensiveInStock, 2_000_000m, 1, VariantA), Candidate(OfferCheaperInStock, 1_500_000m, 1, VariantB) };

        Assert.Equal(VariantB, policy.ResolveVariantId(VariantB, [VariantA, VariantB], candidates));
    }

    [Fact]
    public void Requested_variant_without_candidate_falls_back_to_selected_variant()
    {
        var policy = new PrimaryOfferSelectionPolicy();
        var candidates = new[] { Candidate(OfferCheaperInStock, 1_500_000m, 4, VariantA) };

        Assert.Equal(VariantA, policy.ResolveVariantId(VariantB, [VariantA, VariantB], candidates));
    }

    [Fact]
    public void Unknown_requested_variant_falls_back_to_selected_variant()
    {
        var policy = new PrimaryOfferSelectionPolicy();
        var candidates = new[] { Candidate(OfferCheaperInStock, 1_500_000m, 4, VariantA) };

        Assert.Equal(VariantA, policy.ResolveVariantId(UnknownVariant, [VariantA, VariantB], candidates));
    }

    [Fact]
    public void Null_requested_variant_and_no_candidates_resolve_to_null_variant()
    {
        var policy = new PrimaryOfferSelectionPolicy();

        Assert.Null(policy.ResolveVariantId(null, [VariantA], []));
        Assert.Null(policy.ResolveVariantId(VariantA, [VariantA], []));
    }
}
