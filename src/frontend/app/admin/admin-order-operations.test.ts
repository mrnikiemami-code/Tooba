import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminErrorMessage } from "./admin-error-map.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("orders grid keeps View and adds one operations menu", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /id:\s*"view"/);
  assert.match(screens, /label:\s*"مشاهده"/);
  assert.match(screens, /AdminOrderOperationsMenu/);
  assert.match(screens, /admin-order-view-/);
  assert.equal((screens.match(/AdminOrderOperationsMenu/g) ?? []).length >= 1, true);
  assert.doesNotMatch(screens, /mark_processing|لغو سفارش/);
});

test("orders grid order reference is non-navigation text; View is canonical", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  const orderColumnsBlock = screens.slice(
    screens.indexOf("const orderColumns"),
    screens.indexOf("const sellerColumns"),
  );
  assert.match(orderColumnsBlock, /id:\s*"reference"/);
  assert.match(orderColumnsBlock, /truncatedCell\(row\.reference/);
  assert.doesNotMatch(orderColumnsBlock, /href=\{`\/admin\/orders\/\$\{row\.checkoutId\}`\}/);
  assert.doesNotMatch(orderColumnsBlock, /<Link[\s\S]*row\.reference/);
  assert.doesNotMatch(orderColumnsBlock, /maxWidth:/);
  assert.match(screens, /orderRowActions[\s\S]*id:\s*"view"[\s\S]*href:\s*\(row\)\s*=>\s*`\/admin\/orders\/\$\{row\.checkoutId\}`/);
});

test("operations menu uses portal and human empty label", () => {
  const menu = readFileSync(join(dir, "admin-order-operations-menu.tsx"), "utf8");
  assert.match(menu, /createPortal/);
  assert.match(menu, /هیچ عملیاتی مجاز نیست/);
  assert.doesNotMatch(menu, /عملیات مجازی نیست/);
});

test("formatAdminStatus surfaces return/refund human labels", async () => {
  const { formatAdminStatus } = await import("./admin-api.ts");
  assert.equal(formatAdminStatus("ReturnRequested"), "مرجوعی در انتظار بررسی");
  assert.equal(formatAdminStatus("ReturnApproved"), "مرجوعی تأیید شده");
  assert.equal(formatAdminStatus("RefundPending"), "بازگشت وجه در انتظار");
  assert.equal(formatAdminStatus("RefundCompleted"), "بازگشت وجه انجام شد");
  assert.equal(formatAdminStatus("Refunded"), "بازگشت وجه");
  assert.doesNotMatch(formatAdminStatus("Refunded"), /refund/i);
});

test("order detail header exposes عملیات سفارش menu", () => {
  const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");
  assert.match(detail, /AdminOrderOperationsMenu/);
  assert.match(detail, /عملیات سفارش/);
  assert.match(detail, /admin-order-detail-ops-/);
});

test("operations menu is API-driven and hides via returned actions only", () => {
  const menu = readFileSync(join(dir, "admin-order-operations-menu.tsx"), "utf8");
  const client = readFileSync(join(dir, "admin-order-operations.ts"), "utf8");
  assert.match(menu, /loadAdminOrderOperations/);
  assert.match(menu, /executeAdminOrderOperation/);
  assert.match(menu, /actions\.map/);
  assert.match(menu, /pending/);
  assert.match(client, /\/v1\/admin\/orders\/.*\/operations/);
  assert.match(client, /order\.operation\.denied/);
  assert.doesNotMatch(menu, /order\.cancel/);
});

test("maps order operation errors to FA without raw codes", () => {
  assert.equal(mapAdminErrorMessage("order.operation.denied", "fa"), "مجوز انجام این عملیات وجود ندارد.");
  assert.equal(mapAdminErrorMessage("order.operation.invalid", "fa"), "این عملیات در وضعیت فعلی سفارش مجاز نیست.");
  assert.ok(!mapAdminErrorMessage("order.operation.failed", "fa").includes("order.operation"));
  const client = readFileSync(join(dir, "admin-order-operations.ts"), "utf8");
  assert.match(client, /mapOrderOperationError/);
  assert.match(client, /detail\.trim\(\)/);
});
