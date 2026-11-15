/**
 * TB-270-R3 — the create/recover → plan handoff must land on the parcel that REALLY exists.
 *
 * Proven here (pure, transport-free):
 *  - a resend is resolved by its exact reviewed occurrence key, or only when exactly one `Created`
 *    candidate of that source exists;
 *  - a damaged-goods replacement is resolved through the decision's own replacement link;
 *  - an ambiguous or absent match returns `null` so the operator is never sent to a parcel that was not
 *    proven to be the one this operation produced.
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

test("resend: an already-dispatched child is never chosen, and a damaged replacement is not a resend", () => {
  const data = snapshot([
    { shipmentId: "sh-dispatched", status: "Dispatched", resendOfShipmentId: "sh-source" },
    { shipmentId: "sh-replacement", status: "Created", resendOfShipmentId: "sh-source", damagedResolutionId: "d-1" },
    { shipmentId: "sh-fresh", status: "Created", resendOfShipmentId: "sh-source" },
  ]);
  assert.equal(
    resolveCreatedParcelId(data, { kind: "resend", sourceShipmentId: "sh-source", idempotencyKey: "unmatched" }),
    "sh-fresh",
  );
  // TB-270-R3: the EXACT recorded occurrence of a completed child is history, not a planning target — an
  // exact replay after dispatch must never open a calendar for a parcel that already left the warehouse.
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

test("replacement: the decision's own replacement link is the exact identity", () => {
  const data = snapshot([
    {
      shipmentId: "sh-source",
      status: "Returned",
      damagedResolutions: [
        {
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

test("replacement: a decision that never linked a parcel resolves to nothing", () => {
  const data = snapshot([
    {
      shipmentId: "sh-source",
      status: "Delivered",
      damagedResolutions: [
        { occurrenceKey: "dmg-key", outcome: "replacement", status: "Pending", replacementShipmentId: null },
      ],
    },
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
