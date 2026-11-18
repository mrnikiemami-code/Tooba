/**
 * TB-279 / TB-279-R1: the customer submission is scoped to ONE authenticated VIEWING customer and one line may be split
 * into two deliberate portions with DIFFERENT reasons — each portion its own child case at the owning directory's grain.
 *
 * TB-279-R1 strengthens the scope: an act with no viewer has NO key at all (no shared fallback), and another viewer can
 * neither read, adopt nor clear it.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  buildReturnSubmission,
  clearReturnSubmissionIfCurrent,
  hasUnresolvedCustomerReturnSubmission,
  readReturnSubmission,
  serializeReturnSubmission,
  storeReturnSubmission,
  submissionStorageKey,
  type SubmissionSelection,
  type SubmissionStorage,
} from "./return-submission.ts";

function memoryStorage(): SubmissionStorage & { dump(): Record<string, string> } {
  const values = new Map<string, string>();
  return {
    get length() {
      return values.size;
    },
    key(index: number) {
      return Array.from(values.keys())[index] ?? null;
    },
    getItem(key: string) {
      return values.get(key) ?? null;
    },
    setItem(key: string, value: string) {
      values.set(key, value);
    },
    removeItem(key: string) {
      values.delete(key);
    },
    dump() {
      return Object.fromEntries(values);
    },
  };
}

const firstPortion: SubmissionSelection = {
  sellerOrderId: "so-1",
  orderLineId: "ol-1",
  productLabel: "کالای نمونه",
  unitLabel: "عدد",
  unitDecimalPlaces: 0,
  quantityUnits: 2_000_000,
  quantityText: "2",
  reason: "آسیب‌دیده",
};

const secondPortion: SubmissionSelection = {
  ...firstPortion,
  quantityUnits: 1_000_000,
  quantityText: "1",
  reason: "سایز اشتباه",
};

test("two portions of ONE line with different reasons freeze two children with distinct stable keys", () => {
  const act = buildReturnSubmission({
    checkoutId: "co-1",
    destination: "OriginalPayment",
    viewerId: "user-1",
    selections: [firstPortion, secondPortion],
    submissionKey: "sub-1",
    recordedAt: "2026-11-18T00:00:00.000Z",
    newChildKey: (() => {
      let sequence = 0;
      return () => `key-${(sequence += 1)}`;
    })(),
  });
  assert.ok(act);

  assert.equal(act.children.length, 2);
  assert.notEqual(act.children[0].key, act.children[1].key);
  assert.equal(act.children[0].orderLineId, act.children[1].orderLineId);
  assert.equal(act.children[0].quantityUnits, 2_000_000);
  assert.equal(act.children[1].quantityUnits, 1_000_000);
  assert.notEqual(act.children[0].idempotencyKey, act.children[1].idempotencyKey);
  assert.match(act.children[1].body, /"orderLineId":"ol-1"/);
});

test("the durable act is scoped to the viewing customer, never to a shared checkout key", () => {
  const storage = memoryStorage();
  const act = buildReturnSubmission({
    checkoutId: "co-1",
    destination: "OriginalPayment",
    viewerId: "user-1",
    selections: [firstPortion],
    submissionKey: "sub-1",
    recordedAt: "2026-11-18T00:00:00.000Z",
    newChildKey: () => "key",
  });
  assert.ok(act);

  assert.ok(storeReturnSubmission(storage, act));
  assert.equal(submissionStorageKey("co-1", "user-1"), "tb279:customer-return-submission:user-1:co-1");
  assert.ok(storage.dump()["tb279:customer-return-submission:user-1:co-1"]);

  // viewer 1 sees its own act…
  assert.equal(readReturnSubmission(storage, "co-1", "user-1").kind, "found");
  // …viewer 2 neither sees nor clears it.
  assert.equal(readReturnSubmission(storage, "co-1", "user-2").kind, "none");
  assert.equal(hasUnresolvedCustomerReturnSubmission(storage, "co-1", "user-2"), false);
  assert.equal(clearReturnSubmissionIfCurrent(storage, act, "user-2"), false);
  assert.equal(readReturnSubmission(storage, "co-1", "user-1").kind, "found");

  // The owning viewer can clear the exact act.
  assert.equal(clearReturnSubmissionIfCurrent(storage, act, "user-1"), true);
  assert.equal(readReturnSubmission(storage, "co-1", "user-1").kind, "none");
});

test("there is no viewerless fallback key: an act without a viewer cannot be built, stored or read", () => {
  const storage = memoryStorage();
  assert.equal(
    buildReturnSubmission({
      checkoutId: "co-2",
      destination: "OriginalPayment",
      viewerId: null,
      selections: [firstPortion],
      submissionKey: "sub-2",
    }),
    null,
  );
  // A stored envelope with no viewer is unreadable evidence: it is never adopted by a named viewer or by a nameless read.
  const act = buildReturnSubmission({
    checkoutId: "co-2",
    destination: "OriginalPayment",
    viewerId: "user-2",
    selections: [firstPortion],
    submissionKey: "sub-2",
    recordedAt: "2026-11-18T00:00:00.000Z",
    newChildKey: () => "key",
  });
  assert.ok(act);
  const raw = JSON.parse(serializeReturnSubmission(act)) as Record<string, unknown>;
  raw.viewerId = null;
  storage.setItem(submissionStorageKey("co-2", "user-2"), JSON.stringify(raw));
  assert.equal(readReturnSubmission(storage, "co-2", "user-2").kind, "unreadable");
  assert.equal(readReturnSubmission(storage, "co-2").kind, "viewer_required");
  // Nothing may be written for a nameless scope either.
  assert.equal(storeReturnSubmission(storage, { ...act, viewerId: "" }), false);
});

test("the viewer scope survives serialization, so a child-outcome write cannot drift into another key", () => {
  const act = buildReturnSubmission({
    checkoutId: "co-3",
    destination: "OriginalPayment",
    viewerId: "user-3",
    selections: [firstPortion],
    submissionKey: "sub-3",
    recordedAt: "2026-11-18T00:00:00.000Z",
    newChildKey: () => "key",
  });
  assert.ok(act);
  const raw = serializeReturnSubmission(act);
  const storage = memoryStorage();
  storage.setItem(submissionStorageKey("co-3", "user-3"), raw);

  const read = readReturnSubmission(storage, "co-3", "user-3");
  assert.equal(read.kind, "found");
  assert.equal(read.kind === "found" ? read.submission.viewerId : null, "user-3");
  assert.ok(storeReturnSubmission(storage, read.kind === "found" ? read.submission : act));
  assert.ok(storage.dump()["tb279:customer-return-submission:user-3:co-3"]);
  assert.equal(storage.dump()["tb279:customer-return-submission:co-3"], undefined);
});
