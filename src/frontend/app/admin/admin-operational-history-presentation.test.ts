import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import {
  presentOperationalHistoryEntry,
  presentOperationalHistoryText,
} from "./admin-operational-history-presentation.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");

test("GATEWAY_REJECTED renders human FA and EN", () => {
  assert.equal(presentOperationalHistoryText("GATEWAY_REJECTED", "fa"), "ردشده توسط درگاه پرداخت");
  assert.equal(presentOperationalHistoryText("GATEWAY_REJECTED", "en"), "Rejected by payment gateway");
});

test("unknown technical tokens use a safe human fallback", () => {
  assert.equal(
    presentOperationalHistoryText("SOME_UNKNOWN_CODE", "fa"),
    "جزئیات این رویداد برای نمایش آماده نیست.",
  );
  assert.equal(
    presentOperationalHistoryText("SOME_UNKNOWN_CODE", "en"),
    "This event detail is not available as a display label.",
  );
  assert.doesNotMatch(presentOperationalHistoryText("SOME_UNKNOWN_CODE", "fa"), /SOME_UNKNOWN_CODE/);
});

test("human summaries stay unchanged and audit mapping is presentation-only", () => {
  assert.equal(presentOperationalHistoryText("پرداخت ناموفق", "fa"), "پرداخت ناموفق");
  assert.equal(presentOperationalHistoryText("9485771 IRR", "en"), "9485771 IRR");
  const presented = presentOperationalHistoryEntry(
    {
      labelFa: "پرداخت ناموفق",
      labelEn: "Payment failed",
      summaryFa: "GATEWAY_REJECTED",
      summaryEn: "GATEWAY_REJECTED",
    },
    "fa",
  );
  assert.equal(presented.summary, "ردشده توسط درگاه پرداخت");
  assert.equal(presented.label, "پرداخت ناموفق");
});

test("order detail renderer maps operational history through the presenter", () => {
  assert.match(detail, /presentOperationalHistoryEntry/);
  assert.doesNotMatch(detail, /\{entry\.summaryFa\}/);
  assert.doesNotMatch(detail, /\{entry\.labelFa\}/);
  assert.match(detail, /location\.pathname/);
  assert.match(detail, /\\\/en/);
});
