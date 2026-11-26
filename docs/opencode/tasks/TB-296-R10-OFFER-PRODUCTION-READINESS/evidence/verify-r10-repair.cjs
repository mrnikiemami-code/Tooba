const fs = require("fs");
const path = require("path");

const HOST = "http://127.0.0.1:5088";
const OUT = process.argv[2] || path.join(__dirname, "r10-repair");

async function api(url, init) {
  const response = await fetch(url, init);
  const text = await response.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { status: response.status, body };
}

async function main() {
  fs.mkdirSync(OUT, { recursive: true });
  const result = { startedAt: new Date().toISOString(), checks: {}, errors: [] };
  const check = (name, ok, detail = "") => {
    result.checks[name] = { ok: Boolean(ok), detail };
    if (!ok) result.errors.push(`${name}: ${detail}`);
  };

  const dev = await api(`${HOST}/v1/admin/dev-context`);
  const adminActor = dev.body.actorUserId;
  const contexts = await api(`${HOST}/v1/seller/dev-contexts`);
  const sellerA = contexts.body.actors[0];
  const headers = {
    "X-Tooba-Dev-Actor-User-Id": adminActor,
    "X-Tooba-Seller-Party-Id": sellerA.sellerPartyId,
    "Accept-Language": "fa-IR",
    "Content-Type": "application/json",
  };

  const seedId = "457e375f-ceec-4b66-b81e-3829c4e15893";
  const original = (await api(`${HOST}/v1/admin/offers/${seedId}`, { headers })).body;
  check("R10-REPAIR.seedIdentity", Boolean(original?.offerId === seedId && original.lastUpdatedAt), seedId);

  const beforeList = await api(`${HOST}/v1/admin/offers?page=1&pageSize=100`, { headers });
  const beforeCount = Number(beforeList.body?.totalCount ?? beforeList.body?.items?.length ?? 0);
  const rejected = await api(`${HOST}/v1/admin/offers`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      sellerPartyId: original.sellerPartyId,
      catalogVariantId: original.catalogVariantId,
      sellerSku: `TB296-R10-REJECT-${Date.now()}`,
      status: "Draft",
      channel: "Direct",
      requestId: `r10-reject-${Date.now()}`,
    }),
  });
  const afterList = await api(`${HOST}/v1/admin/offers?page=1&pageSize=100`, { headers });
  const afterCount = Number(afterList.body?.totalCount ?? afterList.body?.items?.length ?? 0);
  check("R10-REPAIR.channelRejectedExactly", rejected.status === 400, String(rejected.status));
  check("R10-REPAIR.channelRejectedZeroWrites", beforeCount === afterCount, JSON.stringify({ beforeCount, afterCount }));

  const stale = await api(`${HOST}/v1/admin/offers/${seedId}/price`, {
    method: "PUT",
    headers,
    body: JSON.stringify({
      amount: Number(original.amount) + 1,
      currency: original.currency || "IRR",
      market: "IR",
      expectedVersion: original.lastUpdatedAt,
    }),
  });
  const staleDetail = await api(`${HOST}/v1/admin/offers/${seedId}`, { headers });
  const staleAgain = await api(`${HOST}/v1/admin/offers/${seedId}/price`, {
    method: "PUT",
    headers,
    body: JSON.stringify({
      amount: Number(original.amount) + 2,
      currency: original.currency || "IRR",
      market: "IR",
      expectedVersion: original.lastUpdatedAt,
    }),
  });
  const afterStale = await api(`${HOST}/v1/admin/offers/${seedId}`, { headers });
  check("R10-REPAIR.sequentialStaleRejected", stale.status === 200 && staleAgain.status === 409, `${stale.status}/${staleAgain.status}`);
  check("R10-REPAIR.sequentialStalePreserved", Number(afterStale.body?.amount) === Number(original.amount) + 1, String(afterStale.body?.amount));

  const concurrentVersion = afterStale.body.lastUpdatedAt;
  const [left, right] = await Promise.all([
    api(`${HOST}/v1/admin/offers/${seedId}/price`, {
      method: "PUT",
      headers,
      body: JSON.stringify({ amount: Number(original.amount) + 10, currency: "IRR", market: "IR", expectedVersion: concurrentVersion }),
    }),
    api(`${HOST}/v1/admin/offers/${seedId}/price`, {
      method: "PUT",
      headers,
      body: JSON.stringify({ amount: Number(original.amount) + 20, currency: "IRR", market: "IR", expectedVersion: concurrentVersion }),
    }),
  ]);
  const concurrent = [left.status, right.status].sort((a, b) => a - b);
  const afterConcurrent = await api(`${HOST}/v1/admin/offers/${seedId}`, { headers });
  const winner = [Number(original.amount) + 10, Number(original.amount) + 20];
  check("R10-REPAIR.concurrentOneWinner", concurrent[0] === 200 && concurrent[1] === 409, JSON.stringify(concurrent));
  check("R10-REPAIR.concurrentWinnerValue", winner.includes(Number(afterConcurrent.body?.amount)), String(afterConcurrent.body?.amount));

  const actorHistory = await api(`${HOST}/v1/admin/offers/${seedId}/history?page=1&pageSize=5`, { headers });
  const priceHistory = (actorHistory.body?.items ?? []).find((item) => item.kind === "PriceChanged");
  check("R10-REPAIR.historyActorRecorded", actorHistory.status === 200 && priceHistory?.actorUserId === adminActor, JSON.stringify({ status: actorHistory.status, actor: priceHistory?.actorUserId }));

  const restore = await api(`${HOST}/v1/admin/offers/${seedId}/price`, {
    method: "PUT",
    headers,
    body: JSON.stringify({
      amount: Number(original.amount),
      currency: original.currency || "IRR",
      market: "IR",
      expectedVersion: afterConcurrent.body.lastUpdatedAt,
    }),
  });
  const restored = await api(`${HOST}/v1/admin/offers/${seedId}`, { headers });
  check("R10-REPAIR.priceRestored", restore.status === 200 && Number(restored.body?.amount) === Number(original.amount), String(restored.body?.amount));

  const beforePositions = (await api(`${HOST}/v1/admin/offers/${seedId}`, { headers })).body.inventoryLocations?.length ?? 0;
  const invalidLocation = await api(`${HOST}/v1/admin/offers/${seedId}/inventory/positions`, {
    method: "POST",
    headers,
    body: JSON.stringify({ locationId: "00000000-0000-4000-8000-000000000000" }),
  });
  const afterPositions = (await api(`${HOST}/v1/admin/offers/${seedId}`, { headers })).body.inventoryLocations?.length ?? 0;
  check("R10-REPAIR.invalidLocationRejected", [400, 403, 404].includes(invalidLocation.status), String(invalidLocation.status));
  check("R10-REPAIR.invalidLocationZeroPosition", beforePositions === afterPositions, JSON.stringify({ beforePositions, afterPositions }));

  const invalidLimits = await api(`${HOST}/v1/admin/offers/${seedId}`, {
    method: "PATCH",
    headers,
    body: JSON.stringify({ minimumOrderQuantity: 3, maximumOrderQuantity: 2, expectedVersion: restored.body.lastUpdatedAt }),
  });
  const afterLimits = (await api(`${HOST}/v1/admin/offers/${seedId}`, { headers })).body;
  check("R10-REPAIR.invalidLimitsRejected", invalidLimits.status === 400, String(invalidLimits.status));
  check("R10-REPAIR.invalidLimitsPreserved", Number(afterLimits.minimumOrderQuantity) === Number(original.minimumOrderQuantity) && Number(afterLimits.maximumOrderQuantity) === Number(original.maximumOrderQuantity), JSON.stringify({ min: afterLimits.minimumOrderQuantity, max: afterLimits.maximumOrderQuantity }));

  const sellerB = contexts.body.actors[1];
  const crossHistory = await api(`${HOST}/v1/seller/offers/${seedId}/history?page=1&pageSize=5`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": sellerB.actorUserId, "X-Tooba-Seller-Party-Id": sellerB.sellerPartyId, "Accept-Language": "fa-IR" },
  });
  check("R10-REPAIR.crossOwnerHistoryDenied", [403, 404].includes(crossHistory.status), String(crossHistory.status));

  const root = path.resolve(process.cwd());
  const workspace = fs.readFileSync(path.join(root, "src/frontend/lib/offer-workspace/offer-workspace.tsx"), "utf8");
  const createApply = fs.readFileSync(path.join(root, "src/frontend/features/offer-create/offer-create-and-apply.ts"), "utf8");
  check("R10-REPAIR.workspaceSize", workspace.split("\n").length <= 800, String(workspace.split("\n").length));
  check("R10-REPAIR.noFlatAdminOfferApi", !fs.existsSync(path.join(root, "src/frontend/app/admin/admin-offer-api.ts")) && fs.existsSync(path.join(root, "src/frontend/app/admin/offers/admin-offer-api.ts")), "feature folder");
  check("R10-REPAIR.noLocalizedNumberInventory", !createApply.includes("Number(position.onHand)") && !createApply.includes("offer-create"), "numeric input");
  const locationDir = fs.readFileSync(path.join(root, "src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryProvisioningDirectory.cs"), "utf8");
  const locationMigration = fs.existsSync(path.join(root, "src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Persistence/Migrations/20261126140000_R10InventoryLocationOwner.cs"));
  check("R10-REPAIR.inventoryOwnerFilterSource", locationDir.includes("x.SellerPartyId == null || x.SellerPartyId == sellerPartyId") && locationMigration, "owner scope migration");
  const channelPolicy = fs.readFileSync(path.join(root, "src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Policies/OfferChannelPolicy.cs"), "utf8");
  check("R10-REPAIR.channelNamedOnlySource", channelPolicy.includes("Enum.GetNames<SalesChannel>") && !channelPolicy.includes("Enum.TryParse<SalesChannel>(configuredChannel"), "named values only");
  const versionGuard = fs.readFileSync(path.join(root, "src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Ports/OfferVersionGuard.cs"), "utf8");
  check("R10-REPAIR.expectedVersionSource", versionGuard.includes("FoundationErrorCodes.ConcurrencyConflict") && versionGuard.includes("UtcTicks / 10"), "version guard");
  const priceCommand = fs.readFileSync(path.join(root, "src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/SetOfferPrice/SetOfferPriceCommand.cs"), "utf8");
  check("R10-REPAIR.historyActorSource", priceCommand.includes("request.ActorUserId") && priceCommand.includes("OfferVersionGuard.Check"), "actor/version");

  const adminLocations = await api(`${HOST}/v1/admin/offers/inventory-locations`, { headers });
  const sellerLocations = await api(`${HOST}/v1/seller/offers/inventory-locations`, { headers: { "X-Tooba-Dev-Actor-User-Id": sellerA.actorUserId, "X-Tooba-Seller-Party-Id": sellerA.sellerPartyId, "Accept-Language": "fa-IR" } });
  check("R10-REPAIR.locationReadsAvailable", adminLocations.status === 200 && sellerLocations.status === 200 && Array.isArray(adminLocations.body) && adminLocations.body.length > 0, JSON.stringify({ admin: adminLocations.status, seller: sellerLocations.status }));

  result.finishedAt = new Date().toISOString();
  result.ok = result.errors.length === 0;
  fs.writeFileSync(path.join(OUT, "verify-repair.json"), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ ok: result.ok, checks: Object.keys(result.checks).length, errors: result.errors }, null, 2));
  if (!result.ok) process.exitCode = 1;
}

main().catch((error) => {
  fs.mkdirSync(OUT, { recursive: true });
  fs.writeFileSync(path.join(OUT, "verify-repair-error.json"), JSON.stringify({ error: String(error) }, null, 2));
  console.error(error);
  process.exitCode = 1;
});
