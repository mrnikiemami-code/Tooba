import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import {
  RETURN_QUEUE_FILTERS,
  RETURN_WORK_STRIPS,
  formatRefundLifecycleStatus,
  formatReturnStatus,
  formatUnitLabel,
  mapAdminReturnWorkCounts,
  mapReturnSnapshot,
  mapReturnWorkQueueList,
  normalizeReturnWorkSelection,
  readReturnWorkSelectionFromUrl,
  writeReturnWorkSelectionToUrl,
} from "../returns/return-api.ts";
import { formatEligibilityLabel } from "../returns/return-eligibility-label.ts";
import {
  buildReturnReviewIntent,
  clearReturnReviewIntentIfCurrent,
  createMemoryIntentStorage,
  createOccurrenceKey,
  discardUnreadableReturnReviewIntent,
  isProvenNoEffectRefusal,
  parseReturnReviewBody,
  readReturnReviewIntent,
  returnIntentStorageKey,
  sameReturnReviewIntent,
  serializeReturnReviewIntent,
  storeReturnReviewIntent,
} from "../returns/return-review-intent.ts";
import { filterOperationsForScope } from "./admin-order-operations-scope.ts";
import { formatQuantityDisplay } from "../../lib/quantity-display.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import { collectReceiptItems, evaluateQuantityEntry } from "../returns/return-receipt-quantities.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("work queue screen is a read-only work list with one reviewed entry per row", () => {
  const source = readFileSync(join(dir, "admin-returns-work-queue-screen.tsx"), "utf8");
  assert.match(source, /AppDataGrid/);
  assert.doesNotMatch(source, /AgGridReact/);
  // One row action: the reviewed case workspace. No kebab menu, no hidden financial mutation.
  assert.match(source, /id: "review-return"/);
  assert.match(source, /\/admin\/returns\/\$\{row\.returnRequestId\}/);
  assert.match(source, /AppGridRowActionsCell/);
  assert.doesNotMatch(source, /adminApproveReturn|adminRetryReturnRefund|approve_return|reject_return/);
  // Work strips replaced the legacy status filters.
  assert.match(source, /RETURN_WORK_STRIPS/);
  assert.match(source, /admin-returns-work-strips/);
  assert.doesNotMatch(source, /RETURN_QUEUE_FILTERS/);
  // Counts come from the server's pre-paging set, never from the fetched page.
  assert.match(source, /queryAdminReturnsWorkQueue/);
  assert.match(source, /counts\[strip\.countKey\]/);
  assert.match(source, /formatQuantityDisplay/);
  assert.match(source, /lineSummary/);
  assert.match(source, /وضعیت مرجوعی/);
  assert.match(source, /وضعیت بازگشت وجه/);
  assert.match(source, /مرحلهٔ کار/);
});

test("work strips are the issued primary set plus a quiet history tab", () => {
  assert.deepEqual(
    RETURN_WORK_STRIPS.map((strip) => strip.id),
    ["open", "receiving", "decision", "refund", "refund_failed", "disposition", "history"],
  );
  assert.equal(RETURN_WORK_STRIPS[0]?.labelFa, "همهٔ کارهای باز");
  assert.equal(RETURN_WORK_STRIPS.at(-1)?.labelFa, "سابقه");
  // Every strip reads a real count key, so a badge is never an invented zero constant.
  for (const strip of RETURN_WORK_STRIPS) {
    assert.equal(typeof strip.countKey, "string");
  }
});

test("AdminReturnsScreen delegates to work queue screen and the detail barrel keeps the workspace", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /AdminReturnsWorkQueueScreen/);
  assert.match(screens, /export function AdminReturnsScreen/);
  // The compatibility re-export is the reviewed case workspace, not the legacy detail card.
  assert.match(screens, /export \{ AdminReturnDetailScreen \} from "\.\/admin-return-workspace"/);
  assert.doesNotMatch(screens, /ReturnDetailCard/);
  assert.doesNotMatch(screens, /تلاش مجدد بازگشت وجه/);
  assert.doesNotMatch(screens, /title="مرجوعی و بازپرداخت"/);
});

