import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

const dir = dirname(fileURLToPath(import.meta.url));
const api = readFileSync(join(dir, "admin-api.ts"), "utf8");
const supply = readFileSync(join(dir, "admin-order-supply.ts"), "utf8");
const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");
const ops = readFileSync(join(dir, "admin-order-operations.ts"), "utf8");

test("supply labels are human FA not raw enums", () => {
  assert.match(api, /Reserved: "تأمین‌شده"/);
  assert.match(api, /AvailableForReacquire: "قابل تأمین"/);
  assert.match(api, /Unavailable: "غیرقابل تأمین"/);
  assert.match(api, /PartiallyUnavailable: "تأمین ناقص"/);
  assert.match(api, /NotApplicable: "نامرتبط"/);
  assert.match(supply, /formatAdminSupplyStatus/);
});

test("shortage and confirm copy is business-safe", () => {
  assert.match(supply, /یک یا چند قلم این سفارش در حال حاضر قابل تأمین نیست/);
  assert.match(screens, /وضعیت تأمین/);
  assert.match(screens, /supplyStatusEnumOptions/);
  assert.doesNotMatch(screens, /supply-status\/\$\{row/);
  assert.match(detail, /admin-order-supply-status/);
  assert.match(detail, /admin-order-supply-shortages/);
  assert.doesNotMatch(detail, /inventory\.reservation\.not_active/);
  assert.match(ops, /canRecoverInventory/);
  assert.match(ops, /canConfirmDeposit/);
});
