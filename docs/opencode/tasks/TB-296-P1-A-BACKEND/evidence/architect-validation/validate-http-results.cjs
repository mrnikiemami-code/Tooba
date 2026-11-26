const fs = require("fs");
const path = require("path");
const HOST = "http://127.0.0.1:5088";
const OFFER = "28cfd5fe-1c07-47e4-edee-ea66909038ae";
const STOCK = "01a21919-2a8d-7000-9262-06f15f54b608";
const INACTIVE = "d1ea1db4-c2e8-43f3-8e8f-925a788a41ae";
const FOREIGN = "b1daf574-99d0-4316-b006-d65598c4b143";
const RANDOM = "00000000-0000-4000-8000-000000000000";

async function call(url, init = {}) {
  const r = await fetch(HOST + url, init);
  const text = await r.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { status: r.status, body };
}

(async () => {
  const dev = await (await fetch(HOST + "/v1/admin/dev-context")).json();
  const contexts = await (await fetch(HOST + "/v1/seller/dev-contexts")).json();
  const a = contexts.actors[0];
  const admin = { "X-Tooba-Dev-Actor-User-Id": dev.actorUserId, "Accept-Language": "fa-IR", "Content-Type": "application/json" };
  const sellerA = { "X-Tooba-Dev-Actor-User-Id": a.actorUserId, "X-Tooba-Seller-Party-Id": a.sellerPartyId, "Accept-Language": "fa-IR", "Content-Type": "application/json" };
  const checks = {};
  const adminDetail = await call(`/v1/admin/offers/${OFFER}`, { headers: admin });
  checks.adminDetail = { status: adminDetail.status, amount: adminDetail.body.amount, offerRevision: adminDetail.body.offerRevision, priceRevision: adminDetail.body.priceRevision, offeredQuantity: adminDetail.body.offeredQuantity, allocationRevision: adminDetail.body.allocationRevision, capabilities: adminDetail.body.capabilities, positions: adminDetail.body.inventoryLocations };
  const sellerDetail = await call(`/v1/seller/offers/${OFFER}`, { headers: sellerA });
  checks.sellerDetail = { status: sellerDetail.status, capabilities: sellerDetail.body.capabilities, reservation: sellerDetail.body.fulfillmentResponsibility };
  checks.sellerPlatformPhysical = await call(`/v1/seller/offers/${OFFER}/inventory`, { method: "PUT", headers: sellerA, body: JSON.stringify({ onHand: 50, reason: "forbidden", stockItemId: STOCK, expectedVersion: "2026-11-26T18:24:07.2074480+00:00", operationId: "99999999-9999-4999-8999-999999999991" }) });
  checks.missingPhysicalRevision = await call(`/v1/admin/offers/${OFFER}/inventory`, { method: "PUT", headers: admin, body: JSON.stringify({ onHand: 130, reason: "missing", stockItemId: STOCK, operationId: "99999999-9999-4999-8999-999999999992" }) });
  checks.randomLocation = await call(`/v1/admin/offers/${OFFER}/inventory/positions`, { method: "POST", headers: admin, body: JSON.stringify({ locationId: RANDOM }) });
  checks.inactiveLocation = await call(`/v1/admin/offers/${OFFER}/inventory/positions`, { method: "POST", headers: admin, body: JSON.stringify({ locationId: INACTIVE }) });
  checks.foreignSellerLocation = await call(`/v1/seller/offers/${OFFER}/inventory/positions`, { method: "POST", headers: sellerA, body: JSON.stringify({ locationId: FOREIGN }) });
  checks.allocationInSingleStore = await call(`/v1/admin/offers/${OFFER}/offered-quantity`, { method: "PUT", headers: admin, body: JSON.stringify({ quantity: 20, expectedVersion: "absent", operationId: "99999999-9999-4999-8999-999999999993" }) });
  checks.stalePrice = await call(`/v1/admin/offers/${OFFER}/price`, { method: "PUT", headers: admin, body: JSON.stringify({ amount: 1300, currency: "IRR", market: "IR", expectedVersion: "stale", operationId: "99999999-9999-4999-8999-999999999994" }) });
  checks.missingPrice = await call(`/v1/admin/offers/${OFFER}/price`, { method: "PUT", headers: admin, body: JSON.stringify({ amount: 1300, currency: "IRR", market: "IR", operationId: "99999999-9999-4999-8999-999999999995" }) });
  const dup = await call(`/v1/admin/offers`, { method: "POST", headers: admin, body: JSON.stringify({ sellerPartyId: a.sellerPartyId, catalogVariantId: adminDetail.body.catalogVariantId, sellerSku: "P1A-DUP-" + Date.now(), status: "Active", channel: "Marketplace", requestId: "p1a-dup-" + Date.now() }) });
  checks.duplicateActive = { status: dup.status, errorCode: dup.body?.errorCode };
  const pages = [];
  for (let p = 1; p <= Math.ceil(7 / 2); p++) pages.push(await call(`/v1/admin/offers/${OFFER}/history?page=${p}&pageSize=2`, { headers: admin }));
  const ids = pages.flatMap(x => (x.body?.items ?? []).map(i => i.eventId));
  checks.history = { statuses: pages.map(x => x.status), totalCounts: pages.map(x => x.body?.totalCount), uniqueEventIds: new Set(ids).size, count: ids.length, items: pages.flatMap(x => x.body?.items ?? []) };
  const adminLoc = await call(`/v1/admin/offers/inventory-locations?sellerPartyId=${a.sellerPartyId}`, { headers: admin });
  const sellerLoc = await call(`/v1/seller/offers/inventory-locations`, { headers: sellerA });
  checks.locations = { adminStatus: adminLoc.status, adminCodes: Array.isArray(adminLoc.body) ? adminLoc.body.map(x => x.code) : adminLoc.body, sellerStatus: sellerLoc.status, sellerCodes: Array.isArray(sellerLoc.body) ? sellerLoc.body.map(x => x.code) : sellerLoc.body };
  const ok = {
    adminDetail: checks.adminDetail.status === 200 && checks.adminDetail.amount === 1200 && checks.adminDetail.priceRevision !== "absent" && checks.adminDetail.positions?.length === 1 && checks.adminDetail.positions[0].revision,
    sellerCapabilities: checks.sellerDetail.status === 200 && checks.sellerDetail.capabilities?.canEditPhysicalInventory === false && checks.sellerDetail.capabilities?.canAssignFulfillment === false,
    sellerPlatformPhysicalDenied: checks.sellerPlatformPhysical.status === 403,
    missingPhysicalDenied: checks.missingPhysicalRevision.status === 409,
    randomLocationDenied: checks.randomLocation.status === 404,
    inactiveLocationDenied: checks.inactiveLocation.status === 404,
    foreignSellerLocationDenied: checks.foreignSellerLocation.status === 403,
    allocationSingleStoreDenied: checks.allocationInSingleStore.status === 403 && checks.allocationInSingleStore.body?.errorCode === "inventory.location.not_eligible",
    stalePriceDenied: checks.stalePrice.status === 409,
    missingPriceDenied: checks.missingPrice.status === 409,
    duplicateActiveDenied: checks.duplicateActive.status === 409 && checks.duplicateActive.errorCode === "offer.listing.duplicate_active",
    stableHistory: checks.history.uniqueEventIds === checks.history.count && checks.history.count === checks.history.totalCounts[0] && checks.history.totalCounts.every(x => x === checks.history.totalCounts[0]),
    historyIdentity: checks.history.items.some(x => x.locationId === "01a03826-97cb-7000-9b33-5dba87e73980" && x.targetId) && checks.history.items.every(x => x.eventId),
    locationsReturned: checks.locations.adminStatus === 200 && checks.locations.sellerStatus === 200 && checks.locations.adminCodes.length > 0,
  };
  const result = { checkedAt: new Date().toISOString(), offer: OFFER, stock: STOCK, checks, ok, passed: Object.values(ok).filter(Boolean).length, failed: Object.entries(ok).filter(([, v]) => !v).map(([k]) => k) };
  fs.writeFileSync(path.join(__dirname, "http-results.json"), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ passed: result.passed, failed: result.failed, key: checks }, null, 2));
  if (result.failed.length) process.exitCode = 1;
})();
