import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import {
  RETURN_QUEUE_FILTERS,
  formatRefundLifecycleStatus,
  formatReturnStatus,
  mapReturnWorkQueueList,
} from "../returns/return-api.ts";
import { filterOperationsForScope } from "./admin-order-operations-scope.ts";
import { formatQuantityDisplay } from "../../lib/quantity-display.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("work queue screen uses AppDataGrid, GUID-free columns, separate refund status", () => {
  const source = readFileSync(join(dir, "admin-returns-work-queue-screen.tsx"), "utf8");
  assert.match(source, /AppDataGrid/);
  assert.doesNotMatch(source, /AgGridReact/);
  assert.match(source, /returns-queue/);
  assert.match(source, /admin\/orders\/\$\{row\.checkoutId\}/);
  assert.match(source, /admin-returns-queue-filters/);
  assert.match(source, /formatQuantityDisplay/);
  assert.match(source, /availableActionCodes\.length > 0/);
  assert.doesNotMatch(source, /returnRequestId\.slice/);
  assert.doesNotMatch(source, /checkoutId\.slice/);
  assert.match(source, /وضعیت مرجوعی/);
  assert.match(source, /وضعیت بازگشت وجه/);
});

test("AdminReturnsScreen delegates to work queue screen", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /AdminReturnsWorkQueueScreen/);
  assert.match(screens, /export function AdminReturnsScreen/);
  assert.match(screens, /hideTechnicalIds/);
  assert.match(screens, /تلاش مجدد بازگشت وجه/);
  assert.doesNotMatch(screens, /title="مرجوعی و بازپرداخت"/);
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
      returnReference: "RET-ORD-9",
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
    },
  ]);
  assert.equal(rows.length, 1);
  assert.equal(rows[0]?.orderReference, "ORD-9");
  assert.equal(rows[0]?.returnStatus, "Approved");
  assert.equal(rows[0]?.refundStatus, "pending");
  assert.equal(rows[0]?.quantityRequested, 0.25);
  assert.equal(formatQuantityDisplay(rows[0]!.quantityRequested), "0.25");
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

test("chrome nav uses بازگشت وجه not بازپرداخت", () => {
  const chrome = readFileSync(join(dir, "admin-chrome-messages.ts"), "utf8");
  assert.match(chrome, /مرجوعی‌ها و بازگشت وجه/);
  assert.doesNotMatch(chrome, /بازپرداخت/);
});
