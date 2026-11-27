import assert from "node:assert/strict";
import test from "node:test";
import { parseOfferHistory } from "./history-contract.ts";
test("history preserves owner event, target and location; malformed or foreign history fails closed", () => {
  const item = { offerId: "offer-a", occurredAt: "2026-11-27T00:00:00Z", kind: "InventoryChanged", eventId: "e", targetId: "stock", locationId: "location" };
  const page = { items: [item], totalCount: 1, page: 1, pageSize: 20 };
  const parsed = parseOfferHistory(page, "offer-a");
  assert.equal(parsed.items[0]?.eventId, "e");
  assert.equal(parsed.items[0]?.targetId, "stock");
  assert.equal(parsed.items[0]?.locationId, "location");
  assert.throws(() => parseOfferHistory(page, "offer-b"));
  assert.throws(() => parseOfferHistory({}, "offer-a"));
});
