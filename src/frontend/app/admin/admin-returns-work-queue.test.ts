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
  mapAdminReturnWorkCounts,
  mapReturnSnapshot,
  mapReturnWorkQueueList,
  normalizeReturnWorkSelection,
  readReturnWorkSelectionFromUrl,
  writeReturnWorkSelectionToUrl,
} from "../returns/return-api.ts";
import {
  buildReviewIntent,
  clearReviewIntent,
  createMemoryIntentStorage,
  createOccurrenceKey,
  readReviewIntent,
  returnIntentStorageKey,
  storeReviewIntent,
} from "../returns/return-review-intent.ts";
import { filterOperationsForScope } from "./admin-order-operations-scope.ts";
import { formatQuantityDisplay } from "../../lib/quantity-display.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";

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
  for (const action of ["record-receipt", "approve", "reject", "resume-refund", "retry-refund", "settle-physical"]) {
    assert.match(workspace, new RegExp(`id="${action}"`), action);
  }
  // Durable review precedes the send.
  assert.match(workspace, /if \(inFlight\.current\) return;/);
  assert.match(workspace, /storeReviewIntent\(storage, returnRequestId, intent\)/);
  assert.match(workspace, /clearReviewIntent\(storage, returnRequestId, intent\.occurrenceKey\)/);
  // Every action is gated by the server's own capabilities.
  assert.match(workspace, /work\.canRecordReceipt/);
  assert.match(workspace, /work\.canDecide/);
  assert.match(workspace, /work\.canResumeRefund/);
  assert.match(workspace, /work\.canRetryRefund/);
  assert.match(workspace, /work\.canSettlePhysical/);
  // Money and physical facts are separate sections, and the mixed line never claims wholly saleable stock.
  assert.match(workspace, /سالم/);
  assert.match(workspace, /معیوب/);
  assert.match(workspace, /سابقهٔ دریافت‌ها/);
  assert.match(workspace, /تلاش‌های بازگشت وجه/);
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
    ["2:kg", "3:بسته"],
  );
  assert.equal(formatQuantityDisplay(rows[0]!.lines[1]!.quantity), "3");
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

test("durable review intent round-trips through the storage seam", () => {
  const storage = createMemoryIntentStorage();
  const key = returnIntentStorageKey("case-1");
  const intent = buildReviewIntent("receipt", { occurrenceKey: "occ-1", items: [] }, "occ-1");
  storeReviewIntent(storage, "case-1", intent);
  assert.equal(storage.getItem(key), JSON.stringify(intent));
  const stored = readReviewIntent(storage, "case-1");
  assert.equal(stored?.action, "receipt");
  assert.equal(stored?.occurrenceKey, "occ-1");
  assert.equal(stored?.body.occurrenceKey, "occ-1");
});

test("compare-and-clear only removes the exact reviewed occurrence", () => {
  const storage = createMemoryIntentStorage();
  storeReviewIntent(storage, "case-2", buildReviewIntent("retry_refund", {}, "occ-old"));
  // A late answer for an older intent must not erase the newer envelope.
  assert.equal(clearReviewIntent(storage, "case-2", "occ-other"), false);
  assert.notEqual(readReviewIntent(storage, "case-2"), null);
  assert.equal(clearReviewIntent(storage, "case-2", "occ-old"), true);
  assert.equal(readReviewIntent(storage, "case-2"), null);
});

test("a corrupt or nameless envelope is not an intent", () => {
  const storage = createMemoryIntentStorage({ [returnIntentStorageKey("case-3")]: "{not json" });
  assert.equal(readReviewIntent(storage, "case-3"), null);
  const blank = createMemoryIntentStorage({ [returnIntentStorageKey("case-4")]: JSON.stringify({ action: "approve" }) });
  assert.equal(readReviewIntent(blank, "case-4"), null);
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
      },
      { returnItemId: "i2", orderLineId: "l2", quantity: 1 },
    ],
    refundAttempts: [],
  });
  assert.equal(snapshot?.items[0]?.saleableRestockedQuantity, 2);
  // An absent restock fact stays unknown, not "the whole requested quantity is saleable".
  assert.equal(snapshot?.items[1]?.saleableRestockedQuantity, null);
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

test("receipt review never preselects arrival/condition and refuses inexact quantities", () => {
  const source = readFileSync(join(dir, "admin-return-workspace.tsx"), "utf8");
  // Nothing is pre-filled: the draft starts empty and the inspection condition starts unselected.
  assert.match(source, /quantity: "",\s*\n\s*condition: "" as ReceiptConditionChoice,/);
  assert.doesNotMatch(source, /condition: "Usable" as const/);
  assert.match(source, /<option value="" disabled>/);
  // Exactly-representable quantities only; a value needing rounding is refused before any intent is stored.
  assert.ok(source.includes("const EXACT_QUANTITY = /^\\d{1,9}(?:\\.\\d{1,6})?$/;"), "exact 6-decimal quantity guard is missing");
  assert.match(source, /مقدار بیش از این دقت گرد نمی‌شود/);
  assert.match(source, /وضعیت بازرسی \(سالم یا معیوب\) هر قلم واردشده را مشخص کنید/);
  // Quantity inputs and select carry the 44px minimum target.
  assert.match(source, /min-h-11 w-28/);
});