test("case workspace is the single action owner and never sends before persisting the intent", () => {
  const workspace = readFileSync(join(dir, "admin-return-workspace.tsx"), "utf8");
  assert.match(workspace, /data-testid="admin-return-workspace"/);
  // TB-280: every published action keeps its stable test id, but they are now produced from ONE ordered plan whose
  // first offered entry is the single prominent next step.
  for (const action of ["record-receipt", "approve", "reject", "start-refund", "resume-refund", "retry-refund", "settle-physical"]) {
    assert.match(workspace, new RegExp(`id: "${action}"`), action);
  }
  assert.match(workspace, /data-testid=\{`admin-return-action-\$\{primaryAction\.id\}`\}/);
  assert.match(workspace, /admin-return-other-actions/);
  // TB-277: approval is a decision only; the first refund is its own published action, never a side effect of approve.
  assert.match(workspace, /work\.canStartRefund/);
  assert.match(workspace, /reviewStartRefund/);
  // Durable review precedes the send, and the answer is inspected BEFORE anything is cleared.
  assert.match(workspace, /if \(inFlight\.current\) return;/);
  assert.match(workspace, /storeReturnReviewIntent\(storage, intent\)/);
  assert.match(workspace, /clearReturnReviewIntentIfCurrent\(storage, intent\)/);
  // TB-276: an unknown outcome keeps the frozen envelope; there is no "ignore the uncertainty" control.
  assert.doesNotMatch(workspace, /نادیده گرفتن/);
  assert.match(workspace, /admin-return-unresolved-intent/);
  // Every action is gated by the server's own capabilities (TB-280 reads them once, in one ordered plan).
  assert.match(workspace, /work\?\.canRecordReceipt === true/);
  assert.match(workspace, /work\?\.canDecide === true/);
  assert.match(workspace, /work\?\.canResumeRefund === true/);
  assert.match(workspace, /work\?\.canRetryRefund === true/);
  assert.match(workspace, /work\?\.canSettlePhysical === true/);
  // Money and physical facts are separate sections, and the mixed line never claims wholly saleable stock.
  assert.match(workspace, /سالم/);
  assert.match(workspace, /معیوب/);
  assert.match(workspace, /دریافت‌های ثبت‌شده/);
  assert.match(workspace, /بازگشت وجه/);
  assert.match(workspace, /min-h-11/);
  // An unknown case is an honest not-found state, never a permanent "loading".
  assert.match(workspace, /admin-return-missing/);
  assert.match(workspace, /result\.status === 404/);
});

test("quick filters cover required FA tabs mapped to backend keys", () => {
  const ids = RETURN_QUEUE_FILTERS.map((x) => x.id);
  for (const id of [
    "all",
    "pending_review",
    "approved",
    "refund_needed",
    "refund_pending",
    "refund_failed",
    "completed",
    "rejected",
  ]) {
    assert.ok(ids.includes(id as (typeof ids)[number]), id);
  }
  assert.equal(RETURN_QUEUE_FILTERS.find((x) => x.id === "pending_review")?.labelFa, "در انتظار بررسی");
  assert.equal(RETURN_QUEUE_FILTERS.find((x) => x.id === "refund_failed")?.labelFa, "بازگشت وجه ناموفق");
});

test("mapReturnWorkQueueList maps work-queue DTO without mixing refund into return status", () => {
  const rows = mapReturnWorkQueueList([
    {
      returnRequestId: "r1",
      sellerOrderId: "so1",
      checkoutId: "c1",
      sellerPartyId: "s1",
      returnReference: "RET-ORD-9-1",
      orderReference: "ORD-9",
      customerDisplayName: "علی",
      sellerDisplayName: "فروشگاه الف",
      productLabel: "زعفران",
      quantityRequested: 0.25,
      unitLabel: "kg",
      returnStatus: "Approved",
      refundStatus: "pending",
      eligibilitySummary: "۶ روز باقی‌مانده · مهلت 2026-09-15",
      createdAt: "2026-09-01T00:00:00Z",
      updatedAt: "2026-09-02T00:00:00Z",
      availableActionCodes: [],
      lines: [
        { productLabel: "زعفران", quantity: 0.25, unitLabel: "kg", condition: "Usable" },
      ],
      workCategory: "refund",
      workBlockerCode: null,
      needsOperator: false,
      pendingRefundAttemptId: "a1",
      failedRefundAttemptId: null,
    },
  ]);
  assert.equal(rows.length, 1);
  assert.equal(rows[0]?.orderReference, "ORD-9");
  assert.equal(rows[0]?.returnStatus, "Approved");
  assert.equal(rows[0]?.refundStatus, "pending");
  assert.equal(rows[0]?.quantityRequested, 0.25);
  assert.equal(rows[0]?.workCategory, "refund");
  assert.equal(rows[0]?.pendingRefundAttemptId, "a1");
  assert.equal(rows[0]?.failedRefundAttemptId, null);
  assert.equal(rows[0]?.lines.length, 1);
  assert.equal(formatQuantityDisplay(rows[0]!.quantityRequested), "0.25");
});

