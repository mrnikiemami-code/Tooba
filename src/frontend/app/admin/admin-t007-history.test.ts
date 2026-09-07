import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../../..");
const detail = fs.readFileSync(
  path.join(root, "src/frontend/app/admin/admin-order-detail-screen.tsx"),
  "utf8",
);
const composer = fs.readFileSync(
  path.join(root, "src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs"),
  "utf8",
);
const history = fs.readFileSync(
  path.join(root, "src/backend/Host/Tooba.Host/Admin/AdminOrderCompletenessComposer.cs"),
  "utf8",
);

test("financial history human FA labels", () => {
  assert.match(detail, /دریافت از مشتری/);
  assert.match(detail, /بازگشت وجه به مشتری/);
  assert.match(detail, /واریز سهم فروشنده/);
  assert.match(detail, /کسر از حساب فروشنده بابت بازگشت وجه/);
  assert.match(composer, /دریافت از مشتری/);
  assert.match(composer, /بازگشت وجه به مشتری/);
  assert.match(composer, /واریز سهم فروشنده/);
  assert.match(composer, /کسر از حساب فروشنده بابت بازگشت وجه/);
  assert.match(composer, /CustomerRefund/);
  assert.match(composer, /SellerRefundAdjustment/);
});

test("operational history is scope-aware and filterable without redesign", () => {
  assert.match(detail, /admin-order-history-filters/);
  assert.match(detail, /همه/);
  assert.match(detail, /ارسال/);
  assert.match(detail, /مرجوعی/);
  assert.match(history, /ایجاد مرسوله/);
  assert.match(history, /ToFaDigits/);
  assert.match(history, /IPartyLookupGateway/);
  assert.match(detail, /تاریخچه عملیات/);
  assert.match(detail, /سابقه پرداخت‌ها \/ واریزها/);
});

test("orders UI preservation markers remain", () => {
  assert.match(detail, /admin-order-history-section/);
  assert.match(detail, /یادداشت داخلی|admin-order-note/);
});
