// Test-only harness: exercises the real Inventory owner contracts against the authorized DEV
// PostgreSQL database. No product, seed, permission, guard, contract or matrix file is modified.
// The caller (p1c-group-f-cases.cjs) owns the offer fixtures and performs exact SQL cleanup.
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Replacement;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Adapters;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Persistence;

var conn = args[0];
var command = args[1];
var cfgPath = args[2];
var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var cfg = JsonSerializer.Deserialize<Cfg>(File.ReadAllText(cfgPath), json)
          ?? throw new InvalidOperationException("config");

var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
ToobaNpgsql.ConfigureModuleContext(optionsBuilder, conn, InventoryDbContext.Schema, typeof(InventoryDbContext));
var options = optionsBuilder.Options;
var clock = new SystemUtcClock();
var ids = new UuidV7IdGenerator();
var tracer = new ModuleCallTracer();
var guard = new OpenInventoryUseCaseGuard();
var offers = new DbOffers(conn);
var catalog = new FakeCatalog(cfg);
var results = new Dictionary<string, object?>
{
    ["command"] = command,
    ["edition"] = cfg.Edition,
    ["runId"] = cfg.RunId,
};
var http = new HttpClient { BaseAddress = new Uri(cfg.HttpBase ?? "http://127.0.0.1:5088") };
http.DefaultRequestHeaders.TryAddWithoutValidation("X-Tooba-Dev-Actor-User-Id", cfg.AdminActor ?? "");
http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "fa-IR");

InventoryDbContext NewDb() => new(options);
InventoryDirectory Dir(bool single)
{
    var commerce = new FixedCommerce(single);
    return new InventoryDirectory(NewDb(), guard, offers, catalog, clock, ids, tracer, commerce);
}
bool singleEdition = string.Equals(cfg.Edition, "SingleStore", StringComparison.OrdinalIgnoreCase);
InventoryDirectory DirMain() => Dir(singleEdition);
InventoryDbContext FaultDb(IInterceptor interceptor)
{
    var builder = new DbContextOptionsBuilder<InventoryDbContext>();
    ToobaNpgsql.ConfigureModuleContext(builder, conn, InventoryDbContext.Schema, typeof(InventoryDbContext));
    builder.AddInterceptors(interceptor);
    return new InventoryDbContext(builder.Options);
}
InventoryDirectory FaultDirectory(InventoryDbContext db) =>
    new(db, guard, offers, catalog, clock, ids, tracer, new FixedCommerce(singleEdition));

async Task<List<PosRef>> Build()
{
    var list = new List<PosRef>();
    var index = 0;
    foreach (var offer in cfg.Offers)
    {
        foreach (var pos in offer.Positions ?? [])
        {
            index++;
            var code = Truncate("P1C-F-" + cfg.RunId + "-" + index, 32).ToUpperInvariant();
            Guid locationId;
            Guid stockItemId;
            if (pos.Eligible)
            {
                if (string.Equals(pos.OwnerKind, "Seller", StringComparison.OrdinalIgnoreCase))
                {
                    locationId = Guid.NewGuid();
                    await using var ldb = NewDb();
                    ldb.Locations.Add(InventoryLocation.Create(locationId, code, "P1C F harness location", clock.UtcNow,
                        InventoryLocationOwnerKind.Seller, pos.SellerOwner ?? offer.SellerId));
                    await ldb.SaveChangesAsync();
                }
                else
                {
                    var creator = DirMain();
                    locationId = await creator.CreateLocationAsync(code, "P1C F harness location", default);
                }
                var directory = DirMain();
                stockItemId = await directory.OpenPositionAsync(offer.OfferId, locationId, default);
                await directory.AdjustAsync(stockItemId, StockAdjustmentKind.Set, pos.Stock,
                    "P1C F harness stock", null, default);
            }
            else
            {
                locationId = Guid.NewGuid();
                await using var db = NewDb();
                var kind = string.Equals(pos.OwnerKind, "Seller", StringComparison.OrdinalIgnoreCase)
                    ? InventoryLocationOwnerKind.Seller : InventoryLocationOwnerKind.Store;
                db.Locations.Add(InventoryLocation.Create(locationId, code, "P1C F ineligible location", clock.UtcNow,
                    kind, kind == InventoryLocationOwnerKind.Seller ? (pos.SellerOwner ?? Guid.NewGuid()) : null));
                var entity = StockPosition.Open(ids.NewId(), offer.OfferId, offer.VariantId, locationId, clock.UtcNow);
                entity.SyncQuantities(pos.Stock, 0, clock.UtcNow);
                db.Positions.Add(entity);
                await db.SaveChangesAsync();
                stockItemId = entity.StockItemId;
            }
            list.Add(new PosRef(offer.Key, offer.OfferId, offer.SellerId, offer.VariantId, locationId, code, stockItemId, pos.Stock, pos.Eligible));
        }
    }
    return list;
}

