/**
 * TB-270-R3 / TB-270-R4 — the create/recover → plan handoff must land on the parcel that REALLY exists.
 *
 * TB-270-R4 Repair C tightened this from "exact key, else the only remaining candidate" to EXACT identity only:
 *  - a resend requires the exact source parcel AND the exact non-empty reviewed occurrence key recorded on the
 *    child itself; a single leftover `Created` child is NOT proof and must open nothing;
 *  - a replacement requires the reviewed decision to live on the REQUESTED source parcel, carry the reviewed
 *    occurrence key, link one replacement parcel, and that parcel must be the decision's own `Created` child;
 *  - an ambiguous, missing or mismatched identity returns `null` so the operator stays on the truthful cards.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { resolveCreatedParcelId } from "./created-parcel.ts";

function snapshot(shipments: unknown[]) {
  return { shipments };
}

test("resend: the exact reviewed occurrence key identifies the created parcel", () => {
  const data = snapshot([
    { shipmentId: "sh-source", status: "Returned", resendOfShipmentId: null },
    { shipmentId: "sh-child-a", status: "Created", resendOfShipmentId: "sh-source", shippingDispatchKey: "key-a" },
    { shipmentId: "sh-child-b", status: "Created", resendOfShipmentId: "sh-source", shippingDispatchKey: "key-b" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "key-b" }),
    "sh-child-b",
  );
});

test("resend: a duplicate key is ambiguous and resolves to nothing", () => {
  const data = snapshot([
    { shipmentId: "sh-child-a", status: "Created", resendOfShipmentId: "sh-source", shippingDispatchKey: "key-a" },
    { shipmentId: "sh-child-b", status: "Created", resendOfShipmentId: "sh-source", shippingDispatchKey: "key-a" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "key-a" }),
    null,
  );
});

test("resend: one Created child with a DIFFERENT occurrence key is never this operation's parcel", () => {
  const data = snapshot([
    { shipmentId: "sh-dispatched", status: "Dispatched", resendOfShipmentId: "sh-source" },
    { shipmentId: "sh-replacement", status: "Created", resendOfShipmentId: "sh-source", damagedResolutionId: "d-1" },
    // The only still-Created resend child, but it belongs to an EARLIER/other occurrence.
    { shipmentId: "sh-fresh", status: "Created", resendOfShipmentId: "sh-source", shippingDispatchKey: "key-other" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "unmatched" }),
    null,
  );
});

test("resend: a missing occurrence key resolves to nothing, even with exactly one Created child", () => {
  const data = snapshot([
    { shipmentId: "sh-fresh", status: "Created", resendOfShipmentId: "sh-source", shippingDispatchKey: "key-a" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "   " }),
    null,
  );
});

test("resend: the same key on a child of ANOTHER source is not this operation", () => {
  const data = snapshot([
    { shipmentId: "sh-other-source", status: "Returned" },
    { shipmentId: "sh-other-child", status: "Created", resendOfShipmentId: "sh-other-source", shippingDispatchKey: "key-a" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "key-a" }),
    null,
  );
});

test("resend: an already-dispatched exact child is history, not a planning target", () => {
  assert.equal(
    resolveCreatedParcelId(
      snapshot([{
        shipmentId: "sh-dispatched",
        status: "Dispatched",
        resendOfShipmentId: "sh-source",
        shippingDispatchKey: "key-old",
      }]),
      { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "key-old" },
    ),
    null,
  );
});

test("resend: two still-created candidates without a key are ambiguous", () => {
  const data = snapshot([
    { shipmentId: "sh-a", status: "Created", resendOfShipmentId: "sh-source" },
    { shipmentId: "sh-b", status: "Created", resendOfShipmentId: "sh-source" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "" }),
    null,
  );
});

test("replacement: the decision of the requested source, its key and its own Created child are the exact identity", () => {
  const data = snapshot([
    {
      shipmentId: "sh-source",
      status: "Returned",
      damagedResolutions: [
        {
          damagedGoodsResolutionId: "d-1",
          occurrenceKey: "dmg-key",
          outcome: "replacement",
          status: "Reserved",
          replacementShipmentId: "sh-replacement",
        },
      ],
    },
    {
      shipmentId: "sh-replacement",
      status: "Created",
      damagedResolutionId: "d-1",
      resendOfShipmentId: "sh-source",
    },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "replacement", sourceShipmentId: "sh-source", idempotencyKey: "dmg-key" }),
    "sh-replacement",
  );
});

test("replacement: the reviewed key recorded on ANOTHER source's decision never opens this source's child", () => {
  const data = snapshot([
    {
      shipmentId: "sh-source",
      status: "Returned",
      damagedResolutions: [
        {
          damagedGoodsResolutionId: "d-source",
          occurrenceKey: "other-key",
          outcome: "replacement",
          status: "Reserved",
          replacementShipmentId: "sh-source-child",
        },
      ],
    },
    {
      shipmentId: "sh-source-child",
      status: "Created",
      damagedResolutionId: "d-source",
      resendOfShipmentId: "sh-source",
    },
    {
      shipmentId: "sh-other-source",
      status: "Returned",
      damagedResolutions: [
        {
          damagedGoodsResolutionId: "d-other",
          occurrenceKey: "dmg-key",
          outcome: "replacement",
          status: "Reserved",
          replacementShipmentId: "sh-other-child",
        },
      ],
    },
    {
      shipmentId: "sh-other-child",
      status: "Created",
      damagedResolutionId: "d-other",
      resendOfShipmentId: "sh-other-source",
    },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "replacement", sourceShipmentId: "sh-source", idempotencyKey: "dmg-key" }),
    null,
  );
});

test("replacement: two decisions answering one key with different parcels are ambiguous", () => {
  const data = snapshot([
    {
      shipmentId: "sh-source",
      status: "Returned",
      damagedResolutions: [
        {
          damagedGoodsResolutionId: "d-1",
          occurrenceKey: "dmg-key",
          outcome: "replacement",
          status: "Reserved",
          replacementShipmentId: "sh-a",
        },
        {
          damagedGoodsResolutionId: "d-2",
          occurrenceKey: "dmg-key",
          outcome: "replacement",
          status: "Reserved",
          replacementShipmentId: "sh-b",
        },
      ],
    },
    { shipmentId: "sh-a", status: "Created", damagedResolutionId: "d-1", resendOfShipmentId: "sh-source" },
    { shipmentId: "sh-b", status: "Created", damagedResolutionId: "d-2", resendOfShipmentId: "sh-source" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "replacement", sourceShipmentId: "sh-source", idempotencyKey: "dmg-key" }),
    null,
  );
});

test("replacement: a decision that never linked a parcel resolves to nothing", () => {
  const data = snapshot([
    {
      shipmentId: "sh-source",
      status: "Delivered",
      damagedResolutions: [
        {
          damagedGoodsResolutionId: "d-1",
          occurrenceKey: "dmg-key",
          outcome: "replacement",
          status: "Pending",
          replacementShipmentId: null,
        },
      ],
    },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "replacement", sourceShipmentId: "sh-source", idempotencyKey: "dmg-key" }),
    null,
  );
});

test("replacement: a linked id whose parcel is not the decision's Created child resolves to nothing", () => {
  const data = snapshot([
    {
      shipmentId: "sh-source",
      status: "Returned",
      damagedResolutions: [
        {
          damagedGoodsResolutionId: "d-1",
          occurrenceKey: "dmg-key",
          outcome: "replacement",
          status: "Fulfilled",
          replacementShipmentId: "sh-child",
        },
      ],
    },
    // Same id, but funded by a DIFFERENT decision.
    { shipmentId: "sh-child", status: "Created", damagedResolutionId: "d-other", resendOfShipmentId: "sh-source" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "replacement", sourceShipmentId: "sh-source", idempotencyKey: "dmg-key" }),
    null,
  );
});

test("no source identity means no resolution at all", () => {
  assert.equal(
    resolveCreatedParcelId(snapshot([{ shipmentId: "sh-a", status: "Created" }]), {
      kind: "resend",
      sourceShipmentId: "   ",
      idempotencyKey: "key-a",
    }),
    null,
  );
});
