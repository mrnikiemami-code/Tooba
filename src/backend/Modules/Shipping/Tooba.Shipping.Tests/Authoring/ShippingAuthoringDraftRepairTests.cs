using FluentValidation;
using MediatR;
using Tooba.BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Authoring.Validation;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1 — repair of the three Architect findings.
/// </summary>
/// <remarks>
/// R190-01 (required collection), R190-02 (preflight before any tracked mutation) are proven here with
/// the real validators, real handlers, real directory and real <see cref="ShippingDbContext"/>. The
/// provider is EF InMemory, so this file says nothing about relational ordering/concurrency: R190-03 is
/// proven separately against a disposable PostgreSQL database.
/// </remarks>
public sealed class ShippingAuthoringDraftRepairTests
{
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void A_missing_region_group_collection_is_rejected_as_required_on_both_write_validators()
    {
        var create = new CreateShippingAuthoringDraftCommandValidator()
            .Validate(new CreateShippingAuthoringDraftCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                new ShippingAuthoringDraftCreateRequest("post", null, null!)));

        Assert.False(create.IsValid);
        Assert.Contains(
            create.Errors,
            error => error.ErrorCode == ShippingAuthoringValidationCodes.RegionGroupsRequired);

        var replace = new ReplaceShippingAuthoringDraftRegionsCommandValidator()
            .Validate(new ReplaceShippingAuthoringDraftRegionsCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                Guid.NewGuid(),
                new ShippingAuthoringDraftReplaceRegionsRequest(1, null!)));

        Assert.False(replace.IsValid);
        Assert.Contains(
            replace.Errors,
            error => error.ErrorCode == ShippingAuthoringValidationCodes.RegionGroupsRequired);
    }

    [Fact]
    public void An_empty_region_group_collection_stays_a_valid_incomplete_draft()
    {
        var create = new CreateShippingAuthoringDraftCommandValidator()
            .Validate(new CreateShippingAuthoringDraftCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                new ShippingAuthoringDraftCreateRequest("post", null, [])));

        Assert.True(create.IsValid);

        var replace = new ReplaceShippingAuthoringDraftRegionsCommandValidator()
            .Validate(new ReplaceShippingAuthoringDraftRegionsCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                Guid.NewGuid(),
                new ShippingAuthoringDraftReplaceRegionsRequest(1, [])));

        Assert.True(replace.IsValid);
    }

    [Fact]
    public void A_null_parent_request_is_reported_as_body_invalid_instead_of_throwing()
    {
        Assert.False(new CreateShippingAuthoringDraftCommandValidator()
            .Validate(new CreateShippingAuthoringDraftCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                null!))
            .IsValid);

        Assert.False(new ReplaceShippingAuthoringDraftRegionsCommandValidator()
            .Validate(new ReplaceShippingAuthoringDraftRegionsCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                Guid.NewGuid(),
                null!))
            .IsValid);
    }

    [Theory]
    [InlineData("null-areas")]
    [InlineData("null-area")]
    [InlineData("empty-group-id")]
    public void Malformed_group_elements_keep_their_existing_stable_shape_codes(string scenario)
    {
        ShippingAuthoringRegionGroupWrite group = scenario switch
        {
            "null-areas" => new ShippingAuthoringRegionGroupWrite(null, null!),
            "null-area" => new ShippingAuthoringRegionGroupWrite(null, [null!]),
            _ => new ShippingAuthoringRegionGroupWrite(Guid.Empty, []),
        };

        var result = new CreateShippingAuthoringDraftCommandValidator()
            .Validate(new CreateShippingAuthoringDraftCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                new ShippingAuthoringDraftCreateRequest("post", null, [group])));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.ErrorCode is ShippingAuthoringValidationCodes.RegionSelectionInvalid
                or ShippingAuthoringValidationCodes.RegionGroupsTooMany);
    }

    [Fact]
    public async Task A_missing_collection_never_reaches_the_handler_through_the_real_validation_behavior()
    {
        var behavior = new ValidationBehavior<CreateShippingAuthoringDraftCommand, Result<ShippingAuthoringDraftItem>>(
            [new CreateShippingAuthoringDraftCommandValidator()]);

        var handlerRan = false;
        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            new CreateShippingAuthoringDraftCommand(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                new ShippingAuthoringDraftCreateRequest("post", null, null!)),
            _ =>
            {
                handlerRan = true;
                return Task.FromResult(Result.Failure<ShippingAuthoringDraftItem>(
                    new SemanticError("shipping.unexpected")));
            },
            CancellationToken.None));

        Assert.False(handlerRan);
    }

    [Fact]
    public async Task A_bad_last_identity_rejects_without_touching_the_same_aggregate_or_tracker()
    {
        using var fixture = new Fixture();
        var owner = ShippingAuthoringOwnerScope.ForStore("store-a");
        var created = await fixture.CreateAsync(owner, [Area(Tehran), Area(Isfahan)]);
        Assert.False(created.IsFailure, created.IsFailure ? created.Errors[0].Code : null);

        var draftId = created.Value.DraftId;
        var tracked = fixture.Tracked(draftId);
        var firstGroup = tracked.RegionGroups.OrderBy(group => group.Order).First();
        var firstGroupId = firstGroup.GroupId;
        var beforeRevision = tracked.Revision;
        var beforeUpdatedAt = tracked.UpdatedAt;
        var beforeGroups = Snapshot(tracked);

        // The FIRST retained group is a real, valid change (different area). The LAST identity is
        // unknown: the whole request must be refused before that first group was applied.
        var unknownLast = await fixture.ReplaceAsync(
            owner,
            draftId,
            expectedRevision: 1,
            [
                new ShippingAuthoringRegionGroupInput(firstGroupId, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(Guid.NewGuid(), [Node(Tehran)]),
            ]);

        Assert.True(unknownLast.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionPayloadInvalid, unknownLast.Errors[0].Code);

        // Same aggregate instance, same values, same revision/timestamps, no pending change.
        Assert.Same(tracked, fixture.Tracked(draftId));
        Assert.Equal(beforeRevision, tracked.Revision);
        Assert.Equal(beforeUpdatedAt, tracked.UpdatedAt);
        Assert.Equal(beforeGroups, Snapshot(tracked));
        Assert.All(
            fixture.Context.ChangeTracker.Entries(),
            entry => Assert.Equal(EntityState.Unchanged, entry.State));

        // A later save in the SAME context cannot publish a partial edit.
        await fixture.Context.SaveChangesAsync();
        var afterSave = await fixture.GetAsync(owner, draftId);
        Assert.Equal(beforeRevision, afterSave.Value.Revision);
        Assert.Equal(beforeGroups, Snapshot(afterSave.Value));

        // A repeated retained identity is refused the same way.
        var repeated = await fixture.ReplaceAsync(
            owner,
            draftId,
            expectedRevision: 1,
            [
                new ShippingAuthoringRegionGroupInput(firstGroupId, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(firstGroupId, [Node(Tehran)]),
            ]);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionPayloadInvalid, repeated.Errors[0].Code);
        Assert.Equal(beforeGroups, Snapshot(fixture.Tracked(draftId)));

        // And a valid replacement afterwards still works, keeping the retained identity.
        var valid = await fixture.ReplaceAsync(
            owner,
            draftId,
            expectedRevision: 1,
            [new ShippingAuthoringRegionGroupInput(firstGroupId, [Node(Isfahan)])]);
        Assert.False(valid.IsFailure, valid.IsFailure ? valid.Errors[0].Code : null);
        Assert.Equal(2, valid.Value.Revision);
        Assert.Equal(firstGroupId, Assert.Single(valid.Value.RegionGroups).GroupId);
    }

    private static string Snapshot(ShippingAuthoringDraft draft) => string.Join(
        "|",
        new[] { $"{draft.Revision}@{draft.UpdatedAt:O}" }
            .Concat(draft.RegionGroups
                .OrderBy(group => group.Order)
                .Select(group => $"{group.GroupId}#{group.Order}#" + string.Join(
                    ",",
                    group.Areas
                        .OrderBy(area => area.NodeKind)
                        .ThenBy(area => area.NodeId)
                        .Select(area => $"{area.NodeKind}:{area.NodeId}:{area.GroupId}")))));

    private static string Snapshot(ShippingAuthoringDraftItem draft) => string.Join(
        "|",
        new[] { $"{draft.Revision}@{draft.UpdatedAt:O}" }
            .Concat(draft.RegionGroups
                .OrderBy(group => group.Order)
                .Select(group => $"{group.GroupId}#{group.Order}#" + string.Join(
                    ",",
                    group.Areas
                        .OrderBy(area => area.Kind)
                        .ThenBy(area => area.Id)
                        .Select(area => $"{area.Kind}:{area.Id}:{group.GroupId}")))));

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.AdministrativeArea, id);

    private static ShippingAuthoringSelectedAreaWrite Area(Guid administrativeAreaId)
        => new(ShippingAuthoringAreaKind.AdministrativeArea, Iran, administrativeAreaId);

    private sealed class Fixture : IDisposable
    {
        private readonly ShippingAuthoringDestinationResolver _resolver =
            new(new FakeGeography(), new ModuleCallTracer());

        public Fixture()
        {
            Context = new ShippingDbContext(
                new DbContextOptionsBuilder<ShippingDbContext>()
                    .UseInMemoryDatabase($"shipping-authoring-repair-{Guid.NewGuid():N}")
                    .Options);
        }

        public ShippingDbContext Context { get; }

        public ShippingAuthoringDraft Tracked(Guid draftId)
            => Context.AuthoringDrafts.Local.Single(draft => draft.DraftId == draftId);

        public Task<Result<ShippingAuthoringDraftItem>> CreateAsync(
            ShippingAuthoringOwnerScope owner,
            IReadOnlyList<ShippingAuthoringSelectedAreaWrite> areas)
            => new CreateShippingAuthoringDraftHandler(
                    new ShippingAuthoringDraftDirectory(Context),
                    _resolver,
                    new FulfillmentShippingMethodOptionValidator(new FakeCatalog(), new ModuleCallTracer()))
                .Handle(
                    new CreateShippingAuthoringDraftCommand(owner, new ShippingAuthoringDraftCreateRequest(
                        "post",
                        null,
                        [new ShippingAuthoringRegionGroupWrite(null, areas)])),
                    CancellationToken.None);

        public async Task<Result<ShippingAuthoringDraftItem>> ReplaceAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            IReadOnlyList<ShippingAuthoringRegionGroupInput> groups)
        {
            var replaced = await new ShippingAuthoringDraftDirectory(Context).ReplaceRegionGroupsAsync(
                owner,
                draftId,
                expectedRevision,
                groups,
                CancellationToken.None);
            return replaced.IsFailure
                ? Result.Failure<ShippingAuthoringDraftItem>(replaced.Errors)
                : Result.Success(new ShippingAuthoringDraftWorkflowAccessor().Map(replaced.Value, owner));
        }

        public Task<Result<ShippingAuthoringDraftItem>> GetAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId)
            => new GetShippingAuthoringDraftHandler(new ShippingAuthoringDraftDirectory(Context))
                .Handle(new GetShippingAuthoringDraftQuery(owner, draftId), CancellationToken.None);

        public void Dispose() => Context.Dispose();
    }

    /// <summary>Maps a domain draft to the wire item without duplicating the production projection.</summary>
    private sealed class ShippingAuthoringDraftWorkflowAccessor
    {
        public ShippingAuthoringDraftItem Map(ShippingAuthoringDraft draft, ShippingAuthoringOwnerScope owner)
            => new(
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
                            .Select(area => new ShippingAuthoringSelectedAreaItem(
                                (ShippingAuthoringAreaKind)area.NodeKind,
                                area.NodeId))
                            .ToArray()))
                    .ToArray());
    }

    private sealed class FakeCatalog : IShippingCatalogReader
    {
        private static readonly Guid ServiceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private static readonly ShippingCatalogServiceSnapshot Post = new(
            ServiceId,
            "post",
            "national",
            "icon",
            "color",
            true,
            0,
            [],
            []);

        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>([Post]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken)
            => Task.FromResult<ShippingCatalogServiceSnapshot?>(serviceId == ServiceId ? Post : null);
    }

    private sealed class FakeGeography : IGeographyLookup
    {
        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CountrySnapshot>>([new CountrySnapshot(Iran, "IR", true)]);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AdministrativeAreaSnapshot>>(
            [
                new AdministrativeAreaSnapshot(Tehran, Iran, null, "TEH", true),
                new AdministrativeAreaSnapshot(Isfahan, Iran, null, "ISF", true),
            ]);

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CitySnapshot>>([]);

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ServiceAreaSnapshot>>([]);

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken)
            => Task.FromResult<DestinationIdentifiers?>(identifiers.CountryId == Iran ? identifiers : null);

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken)
            => Task.FromResult(DestinationResolution.Unresolved());
    }
}
