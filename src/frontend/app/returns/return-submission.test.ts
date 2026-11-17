/**
 * TB-278 — the customer's durable multi-seller return submission.
 *
 * These cases pin the parts the customer surface must not re-invent:
 *
 *  1. one deliberate act freezes ONE child per selected line (possibly across sellers), each with its own stable
 *     idempotency key and its own exact request bytes, and every child starts unresolved;
 *  2. quantities are validated against the line's OWN unit and the owning authority's remaining ceiling — whole
 *     pieces refuse fractions, a fractional unit accepts its exact decimals, and nothing is rounded or clamped;
 *  3. the act is written AND read back before any send, so an unavailable/lying storage refuses the whole act;
 *  4. an authoritative success is terminal, a body-dictated refusal is a no-effect refusal, and every other refusal
 *     keeps the child unresolved because an earlier send of the same bytes may have written;
 *  5. only the exact current act can be cleared, so a newer act is never discarded by a late answer.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  buildReturnSubmission,
  clearReturnSubmissionIfCurrent,
  hasUnresolvedCustomerReturnSubmission,
  parseReturnSubmission,
  readReturnSubmission,
  recordChildOutcome,
  serializeReturnSubmission,
  storeReturnSubmission,
  submissionChildKey,
  summarizeReturnSubmission,
  SUBMISSION_NO_EFFECT_REFUSALS,
  unresolvedChildren,
  validateSubmissionSelection,
  type ReturnSubmission,
  type SubmissionSelection,
  type SubmissionStorage,
} from "./return-submission.ts";

function fakeStorage(initial: Record<string, string> = {}): SubmissionStorage & { values: Map<string, string> } {
  const values = new Map(Object.entries(initial));
  return {
    values,
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
  };
}

let counter = 0;
function nextKey(): string {
  counter += 1;
  return `k-${counter}`;
}

function selection(overrides: Partial<SubmissionSelection> = {}): SubmissionSelection {
  return {
    sellerOrderId: "so-a",
    orderLineId: "ol-socks",
    productLabel: "جوراب نخی",
    unitLabel: "pcs",
    unitDecimalPlaces: 0,
    quantityUnits: 2_000_000,
    quantityText: "2",
    reason: "خرابی کالا",
    ...overrides,
  };
}

test("one act freezes one child per selected line, across sellers, before any send", () => {
  const act = buildReturnSubmission({
    checkoutId: "co-1",
    destination: "Wallet",
    recordedAt: "2026-11-17T00:00:00Z",
    submissionKey: "act-1",
    newChildKey: nextKey,
    selections: [
      selection({ quantityUnits: 2_000_000, quantityText: "2" }),
      selection({
        sellerOrderId: "so-b",
        orderLineId: "ol-lamp",
        productLabel: "چراغ رومیزی",
        unitDecimalPlaces: 3,
        quantityUnits: 1_250_000,
        quantityText: "1.25",
        reason: "آسیب در حمل",
      }),
    ],
  });

  assert.equal(act.version, 1);
  assert.equal(act.submissionKey, "act-1");
  assert.equal(act.children.length, 2);
  assert.deepEqual(act.children.map((child) => child.state), ["pending", "pending"]);
  assert.deepEqual(
    act.children.map((child) => child.key),
    [submissionChildKey("so-a", "ol-socks", "خرابی کالا"), submissionChildKey("so-b", "ol-lamp", "آسیب در حمل")],
  );
  // Distinct idempotency keys: two lines can never collapse into one case by accident.
  assert.notEqual(act.children[0].idempotencyKey, act.children[1].idempotencyKey);

  // Each body is ONE line and carries the exact submitted quantity and its own seller order.
  const body = JSON.parse(act.children[1].body) as {
    sellerOrderId: string;
    idempotencyKey: string;
    refundDestination: string;
    items: Array<{ orderLineId: string; quantity: number }>;
  };
  assert.equal(body.sellerOrderId, "so-b");
  assert.equal(body.idempotencyKey, act.children[1].idempotencyKey);
  assert.equal(body.refundDestination, "Wallet");
  assert.deepEqual(body.items, [{ orderLineId: "ol-lamp", quantity: 1.25 }]);

  const read = readReturnSubmission(fakeStorageWith(act), "co-1");
  assert.equal(read.kind, "found");
  assert.equal(summarizeReturnSubmission(act).pending, 2);
  assert.equal(hasUnresolvedCustomerReturnSubmission(fakeStorageWith(act), "co-1"), true);
});

function fakeStorageWith(act: ReturnSubmission): SubmissionStorage {
  return fakeStorage({ [`tb278:customer-return-submission:${act.checkoutId}`]: serializeReturnSubmission(act) });
}

test("the line's own unit bounds local quantity validation — nothing is clamped or rounded", () => {
  // 2 of 3 pieces from seller A is lawful.
  assert.deepEqual(validateSubmissionSelection(selection({ quantityUnits: 2_000_000 }), 3_000_000), { ok: true });
  // A whole-piece line refuses a fraction…
  assert.deepEqual(validateSubmissionSelection(selection({ quantityUnits: 500_000 }), 3_000_000), {
    ok: false,
    code: "return.quantity_unit_invalid",
  });
  // …and an amount above the owning ceiling is refused, never reduced silently.
  assert.deepEqual(validateSubmissionSelection(selection({ quantityUnits: 4_000_000 }), 3_000_000), {
    ok: false,
    code: "return.quantity_exceeded",
  });
  // A 3-decimal unit accepts its exact decimals and refuses a finer amount.
  assert.deepEqual(
    validateSubmissionSelection(
      selection({ unitDecimalPlaces: 3, unitLabel: "g", quantityUnits: 1_250_000 }),
      2_000_000,
    ),
    { ok: true },
  );
  assert.deepEqual(
    validateSubmissionSelection(
      selection({ unitDecimalPlaces: 3, unitLabel: "g", quantityUnits: 1_234_500 }),
      2_000_000,
    ),
    { ok: false, code: "return.quantity_unit_invalid" },
  );
  // An unanswered owning precision may not be guessed into six decimals.
  assert.deepEqual(
    validateSubmissionSelection(selection({ unitDecimalPlaces: null, quantityUnits: 1_500_000 }), 2_000_000),
    { ok: false, code: "return.quantity_unit_invalid" },
  );
  // Zero / negative / over-scale is refused before any request.
  assert.deepEqual(validateSubmissionSelection(selection({ quantityUnits: 0 }), 2_000_000), {
    ok: false,
    code: "return.quantity_invalid",
  });
  assert.deepEqual(validateSubmissionSelection(selection({ quantityUnits: 1_000_001 }), 2_000_000), {
    ok: false,
    code: "return.quantity_unit_invalid",
  });
});

test("the act is verified in storage; a lying or unavailable storage refuses to send", () => {
  const act = buildReturnSubmission({
    checkoutId: "co-verify",
    destination: "OriginalPayment",
    submissionKey: "act-verify",
    newChildKey: nextKey,
    selections: [selection()],
  });
  const good = fakeStorage();
  assert.equal(storeReturnSubmission(good, act), true);
  assert.equal(good.getItem("tb278:customer-return-submission:co-verify"), serializeReturnSubmission(act));

  // A storage whose write silently vanishes is NOT durable.
  const lying = fakeStorage();
  const originalSet = lying.setItem.bind(lying);
  lying.setItem = () => undefined;
  assert.equal(storeReturnSubmission(lying, act), false);
  // Restoring the honest writer proves the difference came from the verification, not from the act.
  lying.setItem = originalSet;
  assert.equal(storeReturnSubmission(lying, act), true);

  const throwing = fakeStorage();
  throwing.setItem = () => {
    throw new Error("quota");
  };
  assert.equal(storeReturnSubmission(throwing, act), false);
  assert.equal(storeReturnSubmission(null, act), false);
  assert.equal(readReturnSubmission(null, "co-verify").kind, "unavailable");
});

test("unreadable evidence is an explicit state, never 'nothing pending'", () => {
  const broken = fakeStorage({ "tb278:customer-return-submission:co-x": "{not json" });
  assert.equal(readReturnSubmission(broken, "co-x").kind, "unreadable");
  assert.equal(hasUnresolvedCustomerReturnSubmission(broken, "co-x"), true, "a broken envelope needs attention");
  const none = fakeStorage();
  assert.equal(readReturnSubmission(none, "co-x").kind, "none");
  assert.equal(hasUnresolvedCustomerReturnSubmission(none, "co-x"), false);
  assert.equal(parseReturnSubmission(null), null);
});

test("only a body-dictated refusal is a no-effect refusal; every other refusal stays unresolved", () => {
  // Body/immutable-identity refusals can never coexist with a write of those bytes.
  for (const code of ["return.quantity_invalid", "return.quantity_precision", "return.quantity_unit_invalid"]) {
    assert.equal(SUBMISSION_NO_EFFECT_REFUSALS.has(code), true, code);
  }
  // None of these proves that an earlier send of the SAME bytes did not write.
  for (const code of [
    "return.window_expired",
    "return.quantity_exceeded",
    "return.nothing_returnable",
    "return.idempotency_conflict",
    "return.not_owner",
    "return.receipt.state_invalid",
    "admin.authorization.denied",
  ]) {
    assert.equal(SUBMISSION_NO_EFFECT_REFUSALS.has(code), false, code);
  }
});

test("a child's answer is recorded against the exact act: recorded is terminal, unknown keeps its bytes", () => {
  const act = buildReturnSubmission({
    checkoutId: "co-answers",
    destination: "Wallet",
    submissionKey: "act-answers",
    newChildKey: nextKey,
    selections: [
      selection({ sellerOrderId: "so-a", orderLineId: "ol-1" }),
      selection({ sellerOrderId: "so-b", orderLineId: "ol-2" }),
    ],
  });
  const storage = fakeStorage();
  assert.equal(storeReturnSubmission(storage, act), true);
  const [first, second] = act.children;

  // The first child's answer never arrived: its exact bytes stay for the next attempt.
  const afterUnknown = recordChildOutcome(storage, act.submissionKey, first.key, {
    kind: "unknown",
    refusalCode: "host-unreachable",
  });
  assert.ok(afterUnknown);
  assert.equal(afterUnknown?.children[0].state, "unknown");
  assert.equal(afterUnknown?.children[0].body, first.body);
  assert.equal(afterUnknown?.children[0].refusalCode, "host-unreachable");

  // The second child was stored: it can never be sent again, even if a later answer arrives.
  const afterRecorded = recordChildOutcome(storage, act.submissionKey, second.key, {
    kind: "recorded",
    caseId: "rr-2",
    caseNumber: "RET-2",
  });
  assert.ok(afterRecorded);
  const terminal = recordChildOutcome(storage, act.submissionKey, second.key, {
    kind: "unknown",
    refusalCode: "host-unreachable",
  });
  assert.equal(terminal?.children[1].state, "recorded", "a stored case is never downgraded");
  assert.equal(terminal?.children[1].caseId, "rr-2");

  // Only the unresolved child is retried.
  assert.deepEqual(unresolvedChildren(terminal!).map((child) => child.orderLineId), ["ol-1"]);
  const summary = summarizeReturnSubmission(terminal!);
  assert.deepEqual(
    [summary.total, summary.recorded, summary.unknown, summary.resolved, summary.hasPossiblyWritten],
    [2, 1, 1, false, true],
  );

  // A late answer for a DIFFERENT act is never applied to the current one.
  assert.equal(
    recordChildOutcome(storage, "another-act", first.key, { kind: "recorded", caseId: "rr-x", caseNumber: null }),
    null,
  );
});

test("only the exact current act can be cleared; a newer act survives a late answer", () => {
  const older = buildReturnSubmission({
    checkoutId: "co-clear",
    destination: "Wallet",
    submissionKey: "act-old",
    newChildKey: nextKey,
    selections: [selection({ orderLineId: "ol-old" })],
  });
  const newer = buildReturnSubmission({
    checkoutId: "co-clear",
    destination: "Wallet",
    submissionKey: "act-new",
    newChildKey: nextKey,
    selections: [selection({ orderLineId: "ol-new" })],
  });
  const storage = fakeStorage();
  assert.equal(storeReturnSubmission(storage, older), true);
  assert.equal(clearReturnSubmissionIfCurrent(storage, older), true, "the current act can be cleared");
  assert.equal(readReturnSubmission(storage, "co-clear").kind, "none");

  assert.equal(storeReturnSubmission(storage, newer), true);
  assert.equal(clearReturnSubmissionIfCurrent(storage, older), false, "a stale act never clears a newer one");
  const read = readReturnSubmission(storage, "co-clear");
  assert.equal(read.kind, "found");
  assert.equal(read.kind === "found" ? read.submission.submissionKey : null, "act-new");
});
