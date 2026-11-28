import assert from "node:assert/strict";
import test from "node:test";
import { mapEffective, hasGlobalOwnerCapability } from "./access-control-api";

const grant = (scopeKind: unknown, deniedByCeiling = false) => mapEffective({
  userId: "seller-actor",
  permissions: [{ permissionId: "product.create", scopeKind, deniedByCeiling, scopeResourceId: null }],
});

test("actual Host string enum and numeric legacy owner scopes agree", () => {
  for (const kind of ["GlobalWithinOwner", 1, "1"]) {
    assert.equal(hasGlobalOwnerCapability(grant(kind), "product.create"), true);
  }
});

test("unknown or missing scope never implies global owner permission", () => {
  for (const kind of [undefined, null, "", false, "unknown", "Category", 2, "2", 0]) {
    assert.equal(hasGlobalOwnerCapability(grant(kind), "product.create"), false);
  }
});

test("ceiling denial or a different permission never grants Offer creation", () => {
  assert.equal(hasGlobalOwnerCapability(grant("GlobalWithinOwner", true), "product.create"), false);
  assert.equal(hasGlobalOwnerCapability(grant("GlobalWithinOwner"), "product.edit"), false);
});
