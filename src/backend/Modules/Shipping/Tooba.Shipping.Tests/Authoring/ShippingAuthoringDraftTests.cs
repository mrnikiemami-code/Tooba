using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190 — focused owner-isolated draft/region proof.
/// </summary>
/// <remarks>
/// Real domain, real handlers, real directory over <see cref="ShippingDbContext"/>, real Geography
/// proof over a controlled Contracts fixture. The provider is EF InMemory, so this file proves the
/// aggregate/store/handler behavior and the owner filter, NOT relational concurrency, unique-index or
/// PostgreSQL transaction behavior: those are mapped and reviewed in the persistence-model guard and
/// remain a pending real-PostgreSQL acceptance gate.
/// </remarks>
public sealed class ShippingAuthoringDraftTests
{
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Karaj = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TehranCity = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TehranArea = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid SharedNodeId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public void Ancestor_and_descendant_selections_conflict_across_groups_and_independent_branches_do_not()
    {
        var country = Path(Node(ShippingDestinationNodeKind.Country, Iran));
        var province = Path(
            Node(ShippingDestinationNodeKind.Country, Iran),
            Node(ShippingDestinationNodeKind.AdministrativeArea, Tehran));
        var city = Path(
            Node(ShippingDestinationNodeKind.Country, Iran),
            Node(ShippingDestinationNodeKind.AdministrativeArea, Tehran),
            Node(ShippingDestinationNodeKind.City, TehranCity));
        var otherProvince = Path(
            Node(ShippingDestinationNodeKind.Country, Iran),
            Node(ShippingDestinationNodeKind.AdministrativeArea, Isfahan));

        // Country covers every descendant, and "province selected => adding its city" conflicts too.
        Assert.NotNull(ShippingDestinationSelectionOverlap.FindConflict([country, city]));
        Assert.NotNull(ShippingDestinationSelectionOverlap.FindConflict([province, city]));
        Assert.NotNull(ShippingDestinationSelectionOverlap.FindConflict([city, province]));

        // Two independent provinces under the same country do not conflict.
        Assert.Null(ShippingDestinationSelectionOverlap.FindConflict([province, otherProvince]));

        // The same GUID in two different node kinds is two different destinations, not a duplicate.
        var sharedCountry = Path(Node(ShippingDestinationNodeKind.Country, SharedNodeId));
        var sharedCity = Path(
            Node(ShippingDestinationNodeKind.Country, Iran),
            Node(ShippingDestinationNodeKind.City, SharedNodeId));
        Assert.Null(ShippingDestinationSelectionOverlap.FindConflict([sharedCountry, sharedCity]));
    }

