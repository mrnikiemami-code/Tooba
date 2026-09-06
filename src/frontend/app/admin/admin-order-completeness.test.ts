import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminErrorMessage } from "./admin-error-map.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("order detail wires notes, history, and invoice/receipt actions", () => {
  const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");
  const client = readFileSync(join(dir, "admin-order-completeness.ts"), "utf8");
  assert.match(detail, /یادداشت داخلی/);
  assert.match(detail, /تاریخچه عملیات/);
  assert.match(detail, /loadAdminOrderNotes/);
  assert.match(detail, /loadAdminOrderOperationalHistory/);
  assert.match(detail, /admin-order-print-invoice/);
  assert.match(detail, /openAdminOrderHtmlDocument/);
  assert.match(detail, /adminOrderInvoiceUrl/);
  assert.match(detail, /AdminOrderOperationsMenu/);
  assert.match(detail, /سابقه پرداخت‌ها \/ واریزها/);
  assert.match(client, /\/v1\/admin\/orders\/.*\/notes/);
  assert.match(client, /\/operational-history/);
  assert.match(client, /invoice\.html/);
  assert.match(client, /receipt\.html/);
  assert.match(client, /adminHeaders/);
  assert.doesNotMatch(detail, /Bad Request|stack trace|errorCode/);
});

test("maps note/invoice/receipt/history errors without raw codes", () => {
  assert.equal(mapAdminErrorMessage("order.note.invalid", "fa"), "متن یادداشت معتبر نیست.");
  assert.equal(mapAdminErrorMessage("order.invoice.unavailable", "fa"), "فاکتور در دسترس نیست.");
  assert.equal(mapAdminErrorMessage("order.receipt.unavailable", "fa"), "رسید پرداخت در دسترس نیست.");
  assert.equal(mapAdminErrorMessage("order.history.failed", "fa"), "بارگذاری تاریخچه عملیات انجام نشد.");
  assert.ok(!mapAdminErrorMessage("order.note.invalid", "fa").includes("order.note"));
});

test("ops regression — detail still exposes عملیات سفارش menu", () => {
  const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");
  assert.match(detail, /AdminOrderOperationsMenu/);
  assert.match(detail, /عملیات سفارش/);
  assert.match(detail, /admin-order-detail-ops-/);
});
