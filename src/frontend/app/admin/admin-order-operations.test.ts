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
    screens.indexOf("function createOrderColumns"),
    screens.indexOf("const sellerColumns"),
  );
  assert.match(orderColumnsBlock, /id:\s*"reference"/);
  assert.match(orderColumnsBlock, /truncatedCell\(row\.reference/);
  assert.doesNotMatch(orderColumnsBlock, /href=\{`\/admin\/orders\/\$\{row\.checkoutId\}`\}/);
  assert.doesNotMatch(orderColumnsBlock, /<Link[\s\S]*row\.reference/);
  assert.doesNotMatch(orderColumnsBlock, /maxWidth:/);
  assert.match(orderColumnsBlock, /id:\s*"actions"[\s\S]*width:\s*120[\s\S]*minWidth:\s*100/);
  assert.match(screens, /orderRowActions[\s\S]*id:\s*"view"[\s\S]*href:\s*\(row\)\s*=>\s*`\/admin\/orders\/\$\{row\.checkoutId\}`/);
});

test("orders grid exposes filters on all data columns including amount/date/lines", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  const orderColumnsBlock = screens.slice(
    screens.indexOf("function createOrderColumns"),
    screens.indexOf("const sellerColumns"),
  );
  assert.match(orderColumnsBlock, /id:\s*"reference"[\s\S]*filterKind:\s*"text"/);
  assert.match(orderColumnsBlock, /id:\s*"customer"[\s\S]*filterKind:\s*"text"/);
  assert.match(orderColumnsBlock, /id:\s*"sellers"[\s\S]*filterKind:\s*"text"/);
  assert.match(orderColumnsBlock, /id:\s*"lines"[\s\S]*filterKind:\s*"number"/);
  assert.match(orderColumnsBlock, /id:\s*"payment"[\s\S]*filterKind:\s*"status"/);
  assert.match(orderColumnsBlock, /id:\s*"status"[\s\S]*filterKind:\s*"status"/);
  assert.match(orderColumnsBlock, /id:\s*"amount"[\s\S]*filterKind:\s*"money"/);
  assert.match(orderColumnsBlock, /id:\s*"created"[\s\S]*filterKind:\s*"date"/);
  assert.doesNotMatch(orderColumnsBlock, /id:\s*"actions"[\s\S]*filterKind:/);
});

test("orders grid status filter matches composed return/refund labels and has no dead Processing", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  const block = screens.slice(
    screens.indexOf("const orderStatusEnumOptions"),
    screens.indexOf("const orderRowActions"),
  );
  assert.match(block, /ReturnRequested/);
  assert.match(block, /RefundPending/);
  assert.match(block, /RefundFailed/);
  assert.doesNotMatch(block, /value:\s*"Processing"/);
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
    sampleAction("pack_selected"),
    sampleAction("mark_processing"),
    sampleAction("create_shipment"),
    sampleAction("request_return"),
    sampleAction("retry_refund"),
  ];
  const filtered = filterOperationsForScope(actions, "whole-order");
  assert.deepEqual(filtered.map((a) => a.code), ["cancel", "confirm_deposit"]);
  for (const code of GRID_EXCLUDED_OPERATION_CODES) {
    assert.equal(filtered.some((a) => a.code === code), false);
  }
  assert.deepEqual(
    filterOperationsForScope(actions, "detail").map((a) => a.code),
    ["cancel", "confirm_deposit"],
  );
});

test("grid trigger is icon-only without repeating عملیات text on button", () => {
  const menu = readFileSync(join(dir, "admin-order-operations-menu.tsx"), "utf8");
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /iconOnly/);
  assert.match(menu, /!iconOnly \? <span>\{label\}<\/span>/);
  assert.match(menu, /aria-label=\{label\}/);
});

test("formatAdminStatus humanizes fulfillment enums without raw ReadyToFulfill", async () => {
  const { formatAdminStatus } = await import("./admin-api.ts");
  assert.equal(formatAdminStatus("ReadyToFulfill"), "آماده پردازش");
  assert.equal(formatAdminStatus("Packed"), "بسته‌بندی‌شده");
  assert.equal(formatAdminStatus("Dispatched"), "ارسال‌شده");
  assert.equal(formatAdminStatus("PartialDispatched"), "ارسال جزئی");
  assert.notEqual(formatAdminStatus("ReadyToFulfill"), "ReadyToFulfill");
});

