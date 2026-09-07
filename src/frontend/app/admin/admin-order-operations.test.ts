import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import {
  filterOperationsForScope,
  GRID_EXCLUDED_OPERATION_CODES,
  sellerQuickActionLabels,
  type AdminOrderOperationActionLike,
} from "./admin-order-operations-scope.ts";

const dir = dirname(fileURLToPath(import.meta.url));

function sampleAction(code: string): AdminOrderOperationActionLike & { labelFa: string } {
  return {
    code,
    labelFa: code,
  };
}

test("orders grid keeps View and adds one operations menu", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /id:\s*"view"/);
  assert.match(screens, /label:\s*"مشاهده"/);
  assert.match(screens, /AdminOrderOperationsMenu/);
  assert.match(screens, /admin-order-view-/);
  assert.equal((screens.match(/AdminOrderOperationsMenu/g) ?? []).length >= 1, true);
  assert.doesNotMatch(screens, /mark_processing|لغو سفارش/);
  assert.match(screens, /iconOnly/);
  assert.match(screens, /scope="whole-order"/);
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
  assert.match(detail, /AdminOrderItemsShippingPanel/);
  assert.match(detail, /بخش مالی سفارش/);
  assert.match(detail, /یادداشت داخلی/);
  assert.match(detail, /تاریخچه عملیات/);
  assert.match(detail, /سابقه پرداخت‌ها \/ واریزها/);
});

test("operations menu confirms via Dialog and toasts outcome", () => {
  const menu = readFileSync(join(dir, "admin-order-operations-menu.tsx"), "utf8");
  assert.match(menu, /from "react-toastify"/);
  assert.match(menu, /Dialog/);
  assert.match(menu, /toast\.success/);
  assert.match(menu, /toast\.error/);
  assert.match(menu, /عملیات \$\{action\.labelFa\} انجام شد/);
  assert.doesNotMatch(menu, /window\.confirm\(|window\.prompt\(|window\.alert\(/);
  assert.doesNotMatch(menu, /if \(action\.requiresConfirm\)/);
  assert.match(menu, /iconOnly/);
  assert.match(menu, /filterOperationsForScope/);
  assert.match(menu, /Tooltip/);
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

test("whole-order scope filters fulfillment/return actions from grid", () => {
  const actions = [
    sampleAction("cancel"),
    sampleAction("confirm_deposit"),
    sampleAction("mark_packed"),
    sampleAction("create_shipment"),
    sampleAction("request_return"),
    sampleAction("retry_refund"),
  ];
  const filtered = filterOperationsForScope(actions, "whole-order");
  assert.deepEqual(filtered.map((a) => a.code), ["cancel", "confirm_deposit"]);
  for (const code of GRID_EXCLUDED_OPERATION_CODES) {
    assert.equal(filtered.some((a) => a.code === code), false);
  }
  assert.equal(filterOperationsForScope(actions, "detail").length, actions.length);
});

test("grid trigger is icon-only without repeating عملیات text on button", () => {
  const menu = readFileSync(join(dir, "admin-order-operations-menu.tsx"), "utf8");
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /iconOnly/);
  assert.match(menu, /!iconOnly \? <span>\{label\}<\/span>/);
  assert.match(menu, /aria-label=\{label\}/);
});

test("items-shipping panel selection labels switch by selection state", () => {
  assert.equal(sellerQuickActionLabels(false).createShipment, "ایجاد مرسوله");
  assert.equal(sellerQuickActionLabels(true).createShipment, "ایجاد مرسوله از انتخاب‌شده‌ها");
  assert.equal(sellerQuickActionLabels(false).pack, "بسته‌بندی همه اقلام آماده");
  assert.equal(sellerQuickActionLabels(true).pack, "بسته‌بندی انتخاب‌شده‌ها");
});

test("create shipment modal uses Dialog and never prompt", () => {
  const modal = readFileSync(join(dir, "admin-create-shipment-modal.tsx"), "utf8");
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(modal, /Dialog/);
  assert.match(modal, /admin-create-shipment-modal/);
  assert.doesNotMatch(modal, /window\.prompt\(|window\.confirm\(|window\.alert\(/);
  assert.match(panel, /AdminCreateShipmentModal/);
  assert.match(panel, /admin-order-items-shipping/);
  assert.match(panel, /dir="rtl"/);
});
