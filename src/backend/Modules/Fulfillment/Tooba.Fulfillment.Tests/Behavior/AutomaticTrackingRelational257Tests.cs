using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;
using Xunit;
using Xunit.Abstractions;
using AppModels = Tooba.Fulfillment.Application.Fulfillments.Models;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-257 A on REAL storage: the automatic tracking allocation is proved against a real PostgreSQL database through
/// the production command seam (<see cref="FulfillmentDirectory"/>), never an in-memory double. The database unique
/// index <c>ix_shipments_tracking_reference</c> stays the authority, so nothing here can fabricate uniqueness.
/// </summary>
/// <remarks>
/// Gated by <c>TB257_CONNECTION</c>: with no target the test performs no work and claims nothing. Rows are created
/// with the production domain/application paths only (paid-order unit → processing → packing → shipment), are
/// task-labelled, and their exact identities are appended to <c>TB257_EVIDENCE_DIR</c> when supplied. No user order,
/// parcel or tracking row is read or written, and nothing is deleted.
/// </remarks>
public sealed class AutomaticTrackingRelational257Tests
{
    private readonly ITestOutputHelper _output;

    public AutomaticTrackingRelational257Tests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB257_CONNECTION") is { Length: > 0 } raw ? raw : null;

    [Fact]
    public async Task Automatic_allocation_is_unique_per_parcel_and_reconciles_a_retry()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreatePackedFixtureAsync("A", shipments: 2);
        var first = fixture.ShipmentIds[0];
        var second = fixture.ShipmentIds[1];

        await using var lease = fixture.NewDirectory();
        var allocated = await lease.Directory.AssignAutomaticTrackingAsync(
            fixture.FulfillmentId, first, fixture.ActorUserId, CancellationToken.None);
        var firstCode = TrackingOf(allocated, first);
        Assert.Equal(FulfillmentInternalTrackingReference.ForShipment(first), firstCode);

        // A deliberate retry / double click / lost response reconciles the SAME assignment.
        var again = await lease.Directory.AssignAutomaticTrackingAsync(
            fixture.FulfillmentId, first, fixture.ActorUserId, CancellationToken.None);
        Assert.Equal(firstCode, TrackingOf(again, first));

        var other = await lease.Directory.AssignAutomaticTrackingAsync(
            fixture.FulfillmentId, second, fixture.ActorUserId, CancellationToken.None);
        var secondCode = TrackingOf(other, second);
        Assert.Equal(FulfillmentInternalTrackingReference.ForShipment(second), secondCode);
        Assert.NotEqual(firstCode, secondCode, StringComparer.OrdinalIgnoreCase);

        // The real rows carry exactly those codes.
        Assert.Equal(firstCode, await RawTrackingAsync(fixture.Target, first));
        Assert.Equal(secondCode, await RawTrackingAsync(fixture.Target, second));
        Assert.Equal(1, await RawScalarAsync(
            fixture.Target,
            "select count(*) from fulfillment.shipments where lower(tracking_reference) = @code",
            ("code", firstCode.ToLowerInvariant())));

