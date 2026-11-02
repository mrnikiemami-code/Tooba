import assert from "node:assert/strict";
import test from "node:test";
import {
  decodeCapability,
  decodeShipment,
  isCarrierApiGroup,
  normalizeShipmentState,
} from "./order-group-carrier-execution-decode.ts";

test("normalizeShipmentState accepts numeric and string enums", () => {
  assert.equal(normalizeShipmentState(3), "Unknown");
  assert.equal(normalizeShipmentState("Unknown"), "Unknown");
  assert.equal(normalizeShipmentState(1), "Succeeded");
});

test("isCarrierApiGroup ignores methodCode heuristics", () => {
  const row = decodeCapability({
    shippingGroupId: "g1",
    transportMode: "Manual",
    methodCode: "tipax:carrier-standard",
  });
  assert.ok(row);
  assert.equal(isCarrierApiGroup(row), false);
  const api = decodeCapability({ shippingGroupId: "g2", transportMode: "CarrierApi" });
  assert.ok(api);
  assert.equal(isCarrierApiGroup(api), true);
});

test("decodeShipment maps capability flags and state", () => {
  const row = decodeShipment({
    operationId: "op1",
    shippingGroupId: "g1",
    state: 3,
    canTrack: true,
    canCancel: false,
  });
  assert.ok(row);
  assert.equal(row.state, "Unknown");
  assert.equal(row.canTrack, true);
  assert.equal(row.canCancel, false);
});

test("decodeCapability reads configured sender without recipient postal fallback", () => {
  const row = decodeCapability({
    shippingGroupId: "g1",
    transportMode: "CarrierApi",
    postalCode: "9999999999",
    configuredSender: { fullName: "Store", locationCode: "101" },
    canCreate: false,
    blockReasonCodes: ["payment_locked"],
  });
  assert.ok(row);
  assert.equal(row.configuredSender.postalCode, null);
  assert.deepEqual(row.blockReasonCodes, ["payment_locked"]);
});