test("work-queue rows keep per-line units instead of summing incompatible ones", () => {
  const rows = mapReturnWorkQueueList([
    {
      returnRequestId: "r2",
      checkoutId: "c2",
      lines: [
        { productLabel: "زعفران", quantity: 2, unitLabel: "kg", condition: "Usable" },
        { productLabel: "چای", quantity: 3, unitLabel: "بسته", condition: "Damaged" },
      ],
      workCategory: "disposition",
    },
  ]);
  assert.equal(rows[0]?.lines.length, 2);
  assert.deepEqual(
    rows[0]!.lines.map((line) => `${line.quantity}:${line.unitLabel}`),
    ["2:کیلوگرم", "3:بسته"],
  );
  assert.equal(formatQuantityDisplay(rows[0]!.lines[1]!.quantity), "3");
  // TB-276: a standard stored unit code is shown in Persian, an unknown code is passed through unchanged.
  assert.equal(formatUnitLabel("pcs"), "عدد");
  assert.equal(formatUnitLabel("crate-12"), "crate-12");
  assert.equal(formatUnitLabel(null), "واحد");
});

test("an eligibility label shows its own deadline in Jalali with Persian digits", () => {
  // The owning server label carries a real Gregorian deadline and Latin digits; the operator sees the SAME instant as
  // a Jalali day and the same counts in Persian digits.
  const labelled = formatEligibilityLabel("5 روز باقی‌مانده · مهلت 2026-11-21");
  assert.ok(!/\d/.test(labelled), `a Latin digit leaked: ${labelled}`);
  assert.ok(!/\d{4}-\d{2}-\d{2}/.test(labelled), `raw ISO day leaked: ${labelled}`);
  assert.ok(labelled.includes("۱۴۰۵"), `expected a Jalali year, got: ${labelled}`);
  assert.ok(labelled.startsWith("۵ روز باقی‌مانده · مهلت "), `label words must survive: ${labelled}`);
  // Two different stored deadlines must remain distinguishable after formatting.
  assert.notEqual(
    formatEligibilityLabel("مهلت 2026-11-21"),
    formatEligibilityLabel("مهلت 2026-11-22"),
  );
  // Absent and unparseable shapes are never invented into a date; a refused day is passed through with Persian digits.
  assert.equal(formatEligibilityLabel(null), "—");
  assert.equal(formatEligibilityLabel("   "), "—");
  assert.equal(formatEligibilityLabel("مهلت تمام شده"), "مهلت تمام شده");
  assert.equal(formatEligibilityLabel("مهلت 2026-13-45"), "مهلت ۲۰۲۶-۱۳-۴۵");
  // An already-Persian label is not double-converted.
  assert.equal(formatEligibilityLabel("۲ روز باقی‌مانده"), "۲ روز باقی‌مانده");
});

test("an absent work stage is never read as an invented open permission", () => {
  const rows = mapReturnWorkQueueList([{ returnRequestId: "r3", checkoutId: "c3" }]);
  assert.equal(rows[0]?.workCategory, "closed");
  assert.equal(rows[0]?.workBlockerCode, null);
  assert.equal(rows[0]?.needsOperator, false);
  assert.equal(rows[0]?.pendingRefundAttemptId, null);
});