        fixture.Record(_output, $"A first={first} {firstCode} second={second} {secondCode}");
    }

    [Fact]
    public async Task Automatic_allocation_moves_to_the_next_candidate_when_the_canonical_one_is_taken()
    {
        if (Connection is null)
        {
            return;
        }

        var fixture = await CreatePackedFixtureAsync("B", shipments: 2);
        var squatter = fixture.ShipmentIds[0];
        var target = fixture.ShipmentIds[1];
        var canonical = FulfillmentInternalTrackingReference.ForShipment(target);
        var expectedRetry = FulfillmentInternalTrackingReference.ForShipment(target, 1);

        await using var lease = fixture.NewDirectory();
        // An operator stored this parcel's canonical candidate on a DIFFERENT parcel: a legal, unique, real row.
        await lease.Directory.AssignTrackingAsync(
            fixture.FulfillmentId, squatter, fixture.ActorUserId, canonical, CancellationToken.None);

        var allocated = await lease.Directory.AssignAutomaticTrackingAsync(
            fixture.FulfillmentId, target, fixture.ActorUserId, CancellationToken.None);

        var code = TrackingOf(allocated, target);
        Assert.Equal(expectedRetry, code);
        Assert.NotEqual(canonical, code, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(code, await RawTrackingAsync(fixture.Target, target));
        Assert.Equal(canonical, await RawTrackingAsync(fixture.Target, squatter));

        fixture.Record(_output, $"B target={target} {code} squatter={squatter} {canonical}");
    }

    [Fact]
    public async Task Parallel_independent_contexts_allocate_two_distinct_valid_codes()
    {
        if (Connection is null)
        {
            return;
        }

        // Two operators (independent DbContexts, independent units) press the automatic action in the same moment.
        // The candidate is per parcel, so neither request may derive the other's code or block it.
        var left = await CreatePackedFixtureAsync("E1", shipments: 1);
        var right = await CreatePackedFixtureAsync("E2", shipments: 1);

        await using var leftLease = left.NewDirectory();
        await using var rightLease = right.NewDirectory();
        var results = await Task.WhenAll(
            leftLease.Directory.AssignAutomaticTrackingAsync(
                left.FulfillmentId, left.ShipmentIds[0], left.ActorUserId, CancellationToken.None),
            rightLease.Directory.AssignAutomaticTrackingAsync(
                right.FulfillmentId, right.ShipmentIds[0], right.ActorUserId, CancellationToken.None));

        var leftCode = TrackingOf(results[0], left.ShipmentIds[0]);
        var rightCode = TrackingOf(results[1], right.ShipmentIds[0]);
        Assert.Equal(FulfillmentInternalTrackingReference.ForShipment(left.ShipmentIds[0]), leftCode);
        Assert.Equal(FulfillmentInternalTrackingReference.ForShipment(right.ShipmentIds[0]), rightCode);
        Assert.NotEqual(leftCode, rightCode, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(leftCode, await RawTrackingAsync(left.Target, left.ShipmentIds[0]));
        Assert.Equal(rightCode, await RawTrackingAsync(right.Target, right.ShipmentIds[0]));

        left.Record(_output, $"E left={left.ShipmentIds[0]} {leftCode} right={right.ShipmentIds[0]} {rightCode}");
    }

    [Fact]
    public async Task Exact_unique_violation_is_retried_and_any_other_failure_is_not_swallowed()
    {
        if (Connection is null)
        {
            return;
        }

        // A concurrent writer won the same candidate between the read and the save: only the owning catch path may
        // drop that assignment and continue with the next bounded candidate.
        var raced = await CreatePackedFixtureAsync("C", shipments: 1);
        var conflict = new SyntheticProviderFailureInterceptor("ix_shipments_tracking_reference", "23505");
        await using (var lease = raced.NewDirectory(conflict))
        {
            var allocated = await lease.Directory.AssignAutomaticTrackingAsync(
                raced.FulfillmentId, raced.ShipmentIds[0], raced.ActorUserId, CancellationToken.None);
            Assert.Equal(
                FulfillmentInternalTrackingReference.ForShipment(raced.ShipmentIds[0], 1),
                TrackingOf(allocated, raced.ShipmentIds[0]));
        }

        Assert.NotNull(conflict.Injected);
        Assert.NotNull(await RawTrackingAsync(raced.Target, raced.ShipmentIds[0]));
        raced.Record(_output, $"C 23505 retried shipment={raced.ShipmentIds[0]}");

        // A different constraint and/or sqlstate is NOT a tracked-reference collision: it must propagate unchanged
        // and leave the parcel untracked.
        var unrelated = await CreatePackedFixtureAsync("D", shipments: 1);
        var otherFailure = new SyntheticProviderFailureInterceptor("ix_shipments_pkey", "23503");
        await using (var lease = unrelated.NewDirectory(otherFailure))
        {
            var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => lease.Directory.AssignAutomaticTrackingAsync(
                unrelated.FulfillmentId, unrelated.ShipmentIds[0], unrelated.ActorUserId, CancellationToken.None));
            Assert.Same(otherFailure.Injected, thrown);
        }

        Assert.Null(await RawTrackingAsync(unrelated.Target, unrelated.ShipmentIds[0]));
        unrelated.Record(_output, $"D unrelated failure propagated shipment={unrelated.ShipmentIds[0]}");
    }

    private static string TrackingOf(AppModels.FulfillmentSnapshot snapshot, Guid shipmentId) =>
        snapshot.Shipments.Single(x => x.ShipmentId == shipmentId).TrackingReference
        ?? throw new InvalidOperationException($"shipment {shipmentId} carries no tracking reference");

    // ---- fixture -------------------------------------------------------------------------------

    private sealed record Fixture(
        NpgsqlConnectionStringBuilder Target,
        Guid FulfillmentId,
        IReadOnlyList<Guid> ShipmentIds,
        Guid ActorUserId)
    {
        public DirectoryLease NewDirectory(SaveChangesInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
            Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
                options,
                Target.ConnectionString,
                FulfillmentDbContext.Schema,
                typeof(FulfillmentDbContext));
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }

            var db = new FulfillmentDbContext(options.Options);
            return new DirectoryLease(
                db,
                new FulfillmentDirectory(
                    db,
                    new OpenFulfillmentUseCaseGuard(),
                    new UnusedOrderReader(),
                    new UnusedInventoryGateway(),
                    new FulfillmentInstrumentation(),
                    new FixedClock(),
                    new NpgsqlIdGenerator()));
        }

        public void Record(ITestOutputHelper output, string line)
        {
            output.WriteLine(line);
            var directory = Environment.GetEnvironmentVariable("TB257_EVIDENCE_DIR");
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "tb257-relational.txt"), line + Environment.NewLine);
        }
    }

    private sealed class DirectoryLease(FulfillmentDbContext db, FulfillmentDirectory directory) : IAsyncDisposable
    {
        public FulfillmentDirectory Directory { get; } = directory;

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }

    private static async Task<Fixture> CreatePackedFixtureAsync(string tag, int shipments)
    {
        var target = new NpgsqlConnectionStringBuilder(Connection!);
        var fulfillmentId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
        var lineIds = Enumerable.Range(0, shipments).Select(_ => Guid.NewGuid()).ToArray();
        var actorUserId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));

        await using var db = new FulfillmentDbContext(options.Options);
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId,
            Guid.NewGuid,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"TB257-{tag}",
            "09120000000",
            "تهران",
            "تهران",
            "نشانی آزمون تسک 257",
            "1234567890",
            "post",
            "پست",
            lineIds.Select(x => (x, 1m, (Guid?)null)),
            now);
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        await db.SaveChangesAsync();

        var directory = new FulfillmentDirectory(
            db,
            new OpenFulfillmentUseCaseGuard(),
            new UnusedOrderReader(),
            new UnusedInventoryGateway(),
            new FulfillmentInstrumentation(),
            new FixedClock(),
            new NpgsqlIdGenerator());

        await directory.MarkProcessingAsync(fulfillmentId, actorUserId, CancellationToken.None);
        await directory.PackSelectionsAsync(
            fulfillmentId,
            actorUserId,
            lineIds.Select(x => new AppModels.FulfillmentSelectionCommand(x, 1m)).ToArray(),
            CancellationToken.None);

        var shipmentIds = new List<Guid>();
        foreach (var lineId in lineIds)
        {
            var created = await directory.CreateShipmentAsync(
                fulfillmentId,
                actorUserId,
                "پست",
                [new AppModels.ShipmentLineCommand(lineId, 1m)],
                CancellationToken.None);
            shipmentIds.Add(created.CreatedShipmentId);
        }

        return new Fixture(target, fulfillmentId, shipmentIds, actorUserId);
    }

    private static async Task<string?> RawTrackingAsync(NpgsqlConnectionStringBuilder target, Guid shipmentId)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select tracking_reference from fulfillment.shipments where shipment_id = @id";
        command.Parameters.AddWithValue("id", shipmentId);
        return await command.ExecuteScalarAsync() as string;
    }

    private static async Task<int> RawScalarAsync(
        NpgsqlConnectionStringBuilder target,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 10, 9, 30, 0, TimeSpan.Zero);
    }

    private sealed class NpgsqlIdGenerator : IIdGenerator
    {
        public Guid NewId() => Guid.NewGuid();
    }

    private sealed class UnusedOrderReader : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-257 fixture does not read orders.");

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-257 fixture does not read orders.");
    }

    private sealed class UnusedInventoryGateway : IFulfillmentInventoryGateway
    {
        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-257 fixture does not touch inventory.");

        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("TB-257 fixture does not touch inventory.");
    }
}
