/**
 * TB-278/TB-279-R1 — the customer's durable multi-seller return submission.
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
 *  5. only the exact current act can be cleared, so a newer act is never discarded by a late answer;
 *  6. the act is scoped to ONE authenticated viewer and every stored child's exact bytes are re-verified on read, so a
 *     shared session, a foreign viewer or a tampered envelope can never be adopted or replayed.
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
  submissionStorageKey,
  summarizeReturnSubmission,
  SUBMISSION_NO_EFFECT_REFUSALS,
  unresolvedChildren,
  validateSubmissionSelection,
  type ReturnSubmission,
  type SubmissionSelection,
  type SubmissionStorage,
} from "./return-submission.ts";

const VIEWER = "user-278";

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

function buildAct(input: Omit<Parameters<typeof buildReturnSubmission>[0], "viewerId">): ReturnSubmission {
  const act = buildReturnSubmission({ ...input, viewerId: VIEWER });
  assert.ok(act, "the act must be buildable for an authenticated viewer");
  return act;
}

test("one act freezes one child per selected line, across sellers, before any send", () => {
  const act = buildAct({
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

  assert.equal(act.version, 2);
  assert.equal(act.submissionKey, "act-1");
  assert.equal(act.viewerId, VIEWER);
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

  const read = readReturnSubmission(fakeStorageWith(act), act.checkoutId, VIEWER);
  assert.equal(read.kind, "found");
  assert.equal(summarizeReturnSubmission(act).pending, 2);
  assert.equal(hasUnresolvedCustomerReturnSubmission(fakeStorageWith(act), act.checkoutId, VIEWER), true);

  // TB-279-R1: no authenticated viewer means no act and no storage identity at all.
  assert.equal(buildReturnSubmission({ checkoutId: "co-1", destination: "Wallet", selections: [selection()], viewerId: null }), null);
});

function fakeStorageWith(act: ReturnSubmission): SubmissionStorage {
  return fakeStorage({ [submissionStorageKey(act.checkoutId, act.viewerId)]: serializeReturnSubmission(act) });
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
  const act = buildAct({
    checkoutId: "co-verify",
    destination: "OriginalPayment",
    submissionKey: "act-verify",
    newChildKey: nextKey,
    selections: [selection()],
  });
  const good = fakeStorage();
  assert.equal(storeReturnSubmission(good, act), true);
  assert.equal(good.getItem(submissionStorageKey("co-verify", VIEWER)), serializeReturnSubmission(act));

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
  assert.equal(readReturnSubmission(null, "co-verify", VIEWER).kind, "unavailable");
});

test("unreadable evidence is an explicit state, never 'nothing pending'", () => {
  const broken = fakeStorage({ [submissionStorageKey("co-x", VIEWER)]: "{not json" });
  assert.equal(readReturnSubmission(broken, "co-x", VIEWER).kind, "unreadable");
  assert.equal(hasUnresolvedCustomerReturnSubmission(broken, "co-x", VIEWER), true, "a broken envelope needs attention");
  const none = fakeStorage();
  assert.equal(readReturnSubmission(none, "co-x", VIEWER).kind, "none");
  assert.equal(hasUnresolvedCustomerReturnSubmission(none, "co-x", VIEWER), false);
  assert.equal(parseReturnSubmission(null), null);
});

test("the act is viewer-scoped: another customer's act is never read, adopted or cleared", () => {
  const act = buildAct({ checkoutId: "co-scope", destination: "Wallet", submissionKey: "act-scope", selections: [selection()] });
  const storage = fakeStorageWith(act);

  // No viewer: the surface cannot name a scope at all.
  assert.equal(readReturnSubmission(storage, "co-scope").kind, "viewer_required");
  assert.equal(readReturnSubmission(storage, "co-scope", "").kind, "viewer_required");

  // A different viewer reads its OWN (empty) scope, never this act.
  assert.equal(readReturnSubmission(storage, "co-scope", "user-other").kind, "none");
  assert.equal(storeReturnSubmission(storage, { ...act, viewerId: " " }), false);
  // A stale envelope whose own viewer disagrees with its key is unreadable evidence.
  const foreign = fakeStorage({ [submissionStorageKey("co-scope", "user-other")]: serializeReturnSubmission(act) });
  assert.equal(readReturnSubmission(foreign, "co-scope", "user-other").kind, "unreadable");
  assert.equal(clearReturnSubmissionIfCurrent(storage, act, "user-other"), false);
  assert.equal(readReturnSubmission(storage, "co-scope", VIEWER).kind, "found");
});

test("tampered child bytes are refused on read, never replayed", () => {
  const act = buildAct({ checkoutId: "co-tamper", destination: "Wallet", submissionKey: "act-tamper", selections: [selection()] });
  const stored = JSON.parse(serializeReturnSubmission(act)) as ReturnSubmission;

  // A changed quantity in the frozen body no longer matches its own identity.
  const quantityTamper = JSON.parse(serializeReturnSubmission(stored)) as ReturnSubmission;
  quantityTamper.children[0].body = quantityTamper.children[0].body.replace('"quantity":2', '"quantity":3');
  assert.equal(parseReturnSubmission(JSON.stringify(quantityTamper)), null);

  // A foreign line or a changed child key is refused as well.
  const lineTamper = JSON.parse(serializeReturnSubmission(stored)) as ReturnSubmission;
  lineTamper.children[0].orderLineId = "ol-other";
  assert.equal(parseReturnSubmission(JSON.stringify(lineTamper)), null);

  // A `refused` child must carry a body-dictated code; an authority refusal can only stay unknown.
  const forgedRefusal = JSON.parse(serializeReturnSubmission(stored)) as ReturnSubmission;
  forgedRefusal.children[0].state = "refused";
  forgedRefusal.children[0].refusalCode = "return.idempotency_conflict";
  assert.equal(parseReturnSubmission(JSON.stringify(forgedRefusal)), null);

  // A recorded child must name the case it created.
  const missingCase = JSON.parse(serializeReturnSubmission(stored)) as ReturnSubmission;
  missingCase.children[0].state = "recorded";
  missingCase.children[0].caseId = null;
  assert.equal(parseReturnSubmission(JSON.stringify(missingCase)), null);

  // An act recorded without an authenticated viewer is unreadable, never adopted.
  const viewerless = JSON.parse(serializeReturnSubmission(stored)) as Record<string, unknown>;
  viewerless.viewerId = null;
  assert.equal(parseReturnSubmission(JSON.stringify(viewerless)), null);
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
  const act = buildAct({
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
  const scope = { checkoutId: act.checkoutId, viewerId: act.viewerId };
  const [first, second] = act.children;

  // The first child's answer never arrived: its exact bytes stay for the next attempt.
  const afterUnknown = recordChildOutcome(storage, scope, act.submissionKey, first.key, {
    kind: "unknown",
    refusalCode: "host-unreachable",
  });
  assert.ok(afterUnknown);
  assert.equal(afterUnknown?.children[0].state, "unknown");
  assert.equal(afterUnknown?.children[0].body, first.body);
  assert.equal(afterUnknown?.children[0].refusalCode, "host-unreachable");

  // The second child was stored: it can never be sent again, even if a later answer arrives.
  const afterRecorded = recordChildOutcome(storage, scope, act.submissionKey, second.key, {
    kind: "recorded",
    caseId: "rr-2",
    caseNumber: "RET-2",
  });
  assert.ok(afterRecorded);
  const terminal = recordChildOutcome(storage, scope, act.submissionKey, second.key, {
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
    recordChildOutcome(storage, scope, "another-act", first.key, { kind: "recorded", caseId: "rr-x", caseNumber: null }),
    null,
  );
  // An answer from ANOTHER viewer's scope can never touch this act.
  assert.equal(
    recordChildOutcome(storage, { checkoutId: act.checkoutId, viewerId: "user-other" }, act.submissionKey, first.key, {
      kind: "recorded",
      caseId: "rr-y",
      caseNumber: null,
    }),
    null,
  );
});

test("only the exact current act can be cleared; a newer act survives a late answer", () => {
  const older = buildAct({
    checkoutId: "co-clear",
    destination: "Wallet",
    submissionKey: "act-old",
    newChildKey: nextKey,
    selections: [selection({ orderLineId: "ol-old" })],
  });
  const newer = buildAct({
    checkoutId: "co-clear",
    destination: "Wallet",
    submissionKey: "act-new",
    newChildKey: nextKey,
    selections: [selection({ orderLineId: "ol-new" })],
  });
  const storage = fakeStorage();
  assert.equal(storeReturnSubmission(storage, older), true);
  assert.equal(clearReturnSubmissionIfCurrent(storage, older, VIEWER), true, "the current act can be cleared");
  assert.equal(readReturnSubmission(storage, "co-clear", VIEWER).kind, "none");

  assert.equal(storeReturnSubmission(storage, newer), true);
  assert.equal(clearReturnSubmissionIfCurrent(storage, older, VIEWER), false, "a stale act never clears a newer one");
  const read = readReturnSubmission(storage, "co-clear", VIEWER);
  assert.equal(read.kind, "found");
  assert.equal(read.kind === "found" ? read.submission.submissionKey : null, "act-new");

  // A changed stored envelope is not "the exact act": the clear refuses instead of deleting evidence.
  const mutated = JSON.parse(serializeReturnSubmission(newer)) as ReturnSubmission;
  mutated.recordedAt = "2026-11-18T00:00:00Z";
  storage.setItem(submissionStorageKey("co-clear", VIEWER), JSON.stringify(mutated));
  assert.equal(clearReturnSubmissionIfCurrent(storage, newer, VIEWER), false);
  assert.notEqual(readReturnSubmission(storage, "co-clear", VIEWER).kind, "none");
});