test("work counts are read from the server payload and an absent block is unknown, not zero", () => {
  const counts = mapAdminReturnWorkCounts({
    items: [],
    counts: { allOpen: 5, receiving: 2, decision: 1, refund: 1, refundFailed: 0, disposition: 1, history: 3 },
  });
  assert.deepEqual(counts, { allOpen: 5, receiving: 2, decision: 1, refund: 1, refundFailed: 0, disposition: 1, history: 3 });
  assert.equal(mapAdminReturnWorkCounts({ items: [] }), undefined);
});

test("durable review intent round-trips through the storage seam with its exact frozen body", () => {
  const storage = createMemoryIntentStorage();
  const key = returnIntentStorageKey("case-1");
  const intent = buildReturnReviewIntent({
    action: "receipt",
    target: "returns",
    returnRequestId: "case-1",
    occurrenceKey: "occ-1",
    body: { occurrenceKey: "occ-1", items: [{ returnItemId: "i1", quantity: 1, condition: "Usable" }] },
  });
  assert.ok(intent);
  assert.equal(storeReturnReviewIntent(storage, intent), true);
  assert.equal(storage.getItem(key), serializeReturnReviewIntent(intent));
  const stored = readReturnReviewIntent(storage, "case-1");
  assert.equal(stored.kind, "pending");
  if (stored.kind !== "pending") return;
  assert.equal(stored.intent.action, "receipt");
  assert.equal(stored.intent.target, "returns");
  assert.equal(stored.intent.occurrenceKey, "occ-1");
  // The reviewed transport body is the exact frozen string, not a rebuilt object.
  assert.equal(stored.intent.requestBody, intent.requestBody);
  assert.deepEqual(parseReturnReviewBody(stored.intent), JSON.parse(intent.requestBody));
});

test("a storage that silently discards the write is never treated as durable", () => {
  const discarding = {
    getItem: () => null,
    setItem: () => undefined,
    removeItem: () => undefined,
  };
  const intent = buildReturnReviewIntent({
    action: "receipt",
    target: "returns",
    returnRequestId: "case-1b",
    body: { occurrenceKey: "occ-1b", items: [] },
  });
  assert.ok(intent);
  assert.equal(storeReturnReviewIntent(discarding, intent), false);
  assert.equal(readReturnReviewIntent(discarding, "case-1b").kind, "none");
});

test("compare-and-clear needs the exact reviewed command, not just the occurrence", () => {
  const storage = createMemoryIntentStorage();
  const first = buildReturnReviewIntent({
    action: "retry_refund",
    target: "returns",
    returnRequestId: "case-2",
    occurrenceKey: "occ-old",
    body: { expectedFailedAttemptId: "a1" },
  });
  const other = buildReturnReviewIntent({
    action: "retry_refund",
    target: "returns",
    returnRequestId: "case-2",
    occurrenceKey: "occ-old",
    body: { expectedFailedAttemptId: "a2" },
  });
  assert.ok(first && other);
  assert.equal(storeReturnReviewIntent(storage, first), true);
  // Same key, different reviewed body: a late answer for one may never clear the other.
  assert.equal(clearReturnReviewIntentIfCurrent(storage, other), false);
  assert.equal(readReturnReviewIntent(storage, "case-2").kind, "pending");
  // A different occurrence is likewise refused.
  const foreign = { ...first, occurrenceKey: "occ-other" };
  assert.equal(clearReturnReviewIntentIfCurrent(storage, foreign), false);
  assert.equal(clearReturnReviewIntentIfCurrent(storage, first), true);
  assert.equal(readReturnReviewIntent(storage, "case-2").kind, "none");
});