test("grid excludes unpack and cancel_shipment from whole-order kebab", () => {
  assert.equal(GRID_EXCLUDED_OPERATION_CODES.has("unpack"), true);
  assert.equal(GRID_EXCLUDED_OPERATION_CODES.has("unprocess"), true);
  assert.equal(GRID_EXCLUDED_OPERATION_CODES.has("cancel_shipment"), true);
  assert.equal(GRID_EXCLUDED_OPERATION_CODES.has("pack_selected"), true);
  assert.equal(GRID_EXCLUDED_OPERATION_CODES.has("mark_processing"), true);
  assert.equal(GRID_EXCLUDED_OPERATION_CODES.has("mark_packed"), true);
  const actions = [
    sampleAction("cancel"),
    sampleAction("unpack"),
    sampleAction("cancel_shipment"),
    sampleAction("confirm_deposit"),
  ];
  const filtered = filterOperationsForScope(actions, "whole-order");
  assert.deepEqual(filtered.map((a) => a.code), ["cancel", "confirm_deposit"]);
});

test("restore cancelled order stays hidden when projection excludes it", () => {
  const filtered = filterOperationsForScope(
    [{ code: "cancel", sellerOrderId: null, labelFa: "لغو سفارش" }],
    "whole-order",
  );
  assert.equal(filtered.some((a) => a.code === "restore_cancelled_order"), false);
});

test("whole-order menu shows one cancel and payment restore", () => {
  const actions = [
    { code: "cancel", sellerOrderId: "a", labelFa: "لغو سفارش" },
    { code: "cancel", sellerOrderId: "b", labelFa: "لغو سفارش" },
    { code: "cancel", sellerOrderId: null, labelFa: "لغو سفارش" },
    { code: "restore_deposit", sellerOrderId: null, labelFa: "برگشت از رد واریز" },
    { code: "unconfirm_deposit", sellerOrderId: null, labelFa: "برگشت از واریز" },
    { code: "restore_cancelled_order", sellerOrderId: null, labelFa: "بازگردانی سفارش لغوشده" },
    { code: "correct_tracking", sellerOrderId: "a", labelFa: "اصلاح کد رهگیری" },
    { code: "cancel_shipment", sellerOrderId: "a", labelFa: "ابطال مرسوله" },
  ];
  const filtered = filterOperationsForScope(actions, "whole-order");
  assert.equal(filtered.filter((a) => a.code === "cancel").length, 1);
  assert.equal(filtered.find((a) => a.code === "cancel")?.sellerOrderId, null);
  assert.deepEqual(
    filtered.map((a) => a.code),
    ["cancel", "restore_deposit", "unconfirm_deposit", "restore_cancelled_order"],
  );
  assert.equal(GRID_EXCLUDED_OPERATION_CODES.has("correct_tracking"), true);
});

test("operations menu uses tracking dialog for correct_tracking and confirm Dialog", () => {
  const menu = readFileSync(join(dir, "admin-order-operations-menu.tsx"), "utf8");
  assert.match(menu, /action\.code === "assign_tracking" \|\| action\.code === "correct_tracking"/);
  assert.match(menu, /اصلاح کد رهگیری/);
  assert.match(menu, /confirmAction\?\.confirmMessageFa/);
});

test("maps corrective action errors to FA", () => {
  assert.equal(
    mapAdminErrorMessage("payment.restore.invalid_state", "fa"),
    "بازگرداندن واریز در این وضعیت مجاز نیست.",
  );
  assert.equal(
    mapAdminErrorMessage("fulfillment.unconfirm.already_started", "fa"),
    "پس از شروع پردازش نمی‌توان واریز را برگرداند.",
  );
  assert.equal(
    mapAdminErrorMessage("payment.unconfirm.invalid_state", "fa"),
    "برگشت از واریز در این وضعیت مجاز نیست.",
  );
  assert.equal(
    mapAdminErrorMessage("order.restore.inventory_failed", "fa"),
    "بازگردانی ممکن نیست؛ موجودی برای رزرو دوباره کافی نیست. سفارش لغوشده باقی ماند.",
  );
  assert.equal(
    mapAdminErrorMessage("order.restore.seller_payout_completed", "fa"),
    "این سفارش به‌دلیل انجام تسویه/واریز سهم فروشنده قابل بازگردانی نیست.",
  );
  assert.ok(!mapAdminErrorMessage("order.restore.seller_payout_completed", "fa").includes("order.restore"));
  assert.ok(!mapAdminErrorMessage("fulfillment.tracking.locked_after_dispatch", "fa").includes("fulfillment.tracking"));
});