    [Fact]
    public void Replace_keeps_stable_group_identity_and_bumps_the_revision()
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForMarketplace(),
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(ShippingDestinationNodeKind.AdministrativeArea, Tehran)])],
            DateTimeOffset.UnixEpoch);
        var groupId = Assert.Single(draft.RegionGroups).GroupId;

        // A group id that does not belong to this draft can never be re-attached.
        Assert.ThrowsAny<Exception>(() => draft.ReplaceRegionGroups(
            [new ShippingAuthoringRegionGroupInput(Guid.NewGuid(), [Node(ShippingDestinationNodeKind.AdministrativeArea, Isfahan)])],
            DateTimeOffset.UnixEpoch));

        draft.ReplaceRegionGroups(
            [new ShippingAuthoringRegionGroupInput(groupId, [Node(ShippingDestinationNodeKind.AdministrativeArea, Isfahan)])],
            DateTimeOffset.UnixEpoch);
        Assert.Equal(2, draft.Revision);
        Assert.Equal(groupId, Assert.Single(draft.RegionGroups).GroupId);
        Assert.Equal(Isfahan, Assert.Single(draft.RegionGroups[0].Areas).NodeId);
    }

    [Fact]
    public async Task Real_create_is_owner_isolated_and_survives_a_new_context_readback()
    {
        using var fixture = new Fixture();
        var storeA = ShippingAuthoringOwnerScope.ForStore("store-a");
        var storeB = ShippingAuthoringOwnerScope.ForStore("store-b");

        var created = await fixture.CreateAsync(
            storeA,
            new ShippingAuthoringDraftCreateRequest(
                "post",
                null,
                [
                    new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.AdministrativeArea, Tehran)]),
                    new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.AdministrativeArea, Isfahan)]),
                ]));
        Assert.False(created.IsFailure, created.IsFailure ? created.Errors[0].Code : null);

        var draftId = created.Value.DraftId;
        Assert.Equal(1, created.Value.Revision);
        Assert.Equal(2, created.Value.RegionGroups.Count);
        Assert.Equal([0, 1], created.Value.RegionGroups.Select(group => group.Order).ToArray());

        // A brand-new context/service instance sees the same rows: a real store roundtrip, not a cache.
        var read = await fixture.GetAsync(storeA, draftId);
        Assert.False(read.IsFailure);
        Assert.Equal(2, read.Value.RegionGroups.Count);

        // Another store must not resolve the same draft id (safe not-found, no ownership disclosure).
        var foreign = await fixture.GetAsync(storeB, draftId);
        Assert.True(foreign.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreign.Errors[0].Code);

        // Duplicate binding inside one owner is refused; the same binding is independent for another.
        var duplicate = await fixture.CreateAsync(
            storeA,
            new ShippingAuthoringDraftCreateRequest("post", null, []));
        Assert.Equal(ShippingErrorCodes.AuthoringDraftBindingDuplicate, duplicate.Errors[0].Code);

        var otherOwner = await fixture.CreateAsync(
            storeB,
            new ShippingAuthoringDraftCreateRequest("post", null, []));
        Assert.False(otherOwner.IsFailure, otherOwner.IsFailure ? otherOwner.Errors[0].Code : null);

        // Marketplace and seller owners are separate owners from a store and from each other.
        var marketplace = await fixture.CreateAsync(
            ShippingAuthoringOwnerScope.ForMarketplace(),
            new ShippingAuthoringDraftCreateRequest("post", null, []));
        Assert.False(marketplace.IsFailure);

        var sellerA = await fixture.CreateAsync(
            ShippingAuthoringOwnerScope.ForSeller(Guid.Parse("88888888-8888-8888-8888-888888888888")),
            new ShippingAuthoringDraftCreateRequest("post", null, []));
        var sellerB = await fixture.CreateAsync(
            ShippingAuthoringOwnerScope.ForSeller(Guid.Parse("99999999-9999-9999-9999-999999999999")),
            new ShippingAuthoringDraftCreateRequest("post", null, []));
        Assert.False(sellerA.IsFailure);
        Assert.False(sellerB.IsFailure);
        Assert.NotEqual(sellerA.Value.DraftId, sellerB.Value.DraftId);

        var sellerBCannotReadA = await fixture.GetAsync(
            ShippingAuthoringOwnerScope.ForSeller(Guid.Parse("99999999-9999-9999-9999-999999999999")),
            sellerA.Value.DraftId);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, sellerBCannotReadA.Errors[0].Code);
    }

    [Fact]
    public async Task A_late_invalid_group_leaves_the_previous_snapshot_and_revision_intact()
    {
        using var fixture = new Fixture();
        var owner = ShippingAuthoringOwnerScope.ForSeller(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"));

        var created = await fixture.CreateAsync(
            owner,
            new ShippingAuthoringDraftCreateRequest(
                "post",
                "express",
                [new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.AdministrativeArea, Tehran)])]));
        Assert.False(created.IsFailure, created.IsFailure ? created.Errors[0].Code : null);

        // The LAST group reintroduces an ancestor of the first selection: the whole replacement is
        // refused, so the previously stored group and revision stay untouched.
        var rejected = await fixture.ReplaceAsync(
            owner,
            created.Value.DraftId,
            new ShippingAuthoringDraftReplaceRegionsRequest(
                1,
                [
                    new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.AdministrativeArea, Tehran)]),
                    new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.Country, Iran)]),
                ]));
        Assert.True(rejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringRegionSelectionOverlap, rejected.Errors[0].Code);

        var afterRejection = await fixture.GetAsync(owner, created.Value.DraftId);
        Assert.Equal(1, afterRejection.Value.Revision);
        Assert.Single(afterRejection.Value.RegionGroups);
        Assert.Equal(Tehran, Assert.Single(afterRejection.Value.RegionGroups[0].Areas).Id);

        // A stale revision conflicts and still changes nothing.
        var stale = await fixture.ReplaceAsync(
            owner,
            created.Value.DraftId,
            new ShippingAuthoringDraftReplaceRegionsRequest(
                7,
                [new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.AdministrativeArea, Isfahan)])]));
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, stale.Errors[0].Code);

        // A valid replacement advances the revision and keeps exactly the submitted tree.
        var replaced = await fixture.ReplaceAsync(
            owner,
            created.Value.DraftId,
            new ShippingAuthoringDraftReplaceRegionsRequest(
                1,
                [new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.AdministrativeArea, Isfahan)])]));
        Assert.Equal(2, replaced.Value.Revision);
        Assert.Single(replaced.Value.RegionGroups);

        // A retry with the now-current revision is idempotent for identity: no duplicate groups/areas.
        var retried = await fixture.ReplaceAsync(
            owner,
            created.Value.DraftId,
            new ShippingAuthoringDraftReplaceRegionsRequest(
                2,
                [new ShippingAuthoringRegionGroupWrite(null, [Area(ShippingAuthoringAreaKind.AdministrativeArea, Isfahan)])]));
        Assert.Equal(3, retried.Value.Revision);
        Assert.Single(retried.Value.RegionGroups);
        Assert.Single(retried.Value.RegionGroups[0].Areas);
    }

    [Fact]
    public async Task Geography_proof_walks_real_ancestry_and_refuses_a_cycle()
    {
        using var fixture = new Fixture();
        var resolver = fixture.Resolver();

        var deep = await resolver.ResolveAsync([Area(ShippingAuthoringAreaKind.City, TehranCity)], CancellationToken.None);
        Assert.False(deep.IsFailure);
        Assert.Equal([Iran, Tehran, TehranCity], Assert.Single(deep.Value).Chain.Select(node => node.Id).ToArray());

        var serviceArea = await resolver.ResolveAsync(
            [Area(ShippingAuthoringAreaKind.ServiceArea, TehranArea, city: TehranCity)],
            CancellationToken.None);
        Assert.False(serviceArea.IsFailure);
        Assert.Equal(
            [Iran, Tehran, TehranCity, TehranArea],
            Assert.Single(serviceArea.Value).Chain.Select(node => node.Id).ToArray());

        // A declared administrative parent that contradicts the real city fact is refused.
        var mismatchedParent = await resolver.ResolveAsync(
            [Area(ShippingAuthoringAreaKind.City, TehranCity, adminArea: Isfahan)],
            CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.DestinationScopeInvalid, mismatchedParent.Errors[0].Code);

        // A cyclic administrative chain is not a valid ancestry, even when every id exists.
        fixture.Geography.CycleTehranToKaraj = true;
        var cyclic = await resolver.ResolveAsync(
            [Area(ShippingAuthoringAreaKind.AdministrativeArea, Tehran)],
            CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.DestinationScopeInvalid, cyclic.Errors[0].Code);
    }

    private static ShippingDestinationNode Node(ShippingDestinationNodeKind kind, Guid id) => new(kind, id);

    private static ShippingDestinationSelectionPath Path(params ShippingDestinationNode[] chain)
        => ShippingDestinationSelectionPath.FromChain(chain);

    private static ShippingAuthoringSelectedAreaWrite Area(
        ShippingAuthoringAreaKind kind,
        Guid id,
        Guid? adminArea = null,
        Guid? city = null) => kind switch
        {
            ShippingAuthoringAreaKind.Country => new ShippingAuthoringSelectedAreaWrite(kind, id),
            ShippingAuthoringAreaKind.AdministrativeArea => new ShippingAuthoringSelectedAreaWrite(kind, Iran, id),
            ShippingAuthoringAreaKind.City => new ShippingAuthoringSelectedAreaWrite(kind, Iran, adminArea, id),
            ShippingAuthoringAreaKind.ServiceArea => new ShippingAuthoringSelectedAreaWrite(kind, Iran, adminArea, city ?? TehranCity, id),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    /// <summary>Isolated fixture: one InMemory database, the real adapters, controlled Contracts fixtures.</summary>
    private sealed class Fixture : IDisposable
    {
        private readonly string _database = $"shipping-authoring-{Guid.NewGuid():N}";

        public FakeGeography Geography { get; } = new();

        private ShippingDbContext NewContext()
            => new(new DbContextOptionsBuilder<ShippingDbContext>().UseInMemoryDatabase(_database).Options);

        public ShippingAuthoringDestinationResolver Resolver()
            => new(Geography, new ModuleCallTracer());

        private static FulfillmentShippingMethodOptionValidator Catalog()
            => new(new FakeCatalog(), new ModuleCallTracer());

        public Task<Result<ShippingAuthoringDraftItem>> CreateAsync(
            ShippingAuthoringOwnerScope owner,
            ShippingAuthoringDraftCreateRequest request)
            => new CreateShippingAuthoringDraftHandler(
                    new ShippingAuthoringDraftDirectory(NewContext()),
                    Resolver(),
                    Catalog())
                .Handle(new CreateShippingAuthoringDraftCommand(owner, request), CancellationToken.None);

        public Task<Result<ShippingAuthoringDraftItem>> ReplaceAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            ShippingAuthoringDraftReplaceRegionsRequest request)
            => new ReplaceShippingAuthoringDraftRegionsHandler(
                    new ShippingAuthoringDraftDirectory(NewContext()),
                    Resolver(),
                    Catalog())
                .Handle(new ReplaceShippingAuthoringDraftRegionsCommand(owner, draftId, request), CancellationToken.None);

        public Task<Result<ShippingAuthoringDraftItem>> GetAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId)
            => new GetShippingAuthoringDraftHandler(new ShippingAuthoringDraftDirectory(NewContext()))
                .Handle(new GetShippingAuthoringDraftQuery(owner, draftId), CancellationToken.None);

        public void Dispose()
        {
        }
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
            [
                new ShippingCatalogOptionSnapshot(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    ServiceId,
                    "express",
                    true,
                    0,
                    []),
            ]);

        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>([Post]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken)
            => Task.FromResult<ShippingCatalogServiceSnapshot?>(serviceId == ServiceId ? Post : null);
    }

    private sealed class FakeGeography : IGeographyLookup
    {
        private readonly AdministrativeAreaSnapshot _tehran = new(Tehran, Iran, null, "TEH", true);
        private readonly AdministrativeAreaSnapshot _isfahan = new(Isfahan, Iran, null, "ISF", true);
        private readonly AdministrativeAreaSnapshot _karaj = new(Karaj, Iran, Tehran, "KRJ", true);
        private readonly CitySnapshot _tehranCity = new(TehranCity, Iran, Tehran, "THR", true);

        public bool CycleTehranToKaraj { get; set; }

        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CountrySnapshot>>([new CountrySnapshot(Iran, "IR", true)]);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken)
        {
            var tehran = CycleTehranToKaraj
                ? _tehran with { ParentAdministrativeAreaId = Karaj }
                : _tehran;
            return Task.FromResult<IReadOnlyList<AdministrativeAreaSnapshot>>([tehran, _isfahan, _karaj]);
        }

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CitySnapshot>>([_tehranCity]);

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ServiceAreaSnapshot>>([new ServiceAreaSnapshot(TehranArea, cityId, "THR-1", true)]);

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