test("a corrupt, foreign or unsupported envelope is unreadable evidence, never a sendable intent", () => {
  const corrupt = createMemoryIntentStorage({ [returnIntentStorageKey("case-3")]: "{not json" });
  assert.equal(readReturnReviewIntent(corrupt, "case-3").kind, "unreadable");

  const nameless = createMemoryIntentStorage({
    [returnIntentStorageKey("case-4")]: JSON.stringify({ version: "v2", action: "approve", target: "order-operation" }),
  });
  assert.equal(readReturnReviewIntent(nameless, "case-4").kind, "unreadable");

  const foreignCase = buildReturnReviewIntent({
    action: "approve",
    target: "order-operation",
    returnRequestId: "case-5",
    routeTargetId: "checkout-1",
    body: { code: "approve_return", returnRequestId: "case-5", idempotencyKey: "k" },
  });
  assert.ok(foreignCase);
  const swapped = createMemoryIntentStorage({ [returnIntentStorageKey("case-6")]: serializeReturnReviewIntent(foreignCase) });
  assert.equal(readReturnReviewIntent(swapped, "case-6").kind, "unreadable");

  // A body that is not the exact bytes it claims to be is corrupt too.
  const tampered = createMemoryIntentStorage({
    [returnIntentStorageKey("case-7")]: JSON.stringify({
      version: "v2",
      action: "approve",
      target: "order-operation",
      returnRequestId: "case-7",
      routeTargetId: "checkout-1",
      occurrenceKey: "occ-7",
      requestBody: "not-json",
      recordedAt: "",
    }),
  });
  assert.equal(readReturnReviewIntent(tampered, "case-7").kind, "unreadable");

  // Only unreadable bytes may be discarded; valid evidence is never removed by this control.
  assert.equal(discardUnreadableReturnReviewIntent(corrupt, "case-3"), true);
  assert.equal(readReturnReviewIntent(corrupt, "case-3").kind, "none");
  const valid = createMemoryIntentStorage();
  assert.ok(foreignCase);
  assert.equal(storeReturnReviewIntent(valid, foreignCase), true);
  assert.equal(discardUnreadableReturnReviewIntent(valid, "case-5"), false);
  assert.equal(readReturnReviewIntent(valid, "case-5").kind, "pending");
});

test("an unaddressable review is refused before any bytes are stored", () => {
  // An order-operation intent without its frozen checkout can never be built.
  assert.equal(
    buildReturnReviewIntent({
      action: "approve",
      target: "order-operation",
      returnRequestId: "case-8",
      body: { code: "approve_return" },
    }),
    null,
  );
  // A Returns-owned intent must not carry a foreign route identity.
  assert.equal(
    buildReturnReviewIntent({
      action: "receipt",
      target: "returns",
      returnRequestId: "case-8",
      routeTargetId: "checkout-1",
      body: { occurrenceKey: "o", items: [] },
    }),
    null,
  );
});

test("same-intent comparison keeps a retried command identical to the reviewed one", () => {
  const intent = buildReturnReviewIntent({
    action: "settle_physical",
    target: "returns",
    returnRequestId: "case-9",
    occurrenceKey: "occ-9",
    body: { kind: "settle_physical_disposition" },
  });
  assert.ok(intent);
  assert.equal(sameReturnReviewIntent(intent, { ...intent }), true);
  assert.equal(sameReturnReviewIntent(intent, { ...intent, requestBody: '{"kind":"other"}' }), false);
});

test("the proven no-effect refusal list is typed codes only", () => {
  // TB-277-R1: only refusals determined SOLELY by the frozen reviewed body prove that no earlier send could have
  // written. A later denial, actor loss, conflict or generic state answer describes THIS retry only.
  assert.equal(isProvenNoEffectRefusal("return.receipt.line_invalid"), true);
  assert.equal(isProvenNoEffectRefusal("return.receipt.occurrence_required"), true);
  assert.equal(isProvenNoEffectRefusal("admin.authorization.denied"), false);
  assert.equal(isProvenNoEffectRefusal("admin.actor.missing"), false);
  assert.equal(isProvenNoEffectRefusal("order.operation.denied"), false);
  assert.equal(isProvenNoEffectRefusal("return.not_owner"), false);
  assert.equal(isProvenNoEffectRefusal("return.receipt.state_invalid"), false);
  assert.equal(isProvenNoEffectRefusal("return.receipt.conflict"), false);
  assert.equal(isProvenNoEffectRefusal("return.stale"), false);
  assert.equal(isProvenNoEffectRefusal("return.already_approved"), false);
  assert.equal(isProvenNoEffectRefusal("return.idempotency_conflict"), false);
  assert.equal(isProvenNoEffectRefusal("return.refund.first_not_lawful"), false);
  assert.equal(isProvenNoEffectRefusal("return.recovery.attempt_mismatch"), false);
  // TB-277-R2: the owning receipt route answers this SAME code for a mis-precision body AND for an owning unit
  // precision the lookup could not answer. The second is lookup availability in another request, never proof that an
  // earlier still-running send of these exact bytes did not commit — so it may not clear a retained envelope.
  assert.equal(isProvenNoEffectRefusal("return.receipt.unit_quantity_invalid"), false);
  // Uncertainty and "cannot send now" must never resolve a retained intent.
  assert.equal(isProvenNoEffectRefusal("host-unreachable"), false);
  assert.equal(isProvenNoEffectRefusal("return.recovery.gateway_unavailable"), false);
  assert.equal(isProvenNoEffectRefusal("return.rejected"), false);
  assert.equal(isProvenNoEffectRefusal(null), false);
});