async Task<Dictionary<string, object?>> Snap(string label, Guid? offerId = null)
{
    await using var db = NewDb();
    var offerIds = offerId is { } one ? new[] { one } : cfg.Offers.Select(x => x.OfferId).ToArray();
    var allocs = await db.OfferAllocations.AsNoTracking().Where(x => offerIds.Contains(x.OfferId))
        .Select(x => new { x.OfferId, Quantity = x.Quantity, Held = x.Held, Revision = x.Revision.ToString("N") }).ToListAsync();
    var positions = await db.Positions.AsNoTracking().Where(x => offerIds.Contains(x.OfferId))
        .OrderBy(x => x.StockItemId)
        .Select(x => new { x.OfferId, x.StockItemId, x.LocationId, OnHand = x.OnHand, Reserved = x.Reserved, UpdatedAt = x.UpdatedAt.ToString("O") }).ToListAsync();
    var stockIds = positions.Select(x => x.StockItemId).ToArray();
    var reservations = await db.Reservations.AsNoTracking().Where(x => stockIds.Contains(x.StockItemId)).OrderBy(x => x.ReservationId)
        .Select(x => new { x.ReservationId, x.StockItemId, Quantity = x.Quantity, Status = x.Status.ToString(), Purpose = x.Purpose.ToString(), ExpiresAt = x.ExpiresAt == null ? null : x.ExpiresAt.Value.ToString("O") }).ToListAsync();
    var facts = await db.OfferInventoryChanges.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).OrderBy(x => x.OccurredAt)
        .Select(x => new { x.FactId, x.OfferId, x.StockItemId, x.LocationId, x.Kind, x.OperationId, x.OldAllocation, x.NewAllocation, x.OldOnHand, x.NewOnHand, x.Reason, x.ActorUserId, x.OccurredAt }).ToListAsync();
    var reservationIds = reservations.Select(x => x.ReservationId).ToArray();
    var consumptions = await db.ReservationConsumptions.AsNoTracking().Where(x => reservationIds.Contains(x.ReservationId)).CountAsync();
    var releases = await db.HeldObligationReleases.AsNoTracking().Where(x => reservationIds.Contains(x.ReservationId)).CountAsync();
    var offerStates = new List<object>();
    await using (var oc = new NpgsqlConnection(conn))
    {
        await oc.OpenAsync();
        await using var ocmd = new NpgsqlCommand("SELECT offer_id, fulfillment_responsibility FROM offer.offers WHERE offer_id = ANY(@ids)", oc);
        ocmd.Parameters.AddWithValue("ids", offerIds);
        await using var or = await ocmd.ExecuteReaderAsync();
        while (await or.ReadAsync())
            offerStates.Add(new { offer_id = or.GetGuid(0), fulfillment_responsibility = or.IsDBNull(1) ? null : or.GetString(1) });
    }
    return new Dictionary<string, object?>
    {
        ["label"] = label,
        ["offers"] = offerStates,
        ["allocations"] = allocs,
        ["positions"] = positions,
        ["reservations"] = reservations,
        ["facts"] = facts,
        ["consumptions"] = consumptions,
        ["heldReleases"] = releases,
    };
}

async Task<object> SetAlloc(Guid offerId, Guid sellerId, decimal qty, string expected, Guid op)
{
    var dir = DirMain();
    var r = await dir.SetAllocationAsync(new SetSellerOfferAllocation(offerId, sellerId, qty, expected, Guid.NewGuid(), op), default);
    return new Dictionary<string, object?>
    {
        ["ok"] = r.IsSuccess,
        ["code"] = r.IsSuccess ? null : r.Errors.FirstOrDefault()?.Code,
        ["receipt"] = r.IsSuccess ? new { r.Value.Quantity, r.Value.Held, r.Value.Available, r.Value.Revision } : null,
    };
}

async Task<object> Reserve(Guid stockId, decimal qty, string key, DateTimeOffset? expires = null)
{
    var dir = DirMain();
    try
    {
        var r = await dir.ReserveAsync(stockId, qty, "p1c-f-" + cfg.RunId, "p1c-f-" + cfg.RunId + "-" + key, expires ?? clock.UtcNow.AddHours(1), default);
        return new Dictionary<string, object?> { ["ok"] = true, ["reservationId"] = r.ReservationId, ["quantity"] = r.Quantity, ["status"] = r.Status.ToString() };
    }
    catch (ContractOperationException ex)
    {
        return new Dictionary<string, object?> { ["ok"] = false, ["code"] = ex.Code };
    }
}

async Task<object> Run(string label, Func<Task<object>> action)
{
    try { return new Dictionary<string, object?> { ["label"] = label, ["ok"] = true, ["value"] = await action() }; }
    catch (ContractOperationException ex) { return new Dictionary<string, object?> { ["label"] = label, ["ok"] = false, ["code"] = ex.Code }; }
    catch (Exception ex) { return new Dictionary<string, object?> { ["label"] = label, ["ok"] = false, ["error"] = ex.GetType().Name + ": " + ex.Message }; }
}

async Task<(int Status, string Body)> HttpGet(string route)
{
    using var response = await http.GetAsync(route);
    return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
}
async Task<(int Status, string Body)> HttpPut(string route, object payload)
{
    using var response = await http.PutAsJsonAsync(route, payload);
    return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
}
static string RevisionOf(string body)
{
    using var document = JsonDocument.Parse(body);
    return document.RootElement.GetProperty("revisions").GetProperty("offer").GetString() ?? "";
}

