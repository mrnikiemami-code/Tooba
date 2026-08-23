import assert from "node:assert/strict";
import test from "node:test";
import { assertInventoryIsOfferScoped, demoProductWorkspace, groupOffersBySeller, mapPublicationReadiness } from "./workspace-model.ts";

test("commercial grouping keeps multiple sellers", () => {
  const grouped = groupOffersBySeller(demoProductWorkspace);
  assert.equal(grouped.size, 2);
  assert.equal(demoProductWorkspace.prices.length, 2);
  assert.notEqual(demoProductWorkspace.prices[0]?.amountExclusiveOfTax, undefined);
});

test("inventory stays offer-scoped", () => {
  assert.equal(assertInventoryIsOfferScoped(demoProductWorkspace), true);
});

test("publication checks are UI readiness", () => {
  assert.deepEqual(mapPublicationReadiness(demoProductWorkspace), []);
});