test("each reviewed occurrence gets its own identity", () => {
  const first = createOccurrenceKey();
  const second = createOccurrenceKey();
  assert.notEqual(first, second);
  assert.ok(first.length >= 8);
});

test("legacy queue tokens normalize once, and the strip round-trips through the URL", () => {
  // Legacy bookmarks land on the stage they actually mean, never on "everything".
  assert.equal(normalizeReturnWorkSelection("refund_needed"), "refund");
  assert.equal(normalizeReturnWorkSelection("pending_review"), "decision");
  assert.equal(normalizeReturnWorkSelection("completed"), "history");
  assert.equal(normalizeReturnWorkSelection("rejected"), "history");
  assert.equal(normalizeReturnWorkSelection(""), "open");
  assert.equal(normalizeReturnWorkSelection("something-unknown"), "open");

  assert.equal(readReturnWorkSelectionFromUrl("?queueFilter=refund_failed"), "refund_failed");
  assert.equal(readReturnWorkSelectionFromUrl("?workCategory=receiving&queueFilter=approved"), "receiving");
  assert.equal(readReturnWorkSelectionFromUrl(""), "open");

  // Writing drops the legacy token, so the old and the new filter can never both travel.
  assert.equal(writeReturnWorkSelectionToUrl("?queueFilter=approved&page=2", "disposition"), "?page=2&workCategory=disposition");
  assert.equal(writeReturnWorkSelectionToUrl("?workCategory=refund&page=2", "open"), "?page=2");
});

test("a case snapshot maps the mixed inspected/restocked facts without inventing them", () => {
  const snapshot = mapReturnSnapshot({
    returnRequestId: "r1",
    checkoutId: "c1",
    status: "Approved",
    currency: "IRR",
    refundAmount: 1000,
    items: [
      {
        returnItemId: "i1",
        orderLineId: "l1",
        quantity: 3,
        dispositionCondition: "Usable",
        saleableRestockedQuantity: 2,
        productLabel: "لنوو اولترابوک",
        unitLabel: "pcs",
      },
      { returnItemId: "i2", orderLineId: "l2", quantity: 1 },
    ],
    refundAttempts: [],
  });
  assert.equal(snapshot?.items[0]?.saleableRestockedQuantity, 2);
  // An absent restock fact stays unknown, not "the whole requested quantity is saleable".
  assert.equal(snapshot?.items[1]?.saleableRestockedQuantity, null);
  // TB-276: per-line product/unit display facts are mapped exactly; an absent one stays unknown, never invented.
  assert.equal(snapshot?.items[0]?.productLabel, "لنوو اولترابوک");
  assert.equal(snapshot?.items[0]?.unitLabel, "pcs");
  assert.equal(snapshot?.items[1]?.productLabel, null);
  assert.equal(snapshot?.items[1]?.unitLabel, null);
  assert.equal(formatUnitLabel(snapshot?.items[0]?.unitLabel), "عدد");
});

test("returns-queue scope keeps only return ops for matching id", () => {
  const actions = [
    { code: "approve_return", returnRequestId: "r1" },
    { code: "retry_refund", returnRequestId: "r2" },
    { code: "mark_processing", fulfillmentId: "f1" },
    { code: "cancel", returnRequestId: null },
  ];
  const filtered = filterOperationsForScope(actions, "returns-queue", null, "r1");
  assert.deepEqual(filtered.map((x) => x.code), ["approve_return"]);
});