try
{
    var fixture = await Build();
    results["fixture"] = fixture;

    switch (command)
    {
        case "lifecycle":
        {
            var p = fixture[0];
            var steps = new List<object>();
            steps.Add(await Run("allocate20", async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", Guid.NewGuid())));
            steps.Add(await Snap("after allocate20", p.OfferId));
            var h1 = (Dictionary<string, object?>)await Reserve(p.StockItemId, 5m, "hold1");
            steps.Add(h1);
            steps.Add(await Snap("after hold5", p.OfferId));
            var dir1 = DirMain();
            await dir1.ReleaseAsync((Guid)h1["reservationId"]!, default);
            steps.Add(await Snap("after release", p.OfferId));
            var h2 = (Dictionary<string, object?>)await Reserve(p.StockItemId, 5m, "hold2");
            steps.Add(h2);
            var dir2 = DirMain();
            await dir2.ConsumeAsync((Guid)h2["reservationId"]!, "p1c-f-consumer-" + cfg.RunId, default);
            steps.Add(await Snap("after consume", p.OfferId));
            var dir3 = DirMain();
            await dir3.ConsumeAsync((Guid)h2["reservationId"]!, "p1c-f-consumer-" + cfg.RunId, default);
            steps.Add(await Snap("after consume replay", p.OfferId));
            var current = await Snap("revision", p.OfferId);
            var alloc = ((System.Collections.IEnumerable)current["allocations"]!).Cast<object>().First();
            var rev = (string)alloc.GetType().GetProperty("Revision")!.GetValue(alloc)!;
            steps.Add(await Run("stale-allocation", async () => await SetAlloc(p.OfferId, p.SellerId, 15m, "stale", Guid.NewGuid())));
            steps.Add(await Snap("after stale allocation", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "limits":
        {
            var p = fixture[0];
            var steps = new List<object>();
            steps.Add(await Run("allocate20", async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", Guid.NewGuid())));
            steps.Add(await Snap("after allocate20", p.OfferId));
            steps.Add(await Run("hold21", async () => await Reserve(p.StockItemId, 21m, "hold21")));
            steps.Add(await Snap("after denied hold21", p.OfferId));
            var snap = await Snap("revision", p.OfferId);
            var alloc = ((System.Collections.IEnumerable)snap["allocations"]!).Cast<object>().First();
            var rev = (string)alloc.GetType().GetProperty("Revision")!.GetValue(alloc)!;
            steps.Add(await Run("allocate101", async () => await SetAlloc(p.OfferId, p.SellerId, 101m, rev, Guid.NewGuid())));
            steps.Add(await Snap("after denied allocation101", p.OfferId));
            var snap2 = await Snap("revision2", p.OfferId);
            var alloc2 = ((System.Collections.IEnumerable)snap2["allocations"]!).Cast<object>().First();
            var rev2 = (string)alloc2.GetType().GetProperty("Revision")!.GetValue(alloc2)!;
            steps.Add(await Run("allocate0", async () => await SetAlloc(p.OfferId, p.SellerId, 0m, rev2, Guid.NewGuid())));
            steps.Add(await Snap("after allocate0", p.OfferId));
            steps.Add(await Run("hold1-after-zero", async () => await Reserve(p.StockItemId, 1m, "hold1-after-zero")));
            steps.Add(await Snap("after denied hold1", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "race-holds":
        {
            var ps = fixture.Where(x => x.OfferKey == "A").ToArray();
            var steps = new List<object>();
            steps.Add(await Run("allocate20", async () => await SetAlloc(ps[0].OfferId, ps[0].SellerId, 20m, "absent", Guid.NewGuid())));
            var t1 = Task.Run(async () => await Reserve(ps[0].StockItemId, 15m, "race1"));
            var t2 = Task.Run(async () => await Reserve(ps[1].StockItemId, 15m, "race2"));
            var r = await Task.WhenAll(t1, t2);
            steps.Add(r);
            steps.Add(await Snap("after concurrent holds", ps[0].OfferId));
            var snap = await Snap("revision", ps[0].OfferId);
            var alloc = ((System.Collections.IEnumerable)snap["allocations"]!).Cast<object>().First();
            var rev = (string)alloc.GetType().GetProperty("Revision")!.GetValue(alloc)!;
            steps.Add(await Run("reduce-below-held", async () => await SetAlloc(ps[0].OfferId, ps[0].SellerId, 10m, rev, Guid.NewGuid())));
            steps.Add(await Snap("after reduce", ps[0].OfferId));
            results["steps"] = steps;
            break;
        }
        case "race-release-consume":
        {
            var p = fixture[0];
            var steps = new List<object>();
            steps.Add(await Run("allocate20", async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", Guid.NewGuid())));
            var hold = (Dictionary<string, object?>)await Reserve(p.StockItemId, 5m, "partial-hold");
            steps.Add(hold);
            var rid = (Guid)hold["reservationId"]!;
            var release = Task.Run(async () => { try { var d = DirMain(); await d.ReleaseAsync(rid, default); return new Dictionary<string, object?> { ["op"] = "release", ["ok"] = true }; } catch (ContractOperationException ex) { return new Dictionary<string, object?> { ["op"] = "release", ["ok"] = false, ["code"] = ex.Code }; } });
            var consume = Task.Run(async () => { try { var d = DirMain(); await d.ConsumeAsync(rid, "p1c-f-race-consume-" + cfg.RunId, default); return new Dictionary<string, object?> { ["op"] = "consume", ["ok"] = true }; } catch (ContractOperationException ex) { return new Dictionary<string, object?> { ["op"] = "consume", ["ok"] = false, ["code"] = ex.Code }; } });
            steps.Add(await Task.WhenAll(release, consume));
            steps.Add(await Snap("after concurrent release/consume", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "partial-release":
        {
            var p = fixture[0];
            var steps = new List<object>();
            steps.Add(await Run("allocate20", async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", Guid.NewGuid())));
            var hold = (Dictionary<string, object?>)await Reserve(p.StockItemId, 5m, "partial-release-hold");
            steps.Add(hold);
            var rid = (Guid)hold["reservationId"]!;
            var releaser = new InventoryReturnObligationRelease(NewDb(), guard, clock, ids);
            var r1 = await releaser.ReleaseUnconsumedHeldAsync(rid, 2m, "p1c-f-partial-a-" + cfg.RunId, default);
            steps.Add(new { phase = "release2", r1.ReservationId, r1.RequestedQuantity, r1.ReleasedQuantity, r1.Status, r1.Outcome });
            steps.Add(await Snap("after release2", p.OfferId));
            var r2 = await releaser.ReleaseUnconsumedHeldAsync(rid, 3m, "p1c-f-partial-b-" + cfg.RunId, default);
            steps.Add(new { phase = "release3", r2.ReservationId, r2.RequestedQuantity, r2.ReleasedQuantity, r2.Status, r2.Outcome });
            steps.Add(await Snap("after release3", p.OfferId));
            var replay = await releaser.ReleaseUnconsumedHeldAsync(rid, 3m, "p1c-f-partial-b-" + cfg.RunId, default);
            steps.Add(new { phase = "replay", replay.ReleasedQuantity, replay.Status, replay.Outcome });
            steps.Add(await Snap("after replay", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "recovery":
        {
            var p = fixture[0];
            var steps = new List<object>();
            steps.Add(await Run("allocate20", async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", Guid.NewGuid())));
            var hold = (Dictionary<string, object?>)await Reserve(p.StockItemId, 5m, "paid-original");
            steps.Add(hold);
            var rid = (Guid)hold["reservationId"]!;
            var d = DirMain();
            await d.CommitReservationForPaidOrderAsync(rid, default);
            var d2 = DirMain();
            await d2.ConsumeAsync(rid, "p1c-f-paid-consumer-" + cfg.RunId, default);
            steps.Add(await Snap("after paid consume", p.OfferId));
            var restock = new InventoryReturnGateway(NewDb(), guard, DirMain(), clock);
            await restock.RestockFromReturnAsync(rid, 1m, "p1c-f-return-" + cfg.RunId, default);
            steps.Add(await Snap("after return restock", p.OfferId));
            await restock.RestockFromReturnAsync(rid, 1m, "p1c-f-return-" + cfg.RunId, default);
            steps.Add(await Snap("after return restock replay", p.OfferId));
            var replacer = DirMain();
            var replacement = await replacer.ReserveForDamagedReplacementAsync(new DamagedReplacementReservationRequest(rid, 1m, "p1c-f-replacement-" + cfg.RunId, "p1c-f-replacement"), default);
            steps.Add(new { phase = "replacement", replacement.ReservationId, replacement.StockItemId, replacement.Quantity, replacement.Status });
            steps.Add(await Snap("after replacement", p.OfferId));
            var replayReplacement = await replacer.ReserveForDamagedReplacementAsync(new DamagedReplacementReservationRequest(rid, 1m, "p1c-f-replacement-" + cfg.RunId, "p1c-f-replacement"), default);
            steps.Add(new { phase = "replacement-replay", replayReplacement.ReservationId, same = replayReplacement.ReservationId == replacement.ReservationId });
            steps.Add(await Snap("after replacement replay", p.OfferId));
            var expired = (Dictionary<string, object?>)await Reserve(p.StockItemId, 3m, "expired-retry", clock.UtcNow.AddSeconds(1));
            steps.Add(expired);
            await Task.Delay(1500);
            var reclaimer = DirMain();
            var released = await reclaimer.ReleaseExpiredHoldsAsync(clock.UtcNow, 10, default);
            steps.Add(new { phase = "expired-release", released });
            var retryDir = DirMain();
            var retry = await retryDir.EnsureOrderSupplyAsync(new Tooba.Inventory.Application.Orders.EnsureOrderSupplyRequest(
                Guid.NewGuid(), Tooba.Inventory.Application.Orders.OrderSupplyMode.EnsureUnpaidRetryHold, true, "p1c-f unpaid retry", null, null,
                [new Tooba.Inventory.Application.Orders.OrderSupplyLineInput(Guid.NewGuid(), p.OfferId, (Guid)expired["reservationId"]!, 3m, "P1C", null)]), default);
            steps.Add(new { phase = "retry", retry.Outcome, retry.Status, bindings = retry.NewBindingsByOrderLineId });
            steps.Add(await Snap("after retry", p.OfferId));
            var d3 = DirMain();
            await d3.ConsumeAsync(rid, "p1c-f-paid-consumer-" + cfg.RunId, default);
            steps.Add(await Snap("after paid consume replay", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "checkout-reuse":
        {
            var a = fixture.First(x => x.OfferKey == "A");
            var b = fixture.First(x => x.OfferKey == "B");
            var steps = new List<object>();
            steps.Add(await Run("allocateA20", async () => await SetAlloc(a.OfferId, a.SellerId, 20m, "absent", Guid.NewGuid())));
            steps.Add(await Run("allocateB20", async () => await SetAlloc(b.OfferId, b.SellerId, 20m, "absent", Guid.NewGuid())));
            var hold = (Dictionary<string, object?>)await Reserve(a.StockItemId, 2m, "checkout-hold");
            steps.Add(hold);
            var rid = (Guid)hold["reservationId"]!;
            var adapter = new CheckoutInventoryReservationAdapter(DirMain(), DirMain());
            var before = await Snap("before wrong offer", a.OfferId);
            steps.Add(before);
            var wrong = await Run("wrong-offer-reuse", async () =>
            {
                await adapter.ReserveForCheckoutAsync(new CheckoutInventoryReservationRequest(
                    Guid.NewGuid(), null, "p1c-f", clock.UtcNow, clock.UtcNow.AddMinutes(30),
                    [new CheckoutInventoryLineRequest(Guid.NewGuid(), b.OfferId, 1m, rid)]), default);
                return new { ok = true };
            });
            steps.Add(wrong);
            steps.Add(await Snap("after wrong offer", a.OfferId));
            var duplicate = await Run("duplicate-reuse", async () =>
            {
                await adapter.ReserveForCheckoutAsync(new CheckoutInventoryReservationRequest(
                    Guid.NewGuid(), null, "p1c-f", clock.UtcNow, clock.UtcNow.AddMinutes(30),
                    [new CheckoutInventoryLineRequest(Guid.NewGuid(), a.OfferId, 1m, rid),
                     new CheckoutInventoryLineRequest(Guid.NewGuid(), a.OfferId, 1m, rid)]), default);
                return new { ok = true };
            });
            steps.Add(duplicate);
            steps.Add(await Snap("after duplicate", a.OfferId));
            var valid = await adapter.ReserveForCheckoutAsync(new CheckoutInventoryReservationRequest(
                Guid.NewGuid(), null, "p1c-f", clock.UtcNow, clock.UtcNow.AddMinutes(30),
                [new CheckoutInventoryLineRequest(Guid.NewGuid(), a.OfferId, 1m, rid)]), default);
            steps.Add(new { phase = "valid-reuse", returned = valid.Lines[0].ReservationId, same = valid.Lines[0].ReservationId == rid });
            steps.Add(await Snap("after valid reuse", a.OfferId));
            results["steps"] = steps;
            break;
        }
        case "availability":
        {
            var a = fixture.First(x => x.OfferKey == "A");
            var dir = DirMain();
            var availability = await ((IInventoryAvailabilityGateway)dir).GetAvailabilityAsync(a.OfferId, default);
            results["availability"] = availability;
            var unassigned = cfg.Offers.FirstOrDefault(x => string.Equals(x.Key, "U", StringComparison.OrdinalIgnoreCase));
            if (unassigned is not null)
                results["unassignedAvailability"] = await ((IInventoryAvailabilityGateway)dir).GetAvailabilityAsync(unassigned.OfferId, default);
            break;
        }
        case "fault":
        {
            var p = fixture[0];
            var steps = new List<object>();
            var preOp = Guid.NewGuid();
            var actor = Guid.Parse(cfg.AdminActor ?? throw new InvalidOperationException("admin actor required"));
            var preInterceptor = new RejectOwnerCommit();
            var pre = await Run("precommit-fault", async () =>
            {
                await using var db = FaultDb(preInterceptor);
                var outcome = await FaultDirectory(db).SetAllocationAsync(new SetSellerOfferAllocation(p.OfferId, p.SellerId, 20m, "absent", actor, preOp), default);
                return new { outcome.IsSuccess };
            });
            steps.Add(pre);
            steps.Add(new { phase = "precommit-injection", hit = preInterceptor.Hit });
            steps.Add(await Snap("after precommit", p.OfferId));
            var postOp = Guid.NewGuid();
            var request = new SetSellerOfferAllocation(p.OfferId, p.SellerId, 20m, "absent", actor, postOp);
            var committed = await DirMain().SetAllocationAsync(request, default);
            steps.Add(new { phase = "postcommit-real", ok = committed.IsSuccess, code = committed.IsSuccess ? null : committed.Errors.FirstOrDefault()?.Code, operationId = postOp, actor });
            var projectionInterceptor = new RejectHistoryRead();
            var post = await Run("postcommit-projection-fault", async () =>
            {
                await using var db = FaultDb(projectionInterceptor);
                var facts = await ((IOfferInventoryHistoryReader)FaultDirectory(db)).ListAsync(p.OfferId, default);
                return new { count = facts.Count };
            });
            steps.Add(post);
            steps.Add(new { phase = "projection-injection", hit = projectionInterceptor.Hit });
            steps.Add(await Snap("after postcommit fault", p.OfferId));
            var replay = await Run("replay", async () =>
            {
                var outcome = await DirMain().SetAllocationAsync(request, default);
                return new { outcome.IsSuccess, code = outcome.IsSuccess ? null : outcome.Errors.FirstOrDefault()?.Code };
            });
            steps.Add(replay);
            steps.Add(await Snap("after replay", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "race-allocation":
        {
            var p = fixture[0];
            var steps = new List<object>();
            var op1 = Guid.NewGuid(); var op2 = Guid.NewGuid();
            var t1 = Task.Run(async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", op1));
            var t2 = Task.Run(async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", op2));
            steps.Add(await Task.WhenAll(t1, t2));
            var snap1 = await Snap("after first allocation race", p.OfferId);
            steps.Add(snap1);
            var alloc = ((System.Collections.IEnumerable)snap1["allocations"]!).Cast<object>().First();
            var rev = (string)alloc.GetType().GetProperty("Revision")!.GetValue(alloc)!;
            var winOp1 = Guid.NewGuid(); var winOp2 = Guid.NewGuid();
            var u1 = Task.Run(async () => await SetAlloc(p.OfferId, p.SellerId, 31m, rev, winOp1));
            var u2 = Task.Run(async () => await SetAlloc(p.OfferId, p.SellerId, 32m, rev, winOp2));
            steps.Add(await Task.WhenAll(u1, u2));
            steps.Add(await Snap("after mutation race", p.OfferId));
            var missing = await Run("missing-revision", async () => await SetAlloc(p.OfferId, p.SellerId, 25m, "", Guid.NewGuid()));
            steps.Add(missing);
            steps.Add(await Snap("after missing revision", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "single-edition":
        {
            var p = fixture[0];
            var steps = new List<object>();
            var dir = DirMain();
            var availability = await ((IInventoryAvailabilityGateway)dir).GetAvailabilityAsync(p.OfferId, default);
            steps.Add(new { phase = "single-availability", availability });
            steps.Add(await Run("single-allocation-denied", async () => await SetAlloc(p.OfferId, p.SellerId, 20m, "absent", Guid.NewGuid())));
            steps.Add(await Snap("after single allocation attempt", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "ownership":
        {
            var a = fixture.First(x => x.OfferKey == "A");
            var b = fixture.First(x => x.OfferKey == "B");
            var steps = new List<object>();
            var provisioning = new InventoryProvisioningDirectory(NewDb(), ids, clock, new FixedCommerce(false), offers);
            steps.Add(new { phase = "eligible", eligible = await provisioning.ListEligibleLocationsAsync(a.SellerId, default) });
            var own = await provisioning.EnsurePositionAsync(a.OfferId, a.VariantId, a.LocationId, a.SellerId, default);
            steps.Add(new { phase = "own-provision", stockItemId = own.StockItemId, same = own.StockItemId == a.StockItemId });
            steps.Add(await Run("foreign-provision", async () =>
            {
                await provisioning.EnsurePositionAsync(a.OfferId, a.VariantId, b.LocationId, a.SellerId, default);
                return new { ok = true };
            }));
            if (cfg.RetiredLocationId is Guid retired)
                steps.Add(await Run("retired-provision", async () =>
                {
                    await provisioning.EnsurePositionAsync(a.OfferId, a.VariantId, retired, a.SellerId, default);
                    return new { ok = true };
                }));
            steps.Add(await Run("unknown-provision", async () =>
            {
                await provisioning.EnsurePositionAsync(a.OfferId, a.VariantId, Guid.NewGuid(), a.SellerId, default);
                return new { ok = true };
            }));
            var snap = await Snap("before own write", a.OfferId);
            var pos = ((System.Collections.IEnumerable)snap["positions"]!).Cast<object>().First();
            var rev = (string)pos.GetType().GetProperty("UpdatedAt")!.GetValue(pos)!;
            var ownWrite = await DirMain().SetInventoryAsync(new SetSellerOfferInventory(
                a.OfferId, a.SellerId, 90m, "P1C F own stock", a.StockItemId, rev, Guid.NewGuid(), Guid.NewGuid()), default);
            steps.Add(new { phase = "own-write", ok = ownWrite.IsSuccess, code = ownWrite.IsSuccess ? null : ownWrite.Errors.FirstOrDefault()?.Code });
            var snap2 = await Snap("after own write", a.OfferId);
            steps.Add(snap2);
            var pos2 = ((System.Collections.IEnumerable)snap2["positions"]!).Cast<object>().First();
            var rev2 = (string)pos2.GetType().GetProperty("UpdatedAt")!.GetValue(pos2)!;
            var foreignWrite = await DirMain().SetInventoryAsync(new SetSellerOfferInventory(
                a.OfferId, b.SellerId, 80m, "P1C F foreign stock", a.StockItemId, rev2, Guid.NewGuid(), Guid.NewGuid()), default);
            steps.Add(new { phase = "foreign-write-a-by-b", ok = foreignWrite.IsSuccess, code = foreignWrite.IsSuccess ? null : foreignWrite.Errors.FirstOrDefault()?.Code });
            steps.Add(await Snap("after foreign write", a.OfferId));
            var snapB = await Snap("before cross write", b.OfferId);
            var posB = ((System.Collections.IEnumerable)snapB["positions"]!).Cast<object>().First();
            var revB = (string)posB.GetType().GetProperty("UpdatedAt")!.GetValue(posB)!;
            var crossWrite = await DirMain().SetInventoryAsync(new SetSellerOfferInventory(
                b.OfferId, a.SellerId, 70m, "P1C F cross stock", b.StockItemId, revB, Guid.NewGuid(), Guid.NewGuid()), default);
            steps.Add(new { phase = "foreign-write-b-by-a", ok = crossWrite.IsSuccess, code = crossWrite.IsSuccess ? null : crossWrite.Errors.FirstOrDefault()?.Code });
            steps.Add(await Snap("after cross write B", b.OfferId));
            results["steps"] = steps;
            break;
        }
        case "race-hold-assignment":
        {
            var a = fixture.First(x => x.OfferKey == "A");
            var b = fixture.First(x => x.OfferKey == "B");
            var steps = new List<object>();
            steps.Add(await Run("allocateA20", async () => await SetAlloc(a.OfferId, a.SellerId, 20m, "absent", Guid.NewGuid())));
            var detailA = await HttpGet("/v1/admin/offers/" + a.OfferId);
            var holdTask = Reserve(a.StockItemId, 1m, "race-hold");
            var assignTask = HttpPut("/v1/admin/offers/" + a.OfferId + "/fulfillment",
                new { responsibility = "Seller", shippingTypeId = (Guid?)null, expectedVersion = RevisionOf(detailA.Body) });
            await Task.WhenAll(holdTask, assignTask);
            steps.Add(new { phase = "concurrent", hold = await holdTask, assignment = new { status = assignTask.Result.Status, body = assignTask.Result.Body } });
            steps.Add(await Snap("after concurrent hold/assignment", a.OfferId));

            steps.Add(await Run("allocateB20", async () => await SetAlloc(b.OfferId, b.SellerId, 20m, "absent", Guid.NewGuid())));
            var holdB = (Dictionary<string, object?>)await Reserve(b.StockItemId, 2m, "reused-hold");
            steps.Add(new { phase = "reused-hold", holdB });
            var reused = await DirMain().EnsureOrderSupplyAsync(new Tooba.Inventory.Application.Orders.EnsureOrderSupplyRequest(
                Guid.NewGuid(), Tooba.Inventory.Application.Orders.OrderSupplyMode.EnsureUnpaidRetryHold, false, "p1c-f reused hold", null, null,
                [new Tooba.Inventory.Application.Orders.OrderSupplyLineInput(Guid.NewGuid(), b.OfferId, (Guid)holdB["reservationId"]!, 2m, "P1C", null)]), default);
            steps.Add(new { phase = "reused-hold-acceptance", reused.Outcome, reused.Status, bindings = reused.NewBindingsByOrderLineId });
            var detailB = await HttpGet("/v1/admin/offers/" + b.OfferId);
            var assignB = await HttpPut("/v1/admin/offers/" + b.OfferId + "/fulfillment",
                new { responsibility = "Seller", shippingTypeId = (Guid?)null, expectedVersion = RevisionOf(detailB.Body) });
            steps.Add(new { phase = "reused-hold-assignment", status = assignB.Status, body = assignB.Body });
            steps.Add(await Snap("after reused hold assignment", b.OfferId));
            results["steps"] = steps;
            break;
        }
        case "race-allocation-assignment":
        {
            var p = fixture[0];
            var steps = new List<object>();
            var detail = await HttpGet("/v1/admin/offers/" + p.OfferId);
            var allocTask = SetAlloc(p.OfferId, p.SellerId, 20m, "absent", Guid.NewGuid());
            var assignTask = HttpPut("/v1/admin/offers/" + p.OfferId + "/fulfillment",
                new { responsibility = "Seller", shippingTypeId = (Guid?)null, expectedVersion = RevisionOf(detail.Body) });
            await Task.WhenAll(allocTask, assignTask);
            steps.Add(new { phase = "concurrent", allocation = await allocTask, assignment = new { status = assignTask.Result.Status, body = assignTask.Result.Body } });
            steps.Add(await Snap("after concurrent allocation/assignment", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "stale-owner-physical":
        {
            var p = fixture[0];
            var steps = new List<object>();
            var snap = await Snap("before owner change", p.OfferId);
            var position = ((System.Collections.IEnumerable)snap["positions"]!).Cast<object>().First();
            var revision = (string)position.GetType().GetProperty("UpdatedAt")!.GetValue(position)!;
            var detail = await HttpGet("/v1/admin/offers/" + p.OfferId);
            var assignment = await HttpPut("/v1/admin/offers/" + p.OfferId + "/fulfillment",
                new { responsibility = "Seller", shippingTypeId = (Guid?)null, expectedVersion = RevisionOf(detail.Body) });
            steps.Add(new { phase = "assignment", status = assignment.Status, body = assignment.Body });
            steps.Add(await Snap("after assignment", p.OfferId));
            var stale = await DirMain().SetInventoryAsync(new SetSellerOfferInventory(
                p.OfferId, p.SellerId, 90m, "P1C F stale owner write", p.StockItemId, revision, Guid.NewGuid(), Guid.NewGuid(), true), default);
            steps.Add(new { phase = "stale-owner-physical", ok = stale.IsSuccess, code = stale.IsSuccess ? null : stale.Errors.FirstOrDefault()?.Code });
            steps.Add(await Snap("after stale physical", p.OfferId));
            results["steps"] = steps;
            break;
        }
        case "setup":
        {
            results["steps"] = new[] { await Snap("setup", null) };
            break;
        }
        default:
            throw new InvalidOperationException("unknown command " + command);
    }

    results["ok"] = true;
}
catch (Exception ex)
{
    results["ok"] = false;
    results["error"] = ex.GetType().Name + ": " + ex.Message;
    if (ex.InnerException is not null) results["inner"] = ex.InnerException.Message;
}

Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));

static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

sealed class Cfg
{
    public string Edition { get; set; } = "Marketplace";
    public string RunId { get; set; } = "run";
    public Guid? RetiredLocationId { get; set; }
    public string? AdminActor { get; set; }
    public string? HttpBase { get; set; }
    public List<CfgOffer> Offers { get; set; } = [];
}
sealed class CfgOffer
{
    public string Key { get; set; } = "";
    public Guid OfferId { get; set; }
    public Guid SellerId { get; set; }
    public Guid VariantId { get; set; }
    public string? Responsibility { get; set; }
    public List<CfgPos> Positions { get; set; } = [];
}
sealed class CfgPos
{
    public string OwnerKind { get; set; } = "Marketplace";
    public Guid? SellerOwner { get; set; }
    public decimal Stock { get; set; } = 100m;
    public bool Eligible { get; set; } = true;
}
sealed record PosRef(string OfferKey, Guid OfferId, Guid SellerId, Guid VariantId, Guid LocationId, string Code, Guid StockItemId, decimal Stock, bool Eligible);

sealed class DbOffers(string conn) : Tooba.Offer.Contracts.Ports.IOfferLookupGateway
{
    private async Task<List<OfferReference>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var rows = new List<OfferReference>();
        await using var c = new NpgsqlConnection(conn);
        await c.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT offer_id, catalog_variant_id, seller_party_id, status, channel, seller_sku, fulfillment_responsibility " +
            "FROM offer.offers WHERE offer_id = ANY(@ids)", c);
        cmd.Parameters.AddWithValue("ids", ids.ToArray());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var resp = r.IsDBNull(6) ? (OfferFulfillmentResponsibility?)null
                : Enum.Parse<OfferFulfillmentResponsibility>(r.GetString(6), true);
            rows.Add(new OfferReference(
                r.GetGuid(0), r.GetGuid(1), r.GetGuid(2),
                Enum.Parse<SalesChannel>(r.GetString(4), true),
                Enum.Parse<OfferStatus>(r.GetString(3), true),
                r.IsDBNull(5) ? null : r.GetString(5),
                FulfillmentResponsibility: resp));
        }
        return rows;
    }
    public async Task<OfferReference?> FindOfferAsync(Guid id, CancellationToken ct)
    {
        var rows = await ReadAsync([id], ct);
        return rows.SingleOrDefault();
    }
    public async Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var rows = await ReadAsync(ids, ct);
        return rows.ToDictionary(x => x.OfferId);
    }
    public async Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var result = new Dictionary<Guid, int>();
        await using var c = new NpgsqlConnection(conn);
        await c.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT catalog_variant_id, count(*) FROM offer.offers WHERE catalog_variant_id = ANY(@ids) GROUP BY catalog_variant_id", c);
        cmd.Parameters.AddWithValue("ids", ids.ToArray());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) result[r.GetGuid(0)] = (int)r.GetInt64(1);
        return result;
    }
}
sealed class FakeCatalog(Cfg cfg) : ICatalogVariantLookup
{
    private readonly Dictionary<Guid, CatalogVariantLookupResult> _variants = cfg.Offers
        .GroupBy(x => x.VariantId)
        .ToDictionary(g => g.Key, g => new CatalogVariantLookupResult(g.Key, Guid.NewGuid()));
    public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid id, CancellationToken ct) => Task.FromResult(_variants.GetValueOrDefault(id));
    public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());
    public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(IReadOnlyList<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Guid>>(_variants.Keys.ToArray());
}
sealed class FixedCommerce(bool single) : ICurrentCommerceContext
{
    public CommerceContext? Current { get; } = new(
        new EditionContext(single ? ToobaEdition.SingleStore : ToobaEdition.Marketplace, "p1c-f-harness"),
        single
            ? new TenantContext(
                new TenantId("store-alpha"), TenantStatus.Active, new ConnectionReference("p1c-f"),
                "Alpha", "theme-alpha", "IR", "127.0.0.1", "alpha.localhost")
            : null,
        new ConnectionReference("p1c-f"), "p1c-f-harness");
}
// Inject after the real owner SaveChanges and before the actual transaction commit.
sealed class RejectOwnerCommit : DbTransactionInterceptor
{
    public bool Hit { get; private set; }
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
        TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        Hit = true;
        throw new InvalidOperationException("injected-before-owner-commit");
    }
}

// History in Offer is a read projection of durable owner facts, not a second persisted journal.
// Fail the actual database reader before that projection, then retry using an independent context.
sealed class RejectHistoryRead : DbCommandInterceptor
{
    public bool Hit { get; private set; }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("offer_inventory_changes", StringComparison.OrdinalIgnoreCase))
        {
            Hit = true;
            throw new InvalidOperationException("injected-owner-history-read");
        }
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