test("create shipment modal uses Dialog and never prompt", () => {
  const modal = readFileSync(join(dir, "admin-create-shipment-modal.tsx"), "utf8");
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(modal, /Dialog/);
  assert.match(modal, /admin-create-shipment-modal/);
  assert.match(modal, /selections/);
  assert.doesNotMatch(modal, /window\.prompt\(|window\.confirm\(|window\.alert\(/);
  assert.doesNotMatch(modal, /admin-create-shipment-deferred|به T005 موکول/);
  assert.match(panel, /AdminCreateShipmentModal/);
  assert.match(panel, /admin-order-items-shipping/);
  assert.match(panel, /dir="rtl"/);
});

test("operations client posts selections array", () => {
  const client = readFileSync(join(dir, "admin-order-operations.ts"), "utf8");
  assert.match(client, /selections\?:/);
  assert.match(client, /selections: body\.selections/);
});

test("user grid width and flex scroll preservation markers", () => {
  const grid = readFileSync(join(dir, "../../design-system/app-data-grid/AppDataGrid.tsx"), "utf8");
  const bridge = readFileSync(join(dir, "../../design-system/app-data-grid/legacy-grid-bridge.ts"), "utf8");
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.doesNotMatch(grid, /defaultColDef:\s*\{[\s\S]*?flex:\s*1/);
  assert.match(bridge, /maxWidth/);
  assert.match(screens, /id:\s*"actions"[\s\S]*width:\s*120[\s\S]*minWidth:\s*100/);
});

test("orders grid reloads after kebab operation via onCompleted and reloadToken", () => {
  const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
  assert.match(screens, /function createOrderColumns\(onOperationCompleted\?: \(\) => void\)/);
  assert.match(screens, /onCompleted=\{onOperationCompleted\}/);
  assert.match(screens, /const \[reloadToken, setReloadToken\] = useState\(0\)/);
  assert.match(screens, /createOrderColumns\(\(\) => setReloadToken\(\(value\) => value \+ 1\)\)/);
  assert.match(screens, /reloadToken=\{reloadToken\}/);
  assert.match(screens, /reloadToken\?: number/);
  assert.match(screens, /void reloadToken;/);
});

test("cancelled order blocks forward payment action with human FA", () => {
  assert.equal(
    mapAdminErrorMessage("order.cancelled.blocks_action", "fa"),
    "سفارش لغوشده است؛ این عملیات مجاز نیست.",
  );
  assert.equal(
    mapAdminErrorMessage("order.cancelled.blocks_action", "en"),
    "This order is cancelled; the action is not allowed.",
  );
});

test("whole-order cancel after dispatch maps human FA and keeps one cancel", () => {
  assert.equal(
    mapAdminErrorMessage("inventory.reservation.not_active", "fa"),
    "رزرو موجودی این سفارش دیگر فعال نیست. اطلاعات سفارش را تازه‌سازی کنید یا وضعیت رزرو را بررسی کنید.",
  );
  assert.ok(!mapAdminErrorMessage("inventory.reservation.not_active", "fa").includes("Held"));
  assert.equal(
    mapAdminErrorMessage("order.cancel.forbidden", "fa"),
    "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.",
  );
  assert.ok(!mapAdminErrorMessage("order.cancel.forbidden", "fa").includes("order.cancel"));
  const menu = readFileSync(join(dir, "admin-order-operations-menu.tsx"), "utf8");
  assert.match(menu, /confirmAction\?\.confirmMessageFa/);
  assert.match(menu, /await refresh\(\)/);
  assert.match(menu, /onCompleted\?\.\(\)/);
});