test("refund lifecycle labels stay distinct from return status", () => {
  assert.equal(formatReturnStatus("Requested"), "در انتظار بررسی");
  assert.equal(formatReturnStatus("Approved"), "تأیید شده");
  assert.equal(formatRefundLifecycleStatus("none"), "بدون بازگشت وجه");
  assert.equal(formatRefundLifecycleStatus("failed"), "بازگشت وجه ناموفق");
  assert.equal(formatReturnStatus("RefundFailed"), "بازگشت وجه ناموفق");
});

test("admin error map localizes return/refund codes without Bad Request", () => {
  assert.equal(mapAdminErrorMessage("return.expired", "fa"), "مهلت مرجوعی تمام شده است.");
  assert.equal(mapAdminErrorMessage("return.non_returnable", "fa"), "این کالا طبق سیاست سفارش قابل مرجوعی نیست.");
  assert.equal(mapAdminErrorMessage("return.stale", "fa"), "وضعیت مرجوعی تغییر کرده است. صفحه را تازه کنید.");
  assert.equal(mapAdminErrorMessage("refund.already_completed", "fa"), "بازگشت وجه قبلاً تکمیل شده است.");
  assert.ok(!mapAdminErrorMessage("return.expired", "fa").includes("Bad Request"));
});

test("receipt and recovery refusals reach the operator in plain Persian", () => {
  for (const code of [
    "return.receipt.state_invalid",
    "return.receipt.line_invalid",
    "return.receipt.actor_required",
    "return.recovery.receiving_required",
    "return.recovery.evidence_conflict",
    "return.recovery.stock_binding_required",
    "return.recovery.physical_resolution_required",
    "return.recovery.attempt_unbound",
    "return.recovery.attempt_mismatch",
    "return.recovery.reason_required",
    "return.recovery.reason_conflict",
  ]) {
    const message = mapAdminErrorMessage(code, "fa");
    assert.ok(message && !message.includes("Bad Request"), code);
    assert.ok(!/[a-z]+\.[a-z]+/.test(message), `${code} leaked a raw token: ${message}`);
  }
});

test("chrome nav uses بازگشت وجه not بازپرداخت", () => {
  const chrome = readFileSync(join(dir, "admin-chrome-messages.ts"), "utf8");
  assert.match(chrome, /مرجوعی‌ها و بازگشت وجه/);
  assert.doesNotMatch(chrome, /بازپرداخت/);
});

test("receipt review never preselects arrival and bounds each portion by the unit and the shared ceiling", () => {
  const line = {
    returnItemId: "ri-1",
    productLabel: "کالای سفارش",
    unitLabel: "pcs",
    decimalPlaces: 0,
    ceilingUnits: 2_000_000,
    usable: "",
    damaged: "",
  };

  // Nothing is pre-filled: an untouched draft submits nothing at all.
  assert.deepEqual(collectReceiptItems([line]), { items: [], problem: null, sumProblem: null });

  // A whole-piece unit refuses a fraction before anything is stored, and the ceiling is inclusive.
  assert.equal(evaluateQuantityEntry("0.5", 0, 2_000_000).problem, "precision");
  assert.equal(evaluateQuantityEntry("2", 0, 2_000_000).units, 2_000_000);
  assert.equal(evaluateQuantityEntry("3", 0, 2_000_000).problem, "over_ceiling");

  // Usable + damaged share ONE exact ceiling; neither portion may fund the other.
  const over = collectReceiptItems([{ ...line, usable: "2", damaged: "1" }]);
  assert.equal(over.items.length, 0);
  assert.match(over.problem ?? over.sumProblem ?? "", /بیشتر است/);

  // An unanswered owning unit precision is a BLOCKED line, never a six-decimal guess.
  assert.equal(evaluateQuantityEntry("1", null, 2_000_000).problem, "unit_unknown");
  assert.equal(collectReceiptItems([{ ...line, decimalPlaces: null, usable: "1" }]).items.length, 0);

  // The bounded quantity fields carry the 44px minimum target.
  const source = readFileSync(join(dir, "admin-return-workspace.tsx"), "utf8");
  assert.match(source, /min-h-11 w-full/);
});
